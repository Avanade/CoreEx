using CoreEx.Results;

namespace CoreEx.AspNetCore.Test.Unit;

partial class WebApiTestsBase<TWebApi, TResult>
{
    [Test]
    public void DeleteWithResult_Success()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.DeleteWithResultAsync(Test.CreateHttpRequest(HttpMethod.Delete), (ro, ct) => Result.SuccessTask))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void DeleteWithResult_Not_Found()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.DeleteWithResultAsync(Test.CreateHttpRequest(HttpMethod.Delete), (ro, ct) => Result.NotFoundError().AsTask()))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void DeleteWithResult_Not_Found_Throw()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.DeleteWithResultAsync(Test.CreateHttpRequest(HttpMethod.Delete), (ro, ct) => throw new NotFoundException()))
            .ToHttpResponseMessageAssertor()
            .AssertNoContent();
    }

    [Test]
    public void DeleteWithResult_Response_Success()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.DeleteWithResultAsync<int>(Test.CreateHttpRequest(HttpMethod.Delete), (ro, ct) => Result.Go(123).AsTask()))
            .ToHttpResponseMessageAssertor()
            .AssertOK()
            .AssertValue(123);
    }
}