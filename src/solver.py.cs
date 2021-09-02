/*
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
*/
using SchemaParser = parser.SchemaParser;
using schema;
using retypd;

/*
using os;
*/

using static networkx;

using System.Collections;

using System.Collections.Generic;

using System.Linq;

using System;
using System.IO;

public static class solver {
    
    // The driver for the retypd analysis.
    
    // Represents the constraint graph in the slides. Essentially the same as the transducer from
    //     Appendix D. Edge weights use the formulation from the paper.
    //     
    public class ConstraintGraph {
        
        public networkx.DiGraph<Node> graph;
        
        public ConstraintGraph(ConstraintSet constraints) {
            this.graph = new networkx.DiGraph<Node>();
            foreach (var constraint in constraints.subtype) {
                this.add_edges(constraint.left, constraint.right);
            }
        }
        
        // Add an edge to the graph. The optional atts dict should include, if anything, a mapping
        //         from the string 'label' to an EdgeLabel object.
        //         
        public virtual bool add_edge(Node head, Node tail, Dictionary<string,object>? atts = null) {
            if (!this.graph.Contains(head) || !this.graph[head].Contains(tail))
            {
                this.graph.add_edge(head, tail, atts ?? new Dictionary<string, object>());
                return true;
            }
            return false;
        }
        
        // Add an edge to the underlying graph. Also add its reverse with reversed variance.
        //         
        public virtual bool add_edges(DerivedTypeVariable sub , DerivedTypeVariable sup , Dictionary<string, object>? atts = null) {
            var changed = false;
            var forward_from = new Node(sub, Variance.COVARIANT);
            var forward_to = new Node(sup, Variance.COVARIANT);
            changed = this.add_edge(forward_from, forward_to, atts) || changed;
            var backward_from = forward_to.inverse();
            var backward_to = forward_from.inverse();
            changed = this.add_edge(backward_from, backward_to, atts) || changed;
            return changed;
        }
        
        // Add forget and recall nodes to the graph. Step 4 in the notes.
        //         
        public virtual void add_forget_recall() {
            var existing_nodes = new HashSet<Node>(this.graph.nodes);
            foreach (var n in existing_nodes) {
                var node = n;
                var (capability, prefix) = node.forget_once();
                while (prefix is not null) {
                    var forLabel = new Dictionary<string, object> { { "label", new EdgeLabel(capability!, EdgeLabel.Kind.FORGET) } };
                    var recLabel = new Dictionary<string, object> { { "label", new EdgeLabel(capability!, EdgeLabel.Kind.RECALL) } };
                    this.graph.add_edge(node, prefix, forLabel);
                    this.graph.add_edge(prefix, node, recLabel);
                    node = prefix;
                    (capability, prefix) = node.forget_once();
                }
            }
        }
        
