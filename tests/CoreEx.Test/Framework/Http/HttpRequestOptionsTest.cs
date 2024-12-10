using CoreEx.Entities;
using CoreEx.Http;
using NUnit.Framework;
using System.Linq;
using System.Net.Http;
using HttpRequestOptions = CoreEx.Http.HttpRequestOptions;

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
            Assert.That(hr.RequestUri!.AbsoluteUri, Is.EqualTo("https://unittest/testing?$fields=fielda,fieldb&$exclude=fieldc"));

            hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing?id=123");
            hr.ApplyRequestOptions(ro);
            Assert.That(hr.RequestUri!.AbsoluteUri, Is.EqualTo("https://unittest/testing?id=123&$fields=fielda,fieldb&$exclude=fieldc"));

            hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing?id=123");
            ro.Exclude("<app& le>");
            hr.ApplyRequestOptions(ro);
            Assert.That(hr.RequestUri!.AbsoluteUri, Is.EqualTo("https://unittest/testing?id=123&$fields=fielda,fieldb&$exclude=fieldc,%3capp%26+le%3e"));
        }

        [Test]
        public void Paging()
        {
            var hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            var ro = HttpRequestOptions.Create(PagingArgs.CreateSkipAndTake(20, 25));
            hr.ApplyRequestOptions(ro);
            Assert.That(hr.RequestUri!.AbsoluteUri, Is.EqualTo("https://unittest/testing?$skip=20&$take=25"));

            hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            ro = HttpRequestOptions.Create(PagingArgs.CreateSkipAndTake(20, 25, true));
            hr.ApplyRequestOptions(ro);
            Assert.That(hr.RequestUri!.AbsoluteUri, Is.EqualTo("https://unittest/testing?$skip=20&$take=25&$count=true"));

            hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            ro = HttpRequestOptions.Create(PagingArgs.CreatePageAndSize(2, 25));
            hr.ApplyRequestOptions(ro);
            Assert.That(hr.RequestUri!.AbsoluteUri, Is.EqualTo("https://unittest/testing?$page=2&$size=25"));

            hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            ro = HttpRequestOptions.Create(PagingArgs.CreatePageAndSize(2, 25, true));
            hr.ApplyRequestOptions(ro);
            Assert.That(hr.RequestUri!.AbsoluteUri, Is.EqualTo("https://unittest/testing?$page=2&$size=25&$count=true"));
        }

        [Test]
        public void ETag()
        {
            var hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            var ro = new HttpRequestOptions() { ETag = "abc" };
            hr.ApplyRequestOptions(ro);
            Assert.Multiple(() =>
            {
                Assert.That(hr.Headers.IfMatch, Is.Empty);
                Assert.That(hr.Headers.IfNoneMatch, Has.Count.EqualTo(1));
            });
            Assert.That(hr.Headers.IfNoneMatch.First().Tag, Is.EqualTo("\"abc\""));

            hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            ro = new HttpRequestOptions() { ETag = "\"abc\"" };
            hr.ApplyRequestOptions(ro);
            Assert.Multiple(() =>
            {
                Assert.That(hr.Headers.IfMatch, Is.Empty);
                Assert.That(hr.Headers.IfNoneMatch, Has.Count.EqualTo(1));
            });
            Assert.That(hr.Headers.IfNoneMatch.First().Tag, Is.EqualTo("\"abc\""));

            hr = new HttpRequestMessage(HttpMethod.Head, "https://unittest/testing");
            ro = new HttpRequestOptions() { ETag = "abc" };
            hr.ApplyRequestOptions(ro);
            Assert.Multiple(() =>
            {
                Assert.That(hr.Headers.IfMatch, Is.Empty);
                Assert.That(hr.Headers.IfNoneMatch, Has.Count.EqualTo(1));
            });
            Assert.That(hr.Headers.IfNoneMatch.First().Tag, Is.EqualTo("\"abc\""));

            hr = new HttpRequestMessage(HttpMethod.Post, "https://unittest/testing");
            ro = new HttpRequestOptions() { ETag = "abc" };
            hr.ApplyRequestOptions(ro);
            Assert.Multiple(() =>
            {
                Assert.That(hr.Headers.IfMatch, Has.Count.EqualTo(1));
                Assert.That(hr.Headers.IfNoneMatch, Is.Empty);
            });
            Assert.That(hr.Headers.IfMatch.First().Tag, Is.EqualTo("\"abc\""));

            hr = new HttpRequestMessage(HttpMethod.Put, "https://unittest/testing");
            ro = new HttpRequestOptions() { ETag = "abc" };
            hr.ApplyRequestOptions(ro);
            Assert.Multiple(() =>
            {
                Assert.That(hr.Headers.IfMatch, Has.Count.EqualTo(1));
                Assert.That(hr.Headers.IfNoneMatch, Is.Empty);
            });
            Assert.That(hr.Headers.IfMatch.First().Tag, Is.EqualTo("\"abc\""));

            hr = new HttpRequestMessage(HttpMethod.Patch, "https://unittest/testing");
            ro = new HttpRequestOptions() { ETag = "abc" };
            hr.ApplyRequestOptions(ro);
            Assert.Multiple(() =>
            {
                Assert.That(hr.Headers.IfMatch, Has.Count.EqualTo(1));
                Assert.That(hr.Headers.IfNoneMatch, Is.Empty);
            });
            Assert.That(hr.Headers.IfMatch.First().Tag, Is.EqualTo("\"abc\""));

            hr = new HttpRequestMessage(HttpMethod.Delete, "https://unittest/testing");
            ro = new HttpRequestOptions() { ETag = "abc" };
            hr.ApplyRequestOptions(ro);
            Assert.Multiple(() =>
            {
                Assert.That(hr.Headers.IfMatch, Has.Count.EqualTo(1));
                Assert.That(hr.Headers.IfNoneMatch, Is.Empty);
            });
            Assert.That(hr.Headers.IfMatch.First().Tag, Is.EqualTo("\"abc\""));
        }

        [Test]
        public void IncludeText()
        {
            var hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing?fruit=apple");
            var ro = new HttpRequestOptions() { IncludeText = true };
            hr.ApplyRequestOptions(ro);
            Assert.That(hr.RequestUri!.AbsoluteUri, Is.EqualTo("https://unittest/testing?fruit=apple&$text=true"));
        }

        [Test]
        public void IncludeInActive()
        {
            var hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            var ro = new HttpRequestOptions() { IncludeInactive = true };
            hr.ApplyRequestOptions(ro);
            Assert.That(hr.RequestUri!.AbsoluteUri, Is.EqualTo("https://unittest/testing?$inactive=true"));
        }

        [Test]
        public void UrlQueryString()
        {
            var hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            var ro = new HttpRequestOptions() { UrlQueryString = "fruit=apple" };
            hr.ApplyRequestOptions(ro);
            Assert.That(hr.RequestUri!.AbsoluteUri, Is.EqualTo("https://unittest/testing?fruit=apple"));

            hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing?fruit=banana");
            ro = new HttpRequestOptions() { UrlQueryString = "fruit=apple" };
            hr.ApplyRequestOptions(ro);
            Assert.That(hr.RequestUri!.AbsoluteUri, Is.EqualTo("https://unittest/testing?fruit=banana&fruit=apple"));
        }

        [Test]
        public void QueryArgsQueryString()
        {
            QueryArgs qa = "name eq 'bob'";
            var hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            var ro = new HttpRequestOptions() { IncludeInactive = true }.Include("name", "text");
            ro = ro.WithQuery(qa);
            hr.ApplyRequestOptions(ro);
            Assert.That(hr.RequestUri!.AbsoluteUri, Is.EqualTo("https://unittest/testing?$filter=name+eq+%27bob%27&$fields=name,text&$inactive=true"));
        }

        [Test]
        public void QueryArgsQueryStringWithIncludeText()
        {
            var qa = QueryArgs.Create("name eq 'bob'").IncludeText();
            var hr = new HttpRequestMessage(HttpMethod.Get, "https://unittest/testing");
            var ro = new HttpRequestOptions() { IncludeInactive = true }.Include("name", "text");
            ro = ro.WithQuery(qa);
            hr.ApplyRequestOptions(ro);
            Assert.That(hr.RequestUri!.AbsoluteUri, Is.EqualTo("https://unittest/testing?$filter=name+eq+%27bob%27&$fields=name,text&$text=true&$inactive=true"));
        }
    }
}