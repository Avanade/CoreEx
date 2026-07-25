using CoreEx.Entities;

namespace CoreEx.Data.GraphQL.Test.Unit.Model;

/// <summary>
/// A simple nested-shape test DTO used to prove selection-set projection over an already-materialized object graph (address.street/city).
/// </summary>
/// <remarks>Implements <see cref="IReadOnlyIdentifier{TId}"/> so that introspection tests can prove the <c>id: ID!</c> argument is advertised for an <c>AddGet</c> root bound to an
/// identifiable item type.</remarks>
public class Person : IReadOnlyIdentifier<int>
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int Age { get; set; }

    public Address? Address { get; set; }
}

public class Address
{
    public string? Street { get; set; }

    public string? City { get; set; }
}
