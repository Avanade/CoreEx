using CoreEx.AspNetCore.Mvc;
using CoreEx.AspNetCore.Test.Api.Entities;
using CoreEx.AspNetCore.Test.Api.Services;
using CoreEx.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoreEx.AspNetCore.Test.Api.Controllers;

[ApiController, Route("api/people")]
public class PersonController(WebApi webApi, PersonService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly PersonService _service = service.ThrowIfNull();

    [HttpGet("{id}"), HttpHead("{id}")]
    [ProducesResponseType(typeof(Person), 200)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> GetAsync(string id) => _webApi.GetAsync(Request, (ro, _) => _service.GetAsync(id));

    [HttpGet]
    [ProducesResponseType(typeof(Person[]), 200)]
    [Query(true, true)]
    [Paging(true)]
    public Task<IActionResult> GetItemsAsync() => _webApi.GetAsync(Request, (ro, _) => _service.GetByQueryAsync(ro.QueryArgs, ro.PagingArgs));

    [HttpPost]
    [Accepts<Person>]
    [ProducesResponseType<Person>(201)]
    public Task<IActionResult> PostAsync() => _webApi.PostAsync<Person, Person>(Request, async (ro, ct) =>
    {
        ro.WithLocationUri(p => new Uri($"api/people/{p.Id}", UriKind.Relative));
        return await _service.CreateAsync(ro.Value).ConfigureAwait(false);
    });

    [HttpPut("{id}")]
    [Accepts<Person>]
    [ProducesResponseType<Person>(200)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> PutAsync(string id) => _webApi.PutAsync<Person, Person>(Request, (ro, ct) => _service.UpdateAsync(ro.Value.Adjust(p => p.Id = id)));

    [HttpPatch("{id}")]
    [Accepts<Person>(HttpNames.MergePatchJsonMediaTypeName)]
    [ProducesResponseType(typeof(Person), 200)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> PatchAsync(string id) => _webApi.PatchAsync<Person>(Request,
        get: (ro, _) => _service.GetAsync(id),
        put: (ro, _) => _service.UpdateAsync(ro.Value.Adjust(p => p.Id = id)));

    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    public Task<IActionResult> DeleteAsync(string id) => _webApi.DeleteAsync(Request, (ro, _) => _service.DeleteAsync(id));
}