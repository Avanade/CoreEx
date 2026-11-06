namespace CoreEx.Test.Unit;

public class ValidationResultConfigurationTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void DefaultImplementation_ReturnsSameResultOrConfiguresSameException(bool fails)
    {
        var exception = new ValidationException("Invalid.");
        var implementation = new CustomResult(fails ? exception : null);
        CoreEx.Validation.IValidationResult result = implementation;
        var calls = 0;
        Action<ValidationException> configure = ex =>
        {
            calls++;
            ex.Should().BeSameAs(exception);
            ex.ErrorCode = "configured";
        };

        if (fails)
        {
            Action action = () => result.ThrowOnError(configure);
            action.Should().Throw<ValidationException>().Which.Should().BeSameAs(exception);
            exception.ErrorCode.Should().Be("configured");
        }
        else
            result.ThrowOnError(configure).Should().BeSameAs(result);

        calls.Should().Be(fails ? 1 : 0);
        implementation.Conversions.Should().Be(1);
    }

    [Test]
    public void DefaultImplementation_PreservesUnrelatedExceptionAndStack()
    {
        var exception = new InvalidOperationException("Original.");
        try { throw exception; }
        catch (InvalidOperationException) { }
        var stack = exception.StackTrace;
        CoreEx.Validation.IValidationResult result = new CustomResult(exception);
        Action action = () => result.ThrowOnError(_ => Assert.Fail("Must not configure."));

        action.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(exception);
        exception.StackTrace.Should().StartWith(stack);
    }

    [Test]
    public void DefaultImplementation_RejectsNullBeforeConversion()
    {
        var implementation = new CustomResult(null);
        CoreEx.Validation.IValidationResult result = implementation;
        // Intentionally violate the non-null contract to exercise the runtime guard.
        Action action = () => result.ThrowOnError(null!);

        action.Should().Throw<ArgumentNullException>().WithParameterName("configure");
        implementation.Conversions.Should().Be(0);
    }

    [Test]
    public void DefaultImplementation_PropagatesConfigurationException()
    {
        CoreEx.Validation.IValidationResult result = new CustomResult(new ValidationException());
        var failure = new InvalidOperationException("Configuration failed.");
        Action action = () => result.ThrowOnError(_ => throw failure);

        action.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(failure);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MultiValidatorResult_SupportsFluentAndInterfaceCalls(bool throughInterface)
    {
        var result = new CoreEx.Validation.MultiValidatorResult();
        var calls = 0;
        Action<ValidationException> configure = ex =>
        {
            calls++;
            ex.ErrorCode = "multi";
        };
        (throughInterface ? ((CoreEx.Validation.IValidationResult)result).ThrowOnError(configure) : result.ThrowOnError(configure)).Should().BeSameAs(result);
        calls.Should().Be(0);
        result.Messages.Add(MessageItem.CreateErrorMessage("name", "Name is required."));
        Action action = () =>
        {
            if (throughInterface)
                ((CoreEx.Validation.IValidationResult)result).ThrowOnError(configure);
            else
                result.ThrowOnError(configure);
        };
        var exception = action.Should().Throw<ValidationException>().Which;
        exception.ErrorCode.Should().Be("multi");
        exception.Messages.Should().HaveCount(1);
        calls.Should().Be(1);
    }

    private sealed class CustomResult(Exception? exception) : CoreEx.Validation.IValidationResult
    {
        public object? Value => null;

        public bool HasErrors => exception is not null;

        public MessageItemCollection? Messages => null;

        public int Conversions { get; private set; }

        public Exception? ToException()
        {
            Conversions++;
            return exception;
        }

        public CoreEx.Validation.IValidationResult ThrowOnError() => throw new NotSupportedException();

        public Result ToResult() => exception is null ? Result.Success : new Result(exception);
    }
}
