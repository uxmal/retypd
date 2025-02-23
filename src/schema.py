# Retypd - machine code type inference
# Copyright (C) 2021 GrammaTech, Inc.
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program.  If not, see <https://www.gnu.org/licenses/>.
#
# This project is sponsored by the Office of Naval Research, One Liberty
# Center, 875 N. Randolph Street, Arlington, VA 22203 under contract #
# N68335-17-C-0700.  The content of the information does not necessarily
# reflect the position or policy of the Government and no official
# endorsement should be inferred.

'''Data types for an implementation of retypd analysis.
'''

from abc import ABC
from enum import Enum, unique
from functools import reduce
from typing import Any, Dict, FrozenSet, Generic, Iterable, Iterator, List, Optional, Sequence, \
        Set, Tuple, TypeVar, Union
import logging
import os
import networkx


logging.basicConfig()


@unique
class Variance(Enum):
    '''Represents a capability's variance (or that of some sequence of capabilities).
    '''
    CONTRAVARIANT = 0
    COVARIANT = 1

    @staticmethod
    def invert(variance: 'Variance') -> 'Variance':
        if variance == Variance.CONTRAVARIANT:
            return Variance.COVARIANT
        return Variance.CONTRAVARIANT

    @staticmethod
    def combine(lhs: 'Variance', rhs: 'Variance') -> 'Variance':
        if lhs == rhs:
            return Variance.COVARIANT
        return Variance.CONTRAVARIANT


class AccessPathLabel(ABC):
    '''Abstract class for capabilities that can be part of a path. See Table 1.

    All :py:class:`AccessPathLabel` objects are comparable to each other; objects are ordered by
    their classes (in an arbitrary order defined by the string representation of their type), then
    by values specific to their subclass. So objects of class A always precede objects of class B
    and objects of class A are ordered with respect to each other by :py:method:`_less_than`.
    '''
    def __lt__(self, other: 'AccessPathLabel') -> bool:
        s_type = str(type(self))
        o_type = str(type(other))
        if s_type == o_type:
            return self._less_than(other)
        return s_type < o_type

    def _less_than(self, _other) -> bool:
        '''Compare two objects of the same exact type. Return True if self is less than other; true
        otherwise. Several of the subclasses are singletons, so we return False unless there is a
        need for an overriding implementation.
        '''
        return False

    def variance(self) -> Variance:
        '''Determines if the access path label is covariant or contravariant, per Table 1.
        '''
        return Variance.COVARIANT


class LoadLabel(AccessPathLabel):
    '''A singleton representing the load (read) capability.
    '''
    _instance = None

    def __init__(self) -> None:
        raise ValueError("Can't instantiate; call instance() instead")

    @classmethod
    def instance(cls):
        if cls._instance is None:
            cls._instance = cls.__new__(cls)
        return cls._instance

    def __eq__(self, other: Any) -> bool:
        return self is other

    def __hash__(self) -> int:
        return 0

    def __str__(self) -> str:
        return 'load'


class StoreLabel(AccessPathLabel):
    '''A singleton representing the store (write) capability.
    '''
    _instance = None

    def __init__(self) -> None:
        raise ValueError("Can't instantiate; call instance() instead")

    @classmethod
    def instance(cls):
        if cls._instance is None:
            cls._instance = cls.__new__(cls)
        return cls._instance

    def __eq__(self, other: Any) -> bool:
        return self is other

    def __hash__(self) -> int:
        return 1

    def variance(self) -> Variance:
        return Variance.CONTRAVARIANT

    def __str__(self) -> str:
        return 'store'


class InLabel(AccessPathLabel):
    '''Represents a parameter to a function, specified by an index (e.g., the first argument might
    use index 0, the second might use index 1, and so on). N.B.: this is a capability and is not
    tied to any particular function.
    '''
    def __init__(self, index: int) -> None:
        self.index = index

    def __eq__(self, other: Any) -> bool:
        return isinstance(other, InLabel) and self.index == other.index

    def _less_than(self, other: 'InLabel') -> bool:
        return self.index < other.index

    def __hash__(self) -> int:
        return hash(self.index)

    def variance(self) -> Variance:
        return Variance.CONTRAVARIANT

    def __str__(self) -> str:
        return f'in_{self.index}'


class OutLabel(AccessPathLabel):
    '''Represents a return from a function. This class is a singleton.
    '''
    _instance = None

    def __init__(self) -> None:
        raise ValueError("Can't instantiate; call instance() instead")

    @classmethod
    def instance(cls):
        if cls._instance is None:
            cls._instance = cls.__new__(cls)
        return cls._instance

    def __eq__(self, other: Any) -> bool:
        return self is other

    def __hash__(self) -> int:
        return 2

    def __str__(self) -> str:
        return 'out'


