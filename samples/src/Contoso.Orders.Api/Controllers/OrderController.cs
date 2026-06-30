namespace Contoso.Orders.Api.Controllers;

using Contoso.Order.Workflow.Client;
using Contoso.Order.Workflow.Workflow.Contracts;
using OrderContract = Contoso.Orders.Contracts.Order;

[ApiController, Route("/api/orders"), OpenApiTag("Orders")]
public class OrderController(WebApi webApi, IOrderService service, IOrderWorkflowClient orderWorkflowClient) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IOrderService _service = service.ThrowIfNull();
    private readonly IOrderWorkflowClient _orderWorkflowClient = orderWorkflowClient.ThrowIfNull();

    [HttpPost]
    [Accepts<OrderContract>]
    [ProducesResponseType<OrderContract>(StatusCodes.Status201Created)]
    [IdempotencyKey]
    public Task<IActionResult> PostAsync(CancellationToken cancellationToken = default) => _webApi.PostAsync<OrderContract, OrderContract>(Request, (ro, ct) =>
    {
        ro.WithLocationUri(o => new Uri($"/api/orders/{o.Id}", UriKind.Relative));
        return _service.CreateAsync(ro.Value, ct);
    }, cancellationToken: cancellationToken);

    [HttpPut("{id}")]
    [Accepts<OrderContract>]
    [ProducesResponseType(typeof(OrderContract), StatusCodes.Status200OK)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> PutAsync(string id, CancellationToken cancellationToken = default) => _webApi.PutAsync<OrderContract, OrderContract>(Request, (ro, ct)
        => _service.UpdateAsync(ro.Value.Adjust(o => o.Id = id.Required()), ct), cancellationToken: cancellationToken);

    [HttpPatch("{id}")]
    [Accepts<OrderContract>(HttpNames.MergePatchJsonMediaTypeName)]
    [ProducesResponseType(typeof(OrderContract), StatusCodes.Status200OK)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> PatchAsync(string id, CancellationToken cancellationToken = default) => _webApi.PatchAsync<OrderContract>(Request,
        get: (ro, ct) => _service.GetAsync(id.Required(), ct),
        put: (ro, ct) => _service.UpdateAsync(ro.Value.Adjust(o => o.Id = id.Required()), ct),
        cancellationToken: cancellationToken);

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken = default) => _webApi.DeleteAsync(Request, (_, ct)
        => _service.DeleteAsync(id.Required(), ct), cancellationToken: cancellationToken);

    [HttpPost("orchestrate")]
    [Accepts<OrderWorkflowRequest>]
    [ProducesResponseType(typeof(OrchestrateOrderResponse), StatusCodes.Status202Accepted)]
    [IdempotencyKey]
    public Task<IActionResult> OrchestrateOrderAsync(CancellationToken cancellationToken = default) => _webApi.PostAsync<OrderWorkflowRequest, OrchestrateOrderResponse>(Request, async (ro, ct) =>
    {
        var instanceId = await _orderWorkflowClient.StartAsync(ro.Value, cancellationToken: ct).ConfigureAwait(false);
        return new OrchestrateOrderResponse(instanceId);
    }, HttpStatusCode.Accepted, cancellationToken: cancellationToken);
}

public sealed record OrchestrateOrderResponse(string InstanceId);