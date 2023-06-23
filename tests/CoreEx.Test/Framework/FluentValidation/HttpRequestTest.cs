﻿using CoreEx.AspNetCore.Http;
using CoreEx.FluentValidation;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Models;
using CoreEx.TestFunction.Validators;
using NUnit.Framework;
using System.Net.Http;
using System.Threading.Tasks;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.FluentValidation
{
    [TestFixture]
    public class HttpRequestTest
    {
        [Test]
        public async Task NoBody_NoValidation()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Get, "");
            var vr = await hr.ReadAsJsonValueAsync(new Text.Json.JsonSerializer(), valueIsRequired: false, validator: new ProductValidator().Wrap()).ConfigureAwait(false);
            Assert.IsNotNull(vr);
            Assert.IsTrue(vr.IsValid);
        }

        [Test]
        public async Task NoBody_ValueRequired()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Get, "");
            var vr = await hr.ReadAsJsonValueAsync(new Text.Json.JsonSerializer(), valueIsRequired: true, validator: new ProductValidator().Wrap()).ConfigureAwait(false);
            Assert.IsNotNull(vr);
            Assert.IsFalse(vr.IsValid);
        }

        [Test]
        public async Task Value_Error()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateJsonHttpRequest(HttpMethod.Get, "", new Product { Id = "B2TF", Name = "DeLorean", Price = 88m });
            var vr = await hr.ReadAsJsonValueAsync(new Text.Json.JsonSerializer(), validator: new ProductValidator().Wrap()).ConfigureAwait(false);
            Assert.IsNotNull(vr);
            Assert.IsFalse(vr.IsValid);
            Assert.IsNotNull(vr.ValidationException);

            Assert.That(vr.ValidationException, Is.Not.Null.And.TypeOf<ValidationException>());
            var vex = (ValidationException)vr.ValidationException!;
            Assert.IsNotNull(vex.Messages);
            Assert.AreEqual(1, vex.Messages!.Count);
            Assert.AreEqual("A DeLorean cannot be priced at 88 as that could cause a chain reaction that would unravel the very fabric of the space-time continuum and destroy the entire universe.", vex.Messages!.GetMessagesForProperty(nameof(Product.Name))[0].Text);
        }

        [Test]
        public async Task Value_Success()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateJsonHttpRequest(HttpMethod.Get, "", new Product { Id = "B2TF", Name = "DeLorean", Price = 66m });
            var vr = await hr.ReadAsJsonValueAsync(new Text.Json.JsonSerializer(), validator: new ProductValidator().Wrap()).ConfigureAwait(false);
            Assert.IsNotNull(vr);
            Assert.IsTrue(vr.IsValid);
        }
    }
}