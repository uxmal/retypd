/*
using ABC = abc.ABC;

using Enum = @enum.Enum;

using unique = @enum.unique;

using reduce = functools.reduce;

using Any = typing.Any;

using Iterable = typing.Iterable;

using List = typing.List;

using Optional = typing.Optional;

using Sequence = typing.Sequence;

using Tuple = typing.Tuple;

using logging;

using os;
*/
using System.Linq;

using System.Collections.Generic;
using System;
using System.Collections;

namespace schema {

        // Data types for an implementation of retypd analysis.

    // Represents a capability's variance (or that of some sequence of capabilities).
    //     
    public enum Variance
    {
        CONTRAVARIANT = 0,
        COVARIANT = 1
    }   

    public static class VarianceExtensions { 
        public static Variance invert(this Variance variance) {
            if (variance == Variance.CONTRAVARIANT) {
                return Variance.COVARIANT;
            }
            return Variance.CONTRAVARIANT;
        }
        
        public static Variance combine(Variance lhs, Variance rhs) {
            if (lhs == rhs) {
                return Variance.COVARIANT;
            }
            return Variance.CONTRAVARIANT;
        }
    }
    
    // Abstract class for capabilities that can be part of a path. See Table 1.
    // 
    //     All :py:class:`AccessPathLabel` objects are comparable to each other; objects are ordered by
    //     their classes (in an arbitrary order defined by the string representation of their type), then
    //     by values specific to their subclass. So objects of class A always precede objects of class B
    //     and objects of class A are ordered with respect to each other by :py:method:`_less_than`.
    //     
    public abstract class AccessPathLabel
    {
        public static bool operator ==(AccessPathLabel self, AccessPathLabel other)
        {
            var s_type = self.GetType().Name;
            var o_type = other.GetType().Name;
            if (s_type == o_type)
            {
                return
                    !self._less_than(other) &&
                    !other._less_than(self);
            }
            return false;
        }

        public static bool operator !=(AccessPathLabel self, AccessPathLabel other) =>
            !(self == other);

        public static bool operator <( AccessPathLabel self, AccessPathLabel other) {
            var s_type = self.GetType().Name;
            var o_type = other.GetType().Name;
            if (s_type == o_type) {
                return self._less_than(other);
            }
            return s_type.CompareTo(o_type) < 0;
        }

        public static bool operator >(AccessPathLabel a, AccessPathLabel b) =>
            b < a;

        // Compare two objects of the same exact type. Return True if self is less than other; true
        //         otherwise. Several of the subclasses are singletons, so we return False unless there is a
        //         need for an overriding implementation.
        //         
        public virtual bool _less_than(AccessPathLabel _other) {
            return false;
        }

        public abstract override  bool Equals(object? other);
        public abstract override int GetHashCode();

        // Determines if the access path label is covariant or contravariant, per Table 1.
        //         
        public virtual Variance variance() {
            return Variance.COVARIANT;
        }
    }
    
    // A singleton representing the load (read) capability.
    //     
    public class LoadLabel
        : AccessPathLabel {

        public static readonly LoadLabel instance = new LoadLabel();
        
        private LoadLabel() {
        }
        
        public override bool Equals(object? other) {
            return object.ReferenceEquals(this, other);
        }
        
        public override int GetHashCode() {
            return 0;
        }
        
        public override string ToString() {
            return "load";
        }
    }
    
    // A singleton representing the store (write) capability.
    //     
    public class StoreLabel
        : AccessPathLabel {

        public static readonly StoreLabel instance = new StoreLabel();
        
        private StoreLabel() {
        }
        
        public override bool Equals(object? other) {
            return object.ReferenceEquals(this, other);
        }
        
        public override int GetHashCode() {
            return 1;
        }
        
        public override Variance variance() {
            return Variance.CONTRAVARIANT;
        }
        
        public override string ToString() {
            return "store";
        }
    }
    
