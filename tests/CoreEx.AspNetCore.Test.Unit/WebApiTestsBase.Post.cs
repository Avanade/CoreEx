using CoreEx.Results;
using System.Net;
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
}