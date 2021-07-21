
using ConstraintSet = schema.ConstraintSet;

using DerefLabel = schema.DerefLabel;

using DerivedTypeVariable = schema.DerivedTypeVariable;

using InLabel = schema.InLabel;

using LoadLabel = schema.LoadLabel;

using Node = schema.Node;

using OutLabel = schema.OutLabel;

using StoreLabel = schema.StoreLabel;

using EdgeLabel = schema.EdgeLabel;

using SubtypeConstraint = schema.SubtypeConstraint;

using Variance = schema.Variance;

using Solver = solver.Solver;

using SchemaParser = parser.SchemaParser;

public static class @__init__ {
    
    static @__init__() {
        /*An implementation of retypd based on the paper and slides included in the reference subdirectory.

To invoke, populate a ConstraintSet. Then, instantiate a Solver with the ConstraintSet and a
collection of ""interesting"" variables, such as functions and globals, specified either as strings or
DerivedTypeVariable objects. Then, invoke the solver.

After computation has finished, the constraints are available in the solver object's constraints
attribute.
*/
    }
}
