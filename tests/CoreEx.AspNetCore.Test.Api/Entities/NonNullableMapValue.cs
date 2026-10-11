namespace CoreEx.AspNetCore.Test.Api.Entities;

/// <summary>
/// Provides a recursive dictionary value for OpenAPI non-nullability regression tests.
/// </summary>
public class NonNullableMapValue
{
    /// <summary>
    /// Gets or sets the code advertised as non-nullable.
    /// </summary>
    [CoreEx.Entities.NonNullable]
    public string? RequiredCode { get; set; }

    /// <summary>
    /// Gets or sets the code advertised as nullable.
    /// </summary>
    public string? OptionalCode { get; set; }

    /// <summary>
    /// Gets or sets nested values of the same type.
    /// </summary>
    public CoreEx.Entities.DataMap<NonNullableMapValue>? Children { get; set; }
}
