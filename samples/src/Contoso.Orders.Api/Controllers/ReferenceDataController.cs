namespace Contoso.Orders.Api.Controllers;

[ApiController, Route("/api/refdata")]
public class ReferenceDataController(WebApi webApi) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();

    [HttpGet("order-statuses"), HttpHead("order-statuses")]
    [ProducesResponseType(typeof(OrderStatus[]), StatusCodes.Status200OK)]
    public Task<IActionResult> GetOrderStatusesAsync(CancellationToken cancellationToken = default)
        => _webApi.GetAsync(Request, (ro, ct) => ReferenceDataOrchestrator.Current.QueryAsync<OrderStatus>(ro.QueryArgs, ro.PagingArgs, ct), cancellationToken: cancellationToken);
}