    // Represents a parameter to a function, specified by an index (e.g., the first argument might
    //     use index 0, the second might use index 1, and so on). N.B.: this is a capability and is not
    //     tied to any particular function.
    //     
    public class InLabel
        : AccessPathLabel {
        
        public int index;
        
        public InLabel(int index) {
            this.index = index;
        }
        
        public override bool Equals(object? other) {
            return other is InLabel that && this.index == that.index;
        }
        
        public override bool _less_than(AccessPathLabel that) {
            var other = (InLabel)that;
            return this.index < other.index;
        }
        
        public override int GetHashCode() {
            return this.index.GetHashCode();
        }
        
        public override Variance variance() {
            return Variance.CONTRAVARIANT;
        }
        
        public override string ToString() {
            return $"in_{this.index}";
        }
    }
    
    // Represents a return from a function. This class is a singleton.
    //     
    public class OutLabel
        : AccessPathLabel {
        
        public static readonly OutLabel instance = new OutLabel();
        
        private OutLabel() {
        }
        
        public override bool Equals(object? other) {
            return object.ReferenceEquals(this, other);
        }
        
        public override int GetHashCode() {
            return 2;
        }
        
        public override string ToString() {
            return "out";
        }
    }
    
    // Represents a dereference in an access path. Specifies a size (the number of bytes read or
    //     written) and an offset (the number of bytes from the base).
    //     
    public class DerefLabel
        : AccessPathLabel {
        
        public int offset;
        
        public int size;
        
        public DerefLabel(int size, int offset) {
            this.size = size;
            this.offset = offset;
        }
        
        public override bool Equals(object? other) {
            return other is DerefLabel that && this.size == that.size && this.offset == that.offset;
        }
        
        public override bool _less_than(AccessPathLabel that) {
            var other = (DerefLabel)that;
            if (this.offset == other.offset) {
                return this.size < other.size;
            }
            return this.offset < other.offset;
        }
        
        public override int GetHashCode() {
            return this.offset.GetHashCode() ^ this.size.GetHashCode();
        }
        
        public override string ToString() {
            return $"σ{this.size}@{this.offset}";
        }
    }
    
    // A _derived_ type variable, per Definition 3.1. Immutable (by convention).
    //     
    public class DerivedTypeVariable {
        
        public string _str;
        
        public string @base;
        
        public AccessPathLabel[] path;

        public DerivedTypeVariable(string type_var, AccessPathLabel[]? path = null) {
            this.@base = type_var;
            if (path == null) {
                this.path = Array.Empty<AccessPathLabel>();
            } else {
                this.path = path.ToArray();
            }
            if (this.path.Length > 0) {
                this._str = $"{this.@base}.{string.Join<AccessPathLabel>(".", this.path)}";
            } else {
                this._str = this.@base;
            }
        }
        
        public override bool Equals(object? that) {
            return that is DerivedTypeVariable other &&
                this.@base == other.@base &&
                ComparePaths(this.path ,other.path) == 0;
        }

        public static bool operator == (DerivedTypeVariable self, DerivedTypeVariable other) {
            return (self.@base == other.@base &&
                    ComparePaths(self.path, other.path) == 0);
        }

        public static bool operator !=(DerivedTypeVariable self, DerivedTypeVariable other) =>
            !(self == other);

        private static int ComparePaths(AccessPathLabel[] a, AccessPathLabel[] b)
        {
            int d = a.Length.CompareTo(b.Length);
            if (d == 0)
            {
                for (int i = 0; i < a.Length; ++i)
                {
                    if (a[i] < b[i])
                        return -1;
                    else if (a[i] > b[i])
                        return 1;
                }
            }
            return d;
        }

        public static bool operator < (DerivedTypeVariable self, DerivedTypeVariable other) {
            int d = self.@base.CompareTo(other.@base);
            if (d == 0)
                d = ComparePaths(self.path, other.path);
            return d < 0;
        }

        public static bool operator >(DerivedTypeVariable a, DerivedTypeVariable b) =>
            b < a;
        
        public override int GetHashCode() {
            return this.@base.GetHashCode() ^ this.path.Length.GetHashCode();
        }
        
        // Return the prefix obtained by removing the last item from the type variable's path. If
        //         there is no path, return None.
        //         
        public virtual DerivedTypeVariable? largest_prefix() {
            if (this.path.Length > 0) {
                return new DerivedTypeVariable(this.@base, this.path[0..^1]);
            }
            return null;
        }
        
