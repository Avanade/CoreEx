using CoreEx.AspNetCore;
using CoreEx.TestFunction.Services;
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
    }
}