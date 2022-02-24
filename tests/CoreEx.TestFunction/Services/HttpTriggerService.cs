using AutoMapper;
using CoreEx.TestFunction.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreEx.TestFunction.Services
{
    public class HttpTriggerService
    {
        private readonly BackendHttpClient _backend;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public HttpTriggerService(BackendHttpClient backend, IMapper mapper, ILogger<HttpTriggerService> logger)
        {
            _backend = backend;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            using (_logger.BeginScope(new Dictionary<string, object>() { { "ProductId", product.Id } }))
            {
                if (product.Id == "Zed")
                    throw new ValidationException("Zed is dead.");

                var bep = _mapper.Map<BackendProduct>(product);
                var r = await _backend.EnsureSuccess().PostAsync<BackendProduct, BackendProduct>("", bep).ConfigureAwait(false);

                return _mapper.Map<Product>(r);
            }
        }
    }
}