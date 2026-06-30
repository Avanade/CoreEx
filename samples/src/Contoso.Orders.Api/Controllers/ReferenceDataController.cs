namespace Contoso.Orders.Api.Controllers;

[ApiController, Route("/api/refdata")]
public class ReferenceDataController(WebApi webApi) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();

    [HttpGet("order-statuses"), HttpHead("order-statuses")]
    [ProducesResponseType(typeof(OrderStatus[]), StatusCodes.Status200OK)]
    public Task<IActionResult> GetOrderStatusesAsync([FromQuery] IEnumerable<string>? codes = default, string? text = default, CancellationToken cancellationToken = default)
        => _webApi.GetAsync(Request, (ro, ct) => ReferenceDataOrchestrator.Current.GetWithFilterAsync<OrderStatus>(codes, text, ro.IsIncludeInactive, ct), cancellationToken: cancellationToken);
}