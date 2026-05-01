using CoreEx.Results;
using System.Net;
using System.Net.Mime;

namespace CoreEx.AspNetCore.Test.Unit;

partial class WebApiTestsBase<TWebApi, TResult>
{
    [Test]
    public void PatchWithResult_No_Body_No_Response()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PatchWithResultAsync(Test.CreateHttpRequest(HttpMethod.Patch), (ro, ct) => Task.FromResult(Result.Success)))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void PatchWithResult_Body_No_Response()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PatchWithResultAsync<Person>(Test.CreateJsonHttpRequest(HttpMethod.Patch, "test", Person.GetPerson("xx")), (ro, ct) =>
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
    public void PatchWithResult_Value_No_Response()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PatchWithResultAsync(Test.CreateHttpRequest(HttpMethod.Patch), Person.GetPerson("xx"), (ro, ct) =>
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
    public void PatchWithResult_No_Body_With_Response()
    {
        var person = Person.GetPerson("abcdefg");
        var hr = Test.CreateHttpRequest(HttpMethod.Patch);

        Test.Type<TWebApi>()
            .Run(async w => await w.PatchWithResultAsync<Person>(hr, (ro, ct) => Task.FromResult(Result.Ok(person))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertValue(person)
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"abcdefg\"");
    }

    [Test]
    public void PatchWithResult_No_Body_With_Null_Response()
    {
        var person = Person.GetPerson("abcdefg");

        Test.Type<TWebApi>()
            .Run(async w => await w.PatchWithResultAsync<Person>(Test.CreateHttpRequest(HttpMethod.Patch), (ro, ct) => Result<Person>.Ok(null!).AsTask()))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void PatchWithResult_Body_With_Response()
    {
        var hr = Test.CreateJsonHttpRequest(HttpMethod.Patch, "test", Person.GetPerson("xx"));
        Test.Type<TWebApi>()
            .Run(async w => await w.PatchWithResultAsync<Person, Person2>(hr, (ro, ct) =>
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
    public void PatchWithResult_Value_With_Response()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Patch);
        Test.Type<TWebApi>()
            .Run(async w => await w.PatchWithResultAsync<Person, Person2>(hr, Person.GetPerson("xx"), (ro, ct) =>
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