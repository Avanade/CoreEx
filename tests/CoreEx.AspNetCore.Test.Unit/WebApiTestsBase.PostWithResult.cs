using CoreEx.Results;
using System.Net;
using System.Net.Mime;

namespace CoreEx.AspNetCore.Test.Unit;

partial class WebApiTestsBase<TWebApi, TResult>
{
    [Test]
    public void PostWithResult_No_Body_No_Response()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PostWithResultAsync(Test.CreateHttpRequest(HttpMethod.Post), (ro, ct) => Task.FromResult(Result.Success)))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void PostWithResult_No_Body_No_Response_With_Location()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Post);
        Test.Type<TWebApi>()
            .Run(async w => await w.PostWithResultAsync(hr, (ro, ct) =>
            {
                ro.WithLocationUri(() => new Uri("test", UriKind.Relative));
                return Task.FromResult(Result.Success);
            }, HttpStatusCode.Created))
            .ToHttpResponseMessageAssertor(hr)
            .AssertCreated()
            .AssertLocationHeaderContains("test");
    }

    [Test]
    public void PostWithResult_Body_No_Response()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PostWithResultAsync<Person>(Test.CreateJsonHttpRequest(HttpMethod.Post, "test", Person.GetPerson()), (ro, ct) =>
            {
                ro.ValueOrDefault.Should().NotBeNull();
                ro.ValueOrDefault.FirstName.Should().Be("John");
                ro.ValueOrDefault.LastName.Should().Be("Doe");
                ro.ValueOrDefault.Age.Should().Be(30);
                return Result.SuccessTask;
            }))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void PostWithResult_Value_No_Response()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PostWithResultAsync(Test.CreateHttpRequest(HttpMethod.Post), Person.GetPerson(), (ro, ct) =>
            {
                ro.ValueOrDefault.Should().NotBeNull();
                ro.ValueOrDefault.FirstName.Should().Be("John");
                ro.ValueOrDefault.LastName.Should().Be("Doe");
                ro.ValueOrDefault.Age.Should().Be(30);
                return Result.SuccessTask;
            }))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void PostWithResult_No_Body_With_Response()
    {
        var person = Person.GetPerson("abcdefg");
        var hr = Test.CreateHttpRequest(HttpMethod.Post);

        Test.Type<TWebApi>()
            .Run(async w => await w.PostWithResultAsync<Person>(hr, (ro, ct) => Task.FromResult(Result.Ok(person))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertCreated()
            .AssertValue(person)
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"abcdefg\"");
    }

    [Test]
    public void PostWithResult_No_Body_With_Null_Response()
    {
        var person = Person.GetPerson("abcdefg");

        Test.Type<TWebApi>()
            .Run(async w => await w.PostWithResultAsync<Person>(Test.CreateHttpRequest(HttpMethod.Post), (ro, ct) => Result<Person>.Ok(null!).AsTask()))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void PostWithResult_Body_With_Response()
    {
        var hr = Test.CreateJsonHttpRequest(HttpMethod.Post, "test", Person.GetPerson());
        Test.Type<TWebApi>()
            .Run(async w => await w.PostWithResultAsync<Person, Person2>(hr, (ro, ct) =>
            {
                var p = new Person2
                {
                    LastName = ro.ValueOrDefault!.LastName + "X",
                    FirstName = ro.ValueOrDefault.FirstName + "Y",
                    Age = ro.ValueOrDefault.Age + 10,
                    ETag = "123456"
                };
                return Task.FromResult(Result.Ok(p));
            }))
            .ToHttpResponseMessageAssertor(hr)
            .AssertCreated()
            .AssertJson("""{"firstName":"JohnY","lastName":"DoeX","age":40,"etag":"123456"}""")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"123456\"");
    }

    [Test]
    public void PostWithResult_Value_With_Response()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Post);
        Test.Type<TWebApi>()
            .Run(async w => await w.PostWithResultAsync<Person, Person2>(hr, Person.GetPerson(), (ro, ct) =>
            {
                var p = new Person2
                {
                    LastName = ro.ValueOrDefault!.LastName + "X",
                    FirstName = ro.ValueOrDefault.FirstName + "Y",
                    Age = ro.ValueOrDefault.Age + 10,
                    ETag = "123456"
                };
                return Task.FromResult(Result.Ok(p));
            }))
            .ToHttpResponseMessageAssertor(hr)
            .AssertCreated()
            .AssertJson("""{"firstName":"JohnY","lastName":"DoeX","age":40,"etag":"123456"}""")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"123456\"");
    }
}