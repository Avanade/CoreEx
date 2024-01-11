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

            Assert.Multiple(() =>
            {
                Assert.That(sp.GetService<ProductValidator>(), Is.Not.Null);
                Assert.That(sp.GetService<FV.IValidator<Product>>(), Is.Null);
                Assert.That(sp.GetService<CoreEx.Validation.IValidator<Product>>(), Is.Null);
            });

            var pv = sp.GetRequiredService<ProductValidator>();
            var cv = pv.Wrap();

            var rs = await cv.ValidateAsync(new Product());
            Assert.That(rs.HasErrors, Is.True);

            rs = await cv.ValidateAsync(new Product { Id = "XXX", Name = "Blah", Price = 1.5m });
            Assert.That(rs.HasErrors, Is.False);
        }

        [Test]
        public async Task ServiceCollectionAddWithInterfaces()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddFluentValidators<ProductValidator>(alsoRegisterInterfaces: true);

            var sp = sc.BuildServiceProvider();

            Assert.Multiple(() =>
            {
                Assert.That(sp.GetService<ProductValidator>(), Is.Not.Null);
                Assert.That(sp.GetService<FV.IValidator<Product>>(), Is.Not.Null);
                Assert.That(sp.GetService<CoreEx.Validation.IValidator<Product>>(), Is.Not.Null);
            });

            var iv = sp.GetRequiredService<CoreEx.Validation.IValidator<Product>>();
            Assert.That(iv, Is.Not.Null);

            var pv = sp.GetRequiredService<ProductValidator>();
            var cv = pv.Wrap();

            var rs = await cv.ValidateAsync(new Product());
            Assert.That(rs.HasErrors, Is.True);

            rs = await cv.ValidateAsync(new Product { Id = "XXX", Name = "Blah", Price = 1.5m });
            Assert.That(rs.HasErrors, Is.False);
        }

        [Test]
        public void CreateValidator()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddFluentValidators<ProductValidator>();

            var sp = sc.BuildServiceProvider();

            var pv = FluentValidator.Create<ProductValidator>(sp);
            Assert.That(pv, Is.Not.Null);
        }

        [Test]
        public void CreateValidatorFromInterface()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddFluentValidators<ProductValidator>(alsoRegisterInterfaces: true);

            var sp = sc.BuildServiceProvider();

            var pv = FluentValidator.Create<FV.IValidator<Product>>(sp);
            Assert.That(pv, Is.Not.Null);

            Assert.That(pv.Wrap(), Is.Not.Null);
        }

        [Test]
        public async Task InteropTest()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddFluentValidators<ProductValidator>();

            var sp = sc.BuildServiceProvider();

            var rs = await new Product().Validate().Mandatory().Interop(FluentValidator.Create<ProductValidator>(sp).Wrap()).ValidateAsync();
            Assert.That(rs.HasErrors, Is.True);

            rs = await new Product { Id = "XXX", Name = "Blah", Price = 1.5m }.Validate().Mandatory().Interop(FluentValidator.Create<ProductValidator>(sp).Wrap()).ValidateAsync();
            Assert.That(rs.HasErrors, Is.False);
        }

        [Test]
        public async Task InteropFuncTest()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddFluentValidators<ProductValidator>();

            var sp = sc.BuildServiceProvider();

            var rs = await new Product().Validate().Mandatory().Interop(FluentValidator.Create<ProductValidator>(sp).Wrap()).ValidateAsync();
            Assert.That(rs.HasErrors, Is.True);

            rs = await new Product { Id = "XXX", Name = "Blah", Price = 1.5m }.Validate().Mandatory().Interop(() => FluentValidator.Create<ProductValidator>(sp).Wrap()).ValidateAsync();
            Assert.That(rs.HasErrors, Is.False);
        }
    }
}