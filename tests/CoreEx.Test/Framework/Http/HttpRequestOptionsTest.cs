using CoreEx.Entities;
using CoreEx.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using NUnit.Framework;
using System.Linq;
using System.Net.Http;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.Http
{
    [TestFixture]
    public class HttpRequestOptionsTest
    {
        [Test]
        public void IncludeAndExcludeFields()
        {
            var hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            var ro = new HttpRequestOptions().Include("fielda", "fieldb").Exclude("fieldc");
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual("https://unittest/testing?$fields=fielda,fieldb&$exclude=fieldc", hr.RequestUri.AbsoluteUri);

            hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing?id=123");
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual("https://unittest/testing?id=123&$fields=fielda,fieldb&$exclude=fieldc", hr.RequestUri.AbsoluteUri);

            hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing?id=123");
            ro.Exclude("<app& le>");
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual("https://unittest/testing?id=123&$fields=fielda,fieldb&$exclude=fieldc,%3Capp%26%20le%3E", hr.RequestUri.AbsoluteUri);
        }

        [Test]
        public void Paging()
        {
            var hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            var ro = new HttpRequestOptions() { Paging = PagingArgs.CreateSkipAndTake(20, 25) };
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual("https://unittest/testing?$skip=20&$take=25", hr.RequestUri.AbsoluteUri);

            hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            ro = new HttpRequestOptions() { Paging = PagingArgs.CreateSkipAndTake(20, 25, true) };
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual("https://unittest/testing?$skip=20&$take=25&$count=true", hr.RequestUri.AbsoluteUri);

            hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            ro = new HttpRequestOptions() { Paging = PagingArgs.CreatePageAndSize(2, 25) };
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual("https://unittest/testing?$page=2&$size=25", hr.RequestUri.AbsoluteUri);

            hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            ro = new HttpRequestOptions() { Paging = PagingArgs.CreatePageAndSize(2, 25, true) };
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual("https://unittest/testing?$page=2&$size=25&$count=true", hr.RequestUri.AbsoluteUri);
        }

        [Test]
        public void ETag()
        {
            var hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            var ro = new HttpRequestOptions() { ETag = "abc" };
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual(0, hr.Headers.IfMatch.Count);
            Assert.AreEqual(1, hr.Headers.IfNoneMatch.Count);
            Assert.AreEqual("\"abc\"", hr.Headers.IfNoneMatch.First().Tag);

            hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            ro = new HttpRequestOptions() { ETag = "\"abc\"" };
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual(0, hr.Headers.IfMatch.Count);
            Assert.AreEqual(1, hr.Headers.IfNoneMatch.Count);
            Assert.AreEqual("\"abc\"", hr.Headers.IfNoneMatch.First().Tag);

            hr = new HttpRequestMessage(HttpMethod.Head, "https://unittest/testing");
            ro = new HttpRequestOptions() { ETag = "abc" };
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual(0, hr.Headers.IfMatch.Count);
            Assert.AreEqual(1, hr.Headers.IfNoneMatch.Count);
            Assert.AreEqual("\"abc\"", hr.Headers.IfNoneMatch.First().Tag);

            hr = new HttpRequestMessage(HttpMethod.Post, "https://unittest/testing");
            ro = new HttpRequestOptions() { ETag = "abc" };
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual(1, hr.Headers.IfMatch.Count);
            Assert.AreEqual(0, hr.Headers.IfNoneMatch.Count);
            Assert.AreEqual("\"abc\"", hr.Headers.IfMatch.First().Tag);

            hr = new HttpRequestMessage(HttpMethod.Put, "https://unittest/testing");
            ro = new HttpRequestOptions() { ETag = "abc" };
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual(1, hr.Headers.IfMatch.Count);
            Assert.AreEqual(0, hr.Headers.IfNoneMatch.Count);
            Assert.AreEqual("\"abc\"", hr.Headers.IfMatch.First().Tag);

            hr = new HttpRequestMessage(HttpMethod.Patch, "https://unittest/testing");
            ro = new HttpRequestOptions() { ETag = "abc" };
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual(1, hr.Headers.IfMatch.Count);
            Assert.AreEqual(0, hr.Headers.IfNoneMatch.Count);
            Assert.AreEqual("\"abc\"", hr.Headers.IfMatch.First().Tag);

            hr = new HttpRequestMessage(HttpMethod.Delete, "https://unittest/testing");
            ro = new HttpRequestOptions() { ETag = "abc" };
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual(1, hr.Headers.IfMatch.Count);
            Assert.AreEqual(0, hr.Headers.IfNoneMatch.Count);
            Assert.AreEqual("\"abc\"", hr.Headers.IfMatch.First().Tag);
        }

        [Test]
        public void IncludeText()
        {
            var hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing?fruit=apple");
            var ro = new HttpRequestOptions() { IncludeText = true };
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual("https://unittest/testing?fruit=apple&$text=true", hr.RequestUri.AbsoluteUri);
        }

        [Test]
        public void IncludeInActive()
        {
            var hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            var ro = new HttpRequestOptions() { IncludeInactive = true };
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual("https://unittest/testing?$inactive=true", hr.RequestUri.AbsoluteUri);
        }

        [Test]
        public void UrlQueryString()
        {
            var hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            var ro = new HttpRequestOptions() { UrlQueryString = "fruit=apple" };
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual("https://unittest/testing?fruit=apple", hr.RequestUri.AbsoluteUri);

            hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing?fruit=banana");
            ro = new HttpRequestOptions() { UrlQueryString = "fruit=apple" };
            hr.ApplyRequestOptions(ro);
            Assert.AreEqual("https://unittest/testing?fruit=banana&fruit=apple", hr.RequestUri.AbsoluteUri);
        }

        [Test]
        public void GetRequestOptionsFromQuery()
        {
            var hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            var ro = new HttpRequestOptions { IncludeText = true, IncludeInactive = true, Paging = PagingArgs.CreateSkipAndTake(20, 25, true) }.Include("fielda", "fieldb").Exclude("fieldc");
            hr.ApplyRequestOptions(ro);
            var ro2 = HttpRequestOptions.GetRequestOptions(new QueryCollection(QueryHelpers.ParseQuery(hr.RequestUri.Query)));
            ObjectComparer.Assert(ro, ro2);

            hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            ro = new HttpRequestOptions { IncludeText = true, IncludeInactive = true, Paging = PagingArgs.CreatePageAndSize(2, 15) }.Include("fielda", "fieldb").Exclude("fieldc");
            hr.ApplyRequestOptions(ro);
            ro2 = HttpRequestOptions.GetRequestOptions(new QueryCollection(QueryHelpers.ParseQuery(hr.RequestUri.Query)));
            ObjectComparer.Assert(ro, ro2);

            hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            ro = new HttpRequestOptions();
            hr.ApplyRequestOptions(ro);
            ro2 = HttpRequestOptions.GetRequestOptions(new QueryCollection(QueryHelpers.ParseQuery(hr.RequestUri.Query)));
            ObjectComparer.Assert(ro, ro2);
        }
    }
}