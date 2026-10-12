using CoreEx.Database.Abstractions;
using CoreEx.Database.Extended;
using CoreEx.Database.SqlServer;
using CoreEx.Entities;
using CoreEx.Hosting;
using CoreEx.Mapping.Converters.Abstractions;
using CoreEx.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using System.Data.Common;
using System.Text.Json;

namespace CoreEx.Database.Test.Unit;

[TestFixture]
public class DatabaseInvokerRetryTests
{
    private static DatabaseInvoker CreateInvoker() => new TestInvoker();

    private static ResiliencePipeline<Result> CreateFastCustomPipeline(int maxRetryAttempts = 3)
        => RetryResiliency<FakeDatabase>.Create(r => r.IsFailure, _ => NullLogger.Instance, delay: TimeSpan.FromMilliseconds(1), maxRetryAttempts: maxRetryAttempts);

    [Test]
    public async Task RetryOnTransient_DefaultsToFalse_TransientException_IsNotRetried()
    {
        // Even though the exception is classified as transient, RetryOnTransient is false (default) so it must propagate on the very first attempt.
        var database = new FakeDatabase(isTransient: true);
        var attempts = 0;

        Func<Task> act = () => CreateInvoker().InvokeAsync<int>(database, new SqlServerDatabaseArgs { TransformException = false }, (_, _, _) =>
        {
            attempts++;
            throw new TestDbException("boom");
        }, CancellationToken.None);

        await act.Should().ThrowAsync<TestDbException>().ConfigureAwait(false);
        attempts.Should().Be(1);
    }

    [Test]
    public async Task RetryOnTransient_True_NonTransientException_IsNeverRetried()
    {
        // Classification says "not transient" so, even opted in, it must never be retried - it bypasses the pipeline entirely and propagates unwrapped.
        var database = new FakeDatabase(isTransient: false);
        var attempts = 0;

        Func<Task> act = () => CreateInvoker().InvokeAsync<int>(database, new SqlServerDatabaseArgs { TransformException = false, RetryOnTransient = true, RetryResiliencePipeline = CreateFastCustomPipeline() }, (_, _, _) =>
        {
            attempts++;
            throw new TestDbException("boom");
        }, CancellationToken.None);

        await act.Should().ThrowAsync<TestDbException>().ConfigureAwait(false);
        attempts.Should().Be(1);
    }

    [Test]
    public async Task RetryOnTransient_True_CustomPipeline_RetriesTransientException_UntilSuccess()
    {
        var database = new FakeDatabase(isTransient: true);
        var attempts = 0;

        var result = await CreateInvoker().InvokeAsync<int>(database, new SqlServerDatabaseArgs { TransformException = false, RetryOnTransient = true, RetryResiliencePipeline = CreateFastCustomPipeline() }, (_, _, _) =>
        {
            attempts++;
            if (attempts < 3)
                throw new TestDbException("boom");

            return Task.FromResult(42);
        }, CancellationToken.None).ConfigureAwait(false);

        result.Should().Be(42);
        attempts.Should().Be(3);
    }

    [Test]
    public async Task RetryOnTransient_True_CustomPipeline_ExhaustsRetries_ThenThrows()
    {
        var database = new FakeDatabase(isTransient: true);
        var attempts = 0;

        Func<Task> act = () => CreateInvoker().InvokeAsync<int>(database, new SqlServerDatabaseArgs { TransformException = false, RetryOnTransient = true, RetryResiliencePipeline = CreateFastCustomPipeline(maxRetryAttempts: 2) }, (_, _, _) =>
        {
            attempts++;
            throw new TestDbException("boom");
        }, CancellationToken.None);

        // ExceptionDispatchInfo preserves both the original exception type and its stack trace (no AggregateException wrapping) so the exhausted-retry outcome is indistinguishable, from the
        // caller's perspective, from a single failed attempt - and so it can still be pattern-matched by the outer TransformException catch (see the dedicated test below).
        var ex = await act.Should().ThrowAsync<TestDbException>().ConfigureAwait(false);
        ex.Which.Message.Should().Be("boom");
        attempts.Should().Be(3); // The initial attempt plus 2 retries.
    }

