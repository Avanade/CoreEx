namespace Contoso.Products.Api.Controllers;

[ApiController, Route("/api/refdata")]
public class ReferenceDataController(WebApi webApi) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();

    private static readonly DataMap<string> _mapper = new(StringComparer.OrdinalIgnoreCase)
    {
        { "categories", nameof(Category) },
        { "sub-categories", nameof(SubCategory) },
        { "units-of-measure", nameof(UnitOfMeasure) },
        { "brands", nameof(Brand) },
        { "movement-kinds", nameof(MovementKind) },
        { "movement-statuses", nameof(MovementStatus) }
    };

    [HttpGet("categories"), HttpHead("categories")]
    [ProducesResponseType(typeof(Category[]), 200)]
    public Task<IActionResult> GetCategoriesAsync([FromQuery] IEnumerable<string>? codes = default, string? text = default)
        => _webApi.GetAsync(Request, (ro, ct) => ReferenceDataOrchestrator.Current.GetWithFilterAsync<Category>(codes, text, ro.IsIncludeInactive, ct));

    [HttpGet("sub-categories"), HttpHead("sub-categories")]
    [ProducesResponseType(typeof(SubCategory[]), 200)]
    public Task<IActionResult> GetSubCategoriesAsync([FromQuery] IEnumerable<string>? codes = default, string? text = default)
        => _webApi.GetAsync(Request, (ro, ct) => ReferenceDataOrchestrator.Current.GetWithFilterAsync<SubCategory>(codes, text, ro.IsIncludeInactive, ct));

    [HttpGet("units-of-measure"), HttpHead("units-of-measure")]
    [ProducesResponseType(typeof(UnitOfMeasure[]), 200)]
    public Task<IActionResult> GetUnitsOfMeasureAsync([FromQuery] IEnumerable<string>? codes = default, string? text = default)
        => _webApi.GetAsync(Request, (ro, ct) => ReferenceDataOrchestrator.Current.GetWithFilterAsync<UnitOfMeasure>(codes, text, ro.IsIncludeInactive, ct));

    [HttpGet("brands"), HttpHead("brands")]
    [ProducesResponseType(typeof(Brand[]), 200)]
    public Task<IActionResult> GetBrandsAsync([FromQuery] IEnumerable<string>? codes = default, string? text = default)
        => _webApi.GetAsync(Request, (ro, ct) => ReferenceDataOrchestrator.Current.GetWithFilterAsync<Brand>(codes, text, ro.IsIncludeInactive, ct));

    [HttpGet("movement-kinds"), HttpHead("movement-kinds")]
    [ProducesResponseType(typeof(MovementKind[]), 200)]
    public Task<IActionResult> GetMovementKindsAsync([FromQuery] IEnumerable<string>? codes = default, string? text = default)
        => _webApi.GetAsync(Request, (ro, ct) => ReferenceDataOrchestrator.Current.GetWithFilterAsync<MovementKind>(codes, text, ro.IsIncludeInactive, ct));

    [HttpGet("movement-statuses"), HttpHead("movement-statuses")]
    [ProducesResponseType(typeof(MovementKind[]), 200)]
    public Task<IActionResult> GetMovementStatusesAsync([FromQuery] IEnumerable<string>? codes = default, string? text = default)
        => _webApi.GetAsync(Request, (ro, ct) => ReferenceDataOrchestrator.Current.GetWithFilterAsync<MovementStatus>(codes, text, ro.IsIncludeInactive, ct));

    [HttpGet]
    [ProducesResponseType(typeof(ReferenceDataMultiDictionary), 200)]
    public Task<IActionResult> GetNamedAsync([FromQuery] string[] name)
        => _webApi.GetAsync(Request, (ro, ct) => ReferenceDataOrchestrator.Current.GetNamedAsync(name, ro.IsIncludeInactive, _mapper, ct));
}