namespace CoreEx.IO.Test.Unit;

[TestFixture]
public class FileExtensionPointTests
{
    [Test]
    public async Task UnitValidation_AwaitsEachDiagnosticAndForwardsTheToken()
    {
        var handler = new ReportingHandler();
        var context = CreateContext();
        using var cancellation = new CancellationTokenSource();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = new List<string>();
        var first = true;
        var task = handler.ValidateAsync(new object(), context, async (diagnostic, ct) =>
        {
            ct.Should().Be(cancellation.Token);
            observed.Add(diagnostic.Code);
            if (first)
            {
                first = false;
                await release.Task.ConfigureAwait(false);
            }
        }, cancellation.Token);

        task.IsCompleted.Should().BeFalse();
        observed.Should().Equal("first");
        release.SetResult();
        await task.ConfigureAwait(false);
        observed.Should().Equal("first", "second");
        handler.Context.Should().BeSameAs(context);
    }

    [Test]
    public async Task UnitExecution_ReturnsOriginalResultWithoutThrowing()
    {
        var expected = Result.Fail(new BusinessException("Cannot execute this unit."));
        var handler = new ExecutingHandler(expected);
        var unit = new object();
        var context = CreateContext();
        using var cancellation = new CancellationTokenSource();

        var result = await handler.ExecuteAsync(unit, context, cancellation.Token).ConfigureAwait(false);
        result.Should().Be(expected);
        result.Error.Should().BeSameAs(expected.Error);
        handler.Unit.Should().BeSameAs(unit);
        handler.Context.Should().BeSameAs(context);
        handler.Token.Should().Be(cancellation.Token);
    }

    [Test]
    public async Task DefaultUnitValidation_DoesNothingButDefaultExecutionCannotPretendToSucceed()
    {
        var handler = new DefaultHandler();
        var emitted = false;
        await handler.ValidateAsync(new object(), CreateContext(), (_, _) =>
        {
            emitted = true;
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        emitted.Should().BeFalse();
        Action execution = () => handler.ExecuteAsync(new object(), CreateContext());
        execution.Should().Throw<NotSupportedException>();
    }

    [Test]
    public void UnitEntryPoints_RejectCancellationBeforeCallingOverrides()
    {
        var handler = new ExecutingHandler(Result.Success);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Action execute = () => handler.ExecuteAsync(new object(), CreateContext(), cancellation.Token);
        Action validate = () => handler.ValidateAsync(new object(), CreateContext(), (_, _) => Task.CompletedTask, cancellation.Token);

        execute.Should().Throw<OperationCanceledException>();
        validate.Should().Throw<OperationCanceledException>();
        handler.Unit.Should().BeNull();
    }

    [Test]
    public void UnitEntryPoints_RejectMissingArguments()
    {
        var handler = new DefaultHandler();
        Action noValidationUnit = () => handler.ValidateAsync(null!, CreateContext(), (_, _) => Task.CompletedTask);
        Action noValidationContext = () => handler.ValidateAsync(new object(), null!, (_, _) => Task.CompletedTask);
        Action noSink = () => handler.ValidateAsync(new object(), CreateContext(), null!);
        Action noExecutionUnit = () => handler.ExecuteAsync(null!, CreateContext());
        Action noExecutionContext = () => handler.ExecuteAsync(new object(), null!);

        noValidationUnit.Should().Throw<ArgumentNullException>();
        noValidationContext.Should().Throw<ArgumentNullException>();
        noSink.Should().Throw<ArgumentNullException>();
        noExecutionUnit.Should().Throw<ArgumentNullException>();
        noExecutionContext.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task UnitValidation_PropagatesReportingFaultsWithoutContinuing()
    {
        var error = new IOException("Report cannot be written.");
        var calls = 0;
        Func<Task> action = () => new ReportingHandler().ValidateAsync(new object(), CreateContext(), (_, _) =>
        {
            calls++;
            return Task.FromException(error);
        });

        var assertion = await action.Should().ThrowAsync<IOException>().ConfigureAwait(false);
        assertion.Which.Should().BeSameAs(error);
        calls.Should().Be(1);
    }

    [Test]
    public async Task DefaultFileHooks_AreOptionalExtensionPoints()
    {
        var processor = new HookProbeProcessor();
        var context = CreateContext();
        var diagnostic = new FileDiagnostic(FileDiagnosticCategory.Validation, "invalid", "Invalid input.");
        var rejected = new FileProcessingSummary(context, FileProcessingOutcome.Rejected, new(errorCount: 1), false);
        var completed = new FileProcessingSummary(context, FileProcessingOutcome.Executed, new(), true);

        await processor.ValidateFileAsync(context).ConfigureAwait(false);
        await processor.ReportAsync(context, diagnostic).ConfigureAwait(false);
        await processor.RejectAsync(context, rejected).ConfigureAwait(false);
        await processor.CompleteAsync(context, completed).ConfigureAwait(false);
    }

    private static FileProcessingContext CreateContext() => new(new("orders", "1", "store/orders.csv", "attempt", "revision"), FileProcessingMode.ValidateThenExecute);

    private sealed class DefaultHandler : FileUnitHandler<object>;

    private sealed class ReportingHandler : FileUnitHandler<object>
    {
        public FileProcessingContext? Context { get; private set; }

        protected override async Task OnValidateAsync(object unit, FileProcessingContext context, Func<FileDiagnostic, CancellationToken, Task> reportDiagnosticAsync, CancellationToken cancellationToken)
        {
            Context = context;
            await reportDiagnosticAsync(new(FileDiagnosticCategory.Validation, "first", "First error."), cancellationToken).ConfigureAwait(false);
            await reportDiagnosticAsync(new(FileDiagnosticCategory.Validation, "second", "Second error."), cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class ExecutingHandler(Result result) : FileUnitHandler<object>
    {
        public object? Unit { get; private set; }
        public FileProcessingContext? Context { get; private set; }
        public CancellationToken Token { get; private set; }

        protected override Task<Result> OnExecuteAsync(object unit, FileProcessingContext context, CancellationToken cancellationToken)
        {
            Unit = unit;
            Context = context;
            Token = cancellationToken;
            return Task.FromResult(result);
        }
    }

    private sealed class HookProbeProcessor : FileProcessor
    {
        public override Task<FileProcessingSummary> ProcessAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("This probe exposes hooks, not a processing engine.");

        public Task ValidateFileAsync(FileProcessingContext context, CancellationToken cancellationToken = default) => OnValidateFileAsync(context, (_, _) => Task.CompletedTask, cancellationToken);
        public Task ReportAsync(FileProcessingContext context, FileDiagnostic diagnostic, CancellationToken cancellationToken = default) => OnDiagnosticAsync(context, diagnostic, cancellationToken);
        public Task RejectAsync(FileProcessingContext context, FileProcessingSummary summary, CancellationToken cancellationToken = default) => OnRejectedAsync(context, summary, cancellationToken);
        public Task CompleteAsync(FileProcessingContext context, FileProcessingSummary summary, CancellationToken cancellationToken = default) => OnCompletedAsync(context, summary, cancellationToken);
    }
}
