using Contoso.Products.Contracts;
using Contoso.Shopping.Contracts;
using CoreEx;

namespace Contoso.Test.Aspire;

public class E2ETest : WithAspireTester<Projects.Contoso_Aspire>
{
    private readonly string _productsApi = "products-api";
    private readonly string _shoppingApi = "shopping-api";

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        // Migrate the Products and Shopping relational databases and seed each with test data.
        await Test.MigratePostgresDataAsync<Contoso.Products.Test.Common.TestData>("Postgres", ["mutate-data.seed.yaml"], Contoso.Products.Database.Program.ConfigureMigrationArgs);
        await Test.MigrateSqlServerDataAsync<Contoso.Shopping.Test.Common.TestData>("SqlServer", ["mutate-data.seed.yaml"], Contoso.Shopping.Database.Program.ConfigureMigrationArgs);

        // Clear the Redis cache.
        await Test.ClearRedisCacheAsync("redis");

        // Reset the Azure Service Bus queues and topics/subscriptions to an initial state.
        await Test.ResetAzureServiceBusAsync("servicebus", ServiceBus.GetQueues(), ServiceBus.GetTopicsAndSubscriptions());

        // Hang about until the Products and Shopping APIs are available.
        await Test.WaitForResourceAsync([_productsApi, _shoppingApi]);
    }

    [Test]
    public void CreateOrderAndConfirm()
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
                Id = Guid.NewGuid().ToString(),
                Products = new()
                {
                    { product.Id!, new MovementRequestProduct { Quantity = 3, UnitOfMeasureCode = "EA" } },
                }
            })
            .AssertOK();

        Test.Checkpoint("Create a new Basket for a Customer.");

        var basket = Test.Http<Basket>(_shoppingApi)
            .Run(HttpMethod.Post, $"/api/customers/{1004.ToGuid()}/baskets", r => r.WithIdempotencyKey())
            .AssertCreated()
            .Value!;

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

        Test.Checkpoint("Update the Basket's shipping address.");

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

        var attempt = 0;
        while (movements.Single(m => m.ProductId == product.Id).Status != MovementStatus.Confirmed)
        {
            Test.Delay(1000, $"Waiting for the Product's inventory movement to be confirmed (via the Products domain's outbox and Service Bus); iteration {attempt + 1}.");
            movements = Test.Http<Movement[]>(_productsApi)
                .Run(HttpMethod.Get, "/api/inventory/movements", r => r.WithQuery($"referenceid eq '{basket.Id}'"))
                .AssertOK()
                .Value!;

            attempt++;
            if (attempt > 10)
                Assert.Fail("The Product's inventory movement was not confirmed after 10 attempts.");
        }

        Test.Checkpoint("COMPLETE: The Product's inventory movement was successfully confirmed - you little beauty!");
    }
}
