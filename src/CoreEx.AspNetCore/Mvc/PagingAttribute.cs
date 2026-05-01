namespace CoreEx.AspNetCore.Mvc;

/// <summary>
/// An attribute that specifies that the action/operation supports <see cref="PagingArgs"/> (not explicitly defined as a parameter).
/// </summary>
/// <remarks>The is used to enable <i>OpenAPI</i> generated documentation where the operation does not explicitly define the <see cref="PagingArgs"/> as a method parameter; i.e. via <see cref="Microsoft.AspNetCore.Mvc.FromQueryAttribute"/>.</remarks>
/// <param name="supportsCount">Indicates whether <see cref="PagingArgs.IsCountRequested"/> is supported/enabled.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class PagingAttribute(bool supportsCount = false) : Attribute
{
    /// <summary>
    /// Indicates whether the <see cref="PagingArgs.IsCountRequested"/> is supported.
    /// </summary>
    public bool SupportsCount { get; } = supportsCount;
}