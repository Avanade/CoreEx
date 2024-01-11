using CoreEx.AspNetCore.Http;
using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.TestFunction;
using NUnit.Framework;
using System.Net.Http;
using UnitTestEx.NUnit;
using HttpRequestOptions = CoreEx.Http.HttpRequestOptions;

namespace CoreEx.Test.Framework.WebApis
{
    [TestFixture]
    public class WebApiRequestOptionsTest
    {
        [Test]
        public void GetRequestOptions_None()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest");

            var wro = hr.GetRequestOptions();

            Assert.That(wro, Is.Not.Null);
            Assert.That(wro.Request, Is.SameAs(hr));
            Assert.Multiple(() =>
            {
                Assert.That(wro.ETag, Is.Null);
                Assert.That(wro.IncludeText, Is.False);
                Assert.That(wro.IncludeInactive, Is.False);
                Assert.That(wro.IncludeFields, Is.Null);
                Assert.That(wro.ExcludeFields, Is.Null);
                Assert.That(wro.Paging, Is.Null);
            });
        }

        [Test]
        public void GetRequestOptions_Configured()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest");
            var ro = new HttpRequestOptions { ETag = "etag-value", IncludeText = true, IncludeInactive = true, Paging = PagingArgs.CreateSkipAndTake(20, 25, true), UrlQueryString = "fruit=apples" }.Include("fielda", "fieldb").Exclude("fieldc");
            
            hr.ApplyRequestOptions(ro);
            Assert.That(hr.QueryString.Value, Is.EqualTo("?$skip=20&$take=25&$count=true&$fields=fielda,fieldb&$exclude=fieldc&$text=true&$inactive=true&fruit=apples"));

            var wro = hr.GetRequestOptions();

            Assert.That(wro, Is.Not.Null);
            Assert.That(wro.Request, Is.SameAs(hr));
            Assert.Multiple(() =>
            {
                Assert.That(wro.ETag, Is.EqualTo("etag-value"));
                Assert.That(wro.IncludeText, Is.True);
                Assert.That(wro.IncludeInactive, Is.True);
                Assert.That(wro.IncludeFields, Is.EqualTo(new string[] { "fielda", "fieldb" }));
                Assert.That(wro.ExcludeFields, Is.EqualTo(new string[] { "fieldc" }));
                Assert.That(wro.Paging, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(wro.Paging!.Skip, Is.EqualTo(20));
                Assert.That(wro.Paging.Take, Is.EqualTo(25));
                Assert.That(wro.Paging.IsGetCount, Is.True);
            });
        }


        [Test]
        public void GetRequestOptions_Configured_TokenPaging()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest");
            var ro = new HttpRequestOptions { ETag = "etag-value", IncludeText = true, IncludeInactive = true, Paging = PagingArgs.CreateTokenAndTake("token", 25, true), UrlQueryString = "fruit=apples" }.Include("fielda", "fieldb").Exclude("fieldc");

            hr.ApplyRequestOptions(ro);
            Assert.That(hr.QueryString.Value, Is.EqualTo("?$token=token&$take=25&$count=true&$fields=fielda,fieldb&$exclude=fieldc&$text=true&$inactive=true&fruit=apples"));

            var wro = hr.GetRequestOptions();

            Assert.That(wro, Is.Not.Null);
            Assert.That(wro.Request, Is.SameAs(hr));
            Assert.Multiple(() =>
            {
                Assert.That(wro.ETag, Is.EqualTo("etag-value"));
                Assert.That(wro.IncludeText, Is.True);
                Assert.That(wro.IncludeInactive, Is.True);
                Assert.That(wro.IncludeFields, Is.EqualTo(new string[] { "fielda", "fieldb" }));
                Assert.That(wro.ExcludeFields, Is.EqualTo(new string[] { "fieldc" }));
                Assert.That(wro.Paging, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(wro.Paging!.Token, Is.EqualTo("token"));
                Assert.That(wro.Paging.Take, Is.EqualTo(25));
                Assert.That(wro.Paging.IsGetCount, Is.True);
            });
        }
    }
}