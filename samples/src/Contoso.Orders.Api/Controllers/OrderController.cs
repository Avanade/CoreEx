namespace Contoso.Orders.Api.Controllers;

using Contoso.Order.Workflow.Client;
using Contoso.Order.Workflow.Workflow.Contracts;
using OrderContract = Contoso.Orders.Contracts.Order;

[ApiController, Route("/api/orders"), OpenApiTag("Orders")]
public class OrderController(WebApi webApi, IOrderService service, OrderWorkflowClient orderWorkflowClient) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IOrderService _service = service.ThrowIfNull();
    private readonly OrderWorkflowClient _orderWorkflowClient = orderWorkflowClient.ThrowIfNull();

    [HttpPost]
    [Accepts<OrderContract>]
    [ProducesResponseType<OrderContract>(StatusCodes.Status201Created)]
    [IdempotencyKey]
    public Task<IActionResult> PostAsync() => _webApi.PostAsync<OrderContract, OrderContract>(Request, (ro, _) =>
    {
        ro.WithLocationUri(o => new Uri($"/api/orders/{o.Id}", UriKind.Relative));
        return _service.CreateAsync(ro.Value);
    });

    [HttpPut("{id}")]
    [Accepts<OrderContract>]
    [ProducesResponseType(typeof(OrderContract), StatusCodes.Status200OK)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> PutAsync(string id) => _webApi.PutAsync<OrderContract, OrderContract>(Request, (ro, _)
        => _service.UpdateAsync(ro.Value.Adjust(o => o.Id = id.Required())));

    [HttpPatch("{id}")]
    [Accepts<OrderContract>(HttpNames.MergePatchJsonMediaTypeName)]
    [ProducesResponseType(typeof(OrderContract), StatusCodes.Status200OK)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> PatchAsync(string id) => _webApi.PatchAsync<OrderContract>(Request,
        get: (ro, _) => _service.GetAsync(id.Required()),
        put: (ro, _) => _service.UpdateAsync(ro.Value.Adjust(o => o.Id = id)));

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> DeleteAsync(string id) => _webApi.DeleteAsync(Request, (_, _)
        => _service.DeleteAsync(id.Required()));

    [HttpPost("orchestrate")]
    [Accepts<OrderWorkflowRequest>]
    [ProducesResponseType(typeof(OrchestrateOrderResponse), StatusCodes.Status202Accepted)]
    [IdempotencyKey]
    public Task<IActionResult> OrchestrateOrderAsync() => _webApi.PostAsync<OrderWorkflowRequest, OrchestrateOrderResponse>(Request, async (ro, ct) =>
    {
        var instanceId = await _orderWorkflowClient.StartAsync(ro.Value, cancellationToken: ct).ConfigureAwait(false);
        return new OrchestrateOrderResponse(instanceId);
    }, HttpStatusCode.Accepted);
}

public sealed record OrchestrateOrderResponse(string InstanceId);