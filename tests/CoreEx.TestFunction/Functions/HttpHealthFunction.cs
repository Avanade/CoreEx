using CoreEx.AspNetCore.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;

namespace CoreEx.TestFunction.Functions
{
    public class HttpHealthFunction
    {
        private readonly HealthService _health;

        public HttpHealthFunction(HealthService health)
        {
            _health = health;
        }

        [FunctionName("HealthInfo")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "health")] HttpRequest req)
            => await _health.RunAsync().ConfigureAwait(false);
    }
}