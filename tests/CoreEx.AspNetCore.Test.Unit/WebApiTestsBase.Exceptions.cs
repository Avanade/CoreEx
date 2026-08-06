using CoreEx.Localization;
using System.Net;
using System.Net.Mime;

namespace CoreEx.AspNetCore.Test.Unit;

partial class WebApiTestsBase<TWebApi, TResult>
{
    [TestCase(typeof(AuthenticationException), HttpStatusCode.Unauthorized, "authentication")]
    [TestCase(typeof(AuthorizationException), HttpStatusCode.Forbidden, "authorization")]
    [TestCase(typeof(BusinessException), HttpStatusCode.BadRequest, "business")]
    [TestCase(typeof(ConcurrencyException), HttpStatusCode.PreconditionFailed, "concurrency")]
    [TestCase(typeof(ConflictException), HttpStatusCode.Conflict, "conflict")]
    [TestCase(typeof(DataConsistencyException), HttpStatusCode.InternalServerError, "data-consistency")]
    [TestCase(typeof(DuplicateException), HttpStatusCode.Conflict, "duplicate")]
    [TestCase(typeof(NotFoundException), HttpStatusCode.NotFound, "not-found")]
    [TestCase(typeof(TransientException), HttpStatusCode.ServiceUnavailable, "transient")]
    [TestCase(typeof(ValidationException), HttpStatusCode.BadRequest, "validation")]
    [TestCase(typeof(InvalidOperationException), HttpStatusCode.InternalServerError)]
    public void Exception_ProblemHandling(Type type, HttpStatusCode statusCode, string? errorType = null, string? errorCode = null)
    {
        List<string> paths = ["type", "title", "traceid"];

        var ex = type == typeof(BusinessException) ? new BusinessException("Biz") : (Exception)Activator.CreateInstance(type, null)!;
        if (ex is CoreEx.Abstractions.ExtendedException eex && eex.IsError)
        {
            if (!string.IsNullOrEmpty(errorCode))
                eex.ErrorCode = errorCode;
            else
                paths.Add("errorCode");
        }
        else
        {
            paths.Add("errorCode");
            paths.Add("errorType");
            paths.Add("detail");
        }

        Test.Type<TWebApi>()
            .Run(async w =>
            {
                w.ConvertUnhandledExceptionsToProblemDetails = true; // Ensure enabled for unit-testing.
                return await w.PostAsync(Test.CreateHttpRequest(HttpMethod.Post, "test"), (ro, ct) => throw ex);
            })
            .ToHttpResponseMessageAssertor()
            .Assert(statusCode)
            .AssertContentType(MediaTypeNames.Application.ProblemJson)
            .AssertJson($"{{\"title\":\"{ex.Message}\",\"status\":{(int)statusCode},\"errorType\":\"{errorType}\",\"errorCode\":\"{errorCode}\"}}", pathsToIgnore: [.. paths]);
    }

    [Test]
    public void Exception_OwnTokenCancellation_BypassesConvertToProblemDetails()
    {
        // Regression: a cancellation attributable to *this* request's own cancellationToken (e.g. a client disconnect) must bubble up unclassified - even with
        // ConvertUnhandledExceptionsToProblemDetails enabled - rather than being logged as an unhandled error and converted into a 500 the client can never receive.
        using var cts = new CancellationTokenSource();

        Test.Type<TWebApi>()
            .Run(async w =>
            {
                w.ConvertUnhandledExceptionsToProblemDetails = true;
                return await w.PostAsync(Test.CreateHttpRequest(HttpMethod.Post, "test"), (ro, ct) => throw new OperationCanceledException(ct), cancellationToken: cts.Token);
            })
            .AssertException<OperationCanceledException>();
    }

    [Test]
    public void Exception_UnrelatedTokenCancellation_StillConvertedToProblemDetails()
    {
        // Regression (precision check): the own-token bypass above must not blanket-exclude every OperationCanceledException - one tied to an unrelated token
        // (e.g. a deliberate timeout on a downstream call) is still a real error and must still be converted when the flag is enabled.
        using var cts = new CancellationTokenSource();
        using var unrelatedCts = new CancellationTokenSource();

        Test.Type<TWebApi>()
            .Run(async w =>
            {
                w.ConvertUnhandledExceptionsToProblemDetails = true;
                return await w.PostAsync(Test.CreateHttpRequest(HttpMethod.Post, "test"), (ro, ct) => throw new OperationCanceledException(unrelatedCts.Token), cancellationToken: cts.Token);
            })
            .ToHttpResponseMessageAssertor()
            .Assert(HttpStatusCode.InternalServerError);
    }
}