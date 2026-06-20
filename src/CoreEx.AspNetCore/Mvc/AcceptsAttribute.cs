namespace CoreEx.AspNetCore.Mvc;

/// <summary>
/// An attribute that specifies the expected request <b>body</b> <see cref="Type"/> that the action/operation accepts and the supported request content types.
/// </summary>
/// <remarks>The is used to enable <i>OpenApi</i> generated documentation where the operation does not explicitly define the body as a method parameter; i.e. via <see cref="Microsoft.AspNetCore.Mvc.FromBodyAttribute"/>.</remarks>
/// <param name="type">The request <b>body</b> <see cref="Type"/>; defaults to <see cref="MediaTypeNames.Application.Json"/>.</param>
/// <param name="contentType">The primary request <b>body</b> content type.</param>
/// <param name="additionalContentTypes">The additional request <b>body</b> content type(s).</param>
[AttributeUsage(AttributeTargets.Method)]
public class AcceptsAttribute(Type type, string? contentType = MediaTypeNames.Application.Json, params string[] additionalContentTypes) : Attribute
{
    /// <summary>
    /// Gets the request <b>body</b> <see cref="Type"/>.
    /// </summary>
    public Type BodyType { get; } = type.ThrowIfNull();

    /// <summary>
    /// Gets the primary request <b>body</b> content type.
    /// </summary>
    public string ContentType { get; } = contentType.ThrowIfNullOrEmpty();

    /// <summary>
    /// Gets the additional request <b>body</b> content type(s).
    /// </summary>
    public string[] AdditionalContentTypes { get; } = [.. additionalContentTypes];

    /// <summary>
    /// Indicates whether the request body is optional.
    /// </summary>
    public bool IsOptional { get; set; }
}