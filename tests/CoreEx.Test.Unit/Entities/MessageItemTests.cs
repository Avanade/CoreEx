using CoreEx.Entities;
using CoreEx.Localization;

namespace CoreEx.Test.Unit.Entities;

[TestFixture]
public class MessageItemTests
{
    [Test]
    public void Constructor_SetsProperties()
    {
        var ltext = new LText("test message");
        var item = new MessageItem(MessageType.Warning, ltext, "Prop1");
        item.Type.Should().Be(MessageType.Warning);
        item.Text.Should().Be(ltext);
        item.Property.Should().Be("Prop1");
    }

    [Test]
    public void Constructor_NullProperty()
    {
        var ltext = new LText("msg");
        var item = new MessageItem(MessageType.Info, ltext);
        item.Type.Should().Be(MessageType.Info);
        item.Text.Should().Be(ltext);
        item.Property.Should().BeNull();
    }

    [Test]
    public void Type_GetSet()
    {
        var item = new MessageItem(MessageType.Info, new LText("x"))
        {
            Type = MessageType.Error
        };
        item.Type.Should().Be(MessageType.Error);
    }

    [Test]
    public void Text_GetSet()
    {
        var item = new MessageItem(MessageType.Info, new LText("x"));
        var ltext = new LText("new text");
        item.Text = ltext;
        item.Text.Should().Be(ltext);
    }

    [Test]
    public void Property_GetSet()
    {
        var item = new MessageItem(MessageType.Info, new LText("x"))
        {
            Property = "abc"
        };
        item.Property.Should().Be("abc");
    }

    [Test]
    public void WithProperty_SetsPropertyAndReturnsSelf()
    {
        var item = new MessageItem(MessageType.Info, new LText("x"));
        var ret = item.WithProperty("p1");
        ret.Should().BeSameAs(item);
        item.Property.Should().Be("p1");
    }
}