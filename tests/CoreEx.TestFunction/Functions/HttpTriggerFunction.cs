using CoreEx.FluentValidation;
using CoreEx.TestFunction.Models;
using CoreEx.TestFunction.Services;
using CoreEx.TestFunction.Validators;
using CoreEx.WebApis;
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
        private readonly ProductService _service;

        public HttpTriggerFunction(WebApi webApi, ProductService service)
        {
            _webApi = webApi;
            _service = service;
        }

        [FunctionName("HttpTriggerProductGet")]
        public Task<IActionResult> GetAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "products/{id}")] HttpRequest request, string id)
            => _webApi.GetAsync(request, _ => _service.GetProductAsync(id));

        [FunctionName("HttpTriggerProductPost")]
        public Task<IActionResult> PostAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "products")] HttpRequest request)
            => _webApi.PostAsync<Product, Product>(request, r => _service.AddProductAsync(r.Validate<Product, ProductValidator>()));

        [FunctionName("HttpTriggerProductPut")]
        public Task<IActionResult> PutAsync([HttpTrigger(AuthorizationLevel.Function, "put", Route = "products")] HttpRequest request)
            => _webApi.PutAsync<Product, Product>(request, r => _service.UpdateProductAsync(r.Validate<Product, ProductValidator>(), r.Value!.Id!));
    }
}