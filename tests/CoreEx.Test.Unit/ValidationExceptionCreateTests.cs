namespace CoreEx.Test.Unit;

[TestFixture]
public partial class ValidationExceptionCreateTests
{
    public class Person
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public int? OptionalAge { get; set; }
        [System.ComponentModel.DataAnnotations.Display(Name = "Preferred contact name")]
        public string? ContactName { get; set; }
        public Address Address => throw new InvalidOperationException("The expression must not execute getters.");
        public Person? Parent { get; set; }
        public List<Address> Addresses { get; set; } = [];
        public Address[] AddressArray { get; set; } = [];
        public IList<Address> AddressList { get; set; } = new List<Address>();
        public IReadOnlyList<Address> ReadOnlyAddresses { get; set; } = [];
        public Dictionary<string, Address> AddressesByType { get; set; } = [];
        public IDictionary<string, Address> AddressDictionary { get; set; } = new Dictionary<string, Address>();
        public IReadOnlyDictionary<string, List<Address>> AddressGroups { get; set; } = new Dictionary<string, List<Address>>();
        public Dictionary<int, Address> NumericAddresses { get; set; } = [];
        public int ComputedIndex => throw new InvalidOperationException("Index getters must not execute.");
        public static string StaticName => "Name";
        public string Field = "";
    }

    public class Address
    {
        [Localization("SpookyStreet", "Street address")]
        public string? Street { get; set; }
        public int HouseNumber { get; set; }
    }

    public class InheritedPerson : Person;

    [Contract]
    private partial class NamedPerson
    {
        [JsonPropertyName("homeAddress")]
        public NamedAddress? Address { get; set; }
    }

    [Contract]
    private partial class NamedAddress
    {
        [JsonPropertyName("street_name")]
        public string? Street { get; set; }
    }

    [Test]
    public void OmittedOrNullMessage_UsesMetadataFriendlyText()
    {
        ValidationException.Create<Person>(x => x.Name).Messages.Should().ContainSingle().Which.Text.ToString().Should().Be("Name is invalid.");
        ValidationException.Create<Person>(x => x.Name, null).Messages.Should().ContainSingle().Which.Text.ToString().Should().Be("Name is invalid.");
        ValidationException.Create<Person>(x => x.ContactName).Messages.Should().ContainSingle().Which.Text.ToString().Should().Be("Preferred contact name is invalid.");
        ValidationException.Create<Person>(x => x.Address.Street, (LText?)null).Messages.Should().ContainSingle().Which.Text.ToString().Should().Be("Street address is invalid.");
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper };
        var exception = ValidationException.Create<Person>(x => x.Addresses[0].Street, options, (LText?)null);
        var message = exception.Messages.Should().ContainSingle().Subject;
        message.Property.Should().Be("ADDRESSES[0].STREET");
        message.Text.ToString().Should().Be("Street address is invalid.");
        var metadata = RuntimeMetadata.GetJsonPathAndText<Person>(x => x.Address.HouseNumber);
        metadata.Path.Should().Be("address.houseNumber");
        metadata.Text.ToString().Should().Be("House number");
        ValidationException.Create<Person>(x => x.Addresses[0]).Messages.Should().ContainSingle().Which.Text.ToString().Should().Be("Addresses is invalid.");
        ValidationException.Create<Person>(x => x.AddressesByType["home"].Street).Messages.Should().ContainSingle().Which.Text.ToString().Should().Be("Street address is invalid.");
    }

    [Test]
    public void DirectProperty_UsesDefaultJsonNamingAndPreservesExceptionSemantics()
    {
        var exception = ValidationException.Create<Person>(x => x.Name, "Name is spooky.");

        var message = exception.Messages.Should().ContainSingle().Subject;
        message.Property.Should().Be(RuntimeMetadata.GetForExpression<Person, string?>(x => x.Name).GetJsonName());
        message.Text.ToString().Should().Be("Name is spooky.");
        message.Type.Should().Be(MessageType.Error);
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.ErrorType.Should().Be("validation");
    }

    [Test]
    public void NestedProperty_ResolvesEverySegmentWithoutExecutingGetters()
    {
        var exception = ValidationException.Create<Person>(x => x.Address.Street, "Street is spooky.");

        exception.Messages.Should().ContainSingle().Which.Property.Should().Be("address.street");
        // Null-forgiving is safe because nullable intermediate values are never evaluated.
        RuntimeMetadata.GetJsonPath<Person>(x => x.Parent!.Parent!.Address.Street).Should().Be("parent.parent.address.street");
        RuntimeMetadata.GetJsonPath<InheritedPerson>(x => x.Address.Street).Should().Be("address.street");
    }

    [Test]
    public void ExplicitNames_TakePrecedenceAtEveryLevel()
    {
        // Null-forgiving is safe: only the expression structure is inspected, not the nullable address value.
        var exception = ValidationException.Create<NamedPerson>(x => x.Address!.Street, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper }, "Spooky.");

        exception.Messages.Should().ContainSingle().Which.Property.Should().Be("homeAddress.street_name");
    }

    [Test]
    public void ExplicitOptions_ApplyToEverySegment()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper };
        var exception = ValidationException.Create<Person>(x => x.Address.HouseNumber, options, "House {0} is spooky.", 13);

        var message = exception.Messages.Should().ContainSingle().Subject;
        message.Property.Should().Be("ADDRESS.HOUSE_NUMBER");
        message.Text.ToString().Should().Be("House 13 is spooky.");
        RuntimeMetadata.GetJsonPath<Person>(x => x.Address.HouseNumber, new JsonSerializerOptions()).Should().Be("Address.HouseNumber");
    }

    [Test]
    public void Boxing_SupportsValueTypesAndNullableValueTypes()
    {
        ValidationException.Create<Person>(x => x.Age, "Spooky.").Messages.Should().ContainSingle().Which.Property.Should().Be("age");
        ValidationException.Create<Person>(x => x.OptionalAge, "Spooky.").Messages.Should().ContainSingle().Which.Property.Should().Be("optionalAge");
        ValidationException.Create<Person>(x => x.Address.HouseNumber, "Spooky.").Messages.Should().ContainSingle().Which.Property.Should().Be("address.houseNumber");
    }

    [Test]
    public void FormattedAndPreformattedLText_PreserveExistingFactoryBehavior()
    {
        var expected = ValidationException.Create("name", "Name {0} is spooky.", "Casper");
        var actual = ValidationException.Create<Person>(x => x.Name, "Name {0} is spooky.", "Casper");
        actual.Messages.Should().ContainSingle().Which.Text.Should().Be(expected.Messages.Should().ContainSingle().Subject.Text);

        var text = new LText("SpookyName", "Name {0} is spooky.", "Casper");
        ValidationException.Create<Person>(x => x.Name, text).Messages.Should().ContainSingle().Which.Text.Should()
            .Be(ValidationException.Create("name", text).Messages.Should().ContainSingle().Subject.Text);
        ValidationException.Create<Person>(x => x.Name, text).Messages.Should().ContainSingle().Which.Text.ToString().Should().Be("Name Casper is spooky.");
        ValidationException.Create<Person>(x => x.Name, null, text).Messages.Should().ContainSingle().Which.Property.Should().Be("name");
    }

    [Test]
    public void NullExpression_IsRejected()
    {
        Expression<Func<Person, object?>>? expression = null;
        // Deliberately pass null to verify the runtime guard.
        var action = () => ValidationException.Create(expression!, "Spooky.");
        action.Should().Throw<ArgumentNullException>().WithParameterName("propertyExpression");
    }

    [Test]
    public void ArrayAndListIndexes_UseValidatorNotationWithoutAccessingItems()
    {
        RuntimeMetadata.GetJsonPath<Person>(x => x.AddressArray[2].Street).Should().Be("addressArray[2].street");
        RuntimeMetadata.GetJsonPath<Person>(x => x.Addresses[2].Street).Should().Be("addresses[2].street");
        RuntimeMetadata.GetJsonPath<Person>(x => x.AddressList[2].Street).Should().Be("addressList[2].street");
        RuntimeMetadata.GetJsonPath<Person>(x => x.ReadOnlyAddresses[2].Street).Should().Be("readOnlyAddresses[2].street");
        RuntimeMetadata.GetJsonPath<Person>(x => x.AddressArray[2]).Should().Be("addressArray[2]");
        RuntimeMetadata.GetJsonPath<Person>(x => x.AddressesByType["home"].HouseNumber).Should().Be("addressesByType.home.houseNumber");
    }

    [TestCase("home")]
    [TestCase("HomeAddress")]
    [TestCase("home.with[brackets]")]
    [TestCase("")]
    public void DictionaryKeys_UseSerializerPolicyAsInValidators(string key)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper, DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseUpper };
        ValidationException.Create<Person>(x => x.AddressesByType[key].Street, options, "Spooky.")
            .Messages.Should().ContainSingle().Which.Property.Should().Be($"ADDRESSES_BY_TYPE.{options.DictionaryKeyPolicy.ConvertName(key)}.STREET");
        RuntimeMetadata.GetJsonPath<Person>(x => x.AddressDictionary[key].Street, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            .Should().Be($"addressDictionary.{key}.street");
    }

    [Test]
    public void CapturedLocals_UseCurrentValuesAndSupportMixedNesting()
    {
        var index = 1;
        var key = "home";
        Expression<Func<Person, object?>> expression = x => x.AddressGroups[key][index].Street;
        RuntimeMetadata.GetJsonPath(expression).Should().Be("addressGroups.home[1].street");
        index = 3;
        key = "work";
        RuntimeMetadata.GetJsonPath(expression).Should().Be("addressGroups.work[3].street");
    }

    [Test]
    public void NullDictionaryKey_IsRejected()
    {
        string? key = null;
        // Deliberately pass a null key to verify rejection without accessing the dictionary.
        var action = () => RuntimeMetadata.GetJsonPath<Person>(x => x.AddressesByType[key!].Street);
        action.Should().Throw<ArgumentException>().WithParameterName("propertyExpression");
    }

    [Test]
    public void UnsupportedExpressions_AreRejected()
    {
        var captured = new Person();
        Expression<Func<Person, object?>>[] expressions =
        [
            x => x,
            x => "constant",
            x => x.Age.ToString(),
            x => captured.Name,
            x => Person.StaticName,
            x => x.Field,
            x => x.Addresses.ToArray()[0].Street,
            x => x.Addresses[-1].Street,
            x => x.Addresses[x.Age].Street,
            x => x.Addresses[captured.ComputedIndex].Street,
            x => x.Addresses[GetIndex()].Street,
            x => x.Addresses[1 + captured.Age].Street,
            x => x.NumericAddresses[0].Street,
            x => x.AddressesByType[GetKey()].Street,
            x => (long)x.Age,
            x => ((Person)(object)x).Name
        ];

        foreach (var expression in expressions)
        {
            var action = () => ValidationException.Create(expression, "Spooky.");
            action.Should().Throw<ArgumentException>().WithParameterName("propertyExpression");
        }
    }

    private static int GetIndex() => throw new InvalidOperationException("Index methods must not execute.");

    private static string GetKey() => throw new InvalidOperationException("Key methods must not execute.");
}
