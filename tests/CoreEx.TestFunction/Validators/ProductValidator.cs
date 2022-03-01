using CoreEx.TestFunction.Models;
using FluentValidation;
using System.Collections.Generic;

namespace CoreEx.TestFunction.Validators
{
    public class ProductValidator : AbstractValidator<Product>
    {
        public ProductValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().Length(0, 100);
            RuleFor(x => x.Price).NotEmpty().ScalePrecision(2, 10).GreaterThan(0).Custom((v, ctx) =>
            {
                if (ctx.InstanceToValidate.Name == "Widget" && v >= 100)
                    ctx.AddFailure($"'{ctx.DisplayName}' must be less than $100.00 for a 'Widget'.");
                else if (ctx.InstanceToValidate.Name == "DeLorean" && ctx.InstanceToValidate.Price == 88m)
                    ctx.AddFailure(nameof(Product.Name), "A DeLorean cannot be priced at 88 as that could cause a chain reaction that would unravel the very fabric of the space-time continuum and destroy the entire universe.");
            });
        }
    }

    public class ProductsValidator : AbstractValidator<List<Product>>
    {
        public ProductsValidator() => RuleForEach(value => value).SetValidator(new ProductValidator());
    }
}