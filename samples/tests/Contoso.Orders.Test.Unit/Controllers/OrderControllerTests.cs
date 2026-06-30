using System.Text;
using System.Text.Json;
using Contoso.Order.Workflow.Client;
using Contoso.Order.Workflow.Workflow.Contracts;
using Contoso.Orders.Api.Controllers;
using Contoso.Orders.Application.Interfaces;
using CoreEx.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DurableTask.Client;

namespace Contoso.Orders.Test.Unit.Controllers;

public class OrderControllerTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void Order_Controller_Orchestrate_Accepted() => Test.Scoped(async test =>
    {
        var request = new OrderWorkflowRequest("ORD-123", 42.50m, "AUD", "unit-test");
        var workflowClient = new MockOrderWorkflowClient("orch-123");

        var controller = new OrderController(new WebApi(executionContext: test.Service), new FakeOrderService(), workflowClient)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext(request, test.Services) }
        };

        var result = await controller.OrchestrateOrderAsync().ConfigureAwait(false);

        result.Should().BeOfType<ContentResult>().Which.StatusCode.Should().Be(StatusCodes.Status202Accepted);

        var content = ((ContentResult)result).Content;
        content.Should().NotBeNull();

        var response = JsonSerializer.Deserialize<OrchestrateOrderResponse>(content!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        response.Should().Be(new OrchestrateOrderResponse("orch-123"));
        workflowClient.StartRequest.Should().Be(request);
    });

    private static DefaultHttpContext CreateHttpContext(OrderWorkflowRequest request, IServiceProvider serviceProvider)
    {
        var json = JsonSerializer.Serialize(request);
        var body = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.ContentType = "application/json";
        httpContext.Request.ContentLength = body.Length;
        httpContext.Request.Body = body;

        return httpContext;
    }

    private sealed class MockOrderWorkflowClient(string instanceId) : IOrderWorkflowClient
    {
        private readonly string _instanceId = instanceId;

        public OrderWorkflowRequest? StartRequest { get; private set; }

        public Task<string> StartAsync(OrderWorkflowRequest request, string? instanceId = null, CancellationToken cancellationToken = default)
        {
            StartRequest = request;
            return Task.FromResult(instanceId ?? _instanceId);
        }

        public Task<OrchestrationMetadata?> GetMetadataAsync(string instanceId, bool getInputsAndOutputs = false, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeOrderService : IOrderService
    {
        public Task<Contoso.Orders.Contracts.Order> CreateAsync(Contoso.Orders.Contracts.Order order, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Contoso.Orders.Contracts.Order?> GetAsync(string id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Contoso.Orders.Contracts.Order> UpdateAsync(Contoso.Orders.Contracts.Order order, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
