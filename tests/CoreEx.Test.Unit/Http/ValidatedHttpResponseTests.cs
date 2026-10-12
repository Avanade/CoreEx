namespace CoreEx.Test.Unit.Http;

[TestFixture]
public class ValidatedHttpResponseTests
{
    private sealed class Widget
    {
        public string? Name { get; set; }
    }

    private sealed class Validator(Func<Widget, CancellationToken, Task<CoreEx.Validation.IValidationResult<Widget>>> validate) : CoreEx.Validation.IValidator<Widget>
    {
        public int Calls { get; private set; }

        public Task<CoreEx.Validation.IValidationResult<Widget>> ValidateAsync(Widget value, CancellationToken cancellationToken = default)
        {
            Calls++;
            return validate(value, cancellationToken);
        }
    }

    private sealed class ValidationResult(Widget? value, bool hasErrors = false, Exception? error = null, CoreEx.Entities.MessageItemCollection? messages = null) : CoreEx.Validation.IValidationResult<Widget>
    {
        public Widget? Value => value;
        public bool HasErrors => hasErrors;
        public CoreEx.Entities.MessageItemCollection? Messages => messages;
        public Exception? ToException() => error;
        public CoreEx.Validation.IValidationResult ThrowOnError()
        {
            if (error is not null)
                throw error;
            return this;
        }
        public CoreEx.Results.Result ToResult() => error is null ? CoreEx.Results.Result.Success : CoreEx.Results.Result.Fail(error);
    }

    private static Validator CreateValidator(Widget? replacement, Exception? error = null)
        => new((_, _) => Task.FromResult<CoreEx.Validation.IValidationResult<Widget>>(new ValidationResult(replacement, error is not null, error)));

    private static HttpResponseMessage Json(string json, System.Net.HttpStatusCode status = System.Net.HttpStatusCode.OK, string mediaType = "application/json")
        => new(status) { Content = new StringContent(json, System.Text.Encoding.UTF8, mediaType) };

