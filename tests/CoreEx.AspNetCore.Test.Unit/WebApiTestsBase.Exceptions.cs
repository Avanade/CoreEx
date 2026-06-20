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
}