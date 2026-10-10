namespace Contoso.Shopping.Application.Policies;

public class CustomerPolicy(ICustomerAdapter customerAdapter)
{
    private static readonly LText _customerText = new("Customer");
    private readonly ICustomerAdapter _customerAdapter = customerAdapter.ThrowIfNull();

    public Task<Result<Customer>> EnsureExistsAsync(string customerId, CancellationToken ct = default) => Result
        .GoAsync(() => _customerAdapter.GetAsync(customerId, ct))
        .OnFailure(r => r.IsNotFoundError ? Result.ValidationError(MessageItem.CreateErrorMessage(nameof(customerId), "{0} was not found.", _customerText)) : r);
}