    private static Task<CoreEx.Results.Result<Widget?>> ConsumeAsync(CoreEx.Http.ValidatedHttpResponse<Widget> response, string method, System.Text.Json.JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        => method switch
        {
            "Result" => ConsumeRequiredAsync(response, options, cancellationToken),
            "OptionalResult" => response.ToResultOrDefaultAsync(options, cancellationToken),
            "Value" => ConsumeValueAsync(response, false, options, cancellationToken),
            "OptionalValue" => ConsumeValueAsync(response, true, options, cancellationToken),
            _ => throw new InvalidOperationException(method)
        };

    private static async Task<CoreEx.Results.Result<Widget?>> ConsumeRequiredAsync(CoreEx.Http.ValidatedHttpResponse<Widget> response, System.Text.Json.JsonSerializerOptions? options, CancellationToken cancellationToken)
    {
        var result = await response.ToResultAsync(options, cancellationToken);
        return result.IsFailure ? CoreEx.Results.Result.Fail(result.Error) : CoreEx.Results.Result<Widget?>.Ok(result.Value);
    }

    private static async Task<CoreEx.Results.Result<Widget?>> ConsumeValueAsync(CoreEx.Http.ValidatedHttpResponse<Widget> response, bool optional, System.Text.Json.JsonSerializerOptions? options, CancellationToken cancellationToken)
        => CoreEx.Results.Result<Widget?>.Ok(optional ? await response.GetValueOrDefaultAsync(options, cancellationToken) : await response.GetValueAsync(options, cancellationToken));

    [TestCase("Result")]
    [TestCase("OptionalResult")]
    [TestCase("Value")]
    [TestCase("OptionalValue")]
    public async Task Success_ReturnsValidationResultValue(string method)
    {
        using var response = Json("{\"name\":\"original\"}");
        var replacement = new Widget { Name = "replacement" };
        var validator = CreateValidator(replacement);
        var result = await ConsumeAsync(response.WithValidator(validator), method);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(replacement);
        validator.Calls.Should().Be(1);
        response.Content.Headers.ContentType.Should().NotBeNull();
    }

    [TestCase("Result")]
    [TestCase("OptionalResult")]
    [TestCase("Value")]
    [TestCase("OptionalValue")]
    public async Task InvalidValue_PreservesStructuredInnerException(string method)
    {
        using var response = Json("{\"name\":\"sensitive-value\"}");
        var error = CoreEx.ValidationException.Create("name", "Name is invalid.");
        var validator = CreateValidator(null, error);
        HttpRequestException exception;
        if (method.EndsWith("Result", StringComparison.Ordinal))
        {
            var result = await ConsumeAsync(response.WithValidator(validator), method);
            result.IsFailure.Should().BeTrue();
            exception = result.Error.Should().BeOfType<HttpRequestException>().Subject;
        }
        else
        {
            Func<Task> act = async () => await ConsumeAsync(response.WithValidator(validator), method);
            exception = (await act.Should().ThrowAsync<HttpRequestException>()).Which;
        }

        exception.InnerException.Should().BeSameAs(error);
        exception.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        exception.Message.Should().Contain(nameof(Widget)).And.NotContain("sensitive-value");
        error.Messages.Should().ContainSingle().Which.Property.Should().Be("name");
        validator.Calls.Should().Be(1);
    }

    [TestCase("Result")]
    [TestCase("OptionalResult")]
    [TestCase("Value")]
    [TestCase("OptionalValue")]
    public async Task SuccessfulValidation_NullValue_PreservesRequiredSemantics(string method)
    {
        using var response = Json("{}");
        var validator = CreateValidator(null);
        if (method == "Value")
        {
            Func<Task> act = async () => await ConsumeAsync(response.WithValidator(validator), method);
            await act.Should().ThrowAsync<HttpRequestException>();
        }
        else
        {
            var result = await ConsumeAsync(response.WithValidator(validator), method);
            if (method == "Result")
                result.Error.Should().BeOfType<HttpRequestException>();
            else
                result.Value.Should().BeNull();
        }
        validator.Calls.Should().Be(1);
    }

    [TestCase("null", false)]
    [TestCase("", false)]
    [TestCase("", true)]
    public async Task MissingValue_SkipsValidation(string body, bool noContent)
    {
        var validator = CreateValidator(new Widget());
        foreach (var method in new[] { "Result", "OptionalResult", "Value", "OptionalValue" })
        {
            using var response = Json(body, noContent ? System.Net.HttpStatusCode.NoContent : System.Net.HttpStatusCode.OK);
            if (method == "Value")
            {
                Func<Task> act = async () => await ConsumeAsync(response.WithValidator(validator), method);
                await act.Should().ThrowAsync<HttpRequestException>();
            }
            else
            {
                var result = await ConsumeAsync(response.WithValidator(validator), method);
                if (method == "Result")
                    result.Error.Should().BeOfType<HttpRequestException>();
                else
                    result.Value.Should().BeNull();
            }
        }
        validator.Calls.Should().Be(0);
    }

    [TestCase("application/json", "{}", false)]
    [TestCase("application/problem+json", "{\"status\":500,\"title\":\"Dependency failed\"}", false)]
    [TestCase("application/problem+json", "{\"status\":400,\"title\":\"Business failed\",\"errorType\":\"business\"}", true)]
    public async Task HttpError_PreservesClassificationAndSkipsValidation(string mediaType, string body, bool business)
    {
        using var response = Json(body, business ? System.Net.HttpStatusCode.BadRequest : System.Net.HttpStatusCode.InternalServerError, mediaType);
        var validator = CreateValidator(new Widget());
        var expected = await response.ToResultAsync<Widget>();
        var actual = await response.WithValidator(validator).ToResultAsync();
        actual.Error.GetType().Should().Be(expected.Error.GetType());
        actual.Error.Message.Should().Be(expected.Error.Message);
        (await response.WithValidator(validator).ToResultOrDefaultAsync()).IsFailure.Should().BeTrue();
        Func<Task> act = async () => await response.WithValidator(validator).GetValueAsync();
        (await act.Should().ThrowAsync<Exception>()).Which.GetType().Should().Be(expected.Error.GetType());
        validator.Calls.Should().Be(0);
    }

    [TestCase("Result")]
    [TestCase("OptionalResult")]
    [TestCase("Value")]
    [TestCase("OptionalValue")]
    public async Task SerializerOptionsAndToken_AreForwarded(string method)
    {
        using var response = Json("{\"Name\":\"case-sensitive\"}");
        using var cts = new CancellationTokenSource();
        var validator = new Validator((value, token) =>
        {
            value.Name.Should().Be("case-sensitive");
            token.Should().Be(cts.Token);
            return Task.FromResult<CoreEx.Validation.IValidationResult<Widget>>(new ValidationResult(value));
        });
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = false };
        (await ConsumeAsync(response.WithValidator(validator), method, options, cts.Token)).IsSuccess.Should().BeTrue();
    }

