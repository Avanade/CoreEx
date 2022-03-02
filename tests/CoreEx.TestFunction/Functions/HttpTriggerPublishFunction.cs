using CoreEx.Events;
using CoreEx.Functions;
using CoreEx.Functions.FluentValidation;
using CoreEx.TestFunction.Models;
using CoreEx.TestFunction.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;

namespace CoreEx.TestFunction.Functions
{
    public class HttpTriggerPublishFunction
    {
        private readonly IHttpTriggerExecutor _executor;
        private readonly IEventPublisherBase _eventPublisher;

        public HttpTriggerPublishFunction(IHttpTriggerExecutor executor, IEventPublisherBase eventPublisher)
        {
            _executor = executor;
            _eventPublisher = eventPublisher;
        }

        [FunctionName("HttpTriggerPublishFunction")]
        public Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "products/publish")] HttpRequest request)
            => _executor.RunPublishAsync<Product, ProductValidator>(request, _eventPublisher, "test-queue");
    }
}