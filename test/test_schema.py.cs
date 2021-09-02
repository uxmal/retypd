// Simple unit tests from the paper and slides.
// 

/*using ABC = abc.ABC;

using networkx;

using unittest;

*/
using ConstraintSet = schema.ConstraintSet;

using SchemaParser = parser.SchemaParser;

using Solver = solver.Solver;

using System.Collections.Generic;

using System.Collections;
using NUnit.Framework;
using schema;
using System.Linq;

public static class test_schema {
    
    [TestFixture]
    public class SchemaTest
        {
        
        public virtual void graphs_are_equal(networkx.DiGraph<Node> graph, Dictionary<(Node,Node), Dictionary<string,object>> edge_set) {
            var edges = graph.edges.ToArray();
            Assert.AreEqual(edges.Length, edge_set.Count);
            foreach (var edge in edges) {
                Assert.IsTrue(edge_set.ContainsKey(edge));
                Assert.Equals(graph[edge.Item1][edge.Item2], edge_set[edge]);
            }
        }
        
        // Convert a collection of edge strings into a dict. Used for comparing a graph against an
        //         expected value.
        //         
        public static Dictionary<(Node, Node), Dictionary<string, object>> edges_to_dict(IEnumerable<string> edges) {
            var graph = new Dictionary<(Node, Node), Dictionary<string,object>> {
            };
            foreach (var edge in edges) {
                var (head, tail, atts) = SchemaParser.parse_edge(edge);
                graph[(head,tail)] = atts;
            }
            return graph;
        }
    }
    
