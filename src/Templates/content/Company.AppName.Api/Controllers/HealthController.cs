namespace Company.AppName.Api.Controllers;

/// <summary>
/// Health Controller
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthService _health;

    public HealthController(HealthService health)
    {
        _health = health;
    }

    /// <summary>
    /// Health Endpoint
    /// </summary>
    [HttpGet()]
    [Route("/health")]
    public async Task<IActionResult> Index() => await _health.RunAsync().ConfigureAwait(false);
}
