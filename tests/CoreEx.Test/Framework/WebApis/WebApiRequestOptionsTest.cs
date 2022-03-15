using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.TestFunction;
using NUnit.Framework;
using System;
using System.Net.Http;
using UnitTestEx.NUnit;

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

            Assert.NotNull(wro);
            Assert.AreSame(hr, wro.Request);
            Assert.IsNull(wro.ETag);
            Assert.IsFalse(wro.IncludeText);
            Assert.IsFalse(wro.IncludeInactive);
            Assert.IsNull(wro.IncludeFields);
            Assert.IsNull(wro.ExcludeFields);
            Assert.IsNull(wro.Paging);
        }

        [Test]
        public void GetRequestOptions_Configured()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest");
            var ro = new HttpRequestOptions { ETag = "etag-value", IncludeText = true, IncludeInactive = true, Paging = PagingArgs.CreateSkipAndTake(20, 25, true), UrlQueryString = "fruit=apples" }.Include("fielda", "fieldb").Exclude("fieldc");
            
            hr.ApplyRequestOptions(ro);
            Assert.NotNull("?$skip=20&$take=25&$count=true&$fields=fielda,fieldb&$exclude=fieldc&$text=true&$inactive=true&fruit=apples", hr.QueryString.Value);

            var wro = hr.GetRequestOptions();

            Assert.NotNull(wro);
            Assert.AreSame(hr, wro.Request);
            Assert.AreEqual("\"etag-value\"", wro.ETag);
            Assert.IsTrue(wro.IncludeText);
            Assert.IsTrue(wro.IncludeInactive);
            Assert.AreEqual(new string[] { "fielda", "fieldb" }, wro.IncludeFields);
            Assert.AreEqual(new string[] { "fieldc" }, wro.ExcludeFields);
            Assert.NotNull(wro.Paging);
            Assert.AreEqual(20, wro.Paging.Skip);
            Assert.AreEqual(25, wro.Paging.Take);
            Assert.IsTrue(wro.Paging.IsGetCount);
        }
    }
}