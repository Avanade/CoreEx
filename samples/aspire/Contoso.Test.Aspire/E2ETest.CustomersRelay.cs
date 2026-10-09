namespace Contoso.Test.Aspire;

public partial class E2ETest
{
    [Test]
    public async Task Customers_Api_Outbox_Relay_PublishesEvents()
    {
        Test.Checkpoint("Create, update and delete a Customer; observe its real Cosmos outbox events on Service Bus.");
        var customer = Test.Http<Contoso.Customers.Contracts.Customer>(_customersApi)
            .Run(HttpMethod.Post, "/api/customers", new Contoso.Customers.Contracts.Customer
            {
                FirstName = "Relay",
                LastName = "Customer",
                Email = "relay.customer@example.com",
                CustomerTypeCode = "IND",
                ContactMethodCode = "EM"
            }, r => r.WithIdempotencyKey())
            .AssertCreated().Value ?? throw new InvalidOperationException("The Customers API returned no customer.");

        var customerId = customer.Id ?? throw new InvalidOperationException("The created customer has no identifier.");
        customer.FirstName = "Updated";
        customer = Test.Http<Contoso.Customers.Contracts.Customer>(_customersApi)
            .Run(HttpMethod.Put, $"/api/customers/{customerId}", customer, r => r.WithIfMatch(customer.ETag))
            .AssertOK().Value ?? throw new InvalidOperationException("The Customers API returned no updated customer.");
        Test.Http(_customersApi).Run(HttpMethod.Delete, $"/api/customers/{customerId}").AssertNoContent();

        Test.Checkpoint("Create, update, activate, deactivate and delete reference data; its outbox uses the separate ref-data container.");
        const string refDataUrl = "/api/refdata/customer-types";
        var refData = Test.Http<Contoso.Customers.Contracts.CustomerType>(_customersApi)
            .Run(HttpMethod.Post, refDataUrl, new Contoso.Customers.Contracts.CustomerType { Code = $"RELAY-{Runtime.NewId()[..8]}", Text = "Relay E2E" })
            .AssertCreated().Value ?? throw new InvalidOperationException("The Customers API returned no customer type.");
        var refDataId = refData.Id ?? throw new InvalidOperationException("The customer type has no identifier.");
        refData = Test.Http<Contoso.Customers.Contracts.CustomerType>(_customersApi)
            .Run(HttpMethod.Patch, $"{refDataUrl}/{refDataId}", new { text = "Relay E2E updated" }, r => r.WithIfMatch(refData.ETag).WithMergePatchJsonContentType())
            .AssertOK().Value ?? throw new InvalidOperationException("The Customers API returned no updated customer type.");
        Test.Http(_customersApi).Run(HttpMethod.Post, $"{refDataUrl}/{refDataId}/activate").AssertOK();
        Test.Http(_customersApi).Run(HttpMethod.Post, $"{refDataUrl}/{refDataId}/deactivate").AssertOK();
        Test.Http(_customersApi).Run(HttpMethod.Delete, $"{refDataUrl}/{refDataId}").AssertNoContent();

        (string Subject, string Type)[] expected =
        [
            (customerId, "contoso.customers.customer.created.v1"),
            (customerId, "contoso.customers.customer.updated.v1"),
            (customerId, "contoso.customers.customer.deleted"),
            (refDataId, "contoso.customers.customertype.created.v1"),
            (refDataId, "contoso.customers.customertype.updated.v1"),
            (refDataId, "contoso.customers.customertype.activated.v1"),
            (refDataId, "contoso.customers.customertype.deactivated.v1"),
            (refDataId, "contoso.customers.customertype.deleted")
        ];

        await using var client = new ServiceBusClient(_serviceBusConnectionString, new ServiceBusClientOptions
        {
            RetryOptions = new ServiceBusRetryOptions { MaxRetries = 0, TryTimeout = TimeSpan.FromSeconds(1) }
        });
        var messages = await Contoso.Customers.Test.Common.ServiceBus.ReceiveEventsAsync(client, expected).ConfigureAwait(false);
        foreach (var key in expected)
        {
            var message = messages[key];
            message.MessageId.Should().NotBeNullOrEmpty();
            message.SessionId.Should().Be(CoreEx.Data.PartitionKey.GetPartitionIdAsString(key.Subject));
            using var json = JsonDocument.Parse(message.Body);
            json.RootElement.GetProperty("id").GetString().Should().Be(message.MessageId);
            if (key.Type.EndsWith(".v1", StringComparison.Ordinal))
                json.RootElement.GetProperty("data").GetProperty("id").GetString().Should().Be(key.Subject);
        }

        using var created = JsonDocument.Parse(messages[(customerId, "contoso.customers.customer.created.v1")].Body);
        created.RootElement.GetProperty("data").GetProperty("firstName").GetString().Should().Be("Relay");
        using var updated = JsonDocument.Parse(messages[(customerId, "contoso.customers.customer.updated.v1")].Body);
        updated.RootElement.GetProperty("data").GetProperty("firstName").GetString().Should().Be("Updated");
    }
}