        // Add "shortcut" edges, per algorithm D.2 in the paper.
        //         
        public virtual void saturate() {
            var changed = false;
            var reaching_R = new Dictionary<Node, HashSet<(AccessPathLabel, Node)>> { };
            void add_forgets(Node dest, HashSet<(AccessPathLabel, Node)> forgets) {
                //if (!reaching_R.ContainsKey(dest) || !(forgets <= reaching_R[dest])) {
                if (!reaching_R.ContainsKey(dest) || !(forgets.IsSubsetOf(reaching_R[dest]))) {
                    changed = true;
                    if (!reaching_R.TryGetValue(dest, out var fs)) {
                        fs = new HashSet<(AccessPathLabel, Node)>();
                        reaching_R.Add(dest, fs);
                    }
                    fs.UnionWith(forgets);
                }
            };
            void add_edge(Node origin, Node dest)  {
                changed = this.add_edge(origin, dest) || changed;
            };
            bool is_contravariant(Node node) {
                return node.suffix_variance == Variance.CONTRAVARIANT;
            };
            foreach (var (head_x, tail_y) in this.graph.edges) {
                var label = (EdgeLabel)this.graph[head_x][tail_y].get("label");
                if (label is not null && label.kind == EdgeLabel.Kind.FORGET) {
                    add_forgets(tail_y, new HashSet<(AccessPathLabel, Node)> { 
                        (label.capability, head_x)});
                }
            }
            while (changed) {
                changed = false;
                foreach (var (head_x, tail_y) in this.graph.edges) {
                    if (this.graph[head_x][tail_y].get("label") is null) {
                        add_forgets(tail_y, reaching_R.get(head_x, new HashSet<(AccessPathLabel, Node)>()));
                    }
                }
                var existing_edges = this.graph.edges.ToList();
                foreach (var (head_x, tail_y) in existing_edges) {
                    var label = (EdgeLabel) this.graph[head_x][tail_y].get("label");
                    if (label is not null && label.kind == EdgeLabel.Kind.RECALL) {
                        var capability_l = label.capability;
                        foreach (var _tup_4 in reaching_R.get(head_x, new HashSet<(AccessPathLabel, Node)>())) {
                            var aplabel = _tup_4.Item1;
                            var origin_z = _tup_4.Item2;
                            if (aplabel == capability_l) {
                                add_edge(origin_z, tail_y);
                            }
                        }
                    }
                }
                var contravariant_vars = this.graph.nodes.Where(is_contravariant).ToList();
                foreach (var x in contravariant_vars) {
                    if (!reaching_R.TryGetValue(x, out var set))
                        continue;
                    foreach (var (capability_l, origin_z) in set) {
                        AccessPathLabel? label = null;
                        if (capability_l == StoreLabel.instance) {
                            label = LoadLabel.instance;
                        }
                        if (capability_l == LoadLabel.instance) {
                            label = StoreLabel.instance;
                        }
                        if (label is not null) {
                            add_forgets(x.inverse(), new HashSet<(AccessPathLabel, Node)>{
                                (label, origin_z)});
                        }
                    }
                }
            }
        }
        
        // A helper for __str__ that formats an edge
        //         
        public static string edge_to_str(networkx.DiGraph<Node> graph, (Node,Node) edge) {
            var width = 2 + graph.nodes.Select(v => v.ToString().Length).Max();
            var (sub, sup) = edge;
            var label = graph[sub][sup].get("label");
            var edge_str = $"{sub.ToString().PadRight(width)}→  {sup.ToString().PadRight(width)}";
            if (label is not null) {
                return edge_str + $" ({label})";
            } else {
                return edge_str;
            }
        }
        
        public static string graph_to_dot(string name, networkx.DiGraph<Node> graph) {
            var nt = Environment.NewLine + "\t";
            string edge_to_str((Node,Node) edge) {
                var (sub, sup) = edge;
                var label = graph[sub][sup].get("label");
                var label_str = "";
                if (label is not null) {
                    label_str = $" [label=\"{label}\"]";
                }
                return $"\"{sub}\" -> \"{sup}\"{label_str};";
            };
            static string node_to_str(Node node) {
                return $"\"{node}\";";
            };
            return $"digraph {name} {{{nt}{string.Join(nt, graph.nodes.Select(node_to_str))}{nt}{string.Join(nt, graph.edges.Select(edge_to_str))}{Environment.NewLine}}}";
        }
        
        public static void write_to_dot(string name , networkx.DiGraph<Node> graph) {
            using (var dotfile = File.CreateText($"{name}.dot")) {
                dotfile.WriteLine(ConstraintGraph.graph_to_dot(name, graph));
            }
        }
        
        public static string graph_to_str(networkx.DiGraph<Node> graph) {
            var nt = Environment.NewLine + "\t";
            string edge_to_str((Node,Node)edge) => ConstraintGraph.edge_to_str(graph, edge);
            return $"{string.Join(nt, graph.edges.Select(edge_to_str))}";
        }
        
        public override string ToString() {
            var nt = Environment.NewLine + "\t";
            return $"ConstraintGraph:{nt}{ConstraintGraph.graph_to_str(this.graph)}";
        }
    }
    
    // Takes a saturated constraint graph and a set of interesting variables and generates subtype
    //     constraints. The constructor does not perform the computation; rather, :py:class:`Solver`
    //     objects are callable as thunks.
    //     
    public class Solver {
        
