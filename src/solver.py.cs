
using Dict = typing.Dict;

using List = typing.List;

using Iterable = typing.Iterable;

using Set = typing.Set;

using Tuple = typing.Tuple;

using Union = typing.Union;

using AccessPathLabel = schema.AccessPathLabel;

using ConstraintSet = schema.ConstraintSet;

using DerivedTypeVariable = schema.DerivedTypeVariable;

using EdgeLabel = schema.EdgeLabel;

using LoadLabel = schema.LoadLabel;

using Node = schema.Node;

using StoreLabel = schema.StoreLabel;

using SubtypeConstraint = schema.SubtypeConstraint;

using Variance = schema.Variance;

using SchemaParser = parser.SchemaParser;

using os;

using networkx;

using System.Collections;

using System.Collections.Generic;

using System.Linq;

using System;

public static class solver {
    
    static solver() {
        // The driver for the retypd analysis.
    }
    
    // Represents the constraint graph in the slides. Essentially the same as the transducer from
    //     Appendix D. Edge weights use the formulation from the paper.
    //     
    public class ConstraintGraph {
        
        public networkx.DiGraph graph;
        
        public ConstraintGraph(ConstraintSet constraints) {
            this.graph = new networkx.DiGraph();
            foreach (var constraint in constraints.subtype) {
                this.add_edges(constraint.left, constraint.right);
            }
        }
        
        // Add an edge to the graph. The optional atts dict should include, if anything, a mapping
        //         from the string 'label' to an EdgeLabel object.
        //         
        public virtual bool add_edge(Node head, Node tail, Hashtable atts) {
            if (!this.graph.Contains(head) || !this.graph[head].Contains(tail)) {
                this.graph.add_edge(head, tail, atts);
                return true;
            }
            return false;
        }
        
        // Add an edge to the underlying graph. Also add its reverse with reversed variance.
        //         
        public virtual bool add_edges(DerivedTypeVariable sub , DerivedTypeVariable sup , Hashtable atts) {
            var changed = false;
            var forward_from = Node(sub, Variance.COVARIANT);
            var forward_to = Node(sup, Variance.COVARIANT);
            changed = this.add_edge(forward_from, forward_to, atts) || changed;
            var backward_from = forward_to.inverse();
            var backward_to = forward_from.inverse();
            changed = this.add_edge(backward_from, backward_to, atts) || changed;
            return changed;
        }
        
        // Add forget and recall nodes to the graph. Step 4 in the notes.
        //         
        public virtual void add_forget_recall() {
            var existing_nodes = new HashSet<object>(this.graph.nodes);
            foreach (var node in existing_nodes) {
                (capability, prefix) = node.forget_once();
                while (prefix) {
                    this.graph.add_edge(node, prefix, label: EdgeLabel(capability, EdgeLabel.Kind.FORGET));
                    this.graph.add_edge(prefix, node, label: EdgeLabel(capability, EdgeLabel.Kind.RECALL));
                    var node = prefix;
                    (capability, prefix) = node.forget_once();
                }
            }
        }
        
        // Add "shortcut" edges, per algorithm D.2 in the paper.
        //         
        public virtual void saturate() {
            object origin_z;
            object capability_l;
            object label;
            object tail_y;
            object head_x;
            var changed = false;
            var reaching_R = new Dictionary<object, object> {
            };
            Func<object, object, object> add_forgets = (dest,forgets) => {
                //LOCAL changed
                if (!reaching_R.Contains(dest) || !(forgets <= reaching_R[dest])) {
                    var changed = true;
                    reaching_R.setdefault(dest, new HashSet<object>()).update(forgets);
                }
            };
            Func<object, object, object> add_edge = (origin,dest) => {
                //LOCAL changed
                var changed = this.add_edge(origin, dest) || changed;
            };
            Func<object, object> is_contravariant = node => {
                return node.suffix_variance == Variance.CONTRAVARIANT;
            };
            foreach (var _tup_1 in this.graph.edges) {
                head_x = _tup_1.Item1;
                tail_y = _tup_1.Item2;
                label = this.graph[head_x][tail_y].get("label");
                if (label && label.kind == EdgeLabel.Kind.FORGET) {
                    add_forgets(tail_y, new HashSet({
                        (label.capability, head_x)}));
                }
            }
            while (changed) {
                changed = false;
                foreach (var _tup_2 in this.graph.edges) {
                    head_x = _tup_2.Item1;
                    tail_y = _tup_2.Item2;
                    if (!this.graph[head_x][tail_y].get("label")) {
                        add_forgets(tail_y, reaching_R.get(head_x, new HashSet<object>()));
                    }
                }
                var existing_edges = this.graph.edges.ToList();
                foreach (var _tup_3 in existing_edges) {
                    head_x = _tup_3.Item1;
                    tail_y = _tup_3.Item2;
                    label = this.graph[head_x][tail_y].get("label");
                    if (label && label.kind == EdgeLabel.Kind.RECALL) {
                        capability_l = label.capability;
                        foreach (var _tup_4 in reaching_R.get(head_x, new HashSet<object>())) {
                            label = _tup_4.Item1;
                            origin_z = _tup_4.Item2;
                            if (label == capability_l) {
                                add_edge(origin_z, tail_y);
                            }
                        }
                    }
                }
                var contravariant_vars = this.graph.nodes.Where(is_contravariant).ToList().ToList();
                foreach (var x in contravariant_vars) {
                    foreach (var _tup_5 in reaching_R.get(x, new HashSet<object>())) {
                        capability_l = _tup_5.Item1;
                        origin_z = _tup_5.Item2;
                        label = null;
                        if (capability_l == StoreLabel.instance()) {
                            label = LoadLabel.instance();
                        }
                        if (capability_l == LoadLabel.instance()) {
                            label = StoreLabel.instance();
                        }
                        if (label) {
                            add_forgets(x.inverse(), new HashSet({
                                (label, origin_z)}));
                        }
                    }
                }
            }
        }
        
