using CoreEx.Data.GraphQL.Internal;
using CoreEx.Entities;

namespace CoreEx.Data.GraphQL.Test.Unit.Model;

/// <summary>
/// A chain of distinct (non-self-referential) nested DTOs, <c>Depth0</c> through <c>Depth8</c>, used to prove <see cref="GraphQLTypeShape.MaxDepth"/> is enforced identically at runtime
/// (<see cref="GraphQLTypeShape.GetFieldMap"/>) and in the generated introspection schema (<see cref="GraphQLIntrospectionSchemaBuilder"/>).
/// </summary>
/// <remarks>Distinct types are used (rather than a single self-referencing type) because <see cref="GraphQLIntrospectionSchemaBuilder"/>'s type registry short-circuits recursion the
/// <i>first</i> time a given CLR type name is encountered (its cycle guard) - a self-referencing type would only ever be visited once, at depth 0, and would never actually exercise the
/// depth cap. <c>Depth7</c>'s <c>Next</c> property is the last one built with a populated <c>fields</c> list (depth 7 &lt; <see cref="GraphQLTypeShape.MaxDepth"/> of 8); <c>Depth8</c> is
/// registered as an empty-<c>fields</c> stub because depth 8 is no longer <c>&lt; MaxDepth</c>. <c>Depth0</c> implements <see cref="IReadOnlyIdentifier{TId}"/> to satisfy
/// <see cref="GraphQLLiteOptions.AddGet{TItem}"/>'s identifier constraint, since it is the root type registered directly via <c>AddGet</c> in the introspection depth-cap test.</remarks>
public class Depth0 : IReadOnlyIdentifier<int> { public int Id { get; set; } public Depth1? Next { get; set; } }
public class Depth1 { public int Id { get; set; } public Depth2? Next { get; set; } }
public class Depth2 { public int Id { get; set; } public Depth3? Next { get; set; } }
public class Depth3 { public int Id { get; set; } public Depth4? Next { get; set; } }
public class Depth4 { public int Id { get; set; } public Depth5? Next { get; set; } }
public class Depth5 { public int Id { get; set; } public Depth6? Next { get; set; } }
public class Depth6 { public int Id { get; set; } public Depth7? Next { get; set; } }
public class Depth7 { public int Id { get; set; } public Depth8? Next { get; set; } }
public class Depth8 { public int Id { get; set; } public Depth9? Next { get; set; } }
public class Depth9 { public int Id { get; set; } }