    public class BasicSchemaTest
        : SchemaTest{
        
        // A simple test from the paper (the right side of Figure 4 on p. 6). This one has no
        //         recursive data structures; as such, the fixed point would suffice. However, we compute type
        //         constraints in the same way as in the presence of recursion.
        //         
        [Test]
        public virtual void test_simple_constraints() {
            var constraints = new ConstraintSet();
            constraints.add(SchemaParser.parse_constraint("p ⊑ q"));
            constraints.add(SchemaParser.parse_constraint("x ⊑ q.store.σ4@0"));
            constraints.add(SchemaParser.parse_constraint("p.load.σ4@0 ⊑ y"));
            var solver = new Solver(constraints, new HashSet<string>{ "x", "y"});
            solver._add_forget_recall_edges();
            var forget_recall = new List<string> {
                "p.load.⊕        ->  p.⊕              (forget load)",
                "p.load.⊖        ->  p.⊖              (forget load)",
                "p.load.⊕        ->  p.load.σ4@0.⊕    (recall σ4@0)",
                "p.load.⊖        ->  p.load.σ4@0.⊖    (recall σ4@0)",
                "p.load.σ4@0.⊕   ->  p.load.⊕         (forget σ4@0)",
                "p.load.σ4@0.⊖   ->  p.load.⊖         (forget σ4@0)",
                "p.load.σ4@0.⊕   ->  y.⊕",
                "p.⊕             ->  p.load.⊕         (recall load)",
                "p.⊖             ->  p.load.⊖         (recall load)",
                "p.⊕             ->  q.⊕",
                "q.⊖             ->  p.⊖",
                "q.⊕             ->  q.store.⊖        (recall store)",
                "q.⊖             ->  q.store.⊕        (recall store)",
                "q.store.⊕       ->  q.⊖              (forget store)",
                "q.store.⊖       ->  q.⊕              (forget store)",
                "q.store.⊕       ->  q.store.σ4@0.⊕   (recall σ4@0)",
                "q.store.⊖       ->  q.store.σ4@0.⊖   (recall σ4@0)",
                "q.store.σ4@0.⊕  ->  q.store.⊕        (forget σ4@0)",
                "q.store.σ4@0.⊖  ->  q.store.⊖        (forget σ4@0)",
                "q.store.σ4@0.⊖  ->  x.⊖",
                "x.⊕             ->  q.store.σ4@0.⊕",
                "y.⊖             ->  p.load.σ4@0.⊖"
            };
            var forget_recall_graph = SchemaTest.edges_to_dict(forget_recall);
            this.graphs_are_equal(solver.constraint_graph.graph, forget_recall_graph);
            solver._saturate();
            var saturated = new List<string> {
                "p.load.⊕        →  p.⊕              (forget load)",
                "p.load.⊖        →  p.⊖              (forget load)",
                "p.load.⊕        →  p.load.⊕",
                "p.load.⊖        →  p.load.⊖",
                "p.load.⊕        →  p.load.σ4@0.⊕    (recall σ4@0)",
                "p.load.⊖        →  p.load.σ4@0.⊖    (recall σ4@0)",
                "p.load.⊖        →  q.store.⊖",
                "p.load.σ4@0.⊕   →  p.load.⊕         (forget σ4@0)",
                "p.load.σ4@0.⊖   →  p.load.⊖         (forget σ4@0)",
                "p.load.σ4@0.⊕   →  p.load.σ4@0.⊕",
                "p.load.σ4@0.⊖   →  p.load.σ4@0.⊖",
                "p.load.σ4@0.⊖   →  q.store.σ4@0.⊖",
                "p.load.σ4@0.⊕   →  y.⊕",
                "p.⊕             →  p.load.⊕         (recall load)",
                "p.⊖             →  p.load.⊖         (recall load)",
                "p.⊕             →  q.⊕",
                "q.⊖             →  p.⊖",
                "q.⊕             →  q.store.⊖        (recall store)",
                "q.⊖             →  q.store.⊕        (recall store)",
                "q.store.⊕       →  p.load.⊕",
                "q.store.⊖       →  q.⊕              (forget store)",
                "q.store.⊕       →  q.⊖              (forget store)",
                "q.store.⊖       →  q.store.⊖",
                "q.store.⊕       →  q.store.⊕",
                "q.store.⊖       →  q.store.σ4@0.⊖   (recall σ4@0)",
                "q.store.⊕       →  q.store.σ4@0.⊕   (recall σ4@0)",
                "q.store.σ4@0.⊕  →  p.load.σ4@0.⊕",
                "q.store.σ4@0.⊕  →  q.store.⊕        (forget σ4@0)",
                "q.store.σ4@0.⊖  →  q.store.⊖        (forget σ4@0)",
                "q.store.σ4@0.⊕  →  q.store.σ4@0.⊕",
                "q.store.σ4@0.⊖  →  q.store.σ4@0.⊖",
                "q.store.σ4@0.⊖  →  x.⊖",
                "x.⊕             →  q.store.σ4@0.⊕",
                "y.⊖             →  p.load.σ4@0.⊖"
            };
            var saturated_graph = SchemaTest.edges_to_dict(saturated);
            this.graphs_are_equal(solver.constraint_graph.graph, saturated_graph);
            solver.graph = new networkx.DiGraph<Node>(solver.constraint_graph.graph);
            solver._remove_self_loops();
            solver._generate_type_vars();
            solver._unforgettable_subgraph_split();
            solver._generate_constraints();
            Assert.IsTrue(solver.constraints.Contains(SchemaParser.parse_constraint("x ⊑ y")));
        }
        
        // Another simple test from the paper (the program modeled in Figure 14 on p. 26).
        //
        [Test]
        public virtual void test_other_simple_constraints() {
            var constraints = new ConstraintSet();
            constraints.add(SchemaParser.parse_constraint("y <= p"));
            constraints.add(SchemaParser.parse_constraint("p <= x"));
            constraints.add(SchemaParser.parse_constraint("A <= x.store"));
            constraints.add(SchemaParser.parse_constraint("y.load <= B"));
            var solver = new Solver(constraints, new HashSet<string> { "A", "B" });
            solver._add_forget_recall_edges();
            var forget_recall = new List<string> {
                "A.⊕        →  x.store.⊕",
                "B.⊖        →  y.load.⊖",
                "p.⊕        →  x.⊕",
                "p.⊖        →  y.⊖",
                "x.⊖        →  p.⊖",
                "x.store.⊖  →  A.⊖",
                "x.store.⊕  →  x.⊖         (forget store)",
                "x.store.⊖  →  x.⊕         (forget store)",
                "x.⊕        →  x.store.⊖   (recall store)",
                "x.⊖        →  x.store.⊕   (recall store)",
                "y.load.⊕   →  B.⊕",
                "y.load.⊕   →  y.⊕         (forget load)",
                "y.load.⊖   →  y.⊖         (forget load)",
                "y.⊕        →  p.⊕",
                "y.⊕        →  y.load.⊕    (recall load)",
                "y.⊖        →  y.load.⊖    (recall load)"
            };
            var forget_recall_graph = SchemaTest.edges_to_dict(forget_recall);
            this.graphs_are_equal(solver.constraint_graph.graph, forget_recall_graph);
            solver._saturate();
            var saturated = new List<string> {
                "A.⊕        →  x.store.⊕",
                "B.⊖        →  y.load.⊖",
                "p.⊕        →  x.⊕",
                "p.⊖        →  y.⊖",
                "x.⊖        →  p.⊖",
                "x.store.⊖  →  A.⊖",
                "x.store.⊕  →  x.⊖         (forget store)",
                "x.store.⊖  →  x.⊕         (forget store)",
                "x.store.⊕  →  x.store.⊕",
                "x.store.⊖  →  x.store.⊖",
                "x.store.⊕  →  y.load.⊕",
                "x.⊕        →  x.store.⊖   (recall store)",
                "x.⊖        →  x.store.⊕   (recall store)",
                "y.load.⊕   →  B.⊕",
                "y.load.⊖   →  x.store.⊖",
                "y.load.⊕   →  y.⊕         (forget load)",
                "y.load.⊖   →  y.⊖         (forget load)",
                "y.load.⊕   →  y.load.⊕",
                "y.load.⊖   →  y.load.⊖",
                "y.⊕        →  p.⊕",
                "y.⊕        →  y.load.⊕    (recall load)",
                "y.⊖        →  y.load.⊖    (recall load)"
            };
            var saturated_graph = SchemaTest.edges_to_dict(saturated);
            this.graphs_are_equal(solver.constraint_graph.graph, saturated_graph);
            solver.graph = new networkx.DiGraph<Node>(solver.constraint_graph.graph);
            solver._remove_self_loops();
            solver._generate_type_vars();
            solver._unforgettable_subgraph_split();
            solver._generate_constraints();
            Assert.IsTrue(solver.constraints.Contains(SchemaParser.parse_constraint("A ⊑ B")));
        }
    }
    
