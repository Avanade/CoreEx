namespace CoreEx.IO.Test.Unit;

[TestFixture]
public class FileContractTests
{
    [Test]
    public void Context_DefaultsAreValidationOnlyAndFailFast()
    {
        var identity = CreateIdentity();
        var context = new FileProcessingContext(identity);

        context.Identity.Should().BeSameAs(identity);
        context.Mode.Should().Be(FileProcessingMode.ValidateOnly);
        context.ErrorMode.Should().Be(FileErrorMode.FailFast);
        context.Progress.RecordsRead.Should().Be(0);
        context.Progress.UnitsAttempted.Should().Be(0);
        context.Progress.ErrorCount.Should().Be(0);
    }

    [Test]
    public void Context_PassesShareIdentityButNotMutableContext()
    {
        var identity = CreateIdentity();
        var validation = new ReconciliationContext(identity) { Total = 123 };
        var execution = new ReconciliationContext(identity);

        execution.Identity.Should().BeSameAs(validation.Identity);
        execution.Progress.Should().NotBeSameAs(validation.Progress);
        execution.Total.Should().Be(0);
        identity.SourceRevision.Should().Be("revision-1");
        identity.AttemptId.Should().Be("attempt-1");
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void Identity_RejectsBlankComponents(int component)
    {
        var values = new[] { "customer", "1", "store/customer.csv", "attempt", "revision" };
        values[component] = " ";
        Action action = () => new FileProcessingIdentity(values[0], values[1], values[2], values[3], values[4]);
        action.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Identity_RevisionIsOptionalAndNotInferredFromSource()
        => new FileProcessingIdentity("customer", "1", "customer.csv", "attempt").SourceRevision.Should().BeNull();

    [Test]
    public void Contracts_RejectInvalidEnumsAndMissingIdentity()
    {
        Action missingIdentity = () => new FileProcessingContext(null!);
        Action invalidMode = () => new FileProcessingContext(CreateIdentity(), (FileProcessingMode)100);
        Action invalidErrorMode = () => new FileProcessingContext(CreateIdentity(), errorMode: (FileErrorMode)100);
        Action invalidCategory = () => new FileDiagnostic((FileDiagnosticCategory)100, "bad", "Bad input.");
        Action invalidSeverity = () => new FileDiagnostic(FileDiagnosticCategory.Validation, "bad", "Bad input.", (MessageType)100);

        missingIdentity.Should().Throw<ArgumentNullException>();
        invalidMode.Should().Throw<ArgumentOutOfRangeException>();
        invalidErrorMode.Should().Throw<ArgumentOutOfRangeException>();
        invalidCategory.Should().Throw<ArgumentOutOfRangeException>();
        invalidSeverity.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Location_LogicalPathDoesNotInventPhysicalCoordinates()
    {
        var location = new FileLocation(propertyPath: "$.orders[3].lines[5].quantity");
        location.PropertyPath.Should().Be("$.orders[3].lines[5].quantity");
        location.LineNumber.Should().BeNull();
        location.LinePosition.Should().BeNull();
        location.FieldIndex.Should().BeNull();
        location.FieldName.Should().BeNull();

        new FileLocation(propertyPath: "/orders/order[4]/quantity").LineNumber.Should().BeNull();
        new FileLocation().PropertyPath.Should().BeNull();
    }

    [Test]
    public void Location_DistinguishesLinePositionFromFieldIndex()
    {
        var location = new FileLocation(42, 17, 0, "Quantity", "Items[2].Quantity");
        location.LineNumber.Should().Be(42);
        location.LinePosition.Should().Be(17);
        location.FieldIndex.Should().Be(0);
        location.FieldName.Should().Be("Quantity");
        new FileLocation(long.MaxValue).LineNumber.Should().Be(long.MaxValue);
    }

    [TestCase(0, null, null)]
    [TestCase(-1, null, null)]
    [TestCase(1, 0, null)]
    [TestCase(1, -1, null)]
    [TestCase(null, 1, null)]
    [TestCase(null, null, -1)]
    public void Location_RejectsInvalidCoordinates(long? lineNumber, int? linePosition, int? fieldIndex)
    {
        Action action = () => new FileLocation(lineNumber, linePosition, fieldIndex);
        var parameter = lineNumber is <= 0 ? nameof(lineNumber) : linePosition.HasValue ? nameof(linePosition) : nameof(fieldIndex);
        action.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be(parameter);
    }

    [TestCase(MessageType.Info, false)]
    [TestCase(MessageType.Warning, false)]
    [TestCase(MessageType.Error, true)]
    public void Diagnostic_UsesCoreExSeverityWithoutAnException(MessageType severity, bool isError)
    {
        var message = new LText("files.required", "A value is required.");
        var location = new FileLocation(propertyPath: "Customer.Name");
        var diagnostic = new FileDiagnostic(FileDiagnosticCategory.Validation, "required", message, severity, location, "Customer");

        diagnostic.IsError.Should().Be(isError);
        diagnostic.Severity.Should().Be(severity);
        diagnostic.Message.Should().Be(message);
        diagnostic.Message.KeyAndOrText.Should().Be("files.required");
        diagnostic.Location.Should().BeSameAs(location);
        diagnostic.RecordType.Should().Be("Customer");
        diagnostic.Code.Should().Be("required");
        diagnostic.Category.Should().Be(FileDiagnosticCategory.Validation);
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public void Diagnostic_RejectsMissingCodeOrMessage(string? value)
    {
        Action missingCode = () => new FileDiagnostic(FileDiagnosticCategory.Mapping, value!, "Bad field.");
        Action missingMessage = () => new FileDiagnostic(FileDiagnosticCategory.Mapping, "bad-field", new LText(value));
        missingCode.Should().Throw<ArgumentException>();
        missingMessage.Should().Throw<ArgumentException>();
    }

    [Test]
    public void OptionalNames_RejectBlankButAllowOmission()
    {
        Action blankField = () => new FileLocation(fieldName: " ");
        Action blankPath = () => new FileLocation(propertyPath: " ");
        Action blankType = () => new FileDiagnostic(FileDiagnosticCategory.Validation, "invalid", "Invalid input.", recordType: " ");
        blankField.Should().Throw<ArgumentException>();
        blankPath.Should().Throw<ArgumentException>();
        blankType.Should().Throw<ArgumentException>();
        new FileDiagnostic(FileDiagnosticCategory.Parsing, "invalid", "Invalid input.").Location.Should().BeNull();
    }

    [Test]
    public void Progress_DistinguishesRecordsUnitsAndFileErrorsBeyondInt32()
    {
        var count = (long)int.MaxValue + 10;
        var progress = new FileProcessingProgress(long.MaxValue, count, 1, count - 2, count - 2, count - 3, 5, count, count);
        progress.BytesRead.Should().Be(long.MaxValue);
        progress.RecordsRead.Should().Be(count);
        progress.InvalidRecords.Should().Be(1);
        progress.UnitsValidated.Should().Be(count - 2);
        progress.UnitsAttempted.Should().Be(count - 2);
        progress.UnitsExecuted.Should().Be(count - 3);
        progress.ErrorCount.Should().Be(5);
        progress.WarningCount.Should().Be(count);
        progress.InformationCount.Should().Be(count);

        var reconciliation = new FileProcessingProgress(errorCount: 1);
        reconciliation.InvalidRecords.Should().Be(0);
        reconciliation.ErrorCount.Should().Be(1);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    public void Progress_RejectsEveryNegativeCounter(int counter)
    {
        var counts = new long[9];
        counts[counter] = -1;
        Action action = () => new FileProcessingProgress(counts[0], counts[1], counts[2], counts[3], counts[4], counts[5], counts[6], counts[7], counts[8]);
        var parameters = new[] { "bytesRead", "recordsRead", "invalidRecords", "unitsValidated", "unitsAttempted", "unitsExecuted", "errorCount", "warningCount", "informationCount" };
        action.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be(parameters[counter]);
    }

    [Test]
    public void Progress_RejectsContradictoryCounts()
    {
        Action tooManyInvalid = () => new FileProcessingProgress(recordsRead: 1, invalidRecords: 2, errorCount: 1);
        Action tooManyValidated = () => new FileProcessingProgress(recordsRead: 1, invalidRecords: 1, unitsValidated: 1, errorCount: 1);
        Action tooManyAttempted = () => new FileProcessingProgress(recordsRead: 1, unitsValidated: 1, unitsAttempted: 2);
        Action tooManyExecuted = () => new FileProcessingProgress(recordsRead: 1, unitsValidated: 1, unitsExecuted: 1);
        Action invalidWithoutErrors = () => new FileProcessingProgress(recordsRead: 1, invalidRecords: 1);

        tooManyInvalid.Should().Throw<ArgumentException>();
        tooManyValidated.Should().Throw<ArgumentException>();
        tooManyAttempted.Should().Throw<ArgumentException>();
        tooManyExecuted.Should().Throw<ArgumentException>();
        invalidWithoutErrors.Should().Throw<ArgumentException>();
    }

    private static FileProcessingIdentity CreateIdentity() => new("customer", "1", "store/customer.csv", "attempt-1", "revision-1");

    private sealed class ReconciliationContext(FileProcessingIdentity identity) : FileProcessingContext(identity, FileProcessingMode.ValidateThenExecute)
    {
        public decimal Total { get; set; }
    }
}
