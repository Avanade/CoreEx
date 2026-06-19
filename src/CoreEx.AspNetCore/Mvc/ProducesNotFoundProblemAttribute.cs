using Microsoft.AspNetCore.Mvc;

namespace CoreEx.AspNetCore.Mvc;

/// <summary>
/// An attribute that specifies that the action/operation may return a <see cref="HttpStatusCode.NotFound"/> <see cref="ProblemDetails"/> response. 
/// </summary>
/// <remarks>This is shorthand for specifying: <c>[ProducesResponseType(typeof(ProblemDetails), 200, MediaTypeNames.Application.ProblemJson)]</c>.</remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ProducesNotFoundProblemAttribute : Attribute { }