    [Test]
    public async Task RetryOnTransient_True_NoCustomPipeline_UsesDefault_RetriesTransientException_UntilSuccess()
    {
        // No RetryResiliencePipeline supplied - exercises the real DatabaseInvokerResiliency default (cached per-TResult) pipeline; a single retry keeps this within a reasonable runtime
        // despite the default's non-trivial (2s+) backoff.
        var database = new FakeDatabase(isTransient: true);
        var attempts = 0;

        var result = await CreateInvoker().InvokeAsync<string>(database, new SqlServerDatabaseArgs { TransformException = false, RetryOnTransient = true }, (_, _, _) =>
        {
            attempts++;
            if (attempts < 2)
                throw new TestDbException("boom");

            return Task.FromResult("ok");
        }, CancellationToken.None).ConfigureAwait(false);

        result.Should().Be("ok");
        attempts.Should().Be(2);
    }

    [Test]
    public async Task RetryOnTransient_True_ExhaustedRetry_StillAppliesOuterTransformException()
    {
        // Confirms the pre-existing TransformException conversion layer remains the outermost handler, applied to whatever the (possibly-retried) exception ends up being.
        var database = new FakeDatabase(isTransient: true, handleDbException: _ => new BusinessException("converted"));

        Func<Task> act = () => CreateInvoker().InvokeAsync<int>(database, new SqlServerDatabaseArgs { TransformException = true, RetryOnTransient = true, RetryResiliencePipeline = CreateFastCustomPipeline(maxRetryAttempts: 1) }, (_, _, _) =>
            throw new TestDbException("boom"), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("converted").ConfigureAwait(false);
    }

    [Test]
    public async Task RetryOnTransient_True_ExhaustedRetry_ResultShapedTResult_ConvertsDirectly_WithoutThrowing()
    {
        // Where TResult is itself an IResult (ROP) and TransformException converts the exhausted-retry exception, the failure must come back as a Result directly - with NO exception ever
        // thrown out of InvokeAsync at all - rather than round-tripping through a throw/catch purely to reapply a conversion already known here.
        var database = new FakeDatabase(isTransient: true, handleDbException: _ => new NotFoundException());
        var attempts = 0;

        Result<int> result = await CreateInvoker().InvokeAsync<Result<int>>(database, new SqlServerDatabaseArgs { TransformException = true, RetryOnTransient = true, RetryResiliencePipeline = CreateFastCustomPipeline(maxRetryAttempts: 1) }, (_, _, _) =>
        {
            attempts++;
            throw new TestDbException("boom");
        }, CancellationToken.None).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        attempts.Should().Be(2); // The initial attempt plus 1 retry.
    }

    private sealed class TestInvoker : DatabaseInvoker;

    private sealed class TestDbException(string message) : DbException(message);

    private sealed class FakeDatabase(bool isTransient, Func<DbException, Exception?>? handleDbException = null) : IDatabase
    {
        public ILogger? Logger => null;

        public DatabaseInvoker Invoker => throw new NotSupportedException();

        public DatabaseArgs DbArgs => throw new NotSupportedException();

        public string DatabaseId => "fake";

        public DateTimeTransform DateTimeTransform { get; set; }

        public bool DateTimeOffsetTransform { get; set; }

        public DatabaseColumns NamedColumns => throw new NotSupportedException();

        public DatabaseWildcard Wildcard { get; set; } = new();

        public ISourceConverter<string?> RowVersionConverter => throw new NotSupportedException();

        public JsonSerializerOptions JsonSerializerOptions => throw new NotSupportedException();

        public DbTransaction? CurrentTransaction => null;

        public bool IsInTransaction => false;

        public void UseTransaction(DbTransaction? transaction) => throw new NotSupportedException();

        public event EventHandler? UseTransactionChanged { add { } remove { } }

        public DbConnection Connection => throw new NotSupportedException();

        public Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public DatabaseCommand Statement(SqlStatement statement) => throw new NotSupportedException();

        public Exception? HandleDbException(DbException dbex) => handleDbException?.Invoke(dbex);

        public bool IsTransientException(Exception exception) => isTransient;

        public DbParameter CreateParameter() => throw new NotSupportedException();

        public string GetNextSavePointName() => throw new NotSupportedException();
    }
}
