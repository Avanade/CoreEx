using Microsoft.AspNetCore.Mvc;

namespace My.Hr.Api.Controllers;

/// <summary>
/// Swagger/OpenAPI documentation for the API.
/// </summary>
[ApiController]
[Route("[controller]")]
public class SwaggerController : ControllerBase
{
    /// <summary>
    /// Swagger/OpenAPI documentation for the API.
    /// </summary>
    [HttpGet()]
    [Route("/")]
    public IActionResult Index()
    {
        return new RedirectResult("~/swagger");
    }
}
