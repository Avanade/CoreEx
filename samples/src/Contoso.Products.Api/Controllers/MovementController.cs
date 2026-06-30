namespace Contoso.Products.Api.Controllers;

[ApiController, Route("/api/inventory"), OpenApiTag("Inventory")]
public class MovementController(WebApi webApi, IMovementService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IMovementService _service = service.ThrowIfNull();

    [HttpPost("reserve")]
    [Accepts<MovementRequest>]
    [ProducesResponseType<Movement[]>(200)]
    [IdempotencyKey]
    public Task<IActionResult> ReserveAsync(CancellationToken cancellationToken = default) => _webApi.PostAsync<MovementRequest, List<Movement>>(Request, (ro, ct)
        => _service.CreateReservationAsync(ro.Value, ct), HttpStatusCode.OK, cancellationToken: cancellationToken);

    [HttpPost("reservation/{referenceId}/confirm")]
    [ProducesResponseType<Movement[]>(200)]
    public Task<IActionResult> ConfirmReservationAsync(string referenceId, CancellationToken cancellationToken = default) => _webApi.PostAsync<List<Movement>>(Request, (ro, ct)
        => _service.ConfirmReservationAsync(referenceId.Required(), ct), HttpStatusCode.OK, cancellationToken: cancellationToken);

    [HttpPost("reservation/{referenceId}/cancel")]
    [ProducesResponseType<Movement[]>(200)]
    public Task<IActionResult> CancelReservationAsync(string referenceId, CancellationToken cancellationToken = default) => _webApi.PostAsync<List<Movement>>(Request, (ro, ct)
        => _service.CancelReservationAsync(referenceId.Required(), ct), HttpStatusCode.OK, cancellationToken: cancellationToken);

    [HttpPost("adjust")]
    [Accepts<MovementRequest>]
    [ProducesResponseType<Movement[]>(200)]
    [IdempotencyKey]
    public Task<IActionResult> AdjustAsync(CancellationToken cancellationToken = default) => _webApi.PostAsync<MovementRequest, List<Movement>>(Request, (ro, ct)
        => _service.AdjustAsync(ro.Value, ct), HttpStatusCode.OK, cancellationToken: cancellationToken);
}