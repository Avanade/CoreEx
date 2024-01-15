using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CoreEx.Http;
using CoreEx.RefData;
using NUnit.Framework;
using HttpRequestOptions = CoreEx.Http.HttpRequestOptions;

namespace CoreEx.Test.Framework.Http
{
    [TestFixture]
    public class TypedHttpClientBaseTest
    {
        [Test]
        public void UpdateRequestUri_FormatArgNone()
        {
            var uri = new TestHttpClient().VerifyUri("product/88");
            Assert.That(uri, Is.EqualTo("/product/88"));
        }

        [Test]
        public void UpdateRequestUri_FormatArgNull()
        {
            var uri = new TestHttpClient().VerifyUri("product/{id}");
            Assert.That(uri, Is.EqualTo("/product/"));

            uri = new TestHttpClient().VerifyUri("product/{id}/other");
            Assert.That(uri, Is.EqualTo("/product//other"));

            uri = new TestHttpClient().VerifyUri("product/{}");
            Assert.That(uri, Is.EqualTo("/product/"));

            uri = new TestHttpClient().VerifyUri("product/{}/other");
            Assert.That(uri, Is.EqualTo("/product//other"));
        }

        [Test]
        public void UpdateRequestUri_FormatArgValue()
        {
            var arg = new HttpArg<int>("id", 88);
            var uri = new TestHttpClient().VerifyUri("product/{id}", null, arg);
            Assert.That(uri, Is.EqualTo("/product/88"));

            uri = new TestHttpClient().VerifyUri("product/{id}/other", null, arg);
            Assert.That(uri, Is.EqualTo("/product/88/other"));

            var arg2 = new HttpArg<string>("id", "&;");
            uri = new TestHttpClient().VerifyUri("product/{id}", null, arg2);
            Assert.That(uri, Is.EqualTo("/product/%26%3B"));

            arg2 = new HttpArg<string>("id", "abc");
            uri = new TestHttpClient().VerifyUri("product/{id}/other", null, arg2);
            Assert.That(uri, Is.EqualTo("/product/abc/other"));
        }

        [Test]
        public void UpdateRequestUri_QueryArg()
        {
            var arg = new HttpArg<int>("id", 88);
            var uri = new TestHttpClient().VerifyUri("product", null, arg);
            Assert.That(uri, Is.EqualTo("/product?id=88"));

            var arg2 = new HttpArg<string>("id", "abc");
            uri = new TestHttpClient().VerifyUri("product", null, arg2);
            Assert.That(uri, Is.EqualTo("/product?id=abc"));

            var arg3 = new HttpArg<DateTime>("id", new DateTime(2022, 01, 31, 08, 45, 59, DateTimeKind.Utc));
            uri = new TestHttpClient().VerifyUri("product", null, arg3);
            Assert.That(uri, Is.EqualTo("/product?id=2022-01-31T08%3a45%3a59.0000000Z"));
        }

        [Test]
        public void UpdateRequestUri_QueryArgs()
        {
            var uri = new TestHttpClient().VerifyUri("product", null,
                new HttpArg<int>("id", 88), new HttpArg<string>("text", "bananas"), new HttpArg<DateTime>("date", new DateTime(2020, 01, 01, 11, 59, 58, DateTimeKind.Utc)),
                new HttpArg<string>("body", "in_the_body_only", HttpArgType.FromBody),
                new HttpArg<int?>("id2", null), new HttpArg<HttpArgType>("type", HttpArgType.FromUri), new HttpArg<char[]>("char", new char[] { 'a', 'b', 'c' }), new HttpArg<Gender>("gender", new Gender { Id = 1, Code = "F", Text = "Female" }));

            Assert.That(uri, Is.EqualTo("/product?id=88&text=bananas&date=2020-01-01T11%3a59%3a58.0000000Z&type=FromUri&char=a&char=b&char=c&gender=F"));

            uri = new TestHttpClient().VerifyUri("product/{id}", null,
                new HttpArg<int>("id", 88), new HttpArg<string>("text", "bananas"), new HttpArg<DateTime>("date", new DateTime(2020, 01, 01, 11, 59, 58, DateTimeKind.Utc)),
                new HttpArg<string>("body", "in_the_body_only", HttpArgType.FromBody),
                new HttpArg<int?>("id2", null), new HttpArg<HttpArgType>("type", HttpArgType.FromUri), new HttpArg<char[]>("char", new char[] { 'a', 'b', 'c' }), new HttpArg<Gender>("gender", new Gender { Id = 1, Code = "F", Text = "Female" }));

            Assert.That(uri, Is.EqualTo("/product/88?text=bananas&date=2020-01-01T11%3a59%3a58.0000000Z&type=FromUri&char=a&char=b&char=c&gender=F"));
        }

