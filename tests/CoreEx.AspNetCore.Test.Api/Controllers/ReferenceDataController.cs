using CoreEx.AspNetCore.Mvc;
using CoreEx.AspNetCore.Test.Api.Entities;
using CoreEx.RefData;
using Microsoft.AspNetCore.Mvc;

namespace CoreEx.AspNetCore.Test.Api.Controllers;

[ApiController, Route("api/refdata")]
public class ReferenceDataController(WebApi webApi) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();

    [HttpGet("genders"), HttpHead("genders")]
    [ProducesResponseType(typeof(Gender[]), 200)]
    public Task<IActionResult> GetGendersAsync() 
        => _webApi.GetAsync(Request, (ro, ct) => ReferenceDataOrchestrator.Current.QueryAsync<Gender>(ro.QueryArgs, ro.PagingArgs, ct));
}
