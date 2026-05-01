using CoreEx.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace CoreEx.AspNetCore.Test.Unit;

partial class WebApiTestsBase<TWebApi, TResult>
{
    [Test]
    public void MergePatch_Invalid_ContentType()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PatchAsync<Person>(Test.CreateHttpRequest(HttpMethod.Patch, "test", r => r.ContentType = MediaTypeNames.Text.Plain), (ro, ct) => throw new InvalidOperationException(), (ro, ct) => throw new InvalidOperationException()))
            .ToHttpResponseMessageAssertor()
            .Assert(HttpStatusCode.UnsupportedMediaType, "Unsupported 'Content-Type' for an HTTP PATCH; only JSON Merge Patch is supported using either: 'application/merge-patch+json' or 'application/json'.");
    }

    [Test]
    public void MergePatch_No_Body()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PatchAsync<Person>(Test.CreateHttpRequest(HttpMethod.Patch, "test", r => r.ContentType = HttpNames.MergePatchJsonMediaTypeName), (ro, ct) => throw new InvalidOperationException(), (ro, ct) => throw new InvalidOperationException()))
            .ToHttpResponseMessageAssertor()
            .AssertBadRequest()
            .AssertErrors(new ApiError("value", "Value is required."));
    }

    [Test]
    public void MergePatch_Invalid_Json()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PatchAsync<Person>(Test.CreateHttpRequest(HttpMethod.Patch, "test", "<xml/>", r => r.ContentType = HttpNames.MergePatchJsonMediaTypeName), (ro, ct) => throw new InvalidOperationException(), (ro, ct) => throw new InvalidOperationException()))
            .ToHttpResponseMessageAssertor()
            .AssertBadRequest()
            .AssertErrors(new ApiError("value", "Value is invalid: '<' is an invalid start of a value. Path: $ | LineNumber: 0 | BytePositionInLine: 0."));
    }

    [Test]
    public void MergePatch_Not_Found()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.PatchAsync<Person>(Test.CreateHttpRequest(HttpMethod.Patch, "test", "{}", r => r.ContentType = HttpNames.MergePatchJsonMediaTypeName), (ro, ct) => Task.FromResult<Person?>(null), (ro, ct) => throw new InvalidOperationException()))
            .ToHttpResponseMessageAssertor()
            .AssertNotFound();
    }

    [Test]
    public void MergePatch_Concurrency()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Patch, "test", """{"age": 30}""", r => r.ContentType = HttpNames.MergePatchJsonMediaTypeName);
        hr.Headers.IfMatch = new EntityTagHeaderValue("\"123456\"", true).ToString();

        Test.Type<TWebApi>()
            .Run(async w => await w.PatchAsync<Person>(hr, (ro, ct) => Task.FromResult<Person?>(Person.GetPerson("abcdefg")), (ro, ct) => throw new InvalidOperationException()))
            .ToHttpResponseMessageAssertor()
            .AssertPreconditionFailed();
    }

    [Test]
    public void MergePatch_No_Changes()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Patch, "test", """{"age": 30}""", r => r.ContentType = HttpNames.MergePatchJsonMediaTypeName);
        hr.Headers.IfMatch = new EntityTagHeaderValue("\"abcdef\"", true).ToString();

        // As there are no changes as a result of merging then the corresponding put operation should not be invoked; should just return as-if it had done something :-)
        Test.Type<TWebApi>()
            .Run(async w => await w.PatchAsync<Person>(hr, (ro, ct) => Task.FromResult<Person?>(Person.GetPerson("abcdef")), (ro, ct) => throw new InvalidOperationException()))
            .ToHttpResponseMessageAssertor()
            .AssertOK()
            .AssertValue(Person.GetPerson("abcdef"));
    }

    [Test]
    public void MergePatch_With_Changes()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Patch, "test", """{"age": 40}""", r => r.ContentType = HttpNames.MergePatchJsonMediaTypeName);
        hr.Headers.IfMatch = new EntityTagHeaderValue("\"abcdef\"", true).ToString();

        var ep = Person.GetPerson("qrstuv");
        ep.Age = 40;

        Test.Type<TWebApi>()
            .Run(async w => await w.PatchAsync<Person>(hr, (ro, ct) => Task.FromResult<Person?>(Person.GetPerson("abcdef")), (ro, ct) => Task.FromResult(ro.Value.Adjust(p => p.ETag = "qrstuv"))))
            .ToHttpResponseMessageAssertor()
            .AssertOK()
            .AssertValue(ep, "etag");
    }
}