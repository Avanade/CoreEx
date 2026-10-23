namespace CoreEx.Test.Unit.Entities;

[TestFixture]
public class MessageItemExpressionTests
{
    public class Request
    {
        [JsonPropertyName("product_id")]
        [Localization("ProductReference", "Product reference")]
        public int ProductId { get; set; }
        public List<Request> Children { get; set; } = [];
        public Dictionary<string, Request> References { get; set; } = [];
    }

    [Test]
    public void DefaultMessage_UsesFriendlyMetadataAndExplicitJsonName()
    {
        var message = MessageItem.CreateErrorMessage<Request>(x => x.ProductId);
        message.Property.Should().Be("product_id");
        message.Text.ToString().Should().Be("Product reference is invalid.");
        message.Type.Should().Be(MessageType.Error);
        MessageItem.CreateErrorMessage<Request>(x => x.ProductId, null).Should().Be(message);
        ValidationException.Create<Request>(x => x.ProductId).Messages.Should().ContainSingle().Which.Should().Be(message);
    }

    [Test]
    public void ExplicitOptionsAndPreformattedText_ArePreserved()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper, DictionaryKeyPolicy = JsonNamingPolicy.CamelCase };
        var text = new LText("MissingProduct", "Product {0} was not found.", 13);
        var message = MessageItem.CreateErrorMessage<Request>(x => x.References["HomeAddress"].ProductId, options, text);
        message.Property.Should().Be("REFERENCES.homeAddress.product_id");
        message.Text.Should().Be(text);
        message.Text.ToString().Should().Be("Product 13 was not found.");
        MessageItem.CreateErrorMessage<Request>(x => x.ProductId, text).Text.Should().Be(text);
        MessageItem.CreateErrorMessage<Request>(x => x.Children[0].ProductId, options, null).Text.ToString().Should().Be("Product reference is invalid.");
    }

    [Test]
    public void Messages_CanBeAggregatedWithoutCreatingExceptionsPerItem()
    {
        var messages = new MessageItemCollection
        {
            MessageItem.CreateErrorMessage<Request>(x => x.ProductId),
            MessageItem.CreateErrorMessage<Request>(x => x.Children[0].ProductId, "Child product is missing.")
        };
        var exception = new ValidationException(messages);
        exception.Messages.Should().HaveCount(2);
        exception.Messages?.Select(x => x.Property).Should().BeEquivalentTo("product_id", "children[0].product_id");
    }

    [Test]
    public void InvalidExpression_IsRejected()
    {
        var action = () => MessageItem.CreateErrorMessage<Request>(x => x.ProductId.ToString());
        action.Should().Throw<ArgumentException>().WithParameterName("propertyExpression");
    }
}
