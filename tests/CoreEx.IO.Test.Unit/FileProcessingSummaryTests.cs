namespace CoreEx.IO.Test.Unit;

[TestFixture]
public class FileProcessingSummaryTests
{
    [TestCase(FileProcessingMode.ValidateOnly, FileProcessingOutcome.Validated)]
    [TestCase(FileProcessingMode.ValidateThenExecute, FileProcessingOutcome.Executed)]
    public void SuccessfulSummary_AllowsEmptyInputAndWarningOnlyInput(FileProcessingMode mode, FileProcessingOutcome outcome)
    {
        var context = CreateContext(mode);
        var progress = new FileProcessingProgress(warningCount: 2, informationCount: 3);
        var summary = new FileProcessingSummary(context, outcome, progress, true);

        summary.IsSuccess.Should().BeTrue();
        summary.ValidationCompleted.Should().BeTrue();
        summary.ExecutionResult.IsSuccess.Should().BeTrue();
        summary.Identity.Should().BeSameAs(context.Identity);
        summary.Mode.Should().Be(mode);
        summary.Outcome.Should().Be(outcome);
        summary.Progress.Should().BeSameAs(progress);
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

    [TestCase(true)]
    [TestCase(false)]
    public void RejectedSummary_DistinguishesFullFromIncompleteValidation(bool completed)
    {
        var progress = new FileProcessingProgress(recordsRead: 10, unitsValidated: 9, errorCount: 1);
        var summary = new FileProcessingSummary(CreateContext(FileProcessingMode.ValidateThenExecute), FileProcessingOutcome.Rejected, progress, completed);
        summary.IsSuccess.Should().BeFalse();
        summary.ValidationCompleted.Should().Be(completed);
        summary.Progress.InvalidRecords.Should().Be(0);
        summary.Progress.UnitsAttempted.Should().Be(0);
        summary.ExecutionResult.IsSuccess.Should().BeTrue();
    }

    [TestCase(FileProcessingOutcome.Validated)]
    [TestCase(FileProcessingOutcome.Executed)]
    [TestCase(FileProcessingOutcome.ExecutionFailed)]
    public void AcceptedSummary_RequiresCompletedErrorFreeValidation(FileProcessingOutcome outcome)
    {
        var context = CreateContext(outcome == FileProcessingOutcome.Validated ? FileProcessingMode.ValidateOnly : FileProcessingMode.ValidateThenExecute);
        var result = outcome == FileProcessingOutcome.ExecutionFailed ? Result.Fail(new BusinessException("Failed.")) : Result.Success;
        var attempted = outcome == FileProcessingOutcome.Validated ? 0 : 1;
        var executed = outcome == FileProcessingOutcome.Executed ? 1 : 0;
        var progress = new FileProcessingProgress(recordsRead: 1, unitsValidated: 1, unitsAttempted: attempted, unitsExecuted: executed);
        var errors = new FileProcessingProgress(recordsRead: 1, unitsValidated: 1, unitsAttempted: attempted, unitsExecuted: executed, errorCount: 1);

        Action incomplete = () => new FileProcessingSummary(context, outcome, progress, false, result);
        Action invalid = () => new FileProcessingSummary(context, outcome, errors, true, result);
        incomplete.Should().Throw<ArgumentException>();
        invalid.Should().Throw<ArgumentException>();
    }

    [TestCase(FileProcessingMode.ValidateThenExecute, FileProcessingOutcome.Validated)]
    [TestCase(FileProcessingMode.ValidateOnly, FileProcessingOutcome.Executed)]
    [TestCase(FileProcessingMode.ValidateOnly, FileProcessingOutcome.ExecutionFailed)]
    public void Summary_RejectsOutcomeForWrongMode(FileProcessingMode mode, FileProcessingOutcome outcome)
    {
        Action action = () => new FileProcessingSummary(CreateContext(mode), outcome, new(), true);
        action.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Summary_RejectsImpossibleRejectionOrValidationExecution()
    {
        var context = CreateContext(FileProcessingMode.ValidateOnly);
        var attempted = new FileProcessingProgress(recordsRead: 1, unitsValidated: 1, unitsAttempted: 1);
        var rejectedAfterAttempt = new FileProcessingProgress(recordsRead: 1, unitsValidated: 1, unitsAttempted: 1, errorCount: 1);
        Action noError = () => new FileProcessingSummary(context, FileProcessingOutcome.Rejected, new(), true);
        Action rejected = () => new FileProcessingSummary(context, FileProcessingOutcome.Rejected, rejectedAfterAttempt, true);
        Action validated = () => new FileProcessingSummary(context, FileProcessingOutcome.Validated, attempted, true);

        noError.Should().Throw<ArgumentException>();
        rejected.Should().Throw<ArgumentException>();
        validated.Should().Throw<ArgumentException>();
    }

    [Test]
    public void ExecutedSummary_RequiresEveryAcceptedUnitToSucceed()
    {
        var context = CreateContext(FileProcessingMode.ValidateThenExecute);
        var incomplete = new FileProcessingProgress(recordsRead: 3, unitsValidated: 3, unitsAttempted: 2, unitsExecuted: 2);
        Action action = () => new FileProcessingSummary(context, FileProcessingOutcome.Executed, incomplete, true);
        action.Should().Throw<ArgumentException>();

        var complete = new FileProcessingProgress(recordsRead: 3, unitsValidated: 3, unitsAttempted: 3, unitsExecuted: 3);
        new FileProcessingSummary(context, FileProcessingOutcome.Executed, complete, true).IsSuccess.Should().BeTrue();
    }

    [Test]
    public void FailedSummary_RequiresOriginalFailureAndExactlyOneUnsuccessfulAttempt()
    {
        var context = CreateContext(FileProcessingMode.ValidateThenExecute);
        var failure = Result.Fail(new BusinessException("Failed."));
        var progress = new FileProcessingProgress(recordsRead: 2, unitsValidated: 2, unitsAttempted: 1);
        Action noFailure = () => new FileProcessingSummary(context, FileProcessingOutcome.ExecutionFailed, progress, true);
        Action noAttempt = () => new FileProcessingSummary(context, FileProcessingOutcome.ExecutionFailed, new(), true, failure);
        Action multipleFailures = () => new FileProcessingSummary(context, FileProcessingOutcome.ExecutionFailed, new(recordsRead: 2, unitsValidated: 2, unitsAttempted: 2), true, failure);
        Action failureOnSuccess = () => new FileProcessingSummary(context, FileProcessingOutcome.Executed, new(), true, failure);

        noFailure.Should().Throw<ArgumentException>();
        noAttempt.Should().Throw<ArgumentException>();
        multipleFailures.Should().Throw<ArgumentException>();
        failureOnSuccess.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Summary_RejectsMissingContextProgressAndInvalidOutcome()
    {
        Action noContext = () => new FileProcessingSummary(null!, FileProcessingOutcome.Validated, new(), true);
        Action noProgress = () => new FileProcessingSummary(CreateContext(FileProcessingMode.ValidateOnly), FileProcessingOutcome.Validated, null!, true);
        Action invalidOutcome = () => new FileProcessingSummary(CreateContext(FileProcessingMode.ValidateOnly), (FileProcessingOutcome)100, new(), true);
        noContext.Should().Throw<ArgumentNullException>();
        noProgress.Should().Throw<ArgumentNullException>();
        invalidOutcome.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static FileProcessingContext CreateContext(FileProcessingMode mode) => new(new("orders", "1", "store/orders.csv", "attempt", "revision"), mode);
}