        // A helper for __str__ that formats an edge
        //         
        [staticmethod]
        public static string edge_to_str(object graph, object edge = Tuple[Node,Node]) {
            var width = 2 + max(map(v => v.ToString().Count, graph.nodes));
            (sub, sup) = edge;
            var label = graph[sub][sup].get("label");
            var edge_str = "{str(sub):<{width}}→  {str(sup):<{width}}";
            if (label) {
                return edge_str + " ({label})";
            } else {
                return edge_str;
            }
        }
        
        [staticmethod]
        public static string graph_to_dot(object name = str, ConstraintGraph graph = networkx.DiGraph) {
            var nt = os.linesep + "\t";
            Func<object, object> edge_to_str = edge => {
                (sub, sup) = edge;
                var label = graph[sub][sup].get("label");
                var label_str = "";
                if (label) {
                    label_str = " [label=\"{label}\"]";
                }
                return "\"{sub}\" -> \"{sup}\"{label_str};";
            };
            Func<object, object> node_to_str = node => {
                return "\"{node}\";";
            };
            return "digraph {name} {{{nt}{nt.join(map(node_to_str, graph.nodes))}{nt}{nt.join(map(edge_to_str, graph.edges))}{os.linesep}}}";
        }
        
        [staticmethod]
        public static void write_to_dot(object name = str, object graph = networkx.DiGraph) {
            using (var dotfile = open("{name}.dot", "w")) {
                Console.WriteLine(ConstraintGraph.graph_to_dot(name, graph), file: dotfile);
            }
        }
        
        [staticmethod]
        public static string graph_to_str(object graph = networkx.DiGraph) {
            var nt = os.linesep + "\t";
            var edge_to_str = edge => ConstraintGraph.edge_to_str(graph, edge);
            return "{nt.join(map(edge_to_str, graph.edges))}";
        }
        
        public override string ToString() {
            var nt = os.linesep + "\t";
            return "ConstraintGraph:{nt}{ConstraintGraph.graph_to_str(self.graph)}";
        }
    }
    
    // Takes a saturated constraint graph and a set of interesting variables and generates subtype
    //     constraints. The constructor does not perform the computation; rather, :py:class:`Solver`
    //     objects are callable as thunks.
    //     
    public class Solver {
        
        public Dictionary<object, object> _type_vars;
        
        public ConstraintGraph constraint_graph;
        
        public object constraints;
        
        public object graph;
        
        public void interesting;
        
        public int next;
        
        public Solver(object constraints = ConstraintSet, object interesting = Iterable[Union[DerivedTypeVariable,str]]) {
            this.constraint_graph = new ConstraintGraph(constraints);
            this.interesting = new HashSet<object>();
            foreach (var var in interesting) {
                if (var is str) {
                    this.interesting.add(DerivedTypeVariable(var));
                } else {
                    this.interesting.add(var);
                }
            }
            this.next = 0;
            this.constraints = new HashSet<object>();
            this._type_vars = new Dictionary<object, object> {
            };
        }
        
        // Passes through to ConstraintGraph.add_forget_recall()
        //         
        public virtual void _add_forget_recall_edges() {
            this.constraint_graph.add_forget_recall();
        }
        
