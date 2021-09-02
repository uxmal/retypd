using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class networkx
{
    public class DiGraph<TItem> where TItem : notnull
    {
        internal Dictionary<TItem, NodeInfo> _nodes;
        internal Dictionary<(TItem, TItem), Dictionary<string, object>> _edges;

        internal record NodeInfo(
            TItem Item, 
            List<TItem> Pred, 
            List<TItem> Succ,
            Dictionary<string, object> Data)
        {
            public NodeInfo(TItem Item) : this(Item, new List<TItem>(), new List<TItem>(), new()) { }
        }


        //Adding and removing nodes and edges
        //Initialize a graph with edges, name, or graph attributes.
        public DiGraph()
        {
            this._nodes = new Dictionary<TItem, NodeInfo>();
            this._edges = new();
            this.graph = new Dictionary<string, object>();
        }
        
        public DiGraph(IEnumerable<(TItem, TItem)> edges) => throw new NotImplementedException();

        public DiGraph(DiGraph<TItem> graph) => throw new NotImplementedException();

        public Dictionary<string, object> graph { get; set; }


        // Add a single node node_for_adding and update node attributes.

        public void add_node(TItem item, Dictionary<string, object>? attr = null) => throw new NotImplementedException();

        //Add multiple nodes.

        public void add_nodes_from(IEnumerable<TItem> nodes_for_adding, Dictionary<string,object>? attr = null) => throw new NotImplementedException();

        //Remove node n.
        public void remove_node(TItem n) => throw new NotImplementedException();


        //Remove multiple nodes.
        public void remove_nodes_from(IEnumerable<TItem> nodes) => throw new NotImplementedException();


        //Add an edge between u and v.

        public void add_edge(TItem u_of_edge, TItem v_of_edge, Dictionary<string, object>? attr = null)
        {
            if (!_nodes.ContainsKey(u_of_edge))
                _nodes.Add(u_of_edge, new NodeInfo(u_of_edge));
            if (!_nodes.ContainsKey(v_of_edge))
                _nodes.Add(v_of_edge, new NodeInfo(v_of_edge));
            var e = (u_of_edge, v_of_edge);
            this._edges.Add(e, attr!);
        }


        //Add all the edges in ebunch_to_add.
        public void add_edges_from(IEnumerable<(TItem, TItem)> ebunch_to_add, Dictionary<string, object>? attr = null) => throw new NotImplementedException();



        // Add weighted edges in ebunch_to_add with specified weight attr
        public void add_weighted_edges_from(IEnumerable<(TItem, TItem, double)> ebunch_to_add, string weight = "weight",
            Dictionary<string, object>? attr = null)
        => throw new NotImplementedException();


        // Remove the edge between u and v.
        public void remove_edge(TItem u, TItem v) => throw new NotImplementedException();


        //Remove all edges specified in ebunch.
        public void remove_edges_from(IEnumerable<(TItem, TItem)> ebunch) => throw new NotImplementedException();



        //Update the graph using nodes/edges/graphs as input.
        public void update(IEnumerable<(TItem, TItem)>? edges = null, IEnumerable<TItem>? nodes = null) => throw new NotImplementedException();


        // Remove all nodes and edges from the graph.

        public void clear() => throw new NotImplementedException();


        //Remove all edges from the graph without altering nodes.
        public void clear_edges() => throw new NotImplementedException();



        //Reporting nodes edges and neighbors

        // A NodeView of the Graph as G.nodes or G.nodes().
        public NodeView<TItem> nodes => new NodeView<TItem>(this._nodes);

        //Iterate over the nodes.
        public IEnumerator<TItem> GetEnumerator() => throw new NotImplementedException();

        //Returns True if the graph contains the node n.
        public bool has_node(TItem n) => throw new NotImplementedException();


        //Returns True if n is a node, False otherwise.
        public bool Contains(TItem n)
        {
            return this._nodes.ContainsKey(n);
        }


        //An OutEdgeView of the DiGraph as G.edges or G.edges().
        public OutEdgeView<TItem> edges => new OutEdgeView<TItem>(this._edges);


        //An OutEdgeView of the DiGraph as G.edges or G.edges().
        public OutEdgeView<TItem> out_edges => throw new NotImplementedException();


        //An InEdgeView of the Graph as G.in_edges or G.in_edges().
        public InEdgeView<TItem> in_edges => throw new NotImplementedException();

        //Returns True if the edge (u, v) is in the graph.
        public bool has_edge(TItem u, TItem v) => throw new NotImplementedException();



        //Returns the attribute dictionary associated with edge (u, v).
        Dictionary<string, object>? get_edge_data(TItem u, TItem v, Dictionary<string, object>? def = null) => throw new NotImplementedException();


        //Returns an iterator over successor nodes of n.
        public IEnumerable<TItem> neighbors(TItem n) => throw new NotImplementedException();



        //Graph adjacency object holding the neighbors of each node.
        public AdjacencyView<TItem> adj => throw new NotImplementedException();


        //Returns a dict of neighbors of node n.
        public AdjacencyView<TItem> this[TItem node] => new AdjacencyView<TItem>(this, node);


        //Returns an iterator over successor nodes of n.
        public IEnumerable<TItem> successors(TItem n) => throw new NotImplementedException();

        //Graph adjacency object holding the successors of each node.
        public AdjacencyView<TItem> succ => throw new NotImplementedException();


        //Returns an iterator over predecessor nodes of n.
        public IEnumerable<TItem> predecessors(TItem n) => throw new NotImplementedException();

        //Graph adjacency object holding the predecessors of each node.
        public AdjacencyView<TItem> pred => throw new NotImplementedException();


        //Returns an iterator over (node, adjacency dict) tuples for all nodes.
        public IEnumerable<(TItem, AdjacencyView<TItem>)> adjacency() => throw new NotImplementedException();

        //Returns an iterator over nodes contained in nbunch that are also in the graph.
        public IEnumerable<TItem> nbunch_iter(IEnumerable<TItem> nbunch) => throw new NotImplementedException();



        //Counting nodes edges and neighbors

        //Returns the number of nodes in the graph.
        public int order() => throw new NotImplementedException();


        //Returns the number of nodes in the graph.
        public int number_of_nodes() => throw new NotImplementedException();

        //Returns the number of nodes in the graph.
        public int Count => throw new NotImplementedException();


        // A DegreeView for the Graph as G.degree or G.degree().
        public DegreeView<TItem> degree => throw new NotImplementedException();

        //An InDegreeView for (node, in_degree) or in_degree for single node.
        public InDegreeView<TItem> in_degree => throw new NotImplementedException();


        //An OutDegreeView for (node, out_degree)
        public OutDegreeView<TItem> out_degree => throw new NotImplementedException();


        //Returns the number of edges or total of all edge weights.
        public int size() => throw new NotImplementedException();

        public double size(string weight) => throw new NotImplementedException();

        //Returns the number of edges between two nodes.

        public int number_of_edges(TItem u, TItem v) => throw new NotImplementedException();



        //Making copies and subgraphs

        //Returns a copy of the graph.
        public DiGraph<TItem> copy() => throw new NotImplementedException();

        //Returns an undirected representation of the digraph.

        //public Graph<TItem> to_undirected([reciprocal, as_view])


        //Returns a directed representation of the graph.

        //DiGraph.to_directed([as_view])



        //Returns a SubGraph view of the subgraph induced on nodes.
        public DiGraph<TItem> subgraph(IEnumerable<TItem> nodes) => throw new NotImplementedException();


        //Returns the subgraph induced by the specified edges.
        public DiGraph<TItem> edge_subgraph(IEnumerable<TItem> edges) => throw new NotImplementedException();


        //Returns the reverse of the graph.

        public DiGraph<TItem> reverse() => throw new NotImplementedException();
    }

    public class AdjacencyView<TItem> : IEnumerable<TItem>
        where TItem : notnull
    {
        private readonly DiGraph<TItem> graph;
        private readonly TItem item;
        private readonly List<(TItem from, TItem to)> edges;

        public AdjacencyView(DiGraph<TItem> g, TItem item)
        {
            this.graph = g;
            this.item = item;
            this.edges = g.edges.Where(e => e.From.Equals(item)).ToList();
        }

        public IEnumerator<TItem> GetEnumerator() => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public AdjacencyView<TItem> copy() => throw new NotImplementedException();

        public Dictionary<string, object> this[TItem node] => this.graph._edges[(item, node)];

        public TItem get(TItem key) => throw new NotImplementedException();

        public IEnumerable<KeyValuePair<TItem, Dictionary<string, object>>> items() => throw new NotImplementedException();

        public IEnumerable<TItem> keys() => throw new NotImplementedException();

        public IEnumerable<Dictionary<string, object>> values() => throw new NotImplementedException();

        public bool Contains(TItem n)
        {
            return edges.Any(e => e.to.Equals(n));
        }
    }

    public class DegreeView<TItem>
    {
    }

    public class InDegreeView<TItem>
    {
    }

    public class InEdgeView<TItem>
    {
    }

    public class NodeView<TItem> : IEnumerable<TItem> 
        where TItem : notnull
    {
        private Dictionary<TItem, DiGraph<TItem>.NodeInfo> nodes;

        internal NodeView(Dictionary<TItem, DiGraph<TItem>.NodeInfo> nodes)
        {
            this.nodes = nodes;
        }

        public IEnumerator<TItem> GetEnumerator() => nodes.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Dictionary<string, object> this[TItem item]
        {
            get
            {
                return nodes[item].Data;
            }
        }
    }

    public class OutDegreeView<TItem>
    {
    }

    public class OutEdgeView<TItem> : IEnumerable<(TItem From, TItem To)>
    {
        private Dictionary<(TItem, TItem), Dictionary<string, object>> edges;

        public OutEdgeView(Dictionary<(TItem, TItem), Dictionary<string, object>> edges)
        {
            this.edges = edges;
        }

        public IEnumerator<(TItem From, TItem To)> GetEnumerator()
        {
            return edges.Keys.Select(e => (e.Item1, e.Item2)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static IEnumerable<HashSet<TItem>> strongly_connected_components<TItem>(DiGraph<TItem> G)
        where TItem : notnull
    {
        var preorder = new Dictionary<TItem, int>();
        var lowlink = new Dictionary<TItem, int>();
        var scc_found = new HashSet<TItem>();
        var scc_queue = new List<TItem>();
        var i = 0;  // Preorder counter
        foreach (var source in G) {
            if (!scc_found.Contains(source)) {
                var queue = new List<TItem> { source };
                while (queue.Count > 0)
                {
                    var v = queue[^1];
                    if (!preorder.ContainsKey(v))
                    {
                        preorder[v] = ++i;
                    }
                    var done = true;
                    foreach (var w in G[v])
                    {
                        if (!preorder.ContainsKey(w))
                        {
                            queue.Add(w);
                            done = false;
                            break;
                        }
                    }
                    if (done)
                        lowlink[v] = preorder[v];
                    foreach (var w in G[v])
                    {
                        if (!scc_found.Contains(w))
                        {
                            if (preorder[w] > preorder[v])
                                lowlink[v] = Math.Min(lowlink[v], lowlink[w]);
                            else
                                lowlink[v] = Math.Min(lowlink[v], preorder[w]);
                        }
                    }
                    queue.RemoveAt(queue.Count - 1);
                    if (lowlink[v] == preorder[v])
                    {
                        var scc = new HashSet<TItem> { v };
                        while (scc_queue.Count > 0 && preorder[scc_queue[^1]] > preorder[v])
                        {
                            var k = scc_queue[^1];
                            scc_queue.RemoveAt(scc_queue.Count - 1);
                            scc.Add(k);
                        }
                        scc_found.UnionWith(scc);
                        yield return scc;
                    }
                    else
                    {
                        scc_queue.Add(v);
                    }
                }
            }
        }
    }
    /*
     * arameters
GNetworkX DiGraph
A directed graph.

scc: list or generator (optional, default=None)
Strongly connected components. If provided, the elements in scc
    must partition the nodes in G. If not provided, it will be calculated
    as scc=nx.strongly_connected_components(G).

Returns
CNetworkX DiGraph
The condensation graph C of G. The node labels are integers corresponding to
    the index of the component in the list of strongly connected components of
    G. C has a graph attribute named ‘mapping’ with a dictionary mapping the
    original nodes to the nodes in C to which they belong.
    Each node in C also has a node attribute ‘members’ with the set of 
    original nodes in G that form SCC that the node in C represents. */
    public static DiGraph<int> condensation<TItem>(DiGraph<TItem> G)
        where TItem : notnull
    {
        var scc = strongly_connected_components(G);
        var mapping = new Dictionary<TItem, int>();
        var members = new Dictionary<int, HashSet<TItem>>();
        var C = new DiGraph<int>();
        // Add mapping dict as graph attribute
        C.graph["mapping"] = mapping;
        if (G.Count == 0)
            return C;
        int i = -1;
        foreach (var component in scc) {
            ++i;
            members[i] = component;
            foreach (var n in component)
                mapping[n] = i;
        }
        var number_of_components = i + 1;
        C.add_nodes_from(Enumerable.Range(0, number_of_components));
        C.add_edges_from(
            from edge in G.edges
            let u = mapping[edge.Item1]
            let v = mapping[edge.Item2]
            where u != v
            select (u, v));
     
        // Add a list of members (ie original nodes) to each node (ie scc) in C.
        networkx.set_node_attributes(C, members, "members");
        return C;
    }

    private static void set_node_attributes<T, V>(DiGraph<T> c, Dictionary<T, V> members, string attrName) where T : notnull
    {
        foreach (var n in members)
        {
            c.nodes[n.Key][attrName] = n.Value!;
        }
    }

    public static DiGraph<TItem> reversed<TItem>(DiGraph<TItem> graph) where TItem : notnull
    {
        throw new NotImplementedException();
    }

    internal static List<TItem> topological_sort<TItem>(DiGraph<TItem> g) where TItem : notnull
    {
        throw new NotImplementedException();
    }
}
