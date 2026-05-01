using CoreEx.Entities;
using CoreEx.Invokers;
using CoreEx.Validation.Abstractions;
using System.Text.Json;

namespace CoreEx.Validation.Test.Unit;

internal static class Helper
{
    private static JsonSerializerOptions? _jsonSerializerOptions;

    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        if (_jsonSerializerOptions is not null)
            return _jsonSerializerOptions;

        _jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };
        _jsonSerializerOptions.Converters.Add(new IgnoreTypeConverter<Type>());
        _jsonSerializerOptions.Converters.Add(new IgnoreTypeConverter<Exception>());
        _jsonSerializerOptions.Converters.Add(new IgnoreTypeConverter<IServiceProvider>());
        _jsonSerializerOptions.Converters.Add(new IgnoreTypeConverter<JsonSerializerOptions>());
        return _jsonSerializerOptions;
    }

    public static IValidationResult<ValidationValue<T>> ValidateAsSuccess<T>(this IValueValidator<T> validator, ValidationArgs? args = null)
    {
        var r = Invoker.RunSync(() => validator.ThrowIfNull().ValidateAsync(args));
        r.Should().NotBeNull();

        Console.WriteLine(JsonSerializer.Serialize(r, GetJsonSerializerOptions()));
        Console.WriteLine("----");

        r.HasErrors.Should().BeFalse();
        return r;
    }

    public static IValidationResult<ValidationValue<T>> ValidateAsError<T>(this IValueValidator<T> validator, string containsErrorText, ValidationArgs? args = null)
    {
        var r = Invoker.RunSync(() => validator.ThrowIfNull().ValidateAsync(args));
        r.Should().NotBeNull();

        Console.WriteLine(JsonSerializer.Serialize(r, GetJsonSerializerOptions()));
        Console.WriteLine("----");

        r.HasErrors.Should().BeTrue();
        r.Messages.Should().NotBeNull().And.HaveCount(1);
        r.Messages[0].Should().NotBeNull();
        r.Messages[0].Type.Should().Be(MessageType.Error);
        r.Messages[0].Text.ToString().Should().Contain(containsErrorText);
        return r;
    }

    public static IValidationResult<ValidationValue<T>> ValidateAsError<T>(this IValueValidator<T> validator, string propertyName, string containsErrorText)
    {
        var r = Invoker.RunSync(() => validator.ThrowIfNull().ValidateAsync());

        Console.WriteLine(JsonSerializer.Serialize(r, GetJsonSerializerOptions()));
        Console.WriteLine("----");

        r.Should().NotBeNull();
        r.HasErrors.Should().BeTrue();
        r.Messages.Should().NotBeNull().And.HaveCount(1);
        r.Messages[0].Should().NotBeNull();
        r.Messages[0].Type.Should().Be(MessageType.Error);
        r.Messages[0].Property.Should().Be(propertyName);
        r.Messages[0].Text.ToString().Should().Contain(containsErrorText);
        return r;
    }

    public static IValidationResult<T> ValidateAsSuccess<T>(this Validator<T> validator, T value) where T : class
    {
        var r = Invoker.RunSync(() => validator.ThrowIfNull().ValidateAsync(value));
        r.Should().NotBeNull();

        Console.WriteLine(JsonSerializer.Serialize(r, GetJsonSerializerOptions()));
        Console.WriteLine("----");

        r.HasErrors.Should().BeFalse();
        return r;
    }

    public static IValidationResult<T> ValidateAsError<T>(this Validator<T> validator, T value, string propertyName, string containsErrorText) where T : class
    {
        var r = Invoker.RunSync(() => validator.ThrowIfNull().ValidateAsync(value));
        r.Should().NotBeNull();

        Console.WriteLine(JsonSerializer.Serialize(r, GetJsonSerializerOptions()));
        Console.WriteLine("----");

        r.HasErrors.Should().BeTrue();
        r.Messages.Should().NotBeNull().And.HaveCount(1);
        r.Messages[0].Should().NotBeNull();
        r.Messages[0].Type.Should().Be(MessageType.Error);
        r.Messages[0].Property.Should().Be(propertyName);
        r.Messages[0].Text.ToString().Should().Contain(containsErrorText);
        return r;
    }

    private sealed class IgnoreTypeConverter<T> : System.Text.Json.Serialization.JsonConverter<T>
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => default;

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => writer.WriteNullValue();
    }
}