﻿using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.RefData;
using CoreEx.RefData.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Http
{
    [TestFixture]
    public class TypedHttpClientBaseTest
    {
        [Test]
        public void UpdateRequestUri_FormatArgNone()
        {
            var uri = new TestHttpClient().VerifyUri("product/88");
            Assert.AreEqual("/product/88", uri);
        }

        [Test]
        public void UpdateRequestUri_FormatArgNull()
        {
            var uri = new TestHttpClient().VerifyUri("product/{id}");
            Assert.AreEqual("/product/", uri);

            uri = new TestHttpClient().VerifyUri("product/{id}/other");
            Assert.AreEqual("/product//other", uri);

            uri = new TestHttpClient().VerifyUri("product/{}");
            Assert.AreEqual("/product/", uri);

            uri = new TestHttpClient().VerifyUri("product/{}/other");
            Assert.AreEqual("/product//other", uri);
        }

        [Test]
        public void UpdateRequestUri_FormatArgValue()
        {
            var arg = new HttpArg<int>("id", 88);
            var uri = new TestHttpClient().VerifyUri("product/{id}", null, arg);
            Assert.AreEqual("/product/88", uri);

            uri = new TestHttpClient().VerifyUri("product/{id}/other", null, arg);
            Assert.AreEqual("/product/88/other", uri);

            var arg2 = new HttpArg<string>("id", "&;");
            uri = new TestHttpClient().VerifyUri("product/{id}", null, arg2);
            Assert.AreEqual("/product/%26%3B", uri);
        }

        [Test]
        public void UpdateRequestUri_QueryArg()
        {
            var arg = new HttpArg<int>("id", 88);
            var uri = new TestHttpClient().VerifyUri("product", null, arg);
            Assert.AreEqual("/product?id=88", uri);
        }

        [Test]
        public void UpdateRequestUri_QueryArgs()
        {
            var uri = new TestHttpClient().VerifyUri("product", null, 
                new HttpArg<int>("id", 88), new HttpArg<string>("text", "bananas"), new HttpArg<DateTime>("date", new DateTime(2020, 01, 01, 11, 59, 58, DateTimeKind.Utc)),
                new HttpArg<string>("body", "in_the_body_only", HttpArgType.FromBody),
                new HttpArg<int?>("id2", null), new HttpArg<HttpArgType>("type", HttpArgType.FromUri), new HttpArg<char[]>("char", new char[] { 'a', 'b', 'c' }), new HttpArg<Gender>("gender", new Gender { Id = 1, Code = "F", Text = "Female" }));

            Assert.AreEqual("/product?id=88&text=bananas&date=2020-01-01T11%3A59%3A58.0000000Z&type=FromUri&char=a&char=b&char=c&gender=F", uri);

            uri = new TestHttpClient().VerifyUri("product/{id}", null, null,
                new HttpArg<int>("id", 88), new HttpArg<string>("text", "bananas"), new HttpArg<DateTime>("date", new DateTime(2020, 01, 01, 11, 59, 58, DateTimeKind.Utc)),
                new HttpArg<string>("body", "in_the_body_only", HttpArgType.FromBody),
                new HttpArg<int?>("id2", null), new HttpArg<HttpArgType>("type", HttpArgType.FromUri), new HttpArg<char[]>("char", new char[] { 'a', 'b', 'c' }), new HttpArg<Gender>("gender", new Gender { Id = 1, Code = "F", Text = "Female" }));

            Assert.AreEqual("/product/88?text=bananas&date=2020-01-01T11%3A59%3A58.0000000Z&type=FromUri&char=a&char=b&char=c&gender=F", uri);
        }

        [Test]
        public void UpdateRequestUri_QueryArg_Class()
        {
            var arg = new HttpArg<Person>("person", new Person { Id = 88, Name = "gary", Date = new DateTime(2020, 01, 01, 11, 59, 58, DateTimeKind.Utc), Amount = -123.85m, Happy = true });
            var uri = new TestHttpClient().VerifyUri("people", null, arg);
            Assert.AreEqual("/people", uri);

            arg = new HttpArg<Person>("person", new Person { Id = 88, Name = "gary", Date = new DateTime(2020, 01, 01, 11, 59, 58, DateTimeKind.Utc), Amount = -123.85m, Happy = true, Codes = new List<int> { 0, 1, 2 } }, HttpArgType.FromUriUseProperties);
            uri = new TestHttpClient().VerifyUri("people", null, arg);
            Assert.AreEqual("/people?Id=88&Name=%22gary%22&Date=%222020-01-01T11%3A59%3A58Z%22&Amount=-123.85&Happy=true&Codes=0&Codes=1&Codes=2", uri);

            arg = new HttpArg<Person>("person", new Person { Id = 88, Name = "gary", Date = new DateTime(2020, 01, 01, 11, 59, 58, DateTimeKind.Utc), Amount = -123.85m, Happy = true, Codes = new List<int> { 0, 1, 2 } }, HttpArgType.FromUriUsePropertiesAndPrefix);
            uri = new TestHttpClient().VerifyUri("people", null, arg, null);
            Assert.AreEqual("/people?person.Id=88&person.Name=%22gary%22&person.Date=%222020-01-01T11%3A59%3A58Z%22&person.Amount=-123.85&person.Happy=true&person.Codes=0&person.Codes=1&person.Codes=2", uri);
        }

        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime Date { get; set; }
            public decimal? Amount { get; set; }
            public bool Happy { get; set; }
            public List<int> Codes { get; set; }
        }

        public class Gender : ReferenceDataBase<int> { }
    }

    public class TestHttpClient : TypedHttpClientBase
    {
        public TestHttpClient() : base(new HttpClient { BaseAddress = new Uri("https://unittest") }, new CoreEx.Text.Json.JsonSerializer()) { }

        public string VerifyUri(string requestUri, HttpRequestOptions requestOptions = null, params IHttpArg[] args)
        {
            var request = CreateRequestAsync(HttpMethod.Post, requestUri, requestOptions, args).Result;
            return request.RequestUri.ToString();
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}