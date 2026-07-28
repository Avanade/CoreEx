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

/// <summary>
/// A value-type (<see langword="struct"/>) nested shape, used to prove <see cref="GraphQLTypeShape"/> correctly recurses into a <i>nullable</i> complex struct property's own
/// properties rather than <see cref="Nullable{T}"/>'s own <c>HasValue</c>/<c>Value</c> properties.
/// </summary>
public readonly record struct Money(decimal Amount, string Currency);

/// <summary>
/// A test DTO with a <see langword="Money?"/> (nullable complex struct) property.
/// </summary>
public class Invoice : IIdentifier<int>
{
    public int Id { get; set; }

    public string? Number { get; set; }

    public Money? Total { get; set; }
}
