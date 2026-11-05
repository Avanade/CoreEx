namespace CoreEx.Validation.Test.Unit;

public class ValidatorExceptionConfigurationTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Failure_ConfiguresOriginalMessagesOnce(bool returnResult)
    {
        var validator = CreateValidator();
        var value = new Probe();
        var args = new ValidationArgs { FullyQualifiedJsonEntityName = "request" };
        var expected = await validator.ValidateAsync(value, args);
        var calls = 0;
        ValidationException? configured = null;
        Action<ValidationException> configure = ex =>
        {
            calls++;
            configured = ex;
            ex.Messages.Should().BeEquivalentTo(expected.Messages);
            ex.ErrorCode = "invalid-request";
            ex.ShouldBeLogged = true;
        };

        ValidationException exception;
        if (returnResult)
        {
            var result = await validator.ValidateWithResultAsync(value, args, configure, default);
            result.IsFailure.Should().BeTrue();
            exception = result.Error.Should().BeOfType<ValidationException>().Subject;
        }
        else
        {
            Func<Task> action = () => validator.ValidateAndThrowAsync(value, args, configure, default);
            exception = (await action.Should().ThrowAsync<ValidationException>()).Which;
        }

        calls.Should().Be(1);
        exception.Should().BeSameAs(configured);
        exception.ErrorCode.Should().Be("invalid-request");
        exception.ShouldBeLogged.Should().BeTrue();
        exception.Messages.Should().HaveCount(1);
        exception.Messages![0].Property.Should().Be("request.name");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Success_DoesNotConfigureAndPreservesValue(bool returnResult)
    {
        var validator = CreateValidator();
        var value = new Probe { Name = "Valid" };
        Action<ValidationException> configure = _ => throw new InvalidOperationException("Must not run.");

        if (returnResult)
        {
            var result = await validator.ValidateWithResultAsync(value, null, configure, default);
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeSameAs(value);
        }
        else
            await validator.ValidateAndThrowAsync(value, null, configure, default);
    }

    [Test]
    public async Task ExistingAndNullConfigurationOverloads_DoNotRetainConfiguration()
    {
        var validator = CreateValidator();
        var value = new Probe();
        await validator.ValidateWithResultAsync(value, null, ex => ex.ErrorCode = "first", default);

        var result = await validator.ValidateWithResultAsync(value, null);
        result.Error.Should().BeOfType<ValidationException>().Which.ErrorCode.Should().BeNull();
        result = await validator.ValidateWithResultAsync(value, args: null, CancellationToken.None);
        result.Error.Should().BeOfType<ValidationException>().Which.ErrorCode.Should().BeNull();
        result = await validator.ValidateWithResultAsync(value, args: null, default);
        result.Error.Should().BeOfType<ValidationException>().Which.ErrorCode.Should().BeNull();
        result = await validator.ValidateWithResultAsync(value, null, null, default);
        result.Error.Should().BeOfType<ValidationException>().Which.ErrorCode.Should().BeNull();

        Func<Task> action = () => validator.ValidateAndThrowAsync(value, null);
        (await action.Should().ThrowAsync<ValidationException>()).Which.ErrorCode.Should().BeNull();
        action = () => validator.ValidateAndThrowAsync(value, CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
        action = () => validator.ValidateAndThrowAsync(value, args: null, CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
        action = () => validator.ValidateAndThrowAsync(value, args: null, default);
        await action.Should().ThrowAsync<ValidationException>();
        action = () => validator.ValidateAndThrowAsync(value, null, null, default);
        await action.Should().ThrowAsync<ValidationException>();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ValidationFailureOutsideRules_PropagatesUnchanged(bool returnResult)
    {
        var failure = new InvalidOperationException("Dependency failed.");
        var validator = CreateValidator().AdditionalAsync((_, _) => throw failure);
        Func<Task> action = () => returnResult
            ? validator.ValidateWithResultAsync(new Probe { Name = "Valid" }, null, _ => Assert.Fail("Must not configure."), default)
            : validator.ValidateAndThrowAsync(new Probe { Name = "Valid" }, null, _ => Assert.Fail("Must not configure."), default);

        (await action.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Cancellation_IsForwardedAndNotConfigured(bool returnResult)
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var failure = new OperationCanceledException(source.Token);
        var validator = CreateValidator().AdditionalAsync((_, token) =>
        {
            token.Should().Be(source.Token);
            throw failure;
        });
        Func<Task> action = () => returnResult
            ? validator.ValidateWithResultAsync(new Probe { Name = "Valid" }, null, _ => Assert.Fail("Must not configure."), source.Token)
            : validator.ValidateAndThrowAsync(new Probe { Name = "Valid" }, null, _ => Assert.Fail("Must not configure."), source.Token);

        (await action.Should().ThrowAsync<OperationCanceledException>()).Which.Should().BeSameAs(failure);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ConfigurationException_PropagatesUnchanged(bool returnResult)
    {
        var failure = new InvalidOperationException("Configuration failed.");
        var validator = CreateValidator();
        Func<Task> action = () => returnResult
            ? validator.ValidateWithResultAsync(new Probe(), null, _ => throw failure, default)
            : validator.ValidateAndThrowAsync(new Probe(), null, _ => throw failure, default);

        (await action.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
    }

    [Test]
    public async Task InterfaceAndBaseReferences_SupportConfiguration()
    {
        var validator = CreateValidator();
        Abstractions.IValidatorEx<Probe> contract = validator;
        Func<Task> action = () => contract.ValidateAndThrowAsync(new Probe(), null, ex => ex.ErrorCode = "interface", default);
        (await action.Should().ThrowAsync<ValidationException>()).Which.ErrorCode.Should().Be("interface");

        Abstractions.ValidatorBase<Probe, Validator<Probe>> baseValidator = validator;
        action = () => baseValidator.ValidateAndThrowAsync(new Probe(), null, ex => ex.ErrorCode = "base", default);
        (await action.Should().ThrowAsync<ValidationException>()).Which.ErrorCode.Should().Be("base");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ConvenienceOverloads_ConfigureOnceOnlyOnFailure(bool returnResult)
    {
        var validator = CreateValidator();
        var calls = 0;
        Action<ValidationException> configure = ex =>
        {
            calls++;
            ex.ErrorCode = "convenient";
        };
        var valid = new Probe { Name = "Valid" };

        if (returnResult)
        {
            (await validator.ValidateWithResultAsync(valid, configure, default)).Value.Should().BeSameAs(valid);
            var result = await validator.ValidateWithResultAsync(new Probe(), configure, default);
            result.Error.Should().BeOfType<ValidationException>().Which.ErrorCode.Should().Be("convenient");
        }
        else
        {
            await validator.ValidateAndThrowAsync(valid, configure, default);
            Func<Task> action = () => validator.ValidateAndThrowAsync(new Probe(), configure, default);
            (await action.Should().ThrowAsync<ValidationException>()).Which.ErrorCode.Should().Be("convenient");
        }

        calls.Should().Be(1);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ConvenienceOverloads_RejectNullConfigurationBeforeValidation(bool returnResult)
    {
        var validator = CreateValidator().AdditionalAsync((_, _) => throw new InvalidOperationException("Must not validate."));
        // Intentionally violate the non-null contract to verify the runtime guard.
        Action action = () =>
        {
            if (returnResult)
                _ = validator.ValidateWithResultAsync(new Probe(), configure: null!, default);
            else
                _ = validator.ValidateAndThrowAsync(new Probe(), configure: null!, default);
        };

        action.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Test]
    public async Task ConvenienceOverloads_AreAvailableThroughInterfaceAndBaseReferences()
    {
        var validator = CreateValidator();
        Abstractions.IValidatorEx<Probe> contract = validator;
        Func<Task> action = () => contract.ValidateAndThrowAsync(new Probe(), ex => ex.ErrorCode = "interface", default);
        (await action.Should().ThrowAsync<ValidationException>()).Which.ErrorCode.Should().Be("interface");

        Abstractions.ValidatorBase<Probe, Validator<Probe>> baseValidator = validator;
        action = () => baseValidator.ValidateAndThrowAsync(new Probe(), ex => ex.ErrorCode = "base", default);
        (await action.Should().ThrowAsync<ValidationException>()).Which.ErrorCode.Should().Be("base");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ContextThrowOnError_ConfiguresOnlyFailureAndReturnsSameContext(bool throughInterface)
    {
        var validator = CreateValidator();
        var valid = await validator.ValidateAsync(new Probe { Name = "Valid" });
        var context = (ValidationContext<Probe>)valid;
        var calls = 0;
        Action<ValidationException> configure = ex =>
        {
            calls++;
            ex.ErrorCode = "context";
        };
        (throughInterface ? valid.ThrowOnError(configure) : context.ThrowOnError(configure)).Should().BeSameAs(valid);
        calls.Should().Be(0);

        var invalid = await validator.ValidateAsync(new Probe());
        var expected = invalid.Messages;
        Action action = () =>
        {
            if (throughInterface)
                invalid.ThrowOnError(configure);
            else
                ((ValidationContext<Probe>)invalid).ThrowOnError(configure);
        };
        var exception = action.Should().Throw<ValidationException>().Which;
        exception.ErrorCode.Should().Be("context");
        exception.Messages.Should().BeEquivalentTo(expected);
        calls.Should().Be(1);
        // Intentionally violate the non-null contract to exercise the runtime guard.
        Action nullAction = () => context.ThrowOnError(null!);
        nullAction.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    private static Validator<Probe> CreateValidator() => Validator.Create<Probe>().HasProperty(x => x.Name, rule => rule.Mandatory());

    private sealed class Probe
    {
        public string? Name { get; set; }
    }
}