class DerefLabel(AccessPathLabel):
    '''Represents a dereference in an access path. Specifies a size (the number of bytes read or
    written) and an offset (the number of bytes from the base) and an optional count (for array-
    like accesses that do size*count accesses in a loop).
    '''
    # An unknown number of elements
    COUNT_NOBOUND = -1
    # A null-terminated string
    COUNT_NULLTERM = -2

    def __init__(self, size: int, offset: int, count: int = 1) -> None:
        self.size = size
        self.offset = offset
        self.count = count

    def __eq__(self, other: Any) -> bool:
        return (isinstance(other, DerefLabel) and
                self.size == other.size and
                self.offset == other.offset and
                self.count == other.count)

    def _less_than(self, other: 'DerefLabel') -> bool:
        return (self.offset, self.size, self.count) < (other.offset, other.size, other.count)

    def __hash__(self) -> int:
        return hash( (self.offset, self.size, self.count) )

    def __str__(self) -> str:
        srep = f'σ{self.size}@{self.offset}'
        if self.count > 1:
            srep += f'*[{self.count}]'
        elif self.count == self.COUNT_NOBOUND:
            srep += '*[nobound]'
        elif self.count == self.COUNT_NULLTERM:
            srep += '*[nullterm]'
        return srep


class DerivedTypeVariable:
    '''A _derived_ type variable, per Definition 3.1. Immutable (by convention).
    '''
    def __init__(self, type_var: str, path: Optional[Sequence[AccessPathLabel]] = None) -> None:
        self._base = type_var
        if path is None:
            self._path: Sequence[AccessPathLabel] = ()
        else:
            self._path = tuple(path)
        # Precomputing the hash is a big performance boost (since we are immutable)
        self._hash = hash( (self._base, self._path) )

    @property
    def base(self):
        return self._base

    @property
    def path(self):
        return self._path

    # We weakly "enforce" mutability
    @base.setter
    def base(self, value):
        raise NotImplementedError("Read-only property")

    @path.setter
    def path(self, value):
        raise NotImplementedError("Read-only property")

    def format(self, separator: str = '.') -> str:
        if self._path:
            return f'{self._base}.{".".join(map(str, self._path))}'
        return self._base

    def __eq__(self, other: Any) -> bool:
        return (isinstance(other, DerivedTypeVariable) and
                self._base == other.base and
                self._path == other.path)

    def __lt__(self, other: 'DerivedTypeVariable') -> bool:
        if self._base == other.base:
            return list(self._path) < list(other.path)
        return self._base < other.base

    def __hash__(self) -> int:
        return self._hash

    @property
    def largest_prefix(self) -> Optional['DerivedTypeVariable']:
        '''Return the prefix obtained by removing the last item from the type variable's path. If
        there is no path, return None.
        '''
        if self._path:
            return DerivedTypeVariable(self._base, self._path[:-1])
        return None

    def all_prefixes(self) -> Set['DerivedTypeVariable']:
        '''Return all prefixes of self, including self.
        '''
        var = self
        result: Set['DerivedTypeVariable'] = set()
        while var:
            result.add(var)
            var = var.largest_prefix
        return result

    def get_suffix(self, other: 'DerivedTypeVariable') -> Optional[Sequence[AccessPathLabel]]:
        '''If self is a prefix of other, return the suffix of other's path that is not part of self.
        Otherwise, return None.
        '''
        if self._base != other.base:
            return None
        if len(self._path) > len(other.path):
            return None
        for s_item, o_item in zip(self._path, other.path):
            if s_item != o_item:
                return None
        return other.path[len(self._path):]

    def remove_suffix(self, suffix: Sequence[AccessPathLabel]) -> Optional['DerivedTypeVariable']:
        result = DerivedTypeVariable(self._base, self._path)
        for label in reversed(suffix):
            if not result.path or result.path[-1] != label:
                return None
            result = result.largest_prefix
        return result

    @property
    def tail(self) -> AccessPathLabel:
        '''Retrieve the last item in the access path, if any. Return None if
        the path is empty.
        '''
        if self._path:
            return self._path[-1]
        return None

    def add_suffix(self, suffix: AccessPathLabel) -> 'DerivedTypeVariable':
        '''Create a new :py:class:`DerivedTypeVariable` identical to :param:`self` (which is
        unchanged) but with suffix appended to its path.
        '''
        path: List[AccessPathLabel] = list(self._path)
        path.append(suffix)
        return DerivedTypeVariable(self._base, path)

    def extend(self, suffix: Iterable[AccessPathLabel]) -> 'DerivedTypeVariable':
        path: List[AccessPathLabel] = list(self._path)
        path.extend(suffix)
        return DerivedTypeVariable(self._base, path)

    def get_single_suffix(self, prefix: 'DerivedTypeVariable') -> Optional[AccessPathLabel]:
        '''If :param:`prefix` is a prefix of :param:`self` with exactly one additional
        :py:class:`AccessPathLabel`, return the additional label. If not, return `None`.
        '''
        if (self._base != prefix.base or
                len(self.path) != (len(prefix.path) + 1) or
                self._path[:-1] != prefix.path):
            return None
        return self.tail

    @property
    def base_var(self) -> 'DerivedTypeVariable':
        return DerivedTypeVariable(self._base)

    @property
    def path_variance(self) -> Variance:
        '''Determine the variance of the access path.
        '''
        variances = map(lambda label: label.variance(), self._path)
        return reduce(Variance.combine, variances, Variance.COVARIANT)

    def __str__(self) -> str:
        return self.format()

    def __repr__(self) -> str:
        return self.format('$')


