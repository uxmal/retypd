using retypd.schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace networkx
{
    public class DiGraph<TNode> where TNode : notnull
    {
        private Dictionary<TNode, List<TNode>> _preds;
        private Dictionary<TNode, List<TNode>> _succs;
        private HashSet<TNode> ns;

        public IEnumerable<(TNode, TNode)> edges()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TNode> nodes { get; internal set; }

        public DiGraph()
        {
            this.ns = new HashSet<TNode>();
            this._preds = new Dictionary<TNode, List<TNode>>();
            this._succs = new Dictionary<TNode, List<TNode>>();
        }

        public DiGraph(DiGraph<TNode> that)
        {
            this.ns = new HashSet<TNode>(that.ns);
            this._preds = new Dictionary<TNode, List<TNode>>(that._preds);
            this._succs = new Dictionary<TNode, List<TNode>>(that._succs);
        }

        public bool Contains(TNode o)
        {
            throw new NotImplementedException();
        }

        public object this[object node] { get { return null; }
            set { }
        }

        internal void add_edge(TNode head, TNode tail, Hashtable atts)
        {
            ns.Add(head);
            ns.Add(tail);
            if (!_preds.TryGetValue(tail, out var p))
            {
                p = new List<TNode>();
                _preds.Add(tail, p);
            }
            p.Add(head);
            if (!_succs.TryGetValue(head, out var s))
            {
                s = new List<TNode>();
                _succs.Add(head, p);
            }
        }

        internal void remove_edge(TNode head, TNode tail)
        {
            throw new NotImplementedException();
        }

        internal void remove_edges_from(IEnumerable<(TNode, TNode)> edges)
        {
            throw new NotImplementedException();
        }

        internal IEnumerable<TNode> predecessors(TNode node)
        {
            throw new NotImplementedException();
        }

        internal IEnumerable<TNode> successors(TNode origin)
        {
            throw new NotImplementedException();
        }
    }
}
