namespace CoreEx.AspNetCore.Mvc;

/// <summary>
/// An attribute that specifies the expected request <b>body</b> <see cref="Type"/> that the action/operation accepts and the supported request content types.
/// </summary>
/// <remarks>The is used to enable <i>OpenApi</i> generated documentation where the operation does not explicitly define the body as a method parameter; i.e. via <see cref="Microsoft.AspNetCore.Mvc.FromBodyAttribute"/>.</remarks>
/// <typeparam name="TRequest">The request <b>body</b> <see cref="Type"/>.</typeparam>
/// <param name="contentType">The primary request <b>body</b> content type.</param>
/// <param name="additionalContentTypes">The additional request <b>body</b> content type(s).</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AcceptsAttribute<TRequest>(string? contentType = MediaTypeNames.Application.Json, params string[] additionalContentTypes) : AcceptsAttribute(typeof(TRequest), contentType, additionalContentTypes) { }