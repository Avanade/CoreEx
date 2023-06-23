using CoreEx.FluentValidation;
using CoreEx.TestFunction.Services;
using CoreEx.TestFunction.Validators;
using CoreEx.AspNetCore.WebApis;
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
            => _webApi.GetAsync(request, (_, __) => _service.GetProductAsync(id));

        [FunctionName("HttpTriggerProductPost")]
        public Task<IActionResult> PostAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "products")] HttpRequest request)
            => _webApi.PostAsync(request, (r, _) => _service.AddProductAsync(r.Value!), validator: new ProductValidator().Wrap());

        [FunctionName("HttpTriggerProductPut")]
        public Task<IActionResult> PutAsync([HttpTrigger(AuthorizationLevel.Function, "put", Route = "products")] HttpRequest request)
            => _webApi.PutAsync(request, (r, _) => _service.UpdateProductAsync(r.Value!, r.Value!.Id!), validator: new ProductValidator().Wrap());
    }
}