        // If self is a prefix of other, return the suffix of other's path that is not part of self.
        //         Otherwise, return None.
        //         
        public virtual AccessPathLabel[]? get_suffix(DerivedTypeVariable other) {
            if (this.@base != other.@base) {
                return null;
            }
            if (this.path.Length > other.path.Length) {
                return null;
            }
            foreach (var (s_item, o_item) in this.path.Zip(other.path)) {
                if (s_item != o_item) {
                    return null;
                }
            }
            return other.path[this.path.Length..];
        }
        
        // Retrieve the last item in the access path, if any. Return None if
        //         the path is empty.
        //         
        public virtual AccessPathLabel? tail() {
            if (this.path.Length > 0) {
                return this.path[^1];
            }
            return null;
        }
        
        // Create a new :py:class:`DerivedTypeVariable` identical to :param:`self` (which is
        //         unchanged) but with suffix appended to its path.
        //         
        public virtual DerivedTypeVariable add_suffix(AccessPathLabel suffix) {
            var path = this.path.ToList();
            path.Add(suffix);
            return new DerivedTypeVariable(this.@base, path.ToArray());
        }
        
        // If :param:`prefix` is a prefix of :param:`self` with exactly one additional
        //         :py:class:`AccessPathLabel`, return the additional label. If not, return `None`.
        //         
        public virtual AccessPathLabel? get_single_suffix(DerivedTypeVariable prefix) {
            if (this.@base != prefix.@base || 
                this.path.Length != prefix.path.Length+ 1 ||
                Enumerable.SequenceEqual(this.path[0..^1], prefix.path)) {
                return null;
            }
            return this.tail();
        }
        
        // Determine the variance of the access path.
        //         
        public virtual Variance path_variance() {
            var variances = this.path.Select(label => label.variance());
            return variances.Aggregate(Variance.COVARIANT, VarianceExtensions.combine);
        }
        
        public override string ToString() {
            return this._str;
        }
    }
    
    // A type constraint of the form left ⊑ right (see Definition 3.3)
    //     
    public class SubtypeConstraint {
        
        public DerivedTypeVariable left;
        
        public DerivedTypeVariable right;
        
        public SubtypeConstraint(DerivedTypeVariable left, DerivedTypeVariable right) {
            this.left = left;
            this.right = right;
        }
        
        public override bool Equals(object? that) {
            return that is SubtypeConstraint other && this.left == other.left && this.right == other.right;
        }
        
        public static bool operator < (SubtypeConstraint self, SubtypeConstraint other) {
            if (self.left == other.left) {
                return self.right < other.right;
            }
            return self.left < other.left;
        }

        public static bool operator >(SubtypeConstraint self, SubtypeConstraint other) =>
            other < self;
        
        public override int GetHashCode() {
            return this.left.GetHashCode() ^ this.right.GetHashCode();
        }
        
        public override string ToString() {
            return $"{this.left} ⊑ {this.right}";
        }
    }
    
    // A (partitioned) set of type constraints
    //     
    public class ConstraintSet {
        
        // public object logger;
        
        public HashSet<SubtypeConstraint> subtype;
        
        public ConstraintSet(IEnumerable<SubtypeConstraint>? subtype = null) {
            if (subtype != null) {
                this.subtype = new HashSet<SubtypeConstraint>(subtype);
            } else {
                this.subtype = new HashSet<SubtypeConstraint>();
            }
            // this.logger = logging.getLogger("ConstraintSet");
        }
        
        // Add a subtype constraint
        //         
        public virtual bool add_subtype(DerivedTypeVariable left, DerivedTypeVariable right) {
            var constraint = new SubtypeConstraint(left, right);
            return this.add(constraint);
        }
        
        public virtual bool add(SubtypeConstraint constraint) {
            if (this.subtype.Contains(constraint)) {
                return false;
            }
            this.subtype.Add(constraint);
            return true;
        }
        
        public override string ToString() {
            var nt = Environment.NewLine + "\t";
            return $"ConstraintSet:{nt}{string.Join(nt, this.subtype)}";
        }
    }
    
