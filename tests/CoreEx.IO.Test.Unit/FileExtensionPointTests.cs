namespace CoreEx.IO.Test.Unit;

[TestFixture]
public class FileExtensionPointTests
{
    [Test]
    public void CoreLifecycle_IsNotAnExternalSubclassExtensionPoint()
    {
        var method = typeof(FileProcessor<object>).GetMethod("ProcessCoreAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The core lifecycle method is missing.");
        method.IsPrivate.Should().BeTrue();
    }

    [Test]
    public async Task UnitValidation_AwaitsEachDiagnosticAndForwardsTheToken()
    {
        var handler = new ReportingHandler();
        var context = CreateContext();
        using var cancellation = new CancellationTokenSource();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = new List<string>();
        var first = true;
        var unit = new ProbeRow();
        var info = new FileUnitInfo(1, new FileLocation());
        var task = handler.ValidateAsync(unit, info, context, async (diagnostic, ct) =>
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
        var args = handler.Arguments ?? throw new InvalidOperationException("The validation hook did not receive arguments.");
        args.Unit.Should().BeSameAs(unit);
        args.Info.Should().BeSameAs(info);
        var typed = args.As<ProbeRow>();
        typed.Unit.Should().BeSameAs(unit);
        typed.Info.Should().BeSameAs(info);
        typed.Context.Should().BeSameAs(context);
        typed.ReportDiagnosticAsync.Should().BeSameAs(args.ReportDiagnosticAsync);
        Action wrongType = () => args.As<string>();
        wrongType.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task UnitExecution_ReturnsOriginalResultWithoutThrowing()
    {
        var expected = Result.Fail(new BusinessException("Cannot execute this unit."));
        var handler = new ExecutingHandler(expected);
        var unit = new ProbeRow();
        var info = new FileUnitInfo(1, new FileLocation());
        var context = CreateContext();
        using var cancellation = new CancellationTokenSource();

        var result = await handler.ExecuteAsync(unit, info, context, cancellation.Token).ConfigureAwait(false);
        result.Should().Be(expected);
        result.Error.Should().BeSameAs(expected.Error);
        handler.Unit.Should().BeSameAs(unit);
        handler.Context.Should().BeSameAs(context);
        handler.Token.Should().Be(cancellation.Token);
        var args = handler.Arguments ?? throw new InvalidOperationException("The execution hook did not receive arguments.");
        var typed = args.As<ProbeRow>();
        typed.Unit.Should().BeSameAs(unit);
        typed.Info.Should().BeSameAs(info);
        typed.Context.Should().BeSameAs(context);
        Action wrongType = () => args.As<string>();
        wrongType.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task DefaultUnitValidation_DoesNothingButDefaultExecutionCannotPretendToSucceed()
    {
        var handler = new DefaultHandler();
        var emitted = false;
        await handler.ValidateAsync(new object(), new(1, new FileLocation()), CreateContext(), (_, _) =>
        {
            emitted = true;
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        emitted.Should().BeFalse();
        Action execution = () => handler.ExecuteAsync(new object(), new(1, new FileLocation()), CreateContext());
        execution.Should().Throw<NotSupportedException>();
    }

    [Test]
    public void UnitEntryPoints_RejectCancellationBeforeCallingOverrides()
    {
        var handler = new ExecutingHandler(Result.Success);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Action execute = () => handler.ExecuteAsync(new object(), new(1, new FileLocation()), CreateContext(), cancellation.Token);
        Action validate = () => handler.ValidateAsync(new object(), new(1, new FileLocation()), CreateContext(), (_, _) => Task.CompletedTask, cancellation.Token);

        execute.Should().Throw<OperationCanceledException>();
        validate.Should().Throw<OperationCanceledException>();
        handler.Unit.Should().BeNull();
    }

    [Test]
    public void UnitEntryPoints_RejectMissingArguments()
    {
        var handler = new DefaultHandler();
        Action noValidationUnit = () => handler.ValidateAsync(null!, new(1, new FileLocation()), CreateContext(), (_, _) => Task.CompletedTask);
        Action noValidationInfo = () => handler.ValidateAsync(new object(), null!, CreateContext(), (_, _) => Task.CompletedTask);
        Action noValidationContext = () => handler.ValidateAsync(new object(), new(1, new FileLocation()), null!, (_, _) => Task.CompletedTask);
        Action noSink = () => handler.ValidateAsync(new object(), new(1, new FileLocation()), CreateContext(), null!);
        Action noExecutionUnit = () => handler.ExecuteAsync(null!, new(1, new FileLocation()), CreateContext());
        Action noExecutionInfo = () => handler.ExecuteAsync(new object(), null!, CreateContext());
        Action noExecutionContext = () => handler.ExecuteAsync(new object(), new(1, new FileLocation()), null!);

        noValidationUnit.Should().Throw<ArgumentNullException>();
        noValidationInfo.Should().Throw<ArgumentNullException>();
        noValidationContext.Should().Throw<ArgumentNullException>();
        noSink.Should().Throw<ArgumentNullException>();
        noExecutionUnit.Should().Throw<ArgumentNullException>();
        noExecutionInfo.Should().Throw<ArgumentNullException>();
        noExecutionContext.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task UnitValidation_PropagatesReportingFaultsWithoutContinuing()
    {
        var error = new IOException("Report cannot be written.");
        var calls = 0;
        Func<Task> action = () => new ReportingHandler().ValidateAsync(new object(), new(1, new FileLocation()), CreateContext(), (_, _) =>
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
        var processor = new ServicesProbe();
        var completed = await processor.ProcessAsync(_ => Open("1\n"), new(CreateContext().Identity)).ConfigureAwait(false);
        var rejected = await processor.ProcessAsync(_ => Open("bad\n"), new(CreateContext().Identity)).ConfigureAwait(false);
        completed.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
        rejected.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
    }

    [Test]
    public async Task Services_UsesTheConstructionProviderAndRemainsOptionalForPureValidation()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var processor = new ServicesProbe(provider);
        processor.Provider.Should().BeSameAs(provider);

        var withoutServices = new ServicesProbe();
        Action access = () => _ = withoutServices.Provider;
        access.Should().Throw<InvalidOperationException>().WithMessage("*without a service provider*");
        var summary = await withoutServices.ProcessAsync(_ => Open("1\n"), new(CreateContext().Identity)).ConfigureAwait(false);
        summary.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void HookArguments_HaveFrameworkOwnedConstructionAndGetOnlyProperties()
    {
        Type[] types = [typeof(FileUnitArgs<object>), typeof(FileUnitValidationArgs<object>), typeof(FileUnitContextArgs<object>),
            typeof(FileValidationArgs), typeof(FileDiagnosticArgs), typeof(FileSummaryArgs), typeof(FileExecutionFailedArgs)];
        foreach (var type in types)
        {
            type.GetConstructors().Should().BeEmpty();
            type.GetProperties().Should().OnlyContain(property => property.SetMethod == null);
        }
    }

    [TestCase(FileProcessingMode.ValidateOnly)]
    [TestCase(FileProcessingMode.ValidateThenExecute)]
    [TestCase(FileProcessingMode.ValidateAndExecute)]
    public async Task LifecycleArguments_PreserveUnitsContextsAndFinalSummary(FileProcessingMode mode)
    {
        await using var provider = new ServiceCollection().AddExecutionContext().BuildServiceProvider();
        var processor = new LifecycleProbe(provider);
        var context = new FileProcessingContext(CreateContext().Identity, mode);
        var summary = await processor.ProcessAsync(_ => Open("1\n"), context).ConfigureAwait(false);

        var observation = processor.Observation ?? throw new InvalidOperationException("The unit was not observed.");
        observation.Unit.Number.Should().Be(1);
        observation.Info.Ordinal.Should().Be(1);
        observation.Context.Should().BeSameAs(context);
        processor.Validation?.Context.Should().BeSameAs(context);
        processor.Completed?.Context.Should().BeSameAs(context);
        processor.Completed?.Summary.Should().BeSameAs(summary);
        processor.ContentFailed.Should().BeNull();
        processor.ExecutionFailed.Should().BeNull();
        processor.Configurations.Should().HaveCount(mode == FileProcessingMode.ValidateOnly ? 1 : 2);
        processor.Configurations[0].Unit.Should().BeSameAs(observation.Unit);
        processor.Configurations[0].Info.Should().BeSameAs(observation.Info);
        processor.Configurations[0].Context.Should().BeSameAs(context);
        processor.Configurations[0].UnitContext.TenantId.Should().Be("configured-before-resolution");
        if (processor.Configurations.Count == 2)
        {
            var execution = processor.Configurations[1];
            execution.UnitContext.Should().NotBeSameAs(processor.Configurations[0].UnitContext);
            if (mode == FileProcessingMode.ValidateThenExecute)
            {
                execution.Unit.Should().NotBeSameAs(observation.Unit);
                execution.Context.Should().NotBeSameAs(context);
            }
            else
            {
                execution.Unit.Should().BeSameAs(observation.Unit);
                execution.Context.Should().BeSameAs(context);
            }
        }
    }

    [Test]
    public async Task ContentFailureArguments_PreserveDiagnosticContextAndReturnedSummary()
    {
        await using var provider = new ServiceCollection().AddExecutionContext().BuildServiceProvider();
        var processor = new LifecycleProbe(provider);
        var context = new FileProcessingContext(CreateContext().Identity);
        var summary = await processor.ProcessAsync(_ => Open("bad\n"), context).ConfigureAwait(false);

        processor.Diagnostic?.Context.Should().BeSameAs(context);
        processor.Diagnostic?.Diagnostic.Code.Should().Be("csv.field.invalid");
        processor.ContentFailed?.Context.Should().BeSameAs(context);
        processor.ContentFailed?.Summary.Should().BeSameAs(summary);
        processor.Completed.Should().BeNull();
        processor.ExecutionFailed.Should().BeNull();
    }

    [TestCase(FileProcessingMode.ValidateThenExecute)]
    [TestCase(FileProcessingMode.ValidateAndExecute)]
    public async Task ExecutionFailureArguments_PreserveOriginalResultAndUnitProvenance(FileProcessingMode mode)
    {
        await using var provider = new ServiceCollection().AddExecutionContext().BuildServiceProvider();
        var failure = Result.Fail(new BusinessException("Unit rejected by the downstream service."));
        var processor = new LifecycleProbe(provider, failure);
        var context = new FileProcessingContext(CreateContext().Identity, mode);
        var summary = await processor.ProcessAsync(_ => Open("1\n"), context).ConfigureAwait(false);

        var args = processor.ExecutionFailed ?? throw new InvalidOperationException("The execution failure hook was not called.");
        args.Context.Should().BeSameAs(context);
        args.Info.Should().BeSameAs(processor.Configurations[1].Info);
        args.Result.Error.Should().BeSameAs(failure.Error);
        args.Result.Should().Be(summary.ExecutionResult);
        processor.Completed.Should().BeNull();
        processor.ContentFailed.Should().BeNull();
    }

    private static Task<Stream> Open(string input) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(input)));

    private static FileProcessingContext CreateContext() => new(new("orders", "1", "store/orders.csv", "attempt", "revision"), FileProcessingMode.ValidateThenExecute);

    private sealed class DefaultHandler : FileUnitHandler<object>;

    private sealed class ReportingHandler : FileUnitHandler<object>
    {
        public FileProcessingContext? Context { get; private set; }
        public FileUnitValidationArgs<object>? Arguments { get; private set; }

        protected override async Task OnValidateAsync(FileUnitValidationArgs<object> args, CancellationToken cancellationToken)
        {
            Arguments = args;
            Context = args.Context;
            await args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Validation, "first", "First error."), cancellationToken).ConfigureAwait(false);
            await args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Validation, "second", "Second error."), cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class ExecutingHandler(Result result) : FileUnitHandler<object>
    {
        public object? Unit { get; private set; }
        public FileProcessingContext? Context { get; private set; }
        public CancellationToken Token { get; private set; }
        public FileUnitArgs<object>? Arguments { get; private set; }

        protected override Task<Result> OnExecuteAsync(FileUnitArgs<object> args, CancellationToken cancellationToken)
        {
            Arguments = args;
            Unit = args.Unit;
            Context = args.Context;
            Token = cancellationToken;
            return Task.FromResult(result);
        }
    }

    public sealed class ProbeRow
    {
        public int? Number { get; set; }
    }

    private sealed class ServicesProbe(IServiceProvider? provider = null) : CsvFileProcessor<ProbeRow>(
        () => new CsvFileMapping<ProbeRow>().MapProperty("Number", x => x.Number, CsvFieldConverters.Nullable<int>(), 0), serviceProvider: provider)
    {
        public IServiceProvider Provider => Services;
    }

    private sealed class LifecycleProbe(IServiceProvider provider, Result result = default) : CsvFileProcessor<ProbeRow>(
        () => new CsvFileMapping<ProbeRow>().MapProperty("Number", x => x.Number, CsvFieldConverters.Nullable<int>(), 0),
        serviceProvider: provider, handlerFactory: scoped => new LifecycleHandler(scoped.GetRequiredService<ExecutionContext>(), result))
    {
        public FileUnitArgs<ProbeRow>? Observation { get; private set; }
        public FileValidationArgs? Validation { get; private set; }
        public FileDiagnosticArgs? Diagnostic { get; private set; }
        public FileSummaryArgs? Completed { get; private set; }
        public FileSummaryArgs? ContentFailed { get; private set; }
        public FileExecutionFailedArgs? ExecutionFailed { get; private set; }
        public List<FileUnitContextArgs<ProbeRow>> Configurations { get; } = [];

        protected override void OnConfigureUnitContext(FileUnitContextArgs<ProbeRow> args)
        {
            args.UnitContext.TenantId = "configured-before-resolution";
            Configurations.Add(args);
        }

        protected override Task OnValidatedUnitAsync(FileUnitArgs<ProbeRow> args, CancellationToken cancellationToken)
        {
            Observation = args;
            return Task.CompletedTask;
        }

        protected override Task OnValidateFileAsync(FileValidationArgs args, CancellationToken cancellationToken)
        {
            Validation = args;
            return Task.CompletedTask;
        }

        protected override Task OnDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken)
        {
            Diagnostic = args;
            return Task.CompletedTask;
        }

        protected override Task OnCompletedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
        {
            Completed = args;
            return Task.CompletedTask;
        }

        protected override Task OnContentFailedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
        {
            ContentFailed = args;
            return Task.CompletedTask;
        }

        protected override Task OnExecutionFailedAsync(FileExecutionFailedArgs args, CancellationToken cancellationToken)
        {
            ExecutionFailed = args;
            return Task.CompletedTask;
        }
    }

    private sealed class LifecycleHandler : FileUnitHandler<ProbeRow>
    {
        private readonly Result _result;

        public LifecycleHandler(ExecutionContext context, Result result)
        {
            context.TenantId.Should().Be("configured-before-resolution");
            _result = result;
        }

        protected override Task<Result> OnExecuteAsync(FileUnitArgs<ProbeRow> args, CancellationToken cancellationToken) => Task.FromResult(_result);
    }
}
