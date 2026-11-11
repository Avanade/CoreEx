namespace CoreEx.IO.Test.Unit;

[TestFixture]
public class FileProcessingSummaryTests
{
    [Test]
    public void SuccessfulValidationSummary_AllowsEmptyInputAndWarnings()
    {
        var context = CreateContext(FileProcessingMode.ValidateOnly);
        var progress = new FileProcessingProgress(warningCount: 2, informationCount: 3);
        var summary = new FileProcessingSummary(context, FileProcessingOutcome.Succeeded, progress, true);

        summary.IsSuccess.Should().BeTrue();
        summary.ValidationCompleted.Should().BeTrue();
        summary.ExecutionResult.IsSuccess.Should().BeTrue();
        summary.Identity.Should().BeSameAs(context.Identity);
        summary.Mode.Should().Be(FileProcessingMode.ValidateOnly);
        summary.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
        summary.Progress.Should().BeSameAs(progress);
    }

    [Test]
    public void SuccessfulExecutionSummary_RequiresEveryAcceptedUnitToSucceed()
    {
        var context = CreateContext(FileProcessingMode.ValidateThenExecute);
        var incomplete = new FileProcessingProgress(recordsRead: 3, unitsValidated: 3, unitsAttempted: 2, unitsExecuted: 2);
        Action action = () => new FileProcessingSummary(context, FileProcessingOutcome.Succeeded, incomplete, true);
        action.Should().Throw<ArgumentException>();

        var complete = new FileProcessingProgress(recordsRead: 3, unitsValidated: 3, unitsAttempted: 3, unitsExecuted: 3);
        new FileProcessingSummary(context, FileProcessingOutcome.Succeeded, complete, true).IsSuccess.Should().BeTrue();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ContentFailureSummary_PreservesFullOrIncompleteValidation(bool completed)
    {
        var progress = new FileProcessingProgress(recordsRead: 10, unitsValidated: 9, errorCount: 1);
        var summary = new FileProcessingSummary(CreateContext(FileProcessingMode.ValidateThenExecute), FileProcessingOutcome.ContentFailed, progress, completed);
        summary.IsSuccess.Should().BeFalse();
        summary.ValidationCompleted.Should().Be(completed);
        summary.Progress.InvalidRecords.Should().Be(0);
        summary.Progress.UnitsAttempted.Should().Be(0);
        summary.ExecutionResult.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ContentFailureAfterSinglePassExecution_PreservesEarlierWrites()
    {
        var progress = new FileProcessingProgress(recordsRead: 3, invalidRecords: 1, unitsRead: 3, unitsValidated: 2, unitsAttempted: 2, unitsExecuted: 2, errorCount: 1);
        var summary = new FileProcessingSummary(CreateContext(FileProcessingMode.ValidateAndExecute), FileProcessingOutcome.ContentFailed, progress, true);
        summary.IsSuccess.Should().BeFalse();
        summary.Progress.UnitsExecuted.Should().Be(2);
        summary.ExecutionResult.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ExecutionFailedSummary_PreservesOriginalFailureAndPartialProgress()
    {
        var error = new BusinessException("Unit execution failed.");
        var original = Result.Fail(error);
        var context = CreateContext(FileProcessingMode.ValidateThenExecute);
        var progress = new FileProcessingProgress(recordsRead: 6, unitsValidated: 3, unitsAttempted: 2, unitsExecuted: 1);
        var summary = new FileProcessingSummary(context, FileProcessingOutcome.ExecutionFailed, progress, true, original);

        summary.IsSuccess.Should().BeFalse();
        summary.ValidationCompleted.Should().BeTrue();
        summary.ExecutionResult.Should().Be(original);
        summary.ExecutionResult.Error.Should().BeSameAs(error);
        summary.Progress.UnitsValidated.Should().Be(3);
        summary.Progress.UnitsAttempted.Should().Be(2);
        summary.Progress.UnitsExecuted.Should().Be(1);
        summary.Progress.ErrorCount.Should().Be(0);
    }

    [Test]
    public void SinglePassExecutionFailureMayFollowContentDiagnostics()
    {
        var failure = Result.Fail(new BusinessException("Failed."));
        var progress = new FileProcessingProgress(recordsRead: 3, invalidRecords: 1, unitsRead: 3, unitsValidated: 1, unitsAttempted: 1, errorCount: 1);
        var summary = new FileProcessingSummary(CreateContext(FileProcessingMode.ValidateAndExecute), FileProcessingOutcome.ExecutionFailed, progress, false, failure);
        summary.Outcome.Should().Be(FileProcessingOutcome.ExecutionFailed);
        summary.ExecutionResult.Should().Be(failure);
    }

    [Test]
    public void Summary_RejectsContradictoryOutcomesAndMissingData()
    {
        var validationContext = CreateContext(FileProcessingMode.ValidateOnly);
        var attempted = new FileProcessingProgress(recordsRead: 1, unitsValidated: 1, unitsAttempted: 1);
        var failedProgress = new FileProcessingProgress(recordsRead: 1, unitsValidated: 1, errorCount: 1);
        var failure = Result.Fail(new BusinessException("Failed."));

        Action noError = () => new FileProcessingSummary(validationContext, FileProcessingOutcome.ContentFailed, new(), true);
        Action validationExecution = () => new FileProcessingSummary(validationContext, FileProcessingOutcome.Succeeded, attempted, true);
        Action contentWithExecutionFailure = () => new FileProcessingSummary(validationContext, FileProcessingOutcome.ContentFailed, failedProgress, true, failure);
        Action executionInValidationMode = () => new FileProcessingSummary(validationContext, FileProcessingOutcome.ExecutionFailed,
            new(unitsRead: 1, unitsValidated: 1, unitsAttempted: 1), false, failure);
        Action failureOnSuccess = () => new FileProcessingSummary(validationContext, FileProcessingOutcome.Succeeded, new(), true, failure);
        Action noContext = () => new FileProcessingSummary(null!, FileProcessingOutcome.Succeeded, new(), true);
        Action noProgress = () => new FileProcessingSummary(validationContext, FileProcessingOutcome.Succeeded, null!, true);
        Action invalidOutcome = () => new FileProcessingSummary(validationContext, (FileProcessingOutcome)100, new(), true);

        noError.Should().Throw<ArgumentException>();
        validationExecution.Should().Throw<ArgumentException>();
        contentWithExecutionFailure.Should().Throw<ArgumentException>();
        executionInValidationMode.Should().Throw<ArgumentException>();
        failureOnSuccess.Should().Throw<ArgumentException>();
        noContext.Should().Throw<ArgumentNullException>();
        noProgress.Should().Throw<ArgumentNullException>();
        invalidOutcome.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static FileProcessingContext CreateContext(FileProcessingMode mode) => new(new("orders", "1", "store/orders.csv", "attempt", "revision"), mode);
}
