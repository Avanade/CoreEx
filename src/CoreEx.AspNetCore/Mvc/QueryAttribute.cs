namespace CoreEx.AspNetCore.Mvc;

/// <summary>
/// An attribute that specifies that the action/operation supports <see cref="QueryArgs"/> (not explicitly defined as a parameter).
/// </summary>
/// <remarks>The is used to enable <i>OpenApi</i> generated documentation where the operation does not explicitly define the <see cref="QueryArgs"/> as a method parameter; i.e. via <see cref="Microsoft.AspNetCore.Mvc.FromQueryAttribute"/>.</remarks>
/// <param name="supportsFilter">Indicates whether <see cref="QueryArgs.Filter"/> is supported/enabled.</param>
/// <param name="supportsOrderBy">Indicates whether <see cref="QueryArgs.OrderBy"/> is supported/enabled.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class QueryAttribute(bool supportsFilter = true, bool supportsOrderBy = false) : Attribute
{
    /// <summary>
    /// Indicates whether the <see cref="QueryArgs.Filter"/> is supported.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>.</remarks>
    public bool SupportsFilter { get; } = supportsFilter;

    /// <summary>
    /// Indicates whether the <see cref="QueryArgs.OrderBy"/> is supported.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>.</remarks>
    public bool SupportsOrderBy { get; } = supportsOrderBy;
}