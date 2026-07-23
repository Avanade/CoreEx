namespace CoreEx.Data.GraphQL.Test.Unit.Model;

/// <summary>
/// A simple nested-shape test DTO used to prove selection-set projection over an already-materialized object graph (address.street/city).
/// </summary>
public class Person
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
