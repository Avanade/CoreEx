namespace CoreEx.Test.Unit.Results;

[TestFixture]
public class ValidationErrorExpressionTests
{
    public class Person
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public List<Person> Children { get; set; } = [];
        public Dictionary<string, Person> Contacts { get; set; } = [];
    }

    [Test]
    public void OmittedOrNullMessages_UseExceptionFactoryDefault()
    {
        var result = CoreEx.Results.Result.ValidationError<Person>(x => x.Name);
        var typed = CoreEx.Results.Result<int>.ValidationError<Person>(x => x.Name, null);
        AssertError(result.Error, "name", "Name is invalid.");
        AssertError(typed.Error, "name", "Name is invalid.");
        var configured = CoreEx.Results.Result.ValidationError<Person>(x => x.Children[0].Name, configure: e => e.Messages.Should().ContainSingle());
        AssertError(configured.Error, "children[0].name", "Name is invalid.");
    }

    [Test]
    public void PlainTextAndPreformattedMessages_MatchExceptionFactory()
    {
        var text = new LText("DuplicateName", "Name {0} is taken.", "Casper");
        var result = CoreEx.Results.Result.ValidationError<Person>(x => x.Name, text);
        var typed = CoreEx.Results.Result<int>.ValidationError<Person>(x => x.Name, text);

        result.IsFailure.Should().BeTrue();
        typed.IsFailure.Should().BeTrue();
        AssertError(result.Error, "name", "Name Casper is taken.");
        AssertError(typed.Error, "name", "Name Casper is taken.");
        AssertError(CoreEx.Results.Result.ValidationError<Person>(x => x.Age, "Invalid.").Error, "age", "Invalid.");
    }

    [Test]
    public void FormattingAndIndexing_MatchExceptionFactory()
    {
        var text = new LText("DuplicateName", "Name {0} is taken.", "Casper");
        var result = CoreEx.Results.Result.ValidationError<Person>(x => x.Children[2].Name, text);
        var typed = CoreEx.Results.Result<int>.ValidationError<Person>(x => x.Children[2].Name, text);

        AssertError(result.Error, "children[2].name", "Name Casper is taken.");
        AssertError(typed.Error, "children[2].name", "Name Casper is taken.");
    }

    [Test]
    public void AdvancedScenarios_UseExceptionFactoryFallback()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper, DictionaryKeyPolicy = JsonNamingPolicy.CamelCase };
        var key = "HomeAddress";
        var text = new LText("DuplicateName", "Name {0} is taken.", "Casper");
        CoreEx.Results.Result plain = ValidationException.Create<Person>(x => x.Contacts[key].Name, options, text);
        CoreEx.Results.Result<int> typedPlain = ValidationException.Create<Person>(x => x.Contacts[key].Name, options, text);
        CoreEx.Results.Result formatted = ValidationException.Create<Person>(x => x.Contacts[key].Name, options, "Name {0} is taken.", "Casper");
        CoreEx.Results.Result<int> typedFormatted = ValidationException.Create<Person>(x => x.Contacts[key].Name, options, "Name {0} is taken.", "Casper");

        foreach (var error in new[] { plain.Error, typedPlain.Error, formatted.Error, typedFormatted.Error })
            AssertError(error, "CONTACTS.homeAddress.NAME", "Name Casper is taken.");

    }

    [Test]
    public void Configuration_ReceivesTheCreatedException()
    {
        var exceptions = new List<ValidationException>();
        var result = CoreEx.Results.Result.ValidationError<Person>(x => x.Name, "Invalid.", e => exceptions.Add(e));
        var typed = CoreEx.Results.Result<int>.ValidationError<Person>(x => x.Name, "Invalid.", e => exceptions.Add(e));
        exceptions.Should().HaveCount(2);
        result.Error.Should().BeSameAs(exceptions[0]);
        typed.Error.Should().BeSameAs(exceptions[1]);
    }

    [Test]
    public void InvalidExpressions_AreRejectedRatherThanReturnedAsFailures()
    {
        var result = () => CoreEx.Results.Result.ValidationError<Person>(x => x.Age.ToString(), "Invalid.");
        var typed = () => CoreEx.Results.Result<int>.ValidationError<Person>(x => x.Age.ToString(), "Invalid.");
        result.Should().Throw<ArgumentException>().WithParameterName("propertyExpression");
        typed.Should().Throw<ArgumentException>().WithParameterName("propertyExpression");
    }

    private static void AssertError(Exception? error, string path, string text)
    {
        var exception = error.Should().BeOfType<ValidationException>().Subject;
        var message = exception.Messages.Should().ContainSingle().Subject;
        message.Property.Should().Be(path);
        message.Text.ToString().Should().Be(text);
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