class SubtypeConstraint:
    '''A type constraint of the form left ⊑ right (see Definition 3.3)
    '''
    def __init__(self, left: DerivedTypeVariable, right: DerivedTypeVariable) -> None:
        self.left = left
        self.right = right

    def __eq__(self, other: Any) -> bool:
        return (isinstance(other, SubtypeConstraint) and
                self.left == other.left and
                self.right == other.right)

    def __lt__(self, other: 'SubtypeConstraint') -> bool:
        if self.left == other.left:
            return self.right < other.right
        return self.left < other.left

    def __hash__(self) -> int:
        return hash(self.left) ^ hash(self.right)

    def __str__(self) -> str:
        return f'{self.left} ⊑ {self.right}'

    def __repr__(self) -> str:
        return str(self)


class ConstraintSet:
    '''A (partitioned) set of type constraints
    '''
    def __init__(self, subtype: Optional[Iterable[SubtypeConstraint]] = None) -> None:
        if subtype:
            self.subtype = set(subtype)
        else:
            self.subtype = set()
        self.logger = logging.getLogger('ConstraintSet')

    def add_subtype(self, left: DerivedTypeVariable, right: DerivedTypeVariable) -> bool:
        '''Add a subtype constraint
        '''
        constraint = SubtypeConstraint(left, right)
        return self.add(constraint)

    def add(self, constraint: SubtypeConstraint) -> bool:
        if constraint in self.subtype:
            return False
        self.subtype.add(constraint)
        return True

    def __or__(self, other: 'ConstraintSet') -> 'ConstraintSet':
        return ConstraintSet(self.subtype | other.subtype)

    def __str__(self) -> str:
        nt = os.linesep + '\t'
        return f'ConstraintSet:{nt}{nt.join(map(str,self.subtype))}'

    def __repr__(self) -> str:
        return f'ConstraintSet({repr(self.subtype)})'

    def __iter__(self) -> Iterator[SubtypeConstraint]:
        return iter(self.subtype)

    def __len__(self) -> int:
        return len(self.subtype)


T = TypeVar('T')

class Lattice(ABC, Generic[T]):
    @property
    def atomic_types(self) -> FrozenSet[T]:
        pass

    @property
    def internal_types(self) -> FrozenSet[T]:
        pass

    @property
    def top(self) -> T:
        pass

    @property
    def bottom(self) -> T:
        pass

    def meet(self, t: T, v: T) -> T:
        pass

    def join(self, t: T, v: T) -> T:
        pass


class LatticeCTypes:
    """
    Class for converting a Lattice type to a CType.
    """
    def atom_to_ctype(self, atom_lower: Any, atom_upper: Any, byte_size: int):
        raise NotImplementedError("Child class must implemented")


MaybeVar = Union[DerivedTypeVariable, str]

def maybe_to_var(mv: MaybeVar) -> DerivedTypeVariable:
    if isinstance(mv, str):
        return DerivedTypeVariable(mv)
    return mv


Key = TypeVar('Key')
Value = TypeVar('Value')
MaybeDict = Union[Dict[Key, Value], Iterable[Tuple[Key, Value]]]

def maybe_to_bindings(md: MaybeDict[Key, Value]) -> Iterable[Tuple[Key, Value]]:
    if isinstance(md, dict):
        return md.items()
    return md


class Program:
    '''An entire binary. Contains a set of global variables, a mapping from procedures to sets of
    constraints, and a call graph.
    '''
    def __init__(self,
                 types: Lattice[DerivedTypeVariable],
                 global_vars: Iterable[MaybeVar],
                 proc_constraints: MaybeDict[MaybeVar, ConstraintSet],
                 callgraph: Union[MaybeDict[MaybeVar, Iterable[MaybeVar]],
                                  networkx.DiGraph]) -> None:
        self.types = types
        self.global_vars = {maybe_to_var(glob) for glob in global_vars}
        self.proc_constraints: Dict[DerivedTypeVariable, ConstraintSet] = {}
        for name, constraints in maybe_to_bindings(proc_constraints):
            var = maybe_to_var(name)
            if var in self.proc_constraints:
                raise ValueError(f'Procedure doubly bound: {name}')
            self.proc_constraints[var] = constraints
        if isinstance(callgraph, networkx.DiGraph):
            self.callgraph = callgraph
        else: # Dict or Iterable[Tuple]
            self.callgraph = networkx.DiGraph()
            for caller, callees in maybe_to_bindings(callgraph):
                caller_var = maybe_to_var(caller)
                self.callgraph.add_node(caller_var)
                for callee in callees:
                    self.callgraph.add_edge(caller_var, maybe_to_var(callee))
