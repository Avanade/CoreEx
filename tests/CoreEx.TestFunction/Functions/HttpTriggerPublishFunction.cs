using CoreEx.FluentValidation;
using CoreEx.TestFunction.Models;
using CoreEx.TestFunction.Validators;
using CoreEx.AspNetCore.WebApis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;

namespace CoreEx.TestFunction.Functions
{
    public class HttpTriggerPublishFunction
    {
        private readonly WebApiPublisher _webApiPublisher;

        public HttpTriggerPublishFunction(WebApiPublisher webApiPublisher)
        {
            _webApiPublisher = webApiPublisher;
        }

        [FunctionName("HttpTriggerPublishFunction")]
        public Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "products/publish")] HttpRequest request)
            => _webApiPublisher.PublishAsync(request, new WebApiPublisherArgs<Product>("test-queue", new ProductValidator().Wrap()));
    }
}