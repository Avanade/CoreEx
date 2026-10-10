namespace Contoso.Test.Aspire;

public partial class E2ETest : WithAspireTester<Projects.Contoso_Aspire>
{
    private readonly string _productsApi = "products-api";
    private readonly string _shoppingApi = "shopping-api";
    private readonly string _customersApi = "customers-api";
    private string _serviceBusConnectionString = string.Empty;

    protected override async Task OnBeforeStartAsync(DistributedApplication app)
    {
        _serviceBusConnectionString = await AspireTesterBase.GetConnectionStringAsync(app, "ServiceBus").ConfigureAwait(false)
            ?? throw new InvalidOperationException("The ServiceBus resource has no connection string.");

        // Migrate the Products, Shopping and Customers databases and seed each with test data.
        await app.MigratePostgresDataAsync<Contoso.Products.Test.Common.TestData>("Postgres", ["mutate-data.seed.yaml"], Contoso.Products.Database.Program.ConfigureMigrationArgs);
        await app.MigrateSqlServerDataAsync<Contoso.Shopping.Test.Common.TestData>("SqlServer", ["mutate-data.seed.yaml"], Contoso.Shopping.Database.Program.ConfigureMigrationArgs);
        await app.MigrateCosmosDataAsync<Contoso.Customers.Test.Common.TestData>("Cosmos", "contoso", ["mutate-data.seed.yaml"], Contoso.Customers.Database.Program.ConfigureProvisionArgs);

        // Clear the Redis cache.
        await app.ClearRedisCacheAsync("redis");

        // Reset the Azure Service Bus queues and topics/subscriptions to an initial state.
        await app.ResetAzureServiceBusAsync("servicebus", ServiceBus.GetQueues(), ServiceBus.GetTopicsAndSubscriptions());
    }

    protected override async Task OnAfterStartAsync(DistributedApplication app)
    {
        // Wait for every host used by the asynchronous checkout flow before running the tests.
        await app.WaitForResourceAsync([_productsApi, _shoppingApi, _customersApi, "products-relay", "products-subscribe", "shopping-relay", "shopping-subscribe", "customers-relay"]);

        // Mock the SendGrid API so that the Shopping domain's Subscribe project can send emails without actually sending them.
        await app.HttpMock("mock-host", "http").Request(HttpMethod.Post, "/v3/mail/send").WithAnyBody().Respond.WithAsync(HttpStatusCode.Accepted);
    }