        // Passes through to ConstraintGraph.saturate()
        //         
        public virtual void _saturate() {
            this.constraint_graph.saturate();
        }
        
        // The algorithm, after saturation, only admits paths such that forget edges all precede
        //         the first recall edge (if there is such an edge). To enforce this, we modify the graph by
        //         splitting each node and the unlabeled and recall edges (but not forget edges!). Recall edges
        //         in the original graph are changed to point to the 'unforgettable' duplicate of their
        //         original target. As a result, no forget edges are reachable after traversing a single recall
        //         edge.
        //         
        public virtual void _unforgettable_subgraph_split() {
            var edges = new HashSet<object>(this.graph.edges);
            foreach (var _tup_1 in edges) {
                var head = _tup_1.Item1;
                var tail = _tup_1.Item2;
                var label = this.graph[head][tail].get("label");
                if (label && label.kind == EdgeLabel.Kind.FORGET) {
                    continue;
                }
                var recall_head = head.split_unforgettable();
                var recall_tail = tail.split_unforgettable();
                var atts = this.graph[head][tail];
                if (label && label.kind == EdgeLabel.Kind.RECALL) {
                    this.graph.remove_edge(head, tail);
                    this.graph.add_edge(head, recall_tail, atts);
                }
                this.graph.add_edge(recall_head, recall_tail, atts);
            }
        }
        
        // Take a string, convert it to a DerivedTypeVariable, and if there is a type variable that
        //         stands in for a prefix of it, change the prefix to the type variable.
        //         
        public virtual void lookup_type_var(Func<object> var_str = str) {
            var var = SchemaParser.parse_variable(var_str);
            return this._get_type_var(var);
        }
        
        // Look up a type variable by name. If it (or a prefix of it) exists in _type_vars, form the
        //         appropriate variable by adding the leftover part of the suffix. If not, return the variable
        //         as passed in.
        //         
        public virtual void _get_type_var(object var = DerivedTypeVariable) {
            foreach (var expanded in this._type_vars.OrderByDescending(_p_1 => _p_1).ToList()) {
                var suffix = expanded.get_suffix(var);
                if (suffix != null) {
                    var type_var = this._type_vars[expanded];
                    return DerivedTypeVariable(type_var.@base, suffix);
                }
            }
            return var;
        }
        
        // Retrieve or generate a type variable. Automatically adds this variable to the set of
        //         interesting variables.
        //         
        public virtual void _make_type_var(object @base = DerivedTypeVariable) {
            if (this._type_vars.Contains(@base)) {
                return;
            }
            var var = DerivedTypeVariable("τ_{self.next}");
            this._type_vars[@base] = var;
            this.interesting.add(@base);
            this.next += 1;
        }
        
        // Return a set of elements from variables for which no prefix exists in variables. For
        //         example, if variables were the set {A, A.load}, return the set {A}.
        //         
        [staticmethod]
        public static void _filter_no_prefix(object variables = Iterable[DerivedTypeVariable]) {
            var selected = new HashSet<object>();
            var candidates = variables.OrderByDescending(_p_1 => _p_1).ToList();
            foreach (var _tup_1 in candidates.Select((_p_2,_p_3) => Tuple.Create(_p_3, _p_2))) {
                var index = _tup_1.Item1;
                var candidate = _tup_1.Item2;
                var emit = true;
                foreach (var other in candidates[index + 1]) {
                    if (other.get_suffix(candidate)) {
                        emit = false;
                        break;
                    }
                }
                if (emit) {
                    selected.add(candidate);
                }
            }
            return selected;
        }
        
