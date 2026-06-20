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
    public Task<IActionResult> ReserveAsync() => _webApi.PostAsync<MovementRequest, List<Movement>>(Request, (ro, _)
        => _service.CreateReservationAsync(ro.Value), HttpStatusCode.OK);

    [HttpPost("reservation/{referenceId}/confirm")]
    [ProducesResponseType<Movement[]>(200)]
    public Task<IActionResult> ConfirmReservationAsync(string referenceId) => _webApi.PostAsync<List<Movement>>(Request, (ro, _)
        => _service.ConfirmReservationAsync(referenceId.Required()), HttpStatusCode.OK);

    [HttpPost("reservation/{referenceId}/cancel")]
    [ProducesResponseType<Movement[]>(200)]
    public Task<IActionResult> CancelReservationAsync(string referenceId) => _webApi.PostAsync<List<Movement>>(Request, (ro, _)
        => _service.CancelReservationAsync(referenceId.Required()), HttpStatusCode.OK);

    [HttpPost("adjust")]
    [Accepts<MovementRequest>]
    [ProducesResponseType<Movement[]>(200)]
    [IdempotencyKey]
    public Task<IActionResult> AdjustAsync() => _webApi.PostAsync<MovementRequest, List<Movement>>(Request, (ro, _)
        => _service.AdjustAsync(ro.Value), HttpStatusCode.OK);
}