using CoreEx.AspNetCore;
using CoreEx.Functions;
using CoreEx.Functions.FluentValidation;
using CoreEx.TestFunction.Models;
using CoreEx.TestFunction.Services;
using CoreEx.TestFunction.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;

namespace CoreEx.TestFunction.Functions
{
    public class HttpTriggerFunction
    {
        private readonly WebApi _webApi;
        private readonly IHttpTriggerExecutor _executor;
        private readonly ProductService _service;

        public HttpTriggerFunction(IHttpTriggerExecutor executor, WebApi webApi, ProductService service)
        {
            _executor = executor;
            _webApi = webApi;
            _service = service;
        }

        [FunctionName("HttpTriggerProductGet")]
        public Task<IActionResult> GetAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "products/{id}")] HttpRequest request, string id)
            => _webApi.GetAsync(request, _ => _service.GetProductAsync(id));

        [FunctionName("HttpTriggerProductPost")]
        public Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "products")] HttpRequest request)
            => _executor.RunWithResultAsync<Product, ProductValidator, Product>(request, _service.UpdateProductAsync);
    }
}