        [Test]
        public void UpdateRequestUri_QueryArg_Class()
        {
            var arg = new HttpArg<Person>("person", new Person { Id = 88, Name = "gary", Date = new DateTime(2020, 01, 01, 11, 59, 58, DateTimeKind.Utc), Amount = -123.85m, Happy = true });
            var uri = new TestHttpClient().VerifyUri("people", null, arg);
            Assert.That(uri, Is.EqualTo("/people"));

            arg = new HttpArg<Person>("person", new Person { Id = 88, Name = "gary", Date = new DateTime(2020, 01, 01, 11, 59, 58, DateTimeKind.Utc), Amount = -123.85m, Happy = true, Codes = new List<int> { 0, 1, 2 } }, HttpArgType.FromUriUseProperties);
            uri = new TestHttpClient().VerifyUri("people", null, arg);
            Assert.That(uri, Is.EqualTo("/people?id=88&name=gary&date=2020-01-01T11%3a59%3a58.0000000Z&amount=-123.85&happy=true&codes=0&codes=1&codes=2"));

            arg = new HttpArg<Person>("person", new Person { Id = 88, Name = "*gary*", Date = new DateTime(2020, 01, 01, 11, 59, 58, DateTimeKind.Utc), Amount = -123.85m, Happy = true, Codes = new List<int> { 0, 1, 2 } }, HttpArgType.FromUriUseProperties);
            uri = new TestHttpClient().VerifyUri("people", null, arg);
            Assert.That(uri, Is.EqualTo("/people?id=88&name=*gary*&date=2020-01-01T11%3a59%3a58.0000000Z&amount=-123.85&happy=true&codes=0&codes=1&codes=2"));

            arg = new HttpArg<Person>("person", new Person { Id = 88, Name = "gary", Date = new DateTime(2020, 01, 01, 11, 59, 58, DateTimeKind.Unspecified), Amount = -123.85m, Happy = true, Codes = new List<int> { 0, 1, 2 } }, HttpArgType.FromUriUsePropertiesAndPrefix);
            uri = new TestHttpClient().VerifyUri("people", null, arg);
            Assert.That(uri, Is.EqualTo("/people?person.id=88&person.name=gary&person.date=2020-01-01T11%3a59%3a58.0000000&person.amount=-123.85&person.happy=true&person.codes=0&person.codes=1&person.codes=2"));
        }

        [Test]
        public void UpdateRequestUri_QueryArg_With_IFormattable()
        {
            var arg = new HttpArg<Coordinates>("coordinates", new Coordinates());
            var uri = new TestHttpClient().VerifyUri("map", null, arg);
            Assert.That(uri, Is.EqualTo("/map?coordinates=1%2c2"));

            var arg2 = new HttpArg<CoordinatesArgs>("ignored", new CoordinatesArgs(), HttpArgType.FromUriUseProperties);
            var uri2 = new TestHttpClient().VerifyUri("map", null, arg2);
            Assert.That(uri2, Is.EqualTo("/map?coordinates=1%2c2"));
        }

        public class Person
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public DateTime Date { get; set; }
            public decimal? Amount { get; set; }
            public bool Happy { get; set; }
            public List<int>? Codes { get; set; }
        }

        public class Gender : ReferenceDataBase<int> { }

        public class Coordinates : IFormattable
        {
            public int Long { get; set; } = 1;
            public int Lat { get; set; } = 2;

            public string ToString(string? format, IFormatProvider? formatProvider) => $"{Long},{Lat}";
        }

        public class CoordinatesArgs
        {
            public Coordinates? Coordinates { get; set; } = new Coordinates();
        }
    }

    public class TestHttpClient : TypedHttpClientBase
    {
        public TestHttpClient() : base(new HttpClient { BaseAddress = new Uri("https://unittest") }, new CoreEx.Text.Json.JsonSerializer()) { }

        public string VerifyUri(string requestUri, HttpRequestOptions? requestOptions = null, params IHttpArg[] args)
        {
            var request = CreateRequestAsync(HttpMethod.Post, requestUri, requestOptions, args).Result;
            return request.RequestUri!.ToString();
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}