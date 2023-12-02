using CoreEx.TestFunction.Models;
using CoreEx.Validation;

namespace CoreEx.TestApi.Validation
{
    public class ProductValidator : Validator<Product>
    {
        public ProductValidator()
        {
            Property(p => p.Name).Mandatory();
            Property(p => p.Price).Between(0, 100);
        }
    }
}