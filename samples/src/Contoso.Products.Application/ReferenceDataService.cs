namespace Contoso.Products.Application;

public partial class ReferenceDataService
{
    partial void OnInitialization() => PreCheckAsync = (value, action, cancellationToken) =>
    {
        if (value is Brand b && b.Code == "YETI" && action == EventAction.Deactivated)
            return Result.BusinessError("YETI brand cannot be deactivated as it is awesome.", c => c.WithErrorCode("yeti-cannot-be-deactivated")).AsTask();

        return Result.SuccessTask;
    };
}
