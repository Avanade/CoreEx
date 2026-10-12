namespace Contoso.Shopping.Test.Unit.Policies;

public class CustomerPolicyTests : WithGenericTester<EntryPoint>
{
    private readonly Mock<ICustomerAdapter> _customerAdapterMock = new();

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _customerAdapterMock.Setup(x => x.GetAsync("existing-customer-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Go(new Customer { Id = "existing-customer-id", FirstName = "Frank", LastName = "Foster", Email = "frank.foster@example.com" }));
        _customerAdapterMock.Setup(x => x.GetAsync("nonexistent-customer-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.NotFoundError());
    }

    [Test]
    public void CustomerPolicy_EnsureExistsAsync_CustomerNotFound_ReturnsValidationError() => Test.Scoped(async test =>
    {
        var policy = new CustomerPolicy(_customerAdapterMock.Object);
        var result = await policy.EnsureExistsAsync("nonexistent-customer-id");
        result.IsFailure.Should().BeTrue();
        result.IsValidationError.Should().BeTrue();

        result.Error.As<ValidationException>().AssertErrors(new ApiError("customerId", "Customer was not found."));
    });

    [Test]
    public void CustomerPolicy_EnsureExistsAsync_CustomerExists_ReturnsSuccess() => Test.Scoped(async test =>
    {
        var policy = new CustomerPolicy(_customerAdapterMock.Object);
        var result = await policy.EnsureExistsAsync("existing-customer-id");
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("frank.foster@example.com");
    });
}
