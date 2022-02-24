using CoreEx.Functions;
using CoreEx.TestFunction.Models;
using CoreEx.TestFunction.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;

namespace CoreEx.TestFunction
{
    public class HttpTriggerFunction
    {
        private readonly IHttpTriggerExecutor _executor;
        private readonly HttpTriggerService _service;

        public HttpTriggerFunction(IHttpTriggerExecutor executor, HttpTriggerService service)
        {
            _executor = executor;
            _service = service;
        }

        [FunctionName("HttpTriggerFunction")]
        public Task<IActionResult> RunNoValidatorAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "novalidator")] HttpRequest request)
            => _executor.RunWithResultAsync<Product, Product>(request, _service.UpdateProductAsync);
    }
}