        // Identify at least one node in each nontrivial SCC and generate a type variable for it.
        //         This ensures that the path exploration algorithm will never loop; instead, it will generate
        //         constraints that are recursive on the type variable.
        // 
        //         To do so, find strongly connected components (such that loops may contain forget or recall
        //         edges but never both). In each nontrivial SCC (in reverse topological order), identify all
        //         vertices with predecessors in a strongly connected component that has not yet been visited.
        //         If any of the identified variables is a prefix of another (e.g., φ and φ.load.σ8@0),
        //         eliminate the longer one. Add these variables to a set of candidates and to the set of
        //         interesting variables, where path execution will stop.
        // 
        //         Once all SCCs have been processed, minimize the set of candidates as before (remove
        //         variables that have a prefix in the set) and emit type variables for each remaining
        //         candidate.
        //         
        public virtual void _generate_type_vars() {
            var forget_graph = networkx.DiGraph(this.graph);
            var recall_graph = networkx.DiGraph(this.graph);
            foreach (var _tup_1 in this.graph.edges) {
                var head = _tup_1.Item1;
                var tail = _tup_1.Item2;
                var label = this.graph[head][tail].get("label");
                if (label) {
                    if (label.kind == EdgeLabel.Kind.FORGET) {
                        recall_graph.remove_edge(head, tail);
                    } else {
                        forget_graph.remove_edge(head, tail);
                    }
                }
            }
            var loop_breakers = new HashSet<object>();
            foreach (var graph in new List<object> {
                forget_graph,
                recall_graph
            }) {
                var condensation = networkx.condensation(graph);
                var visited = new HashSet<object>();
                foreach (var scc_node in reversed(networkx.topological_sort(condensation).ToList())) {
                    var candidates = new HashSet<object>();
                    var scc = condensation.nodes[scc_node]["members"];
                    visited.add(scc_node);
                    if (scc.Count == 1) {
                        continue;
                    }
                    foreach (var node in scc) {
                        foreach (var predecessor in this.graph.predecessors(node)) {
                            var scc_index = condensation.graph["mapping"][predecessor];
                            if (!visited.Contains(scc_index)) {
                                candidates.add(node.@base);
                            }
                        }
                    }
                    candidates = Solver._filter_no_prefix(candidates);
                    loop_breakers |= candidates;
                    this.interesting |= candidates;
                }
            }
            loop_breakers = Solver._filter_no_prefix(loop_breakers);
            foreach (var var in loop_breakers) {
                this._make_type_var(var);
            }
        }
        
        // Find all non-empty paths from origin to nodes that represent interesting type variables.
        //         Return the list of labels encountered along the way as well as the destination reached.
        //         
        public virtual object _find_paths(object origin = Node, object path = new List<object>(), object @string = new List<object>()) {
            if (path && this.interesting.Contains(origin.@base)) {
                return new List<Tuple<List<object>, object>> {
                    (@string, origin)
                };
            }
            if (path.Contains(origin)) {
                return new List<object>();
            }
            path = path.ToList();
            path.append(origin);
            var all_paths = new List<object>();
            if (this.graph.Contains(origin)) {
                foreach (var succ in this.graph[origin]) {
                    var label = this.graph[origin][succ].get("label");
                    var new_string = @string.ToList();
                    if (label) {
                        new_string.append(label);
                    }
                    all_paths += this._find_paths(succ, path, new_string);
                }
            }
            return all_paths;
        }
        
        // Generate constraints by adding the forgets in string to origin and the recalls in string
        //         to dest. If both of the generated vertices are covariant (the empty string's variance is
        //         covariant, so only covariant vertices can represent a derived type variable without an
        //         elided portion of its path) and if the two variables are not equal, emit a constraint.
        //         
        public virtual void _maybe_add_constraint(object origin = Node, object dest = Node, object @string = List[EdgeLabel]) {
            var lhs = origin;
            var rhs = dest;
            foreach (var label in @string) {
                if (label.kind == EdgeLabel.Kind.FORGET) {
                    rhs = rhs.recall(label.capability);
                } else {
                    lhs = lhs.recall(label.capability);
                }
            }
            if (lhs.suffix_variance == Variance.COVARIANT && rhs.suffix_variance == Variance.COVARIANT) {
                var lhs_var = this._get_type_var(lhs.@base);
                var rhs_var = this._get_type_var(rhs.@base);
                if (lhs_var != rhs_var) {
                    var constraint = SubtypeConstraint(lhs_var, rhs_var);
                    this.constraints.add(constraint);
                }
            }
        }
        
        // Now that type variables have been computed, no cycles can be produced. Find paths from
        //         interesting nodes to other interesting nodes and generate constraints.
        //         
        public virtual void _generate_constraints() {
            foreach (var node in this.graph.nodes) {
                if (this.interesting.Contains(node.@base)) {
                    foreach (var _tup_1 in this._find_paths(node)) {
                        var @string = _tup_1.Item1;
                        var dest = _tup_1.Item2;
                        this._maybe_add_constraint(node, dest, @string);
                    }
                }
            }
        }
        
        // Loops from a node directly to itself are not useful, so it's useful to remove them.
        //         
        public virtual void _remove_self_loops() {
            this.graph.remove_edges_from((from node in this.graph.nodes
                select (node, node)).ToHashSet());
        }
        
        // Perform the retypd calculation.
        //         
        public virtual void @__call__() {
            this._add_forget_recall_edges();
            this._saturate();
            this.graph = networkx.DiGraph(this.constraint_graph.graph);
            this._remove_self_loops();
            this._generate_type_vars();
            this._unforgettable_subgraph_split();
            this._generate_constraints();
            return this.constraints;
        }
    }
}
