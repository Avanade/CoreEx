using CoreEx.Results;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace CoreEx.AspNetCore.Test.Unit;

partial class WebApiTestsBase<TWebApi, TResult>
{
    [Test]
    public void PutWithResult_Body_No_Response()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PutWithResultAsync<Person>(Test.CreateJsonHttpRequest(HttpMethod.Put, "test", Person.GetPerson("xx")), (ro, ct) =>
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
    public void PutWithResult_Value_No_Response()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PutWithResultAsync(Test.CreateHttpRequest(HttpMethod.Put), Person.GetPerson("xx"), (ro, ct) =>
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
    public void PutWithResult_Body_With_Response()
    {
        var hr = Test.CreateJsonHttpRequest(HttpMethod.Put, "test", Person.GetPerson("xx"));
        Test.Type<TWebApi>()
            .Run(async w => await w.PutWithResultAsync<Person, Person2>(hr, (ro, ct) =>
            {
                var p = new Person2();
                p.LastName = ro.ValueOrDefault!.LastName + "X";
                p.FirstName = ro.ValueOrDefault.FirstName + "Y";
                p.Age = ro.ValueOrDefault.Age + 10;
                p.ETag = "123456";
                return Task.FromResult(Result.Ok(p));
            }))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertJson("""{"firstName":"JohnY","lastName":"DoeX","age":40,"etag":"123456"}""")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"123456\"");
    }

    [Test]
    public void PutWithResult_Value_With_Response()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Put);
        Test.Type<TWebApi>()
            .Run(async w => await w.PutWithResultAsync<Person, Person2>(hr, Person.GetPerson("xx"), (ro, ct) =>
            {
                var p = new Person2();
                p.LastName = ro.ValueOrDefault!.LastName + "X";
                p.FirstName = ro.ValueOrDefault.FirstName + "Y";
                p.Age = ro.ValueOrDefault.Age + 10;
                p.ETag = "123456";
                return Task.FromResult(Result.Ok(p));
            }))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertJson("""{"firstName":"JohnY","lastName":"DoeX","age":40,"etag":"123456"}""")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"123456\"");
    }

    [Test]
    public void PutWithResult_Body_No_Response_IfMatch()
    {
        var hr = Test.CreateJsonHttpRequest(HttpMethod.Put, "test", Person.GetPerson("123456"));
        hr.Headers.IfMatch = new EntityTagHeaderValue("\"abcdefg\"", true).ToString();

        Test.Type<TWebApi>()
            .Run(async w => await w.PutWithResultAsync<Person>(hr, (ro, ct) =>
            {
                ro.ValueOrDefault.Should().NotBeNull();
                ro.ValueOrDefault.FirstName.Should().Be("John");
                ro.ValueOrDefault.LastName.Should().Be("Doe");
                ro.ValueOrDefault.Age.Should().Be(30);
                ro.ValueOrDefault.ETag.Should().Be("abcdefg"); // ETag should have been overridden.
                ro.ETag.Should().Be("abcdefg");
                return Result.SuccessTask;
            }))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void PutWithResult_Value_No_Response_IfMatch()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Put);
        hr.Headers.IfMatch = new EntityTagHeaderValue("\"abcdefg\"", true).ToString();

        Test.Type<TWebApi>()
            .Run(async w => await w.PutWithResultAsync<Person>(hr, Person.GetPerson("123456"), (ro, ct) =>
            {
                ro.ValueOrDefault.Should().NotBeNull();
                ro.ValueOrDefault.FirstName.Should().Be("John");
                ro.ValueOrDefault.LastName.Should().Be("Doe");
                ro.ValueOrDefault.Age.Should().Be(30);
                ro.ValueOrDefault.ETag.Should().Be("abcdefg"); // ETag should have been overridden.
                ro.ETag.Should().Be("abcdefg");
                return Result.SuccessTask;
            }))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }
}