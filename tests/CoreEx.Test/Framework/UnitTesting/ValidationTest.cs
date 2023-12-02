using NUnit.Framework;
using UnitTestEx.NUnit;
using UnitTestEx;
using UnitTestEx.Expectations;
using CoreEx.TestApi;
using CoreEx.TestApi.Validation;
using CoreEx.TestFunction.Models;

namespace CoreEx.Test.Framework.UnitTesting
{
    [TestFixture]
    public class ValidationTest
    {
        [Test]
        public void Validate_Error()
        {
            GenericTester.Create<Startup>()
                .ExpectErrors("Name is required.", "Price must be between 0 and 100.")
                .Validation().With<ProductValidator, Product>(new Product { Price = 450.95m })
                .AssertErrors("Name is required.", "Price must be between 0 and 100.");
        }

        [Test]
        public void Validate_OK()
        {
            GenericTester.Create<Startup>()
                .ExpectSuccess()
                .Validation().With<ProductValidator, Product>(new Product { Id = "abc", Name = "xyz", Price = 50.95m });
        }
    }
}