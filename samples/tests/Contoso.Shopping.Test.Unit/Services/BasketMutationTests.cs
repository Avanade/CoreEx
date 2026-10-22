namespace Contoso.Shopping.Test.Unit.Services;

public class BasketMutationTests : WithGenericTester<EntryPoint>
{
    private static readonly Result _writeFailure = Result.ConflictError("Write failed.");

    [TestCase("mutated")]
    [TestCase("noop")]
    [TestCase("failed")]
    public void Create_PublishesOnlySuccessfulMutations(string outcome) => Test.Scoped(async test =>
    {
        var harness = new Harness();
        harness.Customers.Setup(x => x.GetAsync("customer-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Customer { Id = "customer-id" }));
        harness.Repository.Setup(x => x.CreateAsync(It.IsAny<Domain.Basket>(), It.IsAny<CancellationToken>()))
            .Returns((Domain.Basket basket, CancellationToken _) =>
            {
                harness.InTransaction.Should().BeTrue();
                return Task.FromResult(WriteResult(basket, outcome));
            });

        var result = await harness.Service.CreateAsync("customer-id").ConfigureAwait(false);

        AssertOutcome(harness, result, outcome, EventAction.Created);
        harness.Repository.Verify(x => x.CreateAsync(It.IsAny<Domain.Basket>(), It.IsAny<CancellationToken>()), Times.Once);
    });

    [TestCase("mutated")]
    [TestCase("noop")]
    [TestCase("failed")]
    public void Update_AfterSuccessfulRead_UsesPersistenceMutationOutcome(string outcome) => Test.Scoped(async test =>
    {
        var harness = new Harness();
        var basket = CreateBasket();
        harness.Repository.Setup(x => x.GetAsync(basket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(basket));
        harness.Repository.Setup(x => x.UpdateAsync(basket, It.IsAny<CancellationToken>()))
            .Returns((Domain.Basket value, CancellationToken _) =>
            {
                harness.InTransaction.Should().BeTrue();
                value.HasChanges.Should().BeTrue();
                return Task.FromResult(WriteResult(value, outcome));
            });

        var result = await harness.Service.UpdateShippingAddressAsync(basket.Id,
            new Address { Street1 = "2 Main St", City = "Sydney", PostCode = "2000", State = "NSW" }).ConfigureAwait(false);

        AssertOutcome(harness, result, outcome, EventAction.Updated);
        harness.Repository.Verify(x => x.GetAsync(basket.Id, It.IsAny<CancellationToken>()), Times.Once);
        harness.Repository.Verify(x => x.UpdateAsync(basket, It.IsAny<CancellationToken>()), Times.Once);
    });

    [TestCase("mutated")]
    [TestCase("noop")]
    [TestCase("failed")]
    public void Checkout_GatesMutationEventButPreservesReservationCommand(string outcome) => Test.Scoped(async test =>
    {
        var harness = new Harness();
        var basket = CreateBasket();
        harness.Repository.Setup(x => x.GetAsync(basket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(basket));
        harness.Repository.Setup(x => x.UpdateAsync(basket, It.IsAny<CancellationToken>()))
            .Returns((Domain.Basket value, CancellationToken _) =>
            {
                harness.InTransaction.Should().BeTrue();
                return Task.FromResult(WriteResult(value, outcome));
            });
        harness.Products.Setup(x => x.ReserveInventoryAsync(basket, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success);
        harness.Products.Setup(x => x.CreateConfirmReservationCommand(basket, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                harness.InTransaction.Should().BeTrue();
                return Task.FromResult(Result.Success);
            });

        var result = await harness.Service.CheckoutAsync(basket.Id).ConfigureAwait(false);

        AssertOutcome(harness, result, outcome, EventAction.CheckedOut);
        harness.Products.Verify(x => x.CreateConfirmReservationCommand(basket, It.IsAny<CancellationToken>()), outcome == "failed" ? Times.Never : Times.Once);
    });

    [Test]
    public void Update_UnchangedAggregate_SkipsPersistenceAndEvent() => Test.Scoped(async test =>
    {
        var harness = new Harness();
        var basket = CreateBasket();
        harness.Repository.Setup(x => x.GetAsync(basket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(basket));

        var result = await harness.Service.UpdateShippingAddressAsync(basket.Id,
            new Address { Street1 = "1 Main St", City = "Sydney", PostCode = "2000", State = "NSW" }).ConfigureAwait(false);

        AssertOutcome(harness, result, "noop", EventAction.Updated);
        harness.Repository.Verify(x => x.UpdateAsync(It.IsAny<Domain.Basket>(), It.IsAny<CancellationToken>()), Times.Never);
    });

    private static Result<DataResult<Domain.Basket>> WriteResult(Domain.Basket basket, string outcome)
        => outcome == "failed" ? _writeFailure : Result.Ok(new DataResult<Domain.Basket>(basket, outcome == "mutated"));

    private static void AssertOutcome(Harness harness, Result<Basket> result, string outcome, EventAction action)
    {
        result.IsSuccess.Should().Be(outcome != "failed");
        if (outcome == "failed")
            result.Error.Should().BeSameAs(_writeFailure.Error);
        else
            result.Value.CustomerId.Should().Be("customer-id");

        harness.Events.Should().HaveCount(outcome == "mutated" ? 1 : 0);
        if (outcome == "mutated")
            harness.Events[0].Action.Should().Be(action.ToString());

        harness.InTransaction.Should().BeFalse();
    }

    private static Domain.Basket CreateBasket() => Domain.Basket.CreateFrom("basket-id", "customer-id", BasketStatus.Active, null,
        new Domain.ValueObjects.Address { Street1 = "1 Main St", City = "Sydney", PostCode = "2000", State = "NSW" },
        [Domain.BasketItem.CreateFrom("item-id", "product-id", "sku", "Product", new Domain.ValueObjects.ItemPricing { UnitOfMeasure = "EA", Quantity = 1, UnitPrice = 10 }, null)], null, null);

    private sealed class Harness
    {
        public Mock<IBasketRepository> Repository { get; } = new(MockBehavior.Strict);
        public Mock<IProductAdapter> Products { get; } = new(MockBehavior.Strict);
        public Mock<ICustomerAdapter> Customers { get; } = new(MockBehavior.Strict);
        public List<EventData> Events { get; } = [];
        public bool InTransaction { get; private set; }
        public BasketService Service { get; }

        public Harness()
        {
            // Exercise only the mutation-publication boundary; persistence/transactions remain covered by API tests.
            var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var queue = new Mock<IEventQueue>(MockBehavior.Strict);
            queue.Setup(x => x.Add(It.IsAny<IEnumerable<EventData>>())).Callback<IEnumerable<EventData>>(events =>
            {
                InTransaction.Should().BeTrue();
                Events.AddRange(events);
            });
            unitOfWork.SetupGet(x => x.Events).Returns(queue.Object);
            unitOfWork.Setup(x => x.TransactionAsync(It.IsAny<Func<CancellationToken, Task<Result<Basket>>>>(), It.IsAny<CancellationToken>()))
                .Returns(async (Func<CancellationToken, Task<Result<Basket>>> work, CancellationToken cancellationToken) =>
                {
                    InTransaction = true;
                    try
                    {
                        return await work(cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        InTransaction = false;
                    }
                });
            Service = new BasketService(unitOfWork.Object, Repository.Object, Products.Object, Customers.Object, NullLogger<BasketService>.Instance);
        }
    }
}
