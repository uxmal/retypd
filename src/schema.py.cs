
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

using System.Linq;

using System.Collections.Generic;

public static class schema {
    
    static schema() {
        @"Data types for an implementation of retypd analysis.
";
        logging.basicConfig();
    }
    
    // Represents a capability's variance (or that of some sequence of capabilities).
    //     
    public class Variance
        : Enum {
        
        public int CONTRAVARIANT;
        
        public int COVARIANT;
        
        public int CONTRAVARIANT = 0;
        
        public int COVARIANT = 1;
        
        [staticmethod]
        public static int invert(object variance = "Variance") {
            if (variance == Variance.CONTRAVARIANT) {
                return Variance.COVARIANT;
            }
            return Variance.CONTRAVARIANT;
        }
        
        [staticmethod]
        public static int combine(object lhs = "Variance", object rhs = "Variance") {
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
    public class AccessPathLabel
        : ABC {
        
        public virtual bool @__lt__(string other = "AccessPathLabel") {
            var s_type = type(this).ToString();
            var o_type = type(other).ToString();
            if (s_type == o_type) {
                return this._less_than(other);
            }
            return s_type < o_type;
        }
        
        // Compare two objects of the same exact type. Return True if self is less than other; true
        //         otherwise. Several of the subclasses are singletons, so we return False unless there is a
        //         need for an overriding implementation.
        //         
        public virtual bool _less_than(string _other) {
            return false;
        }
        
        // Determines if the access path label is covariant or contravariant, per Table 1.
        //         
        public virtual int variance() {
            return Variance.COVARIANT;
        }
    }
    
    // A singleton representing the load (read) capability.
    //     
    public class LoadLabel
        : AccessPathLabel {
        
        public object _instance;
        
        public void _instance = null;
        
        public LoadLabel() {
            throw new ValueError("Can't instantiate; call instance() instead");
        }
        
        [classmethod]
        public static void instance(object cls) {
            if (cls._instance == null) {
                cls._instance = cls.@__new__(cls);
            }
            return cls._instance;
        }
        
        public virtual LoadLabel @__eq__(object other = Any) {
            return object.ReferenceEquals(this, other);
        }
        
        public virtual int @__hash__() {
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
        
        public object _instance;
        
        public void _instance = null;
        
        public StoreLabel() {
            throw new ValueError("Can't instantiate; call instance() instead");
        }
        
        [classmethod]
        public static void instance(object cls) {
            if (cls._instance == null) {
                cls._instance = cls.@__new__(cls);
            }
            return cls._instance;
        }
        
        public virtual StoreLabel @__eq__(object other = Any) {
            return object.ReferenceEquals(this, other);
        }
        
        public virtual int @__hash__() {
            return 1;
        }
        
        public virtual int variance() {
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
        
        public object index;
        
        public InLabel(Func<object> index = @int) {
            this.index = index;
        }
        
        public virtual object @__eq__(object other = Any) {
            return other is InLabel && this.index == other.index;
        }
        
        public virtual bool _less_than(string other = "InLabel") {
            return this.index < other.index;
        }
        
        public virtual int @__hash__() {
            return hash(this.index);
        }
        
        public virtual int variance() {
            return Variance.CONTRAVARIANT;
        }
        
        public override string ToString() {
            return "in_{self.index}";
        }
    }
    
    // Represents a return from a function. This class is a singleton.
    //     
    public class OutLabel
        : AccessPathLabel {
        
        public object _instance;
        
        public void _instance = null;
        
        public OutLabel() {
            throw new ValueError("Can't instantiate; call instance() instead");
        }
        
        [classmethod]
        public static void instance(object cls) {
            if (cls._instance == null) {
                cls._instance = cls.@__new__(cls);
            }
            return cls._instance;
        }
        
        public virtual OutLabel @__eq__(object other = Any) {
            return object.ReferenceEquals(this, other);
        }
        
        public virtual int @__hash__() {
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
        
        public object offset;
        
        public object size;
        
        public DerefLabel(Func<object> size = @int, Func<object> offset = @int) {
            this.size = size;
            this.offset = offset;
        }
        
        public virtual object @__eq__(object other = Any) {
            return other is DerefLabel && this.size == other.size && this.offset == other.offset;
        }
        
        public virtual bool _less_than(string other = "DerefLabel") {
            if (this.offset == other.offset) {
                return this.size < other.size;
            }
            return this.offset < other.offset;
        }
        
        public virtual int @__hash__() {
            return hash(this.offset) ^ hash(this.size);
        }
        
        public override string ToString() {
            return "σ{self.size}@{self.offset}";
        }
    }
    
    // A _derived_ type variable, per Definition 3.1. Immutable (by convention).
    //     
    public class DerivedTypeVariable {
        
        public object _str;
        
        public object @base;
        
        public tuple path;
        
        public DerivedTypeVariable(object type_var = str, list path = null) {
            this.@base = type_var;
            if (path == null) {
                this.path = ValueTuple.Create("<Empty>");
            } else {
                this.path = tuple(path);
            }
            if (this.path) {
                this._str = "{self.base}.{\".\".join(map(str, self.path))}";
            } else {
                this._str = this.@base;
            }
        }
        
        public virtual object @__eq__(object other = Any) {
            return other is DerivedTypeVariable && this.@base == other.@base && this.path == other.path;
        }
        
        public virtual bool @__lt__(string other = "DerivedTypeVariable") {
            if (this.@base == other.@base) {
                return this.path.ToList() < other.path.ToList();
            }
            return this.@base < other.@base;
        }
        
        public virtual int @__hash__() {
            return hash(this.@base) ^ hash(this.path);
        }
        
        // Return the prefix obtained by removing the last item from the type variable's path. If
        //         there is no path, return None.
        //         
        public virtual DerivedTypeVariable largest_prefix() {
            if (this.path) {
                return new DerivedTypeVariable(this.@base, this.path[:: - 1]);
            }
            return null;
        }
        
        // If self is a prefix of other, return the suffix of other's path that is not part of self.
        //         Otherwise, return None.
        //         
        public virtual void get_suffix(string other = "DerivedTypeVariable") {
            if (this.@base != other.@base) {
                return null;
            }
            if (this.path.Count > other.path.Count) {
                return null;
            }
            foreach (var _tup_1 in zip(this.path, other.path)) {
                var s_item = _tup_1.Item1;
                var o_item = _tup_1.Item2;
                if (s_item != o_item) {
                    return null;
                }
            }
            return other.path[this.path.Count];
        }
        
        // Retrieve the last item in the access path, if any. Return None if
        //         the path is empty.
        //         
        public virtual void tail() {
            if (this.path) {
                return this.path[-1];
            }
            return null;
        }
        
        // Create a new :py:class:`DerivedTypeVariable` identical to :param:`self` (which is
        //         unchanged) but with suffix appended to its path.
        //         
        public virtual DerivedTypeVariable add_suffix(AccessPathLabel suffix = AccessPathLabel) {
            var path = this.path.ToList();
            path.append(suffix);
            return new DerivedTypeVariable(this.@base, path);
        }
        
        // If :param:`prefix` is a prefix of :param:`self` with exactly one additional
        //         :py:class:`AccessPathLabel`, return the additional label. If not, return `None`.
        //         
        public virtual void get_single_suffix(string prefix = "DerivedTypeVariable") {
            if (this.@base != prefix.@base || this.path.Count != prefix.path.Count + 1 || this.path[:: - 1] != prefix.path) {
                return null;
            }
            return this.tail();
        }
        
        // Determine the variance of the access path.
        //         
        public virtual void path_variance() {
            var variances = map(label => label.variance(), this.path);
            return reduce(Variance.combine, variances, Variance.COVARIANT);
        }
        
        public override object ToString() {
            return this._str;
        }
    }
    
    // A type constraint of the form left ⊑ right (see Definition 3.3)
    //     
    public class SubtypeConstraint {
        
        public object left;
        
        public object right;
        
        public SubtypeConstraint(object left = DerivedTypeVariable, object right = DerivedTypeVariable) {
            this.left = left;
            this.right = right;
        }
        
        public virtual object @__eq__(object other = Any) {
            return other is SubtypeConstraint && this.left == other.left && this.right == other.right;
        }
        
        public virtual bool @__lt__(string other = "SubtypeConstraint") {
            if (this.left == other.left) {
                return this.right < other.right;
            }
            return this.left < other.left;
        }
        
        public virtual int @__hash__() {
            return hash(this.left) ^ hash(this.right);
        }
        
        public override string ToString() {
            return "{self.left} ⊑ {self.right}";
        }
    }
    
    // A (partitioned) set of type constraints
    //     
    public class ConstraintSet {
        
        public object logger;
        
        public object subtype;
        
        public ConstraintSet(void subtype = null) {
            if (subtype) {
                this.subtype = new HashSet<object>(subtype);
            } else {
                this.subtype = new HashSet<object>();
            }
            this.logger = logging.getLogger("ConstraintSet");
        }
        
        // Add a subtype constraint
        //         
        public virtual bool add_subtype(DerivedTypeVariable left = DerivedTypeVariable, DerivedTypeVariable right = DerivedTypeVariable) {
            var constraint = new SubtypeConstraint(left, right);
            return this.add(constraint);
        }
        
        public virtual bool add(object constraint = SubtypeConstraint) {
            if (this.subtype.Contains(constraint)) {
                return false;
            }
            this.subtype.add(constraint);
            return true;
        }
        
        public override string ToString() {
            var nt = os.linesep + "\t";
            return "ConstraintSet:{nt}{nt.join(map(str,self.subtype))}";
        }
    }
    
    // A forget or recall label in the graph. Instances should never be mutated.
    //     
    public class EdgeLabel {
        
        public int _hash;
        
        public string _str;
        
        public object capability;
        
        public object kind;
        
        public class Kind
            : Enum {
            
            public int FORGET;
            
            public int RECALL;
            
            public int FORGET = 1;
            
            public int RECALL = 2;
        }
        
        public EdgeLabel(AccessPathLabel capability = AccessPathLabel, Kind kind = Kind) {
            object type_str;
            this.capability = capability;
            this.kind = kind;
            if (this.kind == EdgeLabel.Kind.FORGET) {
                type_str = "forget";
            } else {
                type_str = "recall";
            }
            this._str = "{type_str} {self.capability}";
            this._hash = hash(this.capability) ^ hash(this.kind);
        }
        
        public virtual object @__eq__(object other = Any) {
            return other is EdgeLabel && this.capability == other.capability && this.kind == other.kind;
        }
        
        public virtual int @__hash__() {
            return this._hash;
        }
        
        public override string ToString() {
            return this._str;
        }
    }
    
    // A node in the graph of constraints. Node objects are immutable.
    // 
    //     Unforgettable is a flag used to differentiate between two subgraphs later in the algorithm. See
    //     :py:method:`Solver._unforgettable_subgraph_split` for details.
    //     
    public class Node {
        
        public int _hash;
        
        public string _str;
        
        public object _unforgettable;
        
        public object @base;
        
        public object suffix_variance;
        
        public class Unforgettable
            : Enum {
            
            public int POST_RECALL;
            
            public int PRE_RECALL;
            
            public int PRE_RECALL = 0;
            
            public int POST_RECALL = 1;
        }
        
        public Node(DerivedTypeVariable @base = DerivedTypeVariable, int suffix_variance = Variance, int unforgettable = Unforgettable.PRE_RECALL) {
            object summary;
            object variance;
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
            this._hash = hash(this.@base) ^ hash(summary);
        }
        
        public virtual object @__eq__(object other = Any) {
            return other is Node && this.@base == other.@base && this.suffix_variance == other.suffix_variance && this._unforgettable == other._unforgettable;
        }
        
        public virtual int @__hash__() {
            return this._hash;
        }
        
        // "Forget" the last element in the access path, creating a new Node. The new Node has
        //         variance that reflects this change.
        //         
        public virtual object forget_once() {
            if (this.@base.path) {
                var prefix_path = this.@base.path.ToList();
                var last = prefix_path.pop();
                var prefix = new DerivedTypeVariable(this.@base.@base, prefix_path);
                return (last, new Node(prefix, Variance.combine(last.variance(), this.suffix_variance)));
            }
            return (null, null);
        }
        
        // "Recall" label, creating a new Node. The new Node has variance that reflects this
        //         change.
        //         
        public virtual Node recall(AccessPathLabel label = AccessPathLabel) {
            var path = this.@base.path.ToList();
            path.append(label);
            var variance = Variance.combine(this.suffix_variance, label.variance());
            return new Node(new DerivedTypeVariable(this.@base.@base, path), variance);
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
            return new Node(this.@base, Variance.invert(this.suffix_variance), this._unforgettable);
        }
    }
}
