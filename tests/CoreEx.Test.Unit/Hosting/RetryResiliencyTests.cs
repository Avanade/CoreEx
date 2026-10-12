using CoreEx.Hosting;
using CoreEx.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;

namespace CoreEx.Test.Unit.Hosting;

[TestFixture]
public class RetryResiliencyTests
{
    private static ResilienceContext CreateContext(TestOwner owner)
    {
        var ctx = ResilienceContextPool.Shared.Get();
        ctx.Properties.Set(ResilienceOwner<TestOwner>.PropertyKey, owner);
        return ctx;
    }

    [Test]
    public async Task Create_RetriesMatchingFailure_UntilSuccessWithinBudget()
    {
        var owner = new TestOwner();
        var pipeline = RetryResiliency<TestOwner>.Create(r => r.Error is TransientException, o => o.Logger, delay: TimeSpan.FromMilliseconds(1), maxRetryAttempts: 3);

        var ctx = CreateContext(owner);
        try
        {
            var attempts = 0;
            var result = await pipeline.ExecuteAsync(async _ =>
            {
                attempts++;
                return attempts < 3 ? Result.Fail(new TransientException()) : Result.Success;
            }, ctx);

            result.IsSuccess.Should().BeTrue();
            attempts.Should().Be(3);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(ctx);
        }
    }

    [Test]
    public async Task Create_NonMatchingFailure_IsNeverRetried()
    {
        var owner = new TestOwner();
        var pipeline = RetryResiliency<TestOwner>.Create(r => r.Error is TransientException, o => o.Logger, delay: TimeSpan.FromMilliseconds(1), maxRetryAttempts: 3);

        var ctx = CreateContext(owner);
        try
        {
            var attempts = 0;
            var result = await pipeline.ExecuteAsync(async _ =>
            {
                attempts++;
                return Result.Fail(new InvalidOperationException("not retry-worthy"));
            }, ctx);

            result.IsFailure.Should().BeTrue();
            attempts.Should().Be(1);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(ctx);
        }
    }

    [Test]
    public async Task Create_MatchingFailure_ExhaustsRetries_ThenFails()
    {
        var owner = new TestOwner();
        var pipeline = RetryResiliency<TestOwner>.Create(r => r.Error is TransientException, o => o.Logger, delay: TimeSpan.FromMilliseconds(1), maxRetryAttempts: 3);

        var ctx = CreateContext(owner);
        try
        {
            var attempts = 0;
            var result = await pipeline.ExecuteAsync(async _ =>
            {
                attempts++;
                return Result.Fail(new TransientException());
            }, ctx);

            result.IsFailure.Should().BeTrue();
            attempts.Should().Be(4); // The initial attempt plus 3 retries.
        }
        finally
        {
            ResilienceContextPool.Shared.Return(ctx);
        }
    }

    [Test]
    public async Task CreateOfTResult_TypedResult_RetriesMatchingFailure_UntilSuccessWithinBudget()
    {
        // Exercises the generalized Create<TResult>(...) overload directly (not just via the Create(...) forwarder) using Result<int> as TResult - confirms it carries a genuine payload
        // through a successful retry, unlike the payload-less Result overload above.
        var owner = new TestOwner();
        var pipeline = RetryResiliency<TestOwner>.Create<Result<int>>(r => r.Error is TransientException, o => o.Logger, delay: TimeSpan.FromMilliseconds(1), maxRetryAttempts: 3);

        var ctx = CreateContext(owner);
        try
        {
            var attempts = 0;
            var result = await pipeline.ExecuteAsync(async _ =>
            {
                attempts++;
                return attempts < 3 ? new Result<int>(new TransientException()) : new Result<int>(99);
            }, ctx);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(99);
            attempts.Should().Be(3);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(ctx);
        }
    }

    [Test]
    public async Task CreateOfTResult_TypedResult_NonMatchingFailure_IsNeverRetried()
    {
        var owner = new TestOwner();
        var pipeline = RetryResiliency<TestOwner>.Create<Result<int>>(r => r.Error is TransientException, o => o.Logger, delay: TimeSpan.FromMilliseconds(1), maxRetryAttempts: 3);

        var ctx = CreateContext(owner);
        try
        {
            var attempts = 0;
            var result = await pipeline.ExecuteAsync(async _ =>
            {
                attempts++;
                return new Result<int>(new InvalidOperationException("not retry-worthy"));
            }, ctx);

            result.IsFailure.Should().BeTrue();
            attempts.Should().Be(1);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(ctx);
        }
    }

    private sealed class TransientException : Exception;

    private sealed class TestOwner
    {
        public ILogger Logger { get; } = NullLogger.Instance;
    }
}