    [TestCase("Result")]
    [TestCase("OptionalResult")]
    [TestCase("Value")]
    [TestCase("OptionalValue")]
    public async Task UnexpectedValidatorException_PropagatesUnchanged(string method)
    {
        using var response = Json("{}");
        var error = new InvalidOperationException("Validator bug.");
        var validator = new Validator((_, _) => throw error);
        Func<Task> act = async () => await ConsumeAsync(response.WithValidator(validator), method);
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(error);
    }

    [TestCase("Result")]
    [TestCase("OptionalResult")]
    [TestCase("Value")]
    [TestCase("OptionalValue")]
    public async Task CancellationDuringValidation_Propagates(string method)
    {
        using var response = Json("{}");
        using var cts = new CancellationTokenSource();
        var validator = new Validator((_, token) =>
        {
            cts.Cancel();
            token.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Cancellation was not observed.");
        });
        Func<Task> act = async () => await ConsumeAsync(response.WithValidator(validator), method, cancellationToken: cts.Token);
        (await act.Should().ThrowAsync<OperationCanceledException>()).Which.CancellationToken.Should().Be(cts.Token);
    }

    [Test]
    public async Task CancellationBeforeDeserialization_SkipsValidation()
    {
        using var response = Json("{}");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var validator = CreateValidator(new Widget());
        Func<Task> act = async () => await response.WithValidator(validator).ToResultAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
        validator.Calls.Should().Be(0);
    }

    [Test]
    public async Task MalformedJson_PropagatesWithoutValidation()
    {
        using var response = Json("{");
        var validator = CreateValidator(new Widget());
        Func<Task> act = async () => await response.WithValidator(validator).ToResultAsync();
        await act.Should().ThrowAsync<System.Text.Json.JsonException>();
        validator.Calls.Should().Be(0);
    }

    [Test]
    public async Task MissingValidationException_PreservesMessages()
    {
        using var response = Json("{}");
        var messages = new CoreEx.Entities.MessageItemCollection { CoreEx.Entities.MessageItem.CreateErrorMessage("name", "Invalid name.") };
        var validator = new Validator((value, _) => Task.FromResult<CoreEx.Validation.IValidationResult<Widget>>(new ValidationResult(value, true, messages: messages)));
        var result = await response.WithValidator(validator).ToResultAsync();
        result.Error.InnerException.Should().BeOfType<CoreEx.ValidationException>().Which.Messages.Should().BeEquivalentTo(messages);
    }

    [Test]
    public async Task IndependentWrappers_DoNotChangeUnderlyingResponse()
    {
        using var response = Json("{\"name\":\"original\"}");
        var first = new Widget { Name = "first" };
        var second = new Widget { Name = "second" };
        var firstWrapper = response.WithValidator(CreateValidator(first));
        var secondWrapper = response.WithValidator(CreateValidator(second));
        (await response.GetValueAsync<Widget>()).Name.Should().Be("original");
        // Existing converters consume the content stream; refresh content before each subsequent consumption.
        response.Content.Dispose();
        response.Content = new StringContent("{\"name\":\"original\"}", System.Text.Encoding.UTF8, "application/json");
        (await firstWrapper.GetValueAsync()).Should().BeSameAs(first);
        response.Content.Dispose();
        response.Content = new StringContent("{\"name\":\"original\"}", System.Text.Encoding.UTF8, "application/json");
        (await secondWrapper.GetValueOrDefaultAsync()).Should().BeSameAs(second);
    }

    [Test]
    public void NullArguments_AreRejectedImmediately()
    {
        using var response = Json("{}");
        // Null arguments deliberately exercise runtime guards.
        Action noResponse = () => Extensions.WithValidator(null!, CreateValidator(new Widget()));
        Action noValidator = () => response.WithValidator<Widget>(null!);
        noResponse.Should().Throw<ArgumentNullException>();
        noValidator.Should().Throw<ArgumentNullException>();
    }
}
