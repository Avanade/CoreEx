using CoreEx.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace CoreEx.AspNetCore.Test.Api.Controllers;

[ApiController, Route("api/other")]
public class OtherController(WebApi webApi) : Controller
{
    [HttpGet("messages")]
    [ProducesResponseType(204)]
    public IActionResult Messages()
    {
        // Bypasses WebApi but should be picked up by the ExecutionContextMiddleware.
        ExecutionContext.Current.AddWarningMessage("Please pay your invoice.");
        return NoContent();
    }

    [HttpPost("unhandledexception")]
    public IActionResult UnhandledException() => throw new Exception("Oh no, that was unexpected!");

    [HttpPost("unhandledextendedexception")]
    public IActionResult UnhandledExtendedException() => throw new DuplicateException("Oh my, we have one of those already!");

    [HttpPost("idempotency-mvc/{id}")]
    [IdempotencyKey]
    public async Task<IActionResult> IdempotencyWebApiSuccess(int id) => await webApi.PostAsync<object>(Request, (ro, _) =>
    {
        if (id == 88)
            throw new NotFoundException();

        return Task.FromResult<object>(new { Id = id, Name = id == 99 ? new string('x', 512 * 1024) : "Bob" });
    });
}