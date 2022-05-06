using CoreEx.FluentValidation;
using CoreEx.WebApis;
using CoreEx.TestFunction.Models;
using CoreEx.TestFunction.Services;
using CoreEx.TestFunction.Validators;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CoreEx.TestApi.Controllers
{
    [ApiController]
    [Route("products")]
    public class ProductController : ControllerBase
    {
        private readonly WebApi _webApi;
        private readonly ProductService _service;

        public ProductController(WebApi webApi, ProductService service)
        {
            _webApi = webApi;
            _service = service;
        }

        [HttpGet]
        [Route("{id}")]
        public Task<IActionResult> GetAsync(string id) => _webApi.GetAsync(Request, _ => _service.GetProductAsync(id));

        [HttpPost]
        public Task<IActionResult> PostAsync() => _webApi.PostAsync<Product, Product>(Request, r => _service.AddProductAsync(r.Value), validator: new ProductValidator().Convert());

        [HttpPut]
        [Route("{id}")]
        public Task<IActionResult> PutAsync(string id) => _webApi.PutAsync<Product, Product>(Request, r => _service.UpdateProductAsync(r.Value, id));

        [HttpDelete]
        public Task<IActionResult> DeleteAsync(string id) => _webApi.DeleteAsync(Request, _ => _service.DeleteProductAsync(id));
    }
}