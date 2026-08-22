using System.Net;
using System.Net.Http.Headers;

namespace CoreEx.AspNetCore.Test.Unit;

partial class WebApiTestsBase<TWebApi, TResult>
{
    [Test]
    public void Delete_Success()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.DeleteAsync(Test.CreateHttpRequest(HttpMethod.Delete), (ro, ct) => Task.CompletedTask))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void Delete_Not_Found()
    {
        static Task NotFound(WebApiOptions ro, CancellationToken ct) => throw new NotFoundException();

        Test.Type<TWebApi>()
            .Run(async w => await w.DeleteAsync(Test.CreateHttpRequest(HttpMethod.Delete), NotFound))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void Delete_Not_Found_ConvertDisabled_ReturnsRealNotFound()
    {
        // Regression: ConvertNotfoundToDefaultStatusCodeOnDelete must be settable (previously get-only, permanently true) and actually honoured when disabled.
        static Task NotFound(WebApiOptions ro, CancellationToken ct) => throw new NotFoundException();

        Test.Type<TWebApi>()
            .Run(async w =>
            {
                w.ConvertNotfoundToDefaultStatusCodeOnDelete = false;
                return await w.DeleteAsync(Test.CreateHttpRequest(HttpMethod.Delete), NotFound);
            })
            .ToHttpResponseMessageAssertor()
            .AssertNotFound();
    }

    [Test]
    public void Delete_Response_Success()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.DeleteAsync<int>(Test.CreateHttpRequest(HttpMethod.Delete), (ro, ct) => Task.FromResult(123)))
            .ToHttpResponseMessageAssertor()
            .AssertOK()
            .AssertValue(123);
    }

    [Test]
    public void Delete_Response_IfMatchRequired_Missing()
    {
        // No body, no IETag request type — matches the real-world "no body" shape: ro.WithIfMatchRequired().ETag.
        Test.Type<TWebApi>()
            .Run(async w => await w.DeleteAsync<string?>(Test.CreateHttpRequest(HttpMethod.Delete), (ro, ct) => Task.FromResult(ro.WithIfMatchRequired().ETag)))
            .ToHttpResponseMessageAssertor()
            .Assert(HttpStatusCode.PreconditionRequired);
    }

    [Test]
    public void Delete_Response_IfMatchRequired_Present()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Delete);
        hr.Headers.IfMatch = new EntityTagHeaderValue("\"abcdefg\"", true).ToString();

        Test.Type<TWebApi>()
            .Run(async w => await w.DeleteAsync<string?>(hr, (ro, ct) => Task.FromResult(ro.WithIfMatchRequired().ETag)))
            .ToHttpResponseMessageAssertor()
            .AssertOK()
            .AssertValue("abcdefg");
    }
}