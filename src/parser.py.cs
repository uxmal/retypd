/*
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
*/
using System;

using System.Linq;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using schema;

public static class parser {
    
    static parser() {
        //"Parsing helpers, mostly for unit testing.

    }
    
    // Static helper functions for Schema tests. Since this parsing code is unlikely to be useful in
    //     the code itself, it is included here.
    //     
    public class SchemaParser {
        
        public static readonly Regex subtype_pattern = new Regex(@"(\S*) (?:⊑|<=) (\S*)");
        
        public static readonly Regex in_pattern = new Regex("in_([0-9]+)");
        
        public static readonly Regex deref_pattern = new Regex("σ([0-9]+)@([0-9]+)");
        
        public static readonly Regex node_pattern = new Regex(@"(\S+)\.([⊕⊖])");
        
        public static readonly Regex edge_pattern = new Regex(@"(\S+)\s+(?:→|->)\s+(\S+)(\s+\((forget|recall) (\S*)\))?");
        
        public static readonly Regex whitespace_pattern = new Regex(@"\s");
        
        // Parse an AccessPathLabel. Raises ValueError if it is improperly formatted.
        //         
        public static AccessPathLabel parse_label(string label) {
            if (label == "load") {
                return LoadLabel.instance;
            }
            if (label == "store") {
                return StoreLabel.instance;
            }
            if (label == "out") {
                return OutLabel.instance;
            }
            var in_match = SchemaParser.in_pattern.Match(label);
            if (in_match.Success) {
                return new InLabel(Convert.ToInt32(in_match.Groups[1].Value));
            }
            var deref_match = SchemaParser.deref_pattern.Match(label);
            if (deref_match.Success) {
                return new DerefLabel(
                    Convert.ToInt32(deref_match.Groups[1].Value), 
                    Convert.ToInt32(deref_match.Groups[2].Value));
            }
            throw new FormatException();
        }
        
        // Parse a DerivedTypeVariable. Raises ValueError if the string contains whitespace.
        //         
        public static DerivedTypeVariable parse_variable(string var) {
            if (SchemaParser.whitespace_pattern.IsMatch(var)) {
                throw new FormatException();
            }
            var components = var.Split(".");
            var path = (from label in components[1..]
                select SchemaParser.parse_label(label)).ToArray();
            return new DerivedTypeVariable(components[0], path);
        }
        
        // Parse a SubtypeConstraint. Raises a ValueError if constraint does not match
        //         SchemaParser.subtype_pattern.
        //         
        public static SubtypeConstraint parse_constraint(string constraint) {
            var subtype_match = SchemaParser.subtype_pattern.Match(constraint);
            if (subtype_match.Success) {
                return new SubtypeConstraint(
                    SchemaParser.parse_variable(subtype_match.Groups[1].Value),
                    SchemaParser.parse_variable(subtype_match.Groups[2].Value));
            }
            throw new FormatException();
        }
        
        // Parse a Node. Raise a ValueError if it does not match SchemaParser.node_pattern.
        //         
        public static Node parse_node(string node) {
            Variance variance;
            var node_match = SchemaParser.node_pattern.Match(node);
            if (node_match.Success) {
                var var = SchemaParser.parse_variable(node_match.Groups[1].Value);
                if (node_match.Groups[2].Value == "⊕") {
                    variance = Variance.COVARIANT;
                } else if (node_match.Groups[2].Value == "⊖") {
                    variance = Variance.CONTRAVARIANT;
                } else {
                    throw new FormatException();
                }
                return new Node(var, variance);
            }
            throw new FormatException();
        }
        
        // Parse an edge in the graph, which consists of two nodes and an arrow, with an optional
        //         edge label.
        //         
        public static (object, object, Dictionary<string, object>) parse_edge(string edge) {
            EdgeLabel.Kind kind;
            var edge_match = SchemaParser.edge_pattern.Match(edge);
            if (edge_match.Success) {
                var sub = SchemaParser.parse_node(edge_match.Groups[1].Value);
                var sup = SchemaParser.parse_node(edge_match.Groups[2].Value);
                var atts = new Dictionary<string, object> { };
                if (edge_match.Groups[3].Success) {
                    var capability = SchemaParser.parse_label(edge_match.Groups[5].Value);
                    if (edge_match.Groups[4].Value == "forget") {
                        kind = EdgeLabel.Kind.FORGET;
                    } else if (edge_match.Groups[4].Value == "recall") {
                        kind = EdgeLabel.Kind.RECALL;
                    } else {
                        throw new FormatException();
                    }
                    atts["label"] = new EdgeLabel(capability, kind);
                }
                return (sub, sup, atts);
            }
            throw new FormatException();
        }
    }
}