    public class RecursiveSchemaTest
        : SchemaTest {
        
        // A test based on the running example from the paper (Figure 2 on p. 3) and the slides
        //         (slides 67-83, labeled as slides 13-15).
        //         
        [Test]
        public virtual void test_recursive() {
            var constraints = new ConstraintSet();
            constraints.add(SchemaParser.parse_constraint("F.in_0 ⊑ δ"));
            constraints.add(SchemaParser.parse_constraint("α ⊑ φ"));
            constraints.add(SchemaParser.parse_constraint("δ ⊑ φ"));
            constraints.add(SchemaParser.parse_constraint("φ.load.σ4@0 ⊑ α"));
            constraints.add(SchemaParser.parse_constraint("φ.load.σ4@4 ⊑ α'"));
            constraints.add(SchemaParser.parse_constraint("α' ⊑ close.in_0"));
            constraints.add(SchemaParser.parse_constraint("close.out ⊑ F.out"));
            constraints.add(SchemaParser.parse_constraint("close.in_0 ⊑ #FileDescriptor"));
            constraints.add(SchemaParser.parse_constraint("#SuccessZ ⊑ close.out"));
            var solver = new Solver(constraints, new HashSet<string> {
                "F",
                "#FileDescriptor",
                "#SuccessZ"});
            solver._add_forget_recall_edges();
            var forget_recall = new List<string> {
                "close.⊕            →  close.in_0.⊖        (recall in_0)",
                "close.⊖            →  close.in_0.⊕        (recall in_0)",
                "close.⊕            →  close.out.⊕         (recall out)",
                "close.⊖            →  close.out.⊖         (recall out)",
                "close.in_0.⊕       →  close.⊖             (forget in_0)",
                "close.in_0.⊖       →  close.⊕             (forget in_0)",
                "close.in_0.⊕       →  #FileDescriptor.⊕",
                "close.in_0.⊖       →  α'.⊖",
                "close.out.⊕        →  close.⊕             (forget out)",
                "close.out.⊖        →  close.⊖             (forget out)",
                "close.out.⊕        →  F.out.⊕",
                "close.out.⊖        →  #SuccessZ.⊖",
                "F.⊖                →  F.in_0.⊕            (recall in_0)",
                "F.⊕                →  F.in_0.⊖            (recall in_0)",
                "F.⊖                →  F.out.⊖             (recall out)",
                "F.⊕                →  F.out.⊕             (recall out)",
                "#FileDescriptor.⊖  →  close.in_0.⊖",
                "F.in_0.⊕           →  F.⊖                 (forget in_0)",
                "F.in_0.⊖           →  F.⊕                 (forget in_0)",
                "F.in_0.⊕           →  δ.⊕",
                "F.out.⊖            →  close.out.⊖",
                "F.out.⊕            →  F.⊕                 (forget out)",
                "F.out.⊖            →  F.⊖                 (forget out)",
                "#SuccessZ.⊕        →  close.out.⊕",
                "α'.⊕               →  close.in_0.⊕",
                "α.⊕                →  φ.⊕",
                "α.⊖                →  φ.load.σ4@0.⊖",
                "α'.⊖               →  φ.load.σ4@4.⊖",
                "δ.⊖                →  F.in_0.⊖",
                "δ.⊕                →  φ.⊕",
                "φ.load.σ4@0.⊕      →  α.⊕",
                "φ.load.σ4@0.⊕      →  φ.load.⊕            (forget σ4@0)",
                "φ.load.σ4@0.⊖      →  φ.load.⊖            (forget σ4@0)",
                "φ.load.σ4@4.⊕      →  α'.⊕",
                "φ.load.σ4@4.⊕      →  φ.load.⊕            (forget σ4@4)",
                "φ.load.σ4@4.⊖      →  φ.load.⊖            (forget σ4@4)",
                "φ.load.⊖           →  φ.⊖                 (forget load)",
                "φ.load.⊕           →  φ.⊕                 (forget load)",
                "φ.load.⊖           →  φ.load.σ4@0.⊖       (recall σ4@0)",
                "φ.load.⊕           →  φ.load.σ4@0.⊕       (recall σ4@0)",
                "φ.load.⊖           →  φ.load.σ4@4.⊖       (recall σ4@4)",
                "φ.load.⊕           →  φ.load.σ4@4.⊕       (recall σ4@4)",
                "φ.⊖                →  α.⊖",
                "φ.⊖                →  δ.⊖",
                "φ.⊕                →  φ.load.⊕            (recall load)",
                "φ.⊖                →  φ.load.⊖            (recall load)"
            };
            this.graphs_are_equal(solver.constraint_graph.graph, SchemaTest.edges_to_dict(forget_recall));
            solver._saturate();
            var saturated = new List<string> {
                "close.⊖            →  close.in_0.⊕        (recall in_0)",
                "close.⊕            →  close.in_0.⊖        (recall in_0)",
                "close.⊖            →  close.out.⊖         (recall out)",
                "close.⊕            →  close.out.⊕         (recall out)",
                "close.in_0.⊕       →  close.⊖             (forget in_0)",
                "close.in_0.⊖       →  close.⊕             (forget in_0)",
                "close.in_0.⊕       →  close.in_0.⊕",
                "close.in_0.⊖       →  close.in_0.⊖",
                "close.in_0.⊕       →  #FileDescriptor.⊕",
                "close.in_0.⊖       →  α'.⊖",
                "close.out.⊕        →  close.⊕             (forget out)",
                "close.out.⊖        →  close.⊖             (forget out)",
                "close.out.⊕        →  close.out.⊕",
                "close.out.⊖        →  close.out.⊖",
                "close.out.⊕        →  F.out.⊕",
                "close.out.⊖        →  #SuccessZ.⊖",
                "F.⊕                →  F.in_0.⊖            (recall in_0)",
                "F.⊖                →  F.in_0.⊕            (recall in_0)",
                "F.⊕                →  F.out.⊕             (recall out)",
                "F.⊖                →  F.out.⊖             (recall out)",
                "#FileDescriptor.⊖  →  close.in_0.⊖",
                "F.in_0.⊕           →  F.⊖                 (forget in_0)",
                "F.in_0.⊖           →  F.⊕                 (forget in_0)",
                "F.in_0.⊕           →  F.in_0.⊕",
                "F.in_0.⊖           →  F.in_0.⊖",
                "F.in_0.⊕           →  δ.⊕",
                "F.out.⊖            →  close.out.⊖",
                "F.out.⊕            →  F.⊕                 (forget out)",
                "F.out.⊖            →  F.⊖                 (forget out)",
                "F.out.⊕            →  F.out.⊕",
                "F.out.⊖            →  F.out.⊖",
                "#SuccessZ.⊕        →  close.out.⊕",
                "α'.⊕               →  close.in_0.⊕",
                "α.⊕                →  φ.⊕",
                "α.⊖                →  φ.load.σ4@0.⊖",
                "α'.⊖               →  φ.load.σ4@4.⊖",
                "δ.⊖                →  F.in_0.⊖",
                "δ.⊕                →  φ.⊕",
                "φ.load.σ4@0.⊕      →  α.⊕",
                "φ.load.σ4@0.⊕      →  φ.load.⊕            (forget σ4@0)",
                "φ.load.σ4@0.⊖      →  φ.load.⊖            (forget σ4@0)",
                "φ.load.σ4@0.⊕      →  φ.load.σ4@0.⊕",
                "φ.load.σ4@0.⊖      →  φ.load.σ4@0.⊖",
                "φ.load.σ4@4.⊕      →  α'.⊕",
                "φ.load.σ4@4.⊕      →  φ.load.⊕            (forget σ4@4)",
                "φ.load.σ4@4.⊖      →  φ.load.⊖            (forget σ4@4)",
                "φ.load.σ4@4.⊕      →  φ.load.σ4@4.⊕",
                "φ.load.σ4@4.⊖      →  φ.load.σ4@4.⊖",
                "φ.load.⊕           →  φ.⊕                 (forget load)",
                "φ.load.⊖           →  φ.⊖                 (forget load)",
                "φ.load.⊕           →  φ.load.⊕",
                "φ.load.⊖           →  φ.load.⊖",
                "φ.load.⊕           →  φ.load.σ4@0.⊕       (recall σ4@0)",
                "φ.load.⊖           →  φ.load.σ4@0.⊖       (recall σ4@0)",
                "φ.load.⊕           →  φ.load.σ4@4.⊕       (recall σ4@4)",
                "φ.load.⊖           →  φ.load.σ4@4.⊖       (recall σ4@4)",
                "φ.⊖                →  α.⊖",
                "φ.⊖                →  δ.⊖",
                "φ.⊕                →  φ.load.⊕            (recall load)",
                "φ.⊖                →  φ.load.⊖            (recall load)"
            };
            this.graphs_are_equal(solver.constraint_graph.graph, SchemaTest.edges_to_dict(saturated));
            solver.graph = new networkx.DiGraph<Node>(solver.constraint_graph.graph);
            solver._remove_self_loops();
            solver._generate_type_vars();
            solver._unforgettable_subgraph_split();
            solver._generate_constraints();
            var tv = solver.lookup_type_var("φ");
            Assert.IsTrue(solver.constraints.Contains(SchemaParser.parse_constraint("#SuccessZ ⊑ F.out")));
            Assert.IsTrue(solver.constraints.Contains(SchemaParser.parse_constraint($"F.in_0 ⊑ {tv}")));
            Assert.IsTrue(solver.constraints.Contains(SchemaParser.parse_constraint($"{tv}.load.σ4@0 ⊑ {tv}")));
            Assert.IsTrue(solver.constraints.Contains(SchemaParser.parse_constraint($"{tv}.load.σ4@4 ⊑ #FileDescriptor")));
        }
        
        // Same as the preceding test, but end-to-end.
        //         
        [Test]
        public virtual void test_end_to_end() {
            var constraints = new ConstraintSet ();
            constraints.add(SchemaParser.parse_constraint("F.in_0 ⊑ δ"));
            constraints.add(SchemaParser.parse_constraint("α ⊑ φ"));
            constraints.add(SchemaParser.parse_constraint("δ ⊑ φ"));
            constraints.add(SchemaParser.parse_constraint("φ.load.σ4@0 ⊑ α"));
            constraints.add(SchemaParser.parse_constraint("φ.load.σ4@4 ⊑ α'"));
            constraints.add(SchemaParser.parse_constraint("α' ⊑ close.in_0"));
            constraints.add(SchemaParser.parse_constraint("close.out ⊑ F.out"));
            constraints.add(SchemaParser.parse_constraint("close.in_0 ⊑ #FileDescriptor"));
            constraints.add(SchemaParser.parse_constraint("#SuccessZ ⊑ close.out"));
            var solver = new Solver(constraints, new HashSet<string>{
                "F",
                "#FileDescriptor",
                "#SuccessZ" });
            solver.__call__();
            var tv = solver.lookup_type_var("φ");
            Assert.IsTrue(solver.constraints.Contains(SchemaParser.parse_constraint("#SuccessZ ⊑ F.out")));
            Assert.IsTrue(solver.constraints.Contains(SchemaParser.parse_constraint("F.in_0 ⊑ {tv}")));
            Assert.IsTrue(solver.constraints.Contains(SchemaParser.parse_constraint("{tv}.load.σ4@0 ⊑ {tv}")));
            Assert.IsTrue(solver.constraints.Contains(SchemaParser.parse_constraint("{tv}.load.σ4@4 ⊑ #FileDescriptor")));
        }
    }
}