        public Dictionary<DerivedTypeVariable, DerivedTypeVariable> _type_vars;
        public ConstraintGraph constraint_graph;
        public HashSet<SubtypeConstraint> constraints;
        public DiGraph<Node> graph;
        
        // C# doesn't have union types or this would be:
        // IEnumerable<Union(DerivedTypeVariable, string)>
        public HashSet<DerivedTypeVariable> interesting;
        
        public int next;
        
        public Solver(ConstraintSet constraints, IEnumerable<object> interesting) {
            this.constraint_graph = new ConstraintGraph(constraints);
            this.interesting = new HashSet<DerivedTypeVariable>();
            foreach (var var in interesting) {
                if (var is string sVar) {
                    this.interesting.Add(new DerivedTypeVariable(sVar));
                } else {
                    this.interesting.Add((DerivedTypeVariable) var);
                }
            }
            this.next = 0;
            this.constraints = new HashSet<SubtypeConstraint>();
            this._type_vars = new Dictionary<DerivedTypeVariable, DerivedTypeVariable> {};
            this.graph = default!;
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
            var edges = new HashSet<(Node,Node)>(this.graph.edges);
            foreach (var (head,tail) in edges) {
                var label = (EdgeLabel)this.graph[head][tail].get("label");
                if (label is not null && label.kind == EdgeLabel.Kind.FORGET) {
                    continue;
                }
                var recall_head = head.split_unforgettable();
                var recall_tail = tail.split_unforgettable();
                var atts = this.graph[head][tail];
                if (label is not null && label.kind == EdgeLabel.Kind.RECALL) {
                    this.graph.remove_edge(head, tail);
                    this.graph.add_edge(head, recall_tail, atts);
                }
                this.graph.add_edge(recall_head, recall_tail, atts);
            }
        }
        
        // Take a string, convert it to a DerivedTypeVariable, and if there is a type variable that
        //         stands in for a prefix of it, change the prefix to the type variable.
        //         
        public virtual DerivedTypeVariable lookup_type_var(string var_str) {
            var var = SchemaParser.parse_variable(var_str);
            return this._get_type_var(var);
        }
        
        // Look up a type variable by name. If it (or a prefix of it) exists in _type_vars, form the
        //         appropriate variable by adding the leftover part of the suffix. If not, return the variable
        //         as passed in.
        //         
        public virtual DerivedTypeVariable _get_type_var(DerivedTypeVariable var) {
            foreach (var expanded in this._type_vars.Keys.OrderByDescending(_p_1 => _p_1).ToList()) {
                var suffix = expanded.get_suffix(var);
                if (suffix != null) {
                    var type_var = this._type_vars[expanded];
                    return new DerivedTypeVariable(type_var.@base, suffix);
                }
            }
            return var;
        }
        
        // Retrieve or generate a type variable. Automatically adds this variable to the set of
        //         interesting variables.
        //         
        public virtual void _make_type_var(DerivedTypeVariable @base) {
            if (this._type_vars.ContainsKey(@base)) {
                return;
            }
            var var = new DerivedTypeVariable($"τ_{this.next}");
            this._type_vars[@base] = var;
            this.interesting.Add(@base);
            this.next += 1;
        }
        
