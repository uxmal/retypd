
using re;

using AccessPathLabel = schema.AccessPathLabel;

using DerefLabel = schema.DerefLabel;

using DerivedTypeVariable = schema.DerivedTypeVariable;

using EdgeLabel = schema.EdgeLabel;

using InLabel = schema.InLabel;

using LoadLabel = schema.LoadLabel;

using OutLabel = schema.OutLabel;

using StoreLabel = schema.StoreLabel;

using SubtypeConstraint = schema.SubtypeConstraint;

using Variance = schema.Variance;

using Node = schema.Node;

using Dict = typing.Dict;

using Tuple = typing.Tuple;

using System;

using System.Linq;

using System.Collections.Generic;

public static class parser {
    
    static parser() {
        @"Parsing helpers, mostly for unit testing.
";
    }
    
    // Static helper functions for Schema tests. Since this parsing code is unlikely to be useful in
    //     the code itself, it is included here.
    //     
    public class SchemaParser {
        
        public object deref_pattern;
        
        public object edge_pattern;
        
        public object in_pattern;
        
        public object node_pattern;
        
        public object subtype_pattern;
        
        public object whitespace_pattern;
        
        public object subtype_pattern = re.compile(@"(\S*) (?:⊑|<=) (\S*)");
        
        public object in_pattern = re.compile("in_([0-9]+)");
        
        public object deref_pattern = re.compile("σ([0-9]+)@([0-9]+)");
        
        public object node_pattern = re.compile(@"(\S+)\.([⊕⊖])");
        
        public object edge_pattern = re.compile(@"(\S+)\s+(?:→|->)\s+(\S+)(\s+\((forget|recall) (\S*)\))?");
        
        public object whitespace_pattern = re.compile(@"\s");
        
        // Parse an AccessPathLabel. Raises ValueError if it is improperly formatted.
        //         
        [staticmethod]
        public static void parse_label(object label = str) {
            if (label == "load") {
                return LoadLabel.instance();
            }
            if (label == "store") {
                return StoreLabel.instance();
            }
            if (label == "out") {
                return OutLabel.instance();
            }
            var in_match = SchemaParser.in_pattern.match(label);
            if (in_match) {
                return InLabel(Convert.ToInt32(in_match.group(1)));
            }
            var deref_match = SchemaParser.deref_pattern.match(label);
            if (deref_match) {
                return DerefLabel(Convert.ToInt32(deref_match.group(1)), Convert.ToInt32(deref_match.group(2)));
            }
            throw new ValueError();
        }
        
        // Parse a DerivedTypeVariable. Raises ValueError if the string contains whitespace.
        //         
        [staticmethod]
        public static void parse_variable(object var = str) {
            if (SchemaParser.whitespace_pattern.match(var)) {
                throw new ValueError();
            }
            var components = var.split(".");
            var path = (from label in components[1]
                select SchemaParser.parse_label(label)).ToList();
            return DerivedTypeVariable(components[0], path);
        }
        
        // Parse a SubtypeConstraint. Raises a ValueError if constraint does not match
        //         SchemaParser.subtype_pattern.
        //         
        [staticmethod]
        public static void parse_constraint(object constraint = str) {
            var subtype_match = SchemaParser.subtype_pattern.match(constraint);
            if (subtype_match) {
                return SubtypeConstraint(SchemaParser.parse_variable(subtype_match.group(1)), SchemaParser.parse_variable(subtype_match.group(2)));
            }
            throw new ValueError();
        }
        
        // Parse a Node. Raise a ValueError if it does not match SchemaParser.node_pattern.
        //         
        [staticmethod]
        public static void parse_node(object node = str) {
            object variance;
            var node_match = SchemaParser.node_pattern.match(node);
            if (node_match) {
                var var = SchemaParser.parse_variable(node_match.group(1));
                if (node_match.group(2) == "⊕") {
                    variance = Variance.COVARIANT;
                } else if (node_match.group(2) == "⊖") {
                    variance = Variance.CONTRAVARIANT;
                } else {
                    throw new ValueError();
                }
                return Node(var, variance);
            }
            throw new ValueError();
        }
        
        // Parse an edge in the graph, which consists of two nodes and an arrow, with an optional
        //         edge label.
        //         
        [staticmethod]
        public static Tuple<object, object, Dictionary<object, object>> parse_edge(object edge = str) {
            object kind;
            var edge_match = SchemaParser.edge_pattern.match(edge);
            if (edge_match) {
                var sub = SchemaParser.parse_node(edge_match.group(1));
                var sup = SchemaParser.parse_node(edge_match.group(2));
                var atts = new Dictionary<object, object> {
                };
                if (edge_match.group(3)) {
                    var capability = SchemaParser.parse_label(edge_match.group(5));
                    if (edge_match.group(4) == "forget") {
                        kind = EdgeLabel.Kind.FORGET;
                    } else if (edge_match.group(4) == "recall") {
                        kind = EdgeLabel.Kind.RECALL;
                    } else {
                        throw new ValueError();
                    }
                    atts["label"] = EdgeLabel(capability, kind);
                }
                return (sub, sup, atts);
            }
            throw new ValueError();
        }
    }
}
