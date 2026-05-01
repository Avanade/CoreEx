namespace Contoso.Products.Test.Unit.Validators;

public class InventoryReservationRequestValidatorTests : WithGenericTester<EntryPoint>
{
    private readonly Mock<IProductRepository> _mock = new();

    [OneTimeSetUp]
    public void OneTimeSetUp() 
    {
        _mock.Setup(x => x.GetForReservationAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new Dictionary<string, ProductReserve>
            {
                ["P1"] = new ProductReserve { UnitOfMeasureCode = "EA", IsNonStocked = false, IsInactive = false },
                ["P2"] = new ProductReserve { UnitOfMeasureCode = "EA", IsNonStocked = true, IsInactive = false },
                ["P3"] = new ProductReserve { UnitOfMeasureCode = "EA", IsNonStocked = false, IsInactive = true },
                ["P4"] = new ProductReserve { UnitOfMeasureCode = "EA", IsNonStocked = false, IsInactive = false }
            });
    }

    [Test]
    public void Empty_Required() => Test.Scoped(test =>
    {
        var req = new MovementRequest();

        new MovementRequestValidator(_mock.Object).AssertErrors(req,
            ("id", "Identifier is required."),
            ("products", "Products is required."));
    });

    [Test]
    public void Invalid_No_Products() => Test.Scoped(test =>
    {
        var req = new MovementRequest
        {
            Id = "100",
            Products = []
        };

        new MovementRequestValidator(_mock.Object).AssertErrors(req,
            ("products", "Products is required."));
    });

    [Test]
    public void Invalid_Products() => Test.Scoped((Action<UnitTestEx.Hosting.ScopedTypeTester<ExecutionContext>>)(test =>
    {
        var req = new MovementRequest
        {
            Id = "100",
            Products = new()
            {
                ["P1"] = new MovementRequestProduct { UnitOfMeasure = "XX", Quantity = -1 },
                [""] = null!,
                ["P2"] = new MovementRequestProduct { UnitOfMeasure = "EA", Quantity = 1.33m }
            }
        };

        new MovementRequestValidator(_mock.Object).AssertErrors(req,
            ("products.P1.value.unitOfMeasure", "Unit-of-measure is invalid."),
            ("products.P1.value.quantity", "Quantity must be greater than or equal to '0'."),
            ("products.key", "Product is required."),
            ("products.P2.value.quantity", "Quantity exceeds the maximum decimal places (0)."));
    }));

    [Test]
    public void Invalid_Products_Extended() => Test.Scoped((Action<UnitTestEx.Hosting.ScopedTypeTester<ExecutionContext>>)(test =>
    {
        var req = new MovementRequest
        {
            Id = "100",
            Products = new()
            {
                ["P0"] = new MovementRequestProduct { UnitOfMeasure = "L", Quantity = 1 },
                ["P1"] = new MovementRequestProduct { UnitOfMeasure = "L", Quantity = 1 },
                ["P2"] = new MovementRequestProduct { UnitOfMeasure = "EA", Quantity = 1 },
                ["P3"] = new MovementRequestProduct { UnitOfMeasure = "EA", Quantity = 1 }
            }
        };

        new MovementRequestValidator(_mock.Object).AssertErrors(req,
            ("products.P0", "Product was not found."),
            ("products.P1.unitOfMeasure", "Unit-of-measure must be equal to 'Each'."),
            ("products.P2", "Product is non-stocked and therefore cannot be transacted."),
            ("products.P3", "Product is not active and therefore cannot be transacted."));
    }));

    [Test]
    public void Success() => Test.Scoped((Action<UnitTestEx.Hosting.ScopedTypeTester<ExecutionContext>>)(test =>
    {
        var req = new MovementRequest
        {
            Id = "100",
            Products = new()
            {
                ["P1"] = new MovementRequestProduct { UnitOfMeasure = "EA", Quantity = 1 },
                ["P4"] = new MovementRequestProduct { UnitOfMeasure = "EA", Quantity = 1 }
            }
        };

        new MovementRequestValidator(_mock.Object).AssertSuccess(req);
    }));
}