    [Test]
    public async Task CreateOrderAndConfirmAsync()
    {
        Test.Checkpoint("Create and activate a new Product; this should sync to the Shopping domain via the Products domain's outbox and Service Bus.");

        var product = Test.Http<Product>(_productsApi)
            .Run(HttpMethod.Post, "/api/products", new Product { Sku = "YETI-ASR-LR-XO-90", Text = "Yeti ASR LT XO/90", Price = 7900m, SubCategoryCode = "XC", UnitOfMeasureCode = "EA", Tags = ["cross-country", "full-suspension"]}, r => r.WithIdempotencyKey())
            .AssertCreated()
            .Value!;

        product = Test.Http<Product>(_productsApi)
            .Run(HttpMethod.Post, $"/api/products/{product.Id}/activate")
            .AssertOK()
            .Value!;

        Test.Checkpoint("Adjust the quantity-on-hand for the new Product so we can sell it.");

        Test.Http(_productsApi)
            .Run(HttpMethod.Post, "/api/inventory/adjust", new MovementRequest
            {
                Id = Runtime.NewGuid().ToString(),
                Products = new()
                {
                    { product.Id!, new MovementRequestProduct { Quantity = 3, UnitOfMeasureCode = "EA" } },
                }
            })
            .AssertOK();

        Test.Checkpoint("Create a new Customer (with a shipping address) in the Customers domain.");

        var customer = Test.Http<Contoso.Customers.Contracts.Customer>(_customersApi)
            .Run(HttpMethod.Post, "/api/customers", new Contoso.Customers.Contracts.Customer
            {
                FirstName = "Gina",
                LastName = "Garcia",
                Email = "gina.garcia@example.com",
                CustomerTypeCode = "IND",
                ContactMethodCode = "EM",
                ShippingAddress = new Contoso.Customers.Contracts.Address { Street1 = "42 Wallaby Way", City = "Sydney", State = "NSW", PostCode = "2000" }
            }, r => r.WithIdempotencyKey())
            .AssertCreated()
            .Value!;

        Test.Checkpoint("Create a new Basket for the Customer; the Customer is validated in real-time (via the Customers API) and the shipping address is defaulted from it.");

        var basket = Test.Http<Basket>(_shoppingApi)
            .Run(HttpMethod.Post, $"/api/customers/{customer.Id}/baskets", r => r.WithIdempotencyKey())
            .AssertCreated()
            .Value!;

        basket.CustomerId.Should().Be(customer.Id);
        basket.ShippingAddress.Should().NotBeNull();
        basket.ShippingAddress.Street1.Should().Be("42 Wallaby Way");
        basket.ShippingAddress.City.Should().Be("Sydney");
        basket.ShippingAddress.State.Should().Be("NSW");
        basket.ShippingAddress.PostCode.Should().Be("2000");

        Test.Checkpoint("Creating a Basket for a Customer that does not exist should fail validation.");

        Test.Http(_shoppingApi)
            .Run(HttpMethod.Post, $"/api/customers/{Runtime.NewGuid()}/baskets", r => r.WithIdempotencyKey())
            .AssertBadRequest();

        Test.Checkpoint("Add two existing Products to the Basket.");

        basket = Test.Http<Basket>(_shoppingApi)
            .Run(HttpMethod.Post, $"/api/baskets/{basket.Id}/items", new BasketItemAddRequest { ProductId = 28.ToGuid().ToString(), Quantity = 1m }, r => r.WithIdempotencyKey())
            .AssertOK()
            .Value!;

        basket = Test.Http<Basket>(_shoppingApi)
            .Run(HttpMethod.Post, $"/api/baskets/{basket.Id}/items", new BasketItemAddRequest { ProductId = 32.ToGuid().ToString(), Quantity = 1.5m }, r => r.WithIdempotencyKey())
            .AssertOK()
            .Value!;

        Test.Checkpoint("Apply a discount to the Basket");

        basket = Test.Http<Basket>(_shoppingApi)
            .Run(HttpMethod.Put, $"/api/baskets/{basket.Id}/apply-discount/SAVE10")
            .AssertOK()
            .Value!;

        basket.Pricing.Should().NotBeNull();
        basket.Pricing.DiscountPercentage.Should().Be(10m);

        Test.Checkpoint("Update the Basket's shipping address (overriding the address defaulted from the Customer).");

        var address = new Address
        {
            Street1 = "123 Main St",
            City = "Anytown",
            State = "CA",
            PostCode = "12345"
        };

        basket = Test.Http<Basket>(_shoppingApi)
            .Run(HttpMethod.Put, $"/api/baskets/{basket.Id}/shipping-address", address)
            .AssertOK()
            .Value!;

        basket.ShippingAddress.Should().NotBeNull();
        basket.ShippingAddress.Street1.Should().Be("123 Main St");

        Test.Checkpoint("Add the new Product to the Basket; should have sync'd by now.");

        basket = Test.Http<Basket>(_shoppingApi)
            .Run(HttpMethod.Post, $"/api/baskets/{basket.Id}/items", new BasketItemAddRequest { ProductId = product.Id, Quantity = 1m }, r => r.WithIdempotencyKey())
            .AssertOK()
            .Value!;

        Test.Checkpoint("Checkout the Basket.");

        basket = Test.Http<Basket>(_shoppingApi)
            .Run(HttpMethod.Post, $"/api/baskets/{basket.Id}/checkout")
            .AssertOK()
            .Value!;

        basket.StatusCode.Should().Be(BasketStatus.CheckedOut);

        Test.Checkpoint("Confirm that the product was successfully reserved/confirmed.");

        var movements = Test.Http<Movement[]>(_productsApi)
            .Run(HttpMethod.Get, "/api/inventory/movements", r => r.WithQuery($"referenceid eq '{basket.Id}'"))
            .AssertOK()
            .Value!;

        movements.Should().HaveCount(2);    // One of the items is not stocked, so only two movements should be created for the two items that were successfully reserved.

        const int maxConfirmationAttempts = 60;
        var attempt = 0;
        while (movements.Single(m => m.ProductId == product.Id).StatusCode != MovementStatus.Confirmed)
        {
            if (attempt == maxConfirmationAttempts)
                Assert.Fail($"Inventory movement confirmation timed out after {maxConfirmationAttempts} polls: BasketId='{basket.Id}', ProductId='{product.Id}', StatusCode='{movements.Single(m => m.ProductId == product.Id).StatusCode}'. Expected path: Shopping outbox -> Shopping Relay -> contoso-products command queue -> Products Subscribe.");

            Test.Delay(1000, $"Waiting for the Product's inventory movement to be confirmed (via Shopping's outbox, Relay, and the Products command subscriber); iteration {attempt + 1} of {maxConfirmationAttempts}.");
            movements = Test.Http<Movement[]>(_productsApi)
                .Run(HttpMethod.Get, "/api/inventory/movements", r => r.WithQuery($"referenceid eq '{basket.Id}'"))
                .AssertOK()
                .Value!;

            attempt++;
        }

        Test.Checkpoint("The Product's inventory movement was successfully confirmed - you little beauty!");

        Test.Checkpoint("Poll the stand-in SendGrid endpoint for a mail-send request addressed to the Customer's e-mail.");

        var email = customer.Email!;
        var api = await Test.HttpMock("mock-host", "http").GetAdminApiAsync().ConfigureAwait(false);
        for (attempt = 1; !await SendReceivedAsync(api, email).ConfigureAwait(false); attempt++)
        {
            if (attempt > 30)
                Assert.Fail($"No SendGrid mail-send request addressed to '{email}' was received after 30 attempts; the Basket.CheckedOut event did not result in a send.");

            Test.Delay(1000, $"Waiting for the mail-send request; iteration {attempt}.");
        }
    }

    private static async Task<bool> SendReceivedAsync(WireMock.Client.IWireMockAdminApi api, string email)
    {
        var requests = await api.GetRequestsAsync().ConfigureAwait(false);
        return requests.Any(r => r.Request?.Path == "/v3/mail/send" && r.Request.Body?.Contains(email, StringComparison.OrdinalIgnoreCase) == true);
    }
}
