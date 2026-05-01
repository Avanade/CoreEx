namespace CoreEx.AspNetCore.Mvc;

/// <summary>
/// Provides the attribute to indicate that the decorated operation is idempotent and should be handled accordingly.
/// </summary>
/// <param name="isRequired">Indicates whether an <see cref="HttpNames.IdempotencyKeyHeaderName"/> is required for the request.</param>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public partial class IdempotencyKeyAttribute(bool isRequired = false) : Attribute
{
    /// <summary>
    /// Indicates whether an <see cref="HttpNames.IdempotencyKeyHeaderName"/> is required for the request.
    /// </summary>
    public bool IsRequired { get; } = isRequired;
}