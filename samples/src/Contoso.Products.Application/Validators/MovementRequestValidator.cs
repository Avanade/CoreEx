namespace Contoso.Products.Application.Validators;

public class MovementRequestValidator : Validator<Contracts.MovementRequest>
{
    private static readonly Validator<MovementRequestProduct> _productValidator = Validator.Create<MovementRequestProduct>()
        .HasProperty(x => x.UnitOfMeasure, c => c.Mandatory().IsValid())
        .HasProperty(x => x.Quantity, c => c.GreaterThanOrEqualTo(0).PrecisionScale(ctx => ctx.Entity.UnitOfMeasure!.Precision, ctx => ctx.Entity.UnitOfMeasure!.Scale).DependsOn(x => x.UnitOfMeasure));

    private readonly IProductRepository _repository;
    private readonly LText _productText = "Product";

    public MovementRequestValidator(IProductRepository repository)
    {
        _repository = repository.ThrowIfNull();

        Property(x => x.Id).Mandatory().MaximumLength(50);
        Property(x => x.Products).Mandatory().Dictionary(c => c
            .WithKeyValidator(_productText, k => k.Mandatory().MaximumLength(50))
            .WithValueValidator(v => v.Mandatory().Entity(_productValidator)));
    }

    protected async override Task OnValidateAsync(ValidationContext<MovementRequest> context, CancellationToken cancellationToken)
    {
        // Fail-fast where any of the cheap validation errors have occurred.
        if (context.HasErrors)
            return;

        // Get the product(s) info to determine whether request is valid.
        var ids = context.Value.Products!.Select(kvp => kvp.Key).ToArray() ?? [];
        var products = await _repository.GetForReservationAsync(ids, cancellationToken).ConfigureAwait(false);

        // Create extended product validator (dictionary value) that can access the product information for validating the unit of measure.
        var dv = Validator.Create<MovementRequestProduct>()
            .HasProperty(x => x.UnitOfMeasure, c => c.Equal(ctx => products[ctx.GetDictionaryKey<string>()].UnitOfMeasureCode));

        // Create new validator for the request that ensures validity of the product (dictionary key) and value (dictionary value).
        await context.ValidateFurtherAsync(c => c
            .HasProperty(x => x.Products, c => c.Dictionary(c => c
                .WithKeyValidator(_productText, k => k
                    .NotFound().WhenValue(v => !products.ContainsKey(v))
                    .Error("{0} is non-stocked and therefore cannot be transacted.").WhenValue(v => products[v].IsNonStocked)
                    .Error("{0} is not active and therefore cannot be transacted.").WhenValue(v => products[v].IsInactive))
                .WithValueValidator(dv))
            ), cancellationToken).ConfigureAwait(false);
    }
}