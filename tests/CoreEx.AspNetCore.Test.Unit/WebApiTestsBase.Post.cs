using CoreEx.Results;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace CoreEx.AspNetCore.Test.Unit;

partial class WebApiTestsBase<TWebApi, TResult>
{
    [Test]
    public void Post_No_Body_No_Response()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PostAsync(Test.CreateHttpRequest(HttpMethod.Post), (ro, ct) => Task.CompletedTask))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void Post_No_Body_No_Response_With_Location()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Post);
        Test.Type<TWebApi>()
            .Run(async w => await w.PostAsync(hr, (ro, ct) =>
            {
                ro.WithLocationUri(() => new Uri("test", UriKind.Relative));
                return Task.CompletedTask;
            }, HttpStatusCode.Created))
            .ToHttpResponseMessageAssertor(hr)
            .AssertCreated()
            .AssertLocationHeaderContains("test");
    }

    [Test]
    public void Post_Body_No_Response()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PostAsync<Person>(Test.CreateJsonHttpRequest(HttpMethod.Post, "test", Person.GetPerson()), (ro, ct) =>
            {
                ro.ValueOrDefault.Should().NotBeNull();
                ro.ValueOrDefault.FirstName.Should().Be("John");
                ro.ValueOrDefault.LastName.Should().Be("Doe");
                ro.ValueOrDefault.Age.Should().Be(30);
                return Task.CompletedTask;
            }))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void Post_Value_No_Response()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PostAsync(Test.CreateHttpRequest(HttpMethod.Post), Person.GetPerson(), (ro, ct) =>
            {
                ro.ValueOrDefault.Should().NotBeNull();
                ro.ValueOrDefault.FirstName.Should().Be("John");
                ro.ValueOrDefault.LastName.Should().Be("Doe");
                ro.ValueOrDefault.Age.Should().Be(30);
                return Task.CompletedTask;
            }))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void Post_No_Body_With_Response()
    {
        var person = Person.GetPerson("abcdefg");
        var hr = Test.CreateHttpRequest(HttpMethod.Post);

        Test.Type<TWebApi>()
            .Run(async w => await w.PostAsync<Person>(hr, (ro, ct) => Task.FromResult(person)))
            .ToHttpResponseMessageAssertor(hr)
            .AssertCreated()
            .AssertValue(person)
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"abcdefg\"");
    }

    [Test]
    public void Post_No_Body_With_Null_Response()
    {
        var person = Person.GetPerson("abcdefg");

        Test.Type<TWebApi>()
            .Run(async w => await w.PostAsync<Person>(Test.CreateHttpRequest(HttpMethod.Post), (ro, ct) => Task.FromResult((Person)null!)))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void Post_Body_With_Response()
    {
        var hr = Test.CreateJsonHttpRequest(HttpMethod.Post, "test", Person.GetPerson());
        Test.Type<TWebApi>()
            .Run(async w => await w.PostAsync<Person, Person2>(hr, (ro, ct) =>
            {
                var p = new Person2
                {
                    LastName = ro.ValueOrDefault!.LastName + "X",
                    FirstName = ro.ValueOrDefault.FirstName + "Y",
                    Age = ro.ValueOrDefault.Age + 10,
                    ETag = "123456"
                };
                return Task.FromResult(p);
            }))
            .ToHttpResponseMessageAssertor(hr)
            .AssertCreated()
            .AssertJson("""{"firstName":"JohnY","lastName":"DoeX","age":40,"etag":"123456"}""")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"123456\"");
    }

    [Test]
    public void Post_Value_With_Response()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Post);
        Test.Type<TWebApi>()
            .Run(async w => await w.PostAsync<Person, Person2>(hr, Person.GetPerson(), (ro, ct) =>
            {
                var p = new Person2
                {
                    LastName = ro.ValueOrDefault!.LastName + "X",
                    FirstName = ro.ValueOrDefault.FirstName + "Y",
                    Age = ro.ValueOrDefault.Age + 10,
                    ETag = "123456"
                };
                return Task.FromResult(p);
            }))
            .ToHttpResponseMessageAssertor(hr)
            .AssertCreated()
            .AssertJson("""{"firstName":"JohnY","lastName":"DoeX","age":40,"etag":"123456"}""")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"123456\"");
    }

    [Test]
    public void Post_Body_With_Response_IfMatchRequired_Missing()
    {
        // No If-Match header supplied — ro.WithIfMatchRequired() must throw before the response is constructed.
        var hr = Test.CreateJsonHttpRequest(HttpMethod.Post, "test", Person.GetPerson());

        Test.Type<TWebApi>()
            .Run(async w => await w.PostAsync<Person, Person2>(hr, (ro, ct) => Task.FromResult(new Person2 { FirstName = ro.WithIfMatchRequired().Value.FirstName }), HttpStatusCode.OK))
            .ToHttpResponseMessageAssertor()
            .Assert(HttpStatusCode.PreconditionRequired);
    }

    [Test]
    public void Post_Body_With_Response_IfMatchRequired_Present()
    {
        var hr = Test.CreateJsonHttpRequest(HttpMethod.Post, "test", Person.GetPerson());
        hr.Headers.IfMatch = new EntityTagHeaderValue("\"abcdefg\"", true).ToString();

        Test.Type<TWebApi>()
            .Run(async w => await w.PostAsync<Person, Person2>(hr, (ro, ct) => Task.FromResult(new Person2
            {
                // Inline chain — matches real-world controller usage: ro.WithIfMatchRequired().Value.
                FirstName = ro.WithIfMatchRequired().Value.FirstName,
                LastName = ro.Value.LastName,
                Age = ro.Value.Age,
                ETag = "123456"
            }), HttpStatusCode.OK))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertJson("""{"firstName":"John","lastName":"Doe","age":30,"etag":"123456"}""")
            .AssertETagHeader("\"123456\"");
    }
}