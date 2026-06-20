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
    public void Delete_Response_Success()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.DeleteAsync<int>(Test.CreateHttpRequest(HttpMethod.Delete), (ro, ct) => Task.FromResult(123)))
            .ToHttpResponseMessageAssertor()
            .AssertOK()
            .AssertValue(123);
    }
}