        // Return a set of elements from variables for which no prefix exists in variables. For
        //         example, if variables were the set {A, A.load}, return the set {A}.
        //         
        public static HashSet<DerivedTypeVariable> _filter_no_prefix(IEnumerable<DerivedTypeVariable> variables) {
            var selected = new HashSet<DerivedTypeVariable>();
            var candidates = variables.OrderByDescending(_p_1 => _p_1).ToList();
            foreach (var (index, candidate) in candidates.Select((_p_2,_p_3) => (_p_3, _p_2))) {
                var emit = true;
                foreach (var other in candidates.Skip(index + 1)) {
                    if (other.get_suffix(candidate) is not null) {
                        emit = false;
                        break;
                    }
                }
                if (emit) {
                    selected.Add(candidate);
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
            var forget_graph = new networkx.DiGraph<Node>(this.graph);
            var recall_graph = new networkx.DiGraph<Node>(this.graph);
            foreach (var (head, tail) in this.graph.edges) {
                var label = (EdgeLabel)this.graph[head][tail].get("label");
                if (label is not null) {
                    if (label.kind == EdgeLabel.Kind.FORGET) {
                        recall_graph.remove_edge(head, tail);
                    } else {
                        forget_graph.remove_edge(head, tail);
                    }
                }
            }
            var loop_breakers = new HashSet<DerivedTypeVariable>();
            foreach (var graph in new List<DiGraph<Node>> {
                forget_graph,
                recall_graph
            }) {
                var condensation = networkx.condensation(graph);
                var visited = new HashSet<int>();
                foreach (var scc_node in networkx.topological_sort(condensation).AsEnumerable().Reverse()) {
                    var candidates = new HashSet<DerivedTypeVariable>();
                    var scc = (List<Node>)condensation.nodes[scc_node]["members"];
                    visited.Add(scc_node);
                    if (scc.Count == 1) {
                        continue;
                    }
                    foreach (var node in scc) {
                        foreach (var predecessor in this.graph.predecessors(node)) {
                            var mapping = (Dictionary<Node, int>)condensation.graph["mapping"];
                            var scc_index = mapping[predecessor];
                            if (!visited.Contains(scc_index)) {
                                candidates.Add(node.@base);
                            }
                        }
                    }
                    candidates = Solver._filter_no_prefix(candidates);
                    loop_breakers.UnionWith(candidates);
                    this.interesting.UnionWith(candidates);
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
        public virtual List<(List<EdgeLabel>,Node)> _find_paths(
            Node origin,
            List<Node>? path = null,
            List<EdgeLabel>? @string = null)
        {
            path ??= new List<Node>();
            @string ??= new List<EdgeLabel>();
            if (path.Count > 0 && this.interesting.Contains(origin.@base)) {
                return new List<(List<EdgeLabel>, Node)> {
                    (@string, origin)
                };
            }
            if (path.Contains(origin)) {
                return new List<(List<EdgeLabel>, Node)>();
            }
            path = path.ToList();
            path.Add(origin);
            var all_paths = new List<(List<EdgeLabel>, Node)>();
            if (this.graph.Contains(origin)) {
                foreach (var succ in this.graph.successors(origin)) {
                    var label = (EdgeLabel) this.graph[origin][succ].get("label");
                    var new_string = @string.ToList();
                    if (label is not null) {
                        new_string.Add(label);
                    }
                    all_paths.AddRange(this._find_paths(succ, path, new_string));
                }
            }
            return all_paths;
        }
        
        // Generate constraints by adding the forgets in string to origin and the recalls in string
        //         to dest. If both of the generated vertices are covariant (the empty string's variance is
        //         covariant, so only covariant vertices can represent a derived type variable without an
        //         elided portion of its path) and if the two variables are not equal, emit a constraint.
        //         
        public virtual void _maybe_add_constraint(Node origin, Node dest, List<EdgeLabel> @string)
        {
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
                    var constraint = new SubtypeConstraint(lhs_var, rhs_var);
                    this.constraints.Add(constraint);
                }
            }
        }
        
        // Now that type variables have been computed, no cycles can be produced. Find paths from
        //         interesting nodes to other interesting nodes and generate constraints.
        //         
        public virtual void _generate_constraints() {
            foreach (var node in this.graph.nodes) {
                if (this.interesting.Contains(node.@base)) {
                    foreach (var (@string,dest) in this._find_paths(node)) {
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
        public virtual HashSet<SubtypeConstraint> @__call__() {
            this._add_forget_recall_edges();
            this._saturate();
            this.graph = new networkx.DiGraph<Node>(this.constraint_graph.graph);
            this._remove_self_loops();
            this._generate_type_vars();
            this._unforgettable_subgraph_split();
            this._generate_constraints();
            return this.constraints;
        }
    }
}
