using AutoMapper;
using CoreEx.TestFunction.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreEx.TestFunction.Services
{
    public class ProductService
    {
        private readonly BackendHttpClient _backend;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public ProductService(BackendHttpClient backend, IMapper mapper, ILogger<ProductService> logger)
        {
            _backend = backend;
            _mapper = mapper;
            _logger = logger;
        }

        public Task<Product> GetProductAsync(string id)
        {
            if (id == "Zed")
                return null;

            return Task.FromResult(new Product { Id = id, Name = "Apple", Price = 0.79m });
        }

        public Task<Product> AddProductAsync(Product product)
        {
            product.Id = "new";
            return Task.FromResult(product);
        }

        public async Task<Product> UpdateProductAsync(Product product, string id)
        {
            product.Id = id;
            using (_logger.BeginScope(new Dictionary<string, object>() { { "ProductId", product.Id } }))
            {
                if (product.Id == "Zed")
                    throw new ValidationException("Zed is dead.");

                var bep = _mapper.Map<BackendProduct>(product);
                var r = await _backend.ThrowTransientException().ThrowKnownException().EnsureSuccess().PostAsync<BackendProduct, BackendProduct>("products", bep).ConfigureAwait(false);

                return _mapper.Map<Product>(r.Value);
            }
        }

        public Task DeleteProductAsync(string id)
        {
            _logger.LogInformation($"Deleting product {id}.");
            return Task.CompletedTask;
        }
    }
}