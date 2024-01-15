using CoreEx.AspNetCore.WebApis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace CoreEx.TestFunctionIso
{
    public class HttpFunction(WebApi webApi)
    {
        private readonly WebApi _webApi = webApi;

        [Function("HttpFunction")]
        public Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "{id}")] HttpRequest req, string id)
            => _webApi.GetAsync(req, _ => Task.FromResult(new { Message = $"Hello {id}" }));
    }
}