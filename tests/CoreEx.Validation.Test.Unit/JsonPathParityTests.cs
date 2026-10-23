namespace CoreEx.Validation.Test.Unit;

[TestFixture]
public class JsonPathParityTests
{
    public class Person
    {
        [System.Text.Json.Serialization.JsonPropertyName("addressEntries")]
        public List<Address> Addresses { get; set; } = [];
        public Address[] AddressArray { get; set; } = [];
        public Dictionary<string, Address> AddressesByType { get; set; } = [];
        public Dictionary<int, Address> NumericAddresses { get; set; } = [];
        public Dictionary<string, string> Values { get; set; } = [];
    }

    public class Address
    {
        [System.Text.Json.Serialization.JsonPropertyName("street_name")]
        public string? Street { get; set; }
    }

    [TestCase("home")]
    [TestCase("HomeAddress")]
    [TestCase("home.with[brackets]")]
    [TestCase("")]
    [TestCase("ETag")]
    public async Task DictionaryFactoryPath_MatchesValidatorExactly(string key)
    {
        var addressValidator = Validator.Create<Address>().HasProperty(x => x.Street, r => r.Mandatory());
        var validator = Validator.Create<Person>().HasProperty(x => x.AddressesByType, r => r.Dictionary(w => w.WithValueValidator(addressValidator)));
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseUpper,
            DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseUpper
        };
        var result = await validator.ValidateAsync(new Person { AddressesByType = new() { [key] = new() } }, new ValidationArgs { JsonSerializerOptions = options });
        var exception = ValidationException.Create<Person>(x => x.AddressesByType[key].Street, options, "Spooky.");

        exception.Messages.Should().ContainSingle().Which.Property.Should().Be(result.Messages.Should().ContainSingle().Subject.Property);
    }

    [TestCase("HomeAddress", "default")]
    [TestCase("ETag", "default")]
    [TestCase("HomeAddress", "none")]
    [TestCase("HomeAddress", "snake")]
    public async Task DictionaryPaths_MatchSerializedJsonAndPreserveOriginalKey(string key, string policy)
    {
        var options = policy switch
        {
            "none" => new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase },
            "snake" => new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseUpper },
            _ => CoreEx.Json.JsonDefaults.SerializerOptions
        };
        var person = new Person { AddressesByType = new() { [key] = new() } };
        var json = System.Text.Json.JsonSerializer.SerializeToElement(person, options);
        var dictionary = json.GetProperty("addressesByType");
        var serializedKey = dictionary.EnumerateObject().Single().Name;
        var expected = $"addressesByType.{serializedKey}";
        var addressValidator = Validator.Create<Address>().HasProperty(x => x.Street, r => r.Mandatory().When(c => c.GetDictionaryKey<string>() == key));
        var valueValidator = Validator.Create<Person>().HasProperty(x => x.AddressesByType, r => r.Dictionary(w => w.WithValueValidator(addressValidator)));
        var keyValidator = Validator.Create<Person>().HasProperty(x => x.AddressesByType, r => r.Dictionary(w => w.WithKeyValidator(k => k.MaximumLength(1))));
        var args = new ValidationArgs { JsonSerializerOptions = options };

        var valueResult = await valueValidator.ValidateAsync(person, args);
        valueResult.Messages.Should().ContainSingle().Which.Property.Should().Be(expected + ".street_name");
        var keyResult = await keyValidator.ValidateAsync(person, args);
        keyResult.Messages.Should().ContainSingle().Which.Property.Should().Be(expected);
        ValidationException.Create<Person>(x => x.AddressesByType[key].Street, options, "Spooky.")
            .Messages.Should().ContainSingle().Which.Property.Should().Be(expected + ".street_name");

        var clrValue = await valueValidator.ValidateAsync(person, args with { UseJsonNames = false });
        clrValue.Messages.Should().ContainSingle().Which.Property.Should().Be($"AddressesByType[{key}].Street");
        var clrKey = await keyValidator.ValidateAsync(person, args with { UseJsonNames = false });
        clrKey.Messages.Should().ContainSingle().Which.Property.Should().Be($"AddressesByType.{key}");
        person.AddressesByType.Keys.Should().ContainSingle().Which.Should().Be(key);
    }

    [Test]
    public async Task InlineValueAndNonStringKeyPaths_PreserveExpectedBehavior()
    {
        var validator = Validator.Create<Person>()
            .HasProperty(x => x.Values, r => r.Dictionary(w => w.WithValueValidator(v => v.MaximumLength(1))))
            .HasProperty(x => x.NumericAddresses, r => r.Dictionary(w => w.WithValueValidator(
                Validator.Create<Address>().HasProperty(x => x.Street, r => r.Mandatory()))));
        var result = await validator.ValidateAsync(new Person
        {
            Values = new() { ["ETag"] = "too long" },
            NumericAddresses = new() { [13] = new() }
        });

        result.Messages.Should().NotBeNull();
        result.Messages?.Select(x => x.Property).Should().BeEquivalentTo("values.ETag", "numericAddresses.13.street_name");
    }

    [Test]
    public async Task ListAndArrayFactoryPaths_MatchValidatorExactly()
    {
        var addressValidator = Validator.Create<Address>().HasProperty(x => x.Street, r => r.Mandatory());
        var validator = Validator.Create<Person>()
            .HasProperty(x => x.Addresses, r => r.Collection(w => w.WithItemValidator(addressValidator)))
            .HasProperty(x => x.AddressArray, r => r.Collection(w => w.WithItemValidator(addressValidator)));
        var result = await validator.ValidateAsync(new Person { Addresses = [new()], AddressArray = [new()] });

        var listPath = ValidationException.Create<Person>(x => x.Addresses[0].Street, "Spooky.").Messages.Should().ContainSingle().Subject.Property;
        var arrayPath = ValidationException.Create<Person>(x => x.AddressArray[0].Street, "Spooky.").Messages.Should().ContainSingle().Subject.Property;
        result.Messages.Should().NotBeNull();
        result.Messages?.Select(x => x.Property).Should().BeEquivalentTo(listPath, arrayPath);
    }
}
