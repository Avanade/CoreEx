using System.Net;
using System.Net.Mime;

namespace CoreEx.AspNetCore.Test.Unit;

partial class WebApiTestsBase<TWebApi, TResult>
{
    [Test]
    public void Patch_No_Body_No_Response()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PatchAsync(Test.CreateHttpRequest(HttpMethod.Patch), (ro, ct) => Task.CompletedTask))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void Patch_Body_No_Response()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PatchAsync<Person>(Test.CreateJsonHttpRequest(HttpMethod.Patch, "test", Person.GetPerson("xx")), (ro, ct) =>
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
    public void Patch_Value_No_Response()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PatchAsync(Test.CreateHttpRequest(HttpMethod.Patch), Person.GetPerson("xx"), (ro, ct) =>
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
    public void Patch_No_Body_With_Response()
    {
        var person = Person.GetPerson("abcdefg");
        var hr = Test.CreateHttpRequest(HttpMethod.Patch);

        Test.Type<TWebApi>()
            .Run(async w => await w.PatchAsync<Person>(hr, (ro, ct) => Task.FromResult(person)))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertValue(person)
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"abcdefg\"");
    }

    [Test]
    public void Patch_No_Body_With_Null_Response()
    {
        var person = Person.GetPerson("abcdefg");

        Test.Type<TWebApi>()
            .Run(async w => await w.PatchAsync<Person?>(Test.CreateHttpRequest(HttpMethod.Patch), (ro, ct) => Task.FromResult<Person?>(null)))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void Patch_Body_With_Response()
    {
        var hr = Test.CreateJsonHttpRequest(HttpMethod.Patch, "test", Person.GetPerson("xx"));
        Test.Type<TWebApi>()
            .Run(async w => await w.PatchAsync<Person, Person2>(hr, (ro, ct) =>
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
    public void Patch_Value_With_Response()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Patch);
        Test.Type<TWebApi>()
            .Run(async w => await w.PatchAsync<Person, Person2>(hr, Person.GetPerson("xx"), (ro, ct) =>
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