    // A forget or recall label in the graph. Instances should never be mutated.
    //     
    public class EdgeLabel {
        
        public int _hash;
        
        public string _str;
        
        public AccessPathLabel capability;
        
        public Kind kind;
        
        public enum Kind {
            FORGET = 1,
            RECALL = 2,
        }
        
        public EdgeLabel(AccessPathLabel capability, Kind kind) {
            string type_str;
            this.capability = capability;
            this.kind = kind;
            if (this.kind == EdgeLabel.Kind.FORGET) {
                type_str = "forget";
            } else {
                type_str = "recall";
            }
            this._str = $"{type_str} {this.capability}";
            this._hash = this.capability.GetHashCode() ^ this.kind.GetHashCode();
        }
        
        public override bool Equals(object? that) {
            return that is EdgeLabel other &&
                this.capability == other.capability &&
                this.kind == other.kind;
        }
        
        public override int GetHashCode() {
            return this._hash;
        }
        
        public override string ToString() {
            return this._str;
        }
    }

    /// <summary>
    /// A node in the graph of constraints. Node objects are immutable.
    /// 
    /// Unforgettable is a flag used to differentiate between two subgraphs later in the algorithm. See
    /// <see cref="Solver._unforgettable_subgraph_split" /> for details.
    /// </summary>
    public class Node {
        
        private int _hash;
        private string _str;
        
        public Unforgettable _unforgettable;
        public DerivedTypeVariable @base;
        public Variance suffix_variance;
        
        public enum Unforgettable {
            
            PRE_RECALL = 0,
            POST_RECALL = 1,
        }
        
        public Node(DerivedTypeVariable @base, Variance suffix_variance, Unforgettable unforgettable = Unforgettable.PRE_RECALL) {
            int summary;
            string variance;
            this.@base = @base;
            this.suffix_variance = suffix_variance;
            if (suffix_variance == Variance.COVARIANT) {
                variance = ".⊕";
                summary = 2;
            } else {
                variance = ".⊖";
                summary = 0;
            }
            this._unforgettable = unforgettable;
            if (unforgettable == Node.Unforgettable.POST_RECALL) {
                this._str = "R:" + this.@base.ToString() + variance;
                summary += 1;
            } else {
                this._str = this.@base.ToString() + variance;
            }
            this._hash = this.@base.GetHashCode() ^ summary.GetHashCode();
        }
        
        public override bool Equals(object? that) {
            var ret = that is Node other &&
                this.@base == other.@base &&
                this.suffix_variance == other.suffix_variance &&
                this._unforgettable == other._unforgettable;
            return ret;
        }
        
        public override int GetHashCode() {
            return this._hash;
        }
        
        // "Forget" the last element in the access path, creating a new Node. The new Node has
        //         variance that reflects this change.
        //         
        public virtual (AccessPathLabel?, Node?) forget_once() {
            if (this.@base.path.Length > 0) {
                var prefix_path = this.@base.path[0..^1];
                var last = this.@base.path[^1];
                var prefix = new DerivedTypeVariable(this.@base.@base, prefix_path);
                return (last, new Node(prefix, VarianceExtensions.combine(last.variance(), this.suffix_variance)));
            }
            return (null, null);
        }
        
        // "Recall" label, creating a new Node. The new Node has variance that reflects this
        //         change.
        //         
        public virtual Node recall(AccessPathLabel label) {
            var path = this.@base.path.ToList();
            path.Add(label);
            var variance = VarianceExtensions.combine(this.suffix_variance, label.variance());
            return new Node(new DerivedTypeVariable(this.@base.@base, path.ToArray()), variance);
        }
        
        public override string ToString() {
            return this._str;
        }
        
        // Get a duplicate of self for use in the post-recall subgraph.
        //         
        public virtual Node split_unforgettable() {
            return new Node(this.@base, this.suffix_variance, Node.Unforgettable.POST_RECALL);
        }
        
        // Get a Node identical to this one but with inverted variance.
        //         
        public virtual Node inverse() {
            return new Node(this.@base, VarianceExtensions.invert(this.suffix_variance), this._unforgettable);
        }
    }
}
