namespace Contoso.Customers.Api.Controllers;

/// <summary>Provides the reference-data controller.</summary>
/// <remarks>Hand-written (not generated) - <c>Contoso.Customers.CodeGen</c>'s <c>repository: Cosmos</c> mode does not yet generate API-layer code; mirrors the shape/route conventions of a generated
/// <c>ReferenceDataController.g.cs</c> (see e.g. <c>Contoso.Products.Api</c>) by hand.</remarks>
[ApiController, Route("/api/refdata")]
public class ReferenceDataController(WebApi webApi) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();

    [HttpGet("customer-types"), HttpHead("customer-types")]
    [ProducesResponseType(typeof(CustomerType[]), 200)]
    [Query(supportsOrderBy: true), Paging(supportsCount: true)]
    public Task<IActionResult> GetCustomerTypesAsync(CancellationToken cancellationToken)
        => _webApi.GetAsync(Request, (ro, ct) => ReferenceDataOrchestrator.Current.QueryAsync<CustomerType>(ro.QueryArgs, ro.PagingArgs, ct), cancellationToken: cancellationToken);

    [HttpGet("contact-methods"), HttpHead("contact-methods")]
    [ProducesResponseType(typeof(ContactMethod[]), 200)]
    [Query(supportsOrderBy: true), Paging(supportsCount: true)]
    public Task<IActionResult> GetContactMethodsAsync(CancellationToken cancellationToken)
        => _webApi.GetAsync(Request, (ro, ct) => ReferenceDataOrchestrator.Current.QueryAsync<ContactMethod>(ro.QueryArgs, ro.PagingArgs, ct), cancellationToken: cancellationToken);

    [HttpGet]
    [ProducesResponseType(typeof(ReferenceDataMultiDictionary), 200)]
    public Task<IActionResult> GetNamedAsync([FromQuery] string[] name, CancellationToken cancellationToken)
        => _webApi.GetAsync(Request, (ro, ct) => ReferenceDataOrchestrator.Current.GetNamedAsync(name, ro.IsIncludeInactive, ct), cancellationToken: cancellationToken);
}
