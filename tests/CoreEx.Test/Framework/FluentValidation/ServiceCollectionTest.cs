using CoreEx.FluentValidation;
using CoreEx.TestFunction.Models;
using CoreEx.TestFunction.Validators;
using CoreEx.Validation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Threading.Tasks;
using FV = FluentValidation;

namespace CoreEx.Test.Framework.FluentValidation
{
    public class ServiceCollectionTest
    {
        [Test]
        public async Task ServiceCollectionAdd()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddFluentValidators<ProductValidator>(alsoRegisterInterfaces: false);

            var sp = sc.BuildServiceProvider();

            Assert.IsNotNull(sp.GetService<ProductValidator>());
            Assert.IsNull(sp.GetService<FV.IValidator<Product>>());
            Assert.IsNull(sp.GetService<CoreEx.Validation.IValidator<Product>>());

            var pv = sp.GetRequiredService<ProductValidator>();
            var cv = pv.Wrap();

            var rs = await cv.ValidateAsync(new Product());
            Assert.IsTrue(rs.HasErrors);

            rs = await cv.ValidateAsync(new Product { Id = "XXX", Name = "Blah", Price = 1.5m });
            Assert.IsFalse(rs.HasErrors);
        }

        [Test]
        public async Task ServiceCollectionAddWithInterfaces()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddFluentValidators<ProductValidator>(alsoRegisterInterfaces: true);

            var sp = sc.BuildServiceProvider();

            Assert.IsNotNull(sp.GetService<ProductValidator>());
            Assert.IsNotNull(sp.GetService<FV.IValidator<Product>>());
            Assert.IsNotNull(sp.GetService<CoreEx.Validation.IValidator<Product>>());

            var iv = sp.GetRequiredService<CoreEx.Validation.IValidator<Product>>();
            Assert.IsNotNull(iv);

            var pv = sp.GetRequiredService<ProductValidator>();
            var cv = pv.Wrap();

            var rs = await cv.ValidateAsync(new Product());
            Assert.IsTrue(rs.HasErrors);

            rs = await cv.ValidateAsync(new Product { Id = "XXX", Name = "Blah", Price = 1.5m });
            Assert.IsFalse(rs.HasErrors);
        }

        [Test]
        public void CreateValidator()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddFluentValidators<ProductValidator>();

            var sp = sc.BuildServiceProvider();

            var pv = FluentValidator.Create<ProductValidator>(sp);
            Assert.IsNotNull(pv);
        }

        [Test]
        public void CreateValidatorFromInterface()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddFluentValidators<ProductValidator>(alsoRegisterInterfaces: true);

            var sp = sc.BuildServiceProvider();

            var pv = FluentValidator.Create<FV.IValidator<Product>>(sp);
            Assert.IsNotNull(pv);

            Assert.NotNull(pv.Wrap());
        }

        [Test]
        public async Task InteropTest()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddFluentValidators<ProductValidator>();

            var sp = sc.BuildServiceProvider();

            var rs = await new Product().Validate().Mandatory().Interop(FluentValidator.Create<ProductValidator>(sp).Wrap()).ValidateAsync();
            Assert.IsTrue(rs.HasErrors);

            rs = await new Product { Id = "XXX", Name = "Blah", Price = 1.5m }.Validate().Mandatory().Interop(FluentValidator.Create<ProductValidator>(sp).Wrap()).ValidateAsync();
            Assert.IsFalse(rs.HasErrors);
        }

        [Test]
        public async Task InteropFuncTest()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddFluentValidators<ProductValidator>();

            var sp = sc.BuildServiceProvider();

            var rs = await new Product().Validate().Mandatory().Interop(FluentValidator.Create<ProductValidator>(sp).Wrap()).ValidateAsync();
            Assert.IsTrue(rs.HasErrors);

            rs = await new Product { Id = "XXX", Name = "Blah", Price = 1.5m }.Validate().Mandatory().Interop(() => FluentValidator.Create<ProductValidator>(sp).Wrap()).ValidateAsync();
            Assert.IsFalse(rs.HasErrors);
        }
    }
}