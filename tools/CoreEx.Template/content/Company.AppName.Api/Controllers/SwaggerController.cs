namespace Company.AppName.Api.Controllers;

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
