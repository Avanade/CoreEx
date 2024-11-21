using NUnit.Framework;
using UnitTestEx;
using UnitTestEx.Expectations;
using CoreEx;
using CoreEx.TestApi;
using CoreEx.TestApi.Validation;
using CoreEx.TestFunction.Models;
using CoreEx.Localization;
using CoreEx.Validation;

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

        [Test]
        public void Validate_OK_Provide()
        {
            GenericTester.Create<Startup>()
                .ExpectSuccess()
                .Validation().With(new ProductValidator(), new Product { Id = "abc", Name = "xyz", Price = 50.95m });
        }

        [Test]
        public void Validate_Error_Provide()
        {
            GenericTester.Create()
                .ReplaceSingleton<ITextProvider, ValidationTextProvider>()
                .ExpectErrors("Name is required.", "Price must be between 0 and 100.")
                .Validation().With(new ProductValidator(), new Product { Price = 450.95m });
        }
    }
}