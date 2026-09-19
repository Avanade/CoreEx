namespace Contoso.Customers.Api.Controllers;

[ApiController, Route("/api/customers"), OpenApiTag("Customers")]
public class CustomerController(WebApi webApi, ICustomerService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly ICustomerService _service = service.ThrowIfNull();

    [HttpPost]
    [Accepts<Customer>]
    [ProducesResponseType<Customer>(201)]
    [IdempotencyKey]
    public Task<IActionResult> PostAsync(CancellationToken cancellationToken = default) => _webApi.PostAsync<Customer, Customer>(Request, (ro, ct) =>
    {
        ro.WithLocationUri(c => new Uri($"/api/customers/{c.Id}", UriKind.Relative));
        return _service.CreateAsync(ro.Value, ct);
    }, cancellationToken: cancellationToken);

    [HttpPut("{id}")]
    [Accepts<Customer>]
    [ProducesResponseType(typeof(Customer), 200)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> PutAsync(string id, CancellationToken cancellationToken = default) => _webApi.PutAsync<Customer, Customer>(Request, (ro, ct)
        => _service.UpdateAsync(ro.Value.Adjust(c => c.Id = id.Required()), ct), cancellationToken: cancellationToken);

    [HttpPatch("{id}")]
    [Accepts<Customer>(HttpNames.MergePatchJsonMediaTypeName)]
    [ProducesResponseType(typeof(Customer), 200)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> PatchAsync(string id, CancellationToken cancellationToken = default) => _webApi.PatchAsync<Customer>(Request,
        get: (ro, ct) => _service.GetAsync(id.Required(), ct),
        put: (ro, ct) => _service.UpdateAsync(ro.Value.Adjust(c => c.Id = id.Required()), ct),
        cancellationToken: cancellationToken);

    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    public Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken = default) => _webApi.DeleteAsync(Request, (_, ct)
        => _service.DeleteAsync(id.Required(), ct), cancellationToken: cancellationToken);
}
