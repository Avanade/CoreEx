using CoreEx.Entities;
using CoreEx.Localization;

namespace CoreEx.Test.Unit.Entities;

[TestFixture]
public class MessageItemCreateTests
{
    [Test]
    public void CreateMessage_Type_Text()
    {
        var ltext = new LText("msg1");
        var item = MessageItem.CreateMessage(MessageType.Info, ltext);
        item.Type.Should().Be(MessageType.Info);
        item.Text.Should().Be(ltext);
        item.Property.Should().BeNull();
    }

    [Test]
    public void CreateMessage_Type_Format_Values()
    {
        var ltext = new LText("format {0} {1}");
        var item = MessageItem.CreateMessage(MessageType.Warning, ltext, 1, "x");
        item.Type.Should().Be(MessageType.Warning);
        item.Text.Should().NotBeNull();
        item.Text.Value.KeyAndOrText.Should().Be("format {0} {1}");
        item.Text.Value.Args.Should().BeEquivalentTo(new object[] { 1, "x" });
        item.Property.Should().BeNull();
    }

    [Test]
    public void CreateMessage_Property_Type_Text()
    {
        var ltext = new LText("msg2");
        var item = MessageItem.CreateMessage("PropA", MessageType.Error, ltext);
        item.Property.Should().Be("PropA");
        item.Type.Should().Be(MessageType.Error);
        item.Text.Should().Be(ltext);
    }

    [Test]
    public void CreateMessage_Property_Type_Format_Values()
    {
        var ltext = new LText("format {0}");
        var item = MessageItem.CreateMessage("PropB", MessageType.Info, ltext, 99);
        item.Property.Should().Be("PropB");
        item.Type.Should().Be(MessageType.Info);
        item.Text.Should().NotBeNull();
        item.Text.Value.KeyAndOrText.Should().Be("format {0}");
        item.Text.Value.Args.Should().BeEquivalentTo(new object[] { 99 });
    }

    [Test]
    public void CreateErrorMessage_Property_Text()
    {
        var ltext = new LText("errormsg");
        var item = MessageItem.CreateErrorMessage("PropC", ltext);
        item.Property.Should().Be("PropC");
        item.Type.Should().Be(MessageType.Error);
        item.Text.Should().Be(ltext);
    }

    [Test]
    public void CreateErrorMessage_Property_Format_Values()
    {
        var ltext = new LText("error {0}");
        var item = MessageItem.CreateErrorMessage("PropD", ltext, "fail");
        item.Property.Should().Be("PropD");
        item.Type.Should().Be(MessageType.Error);
        item.Text.Should().NotBeNull();
        item.Text.Value.KeyAndOrText.Should().Be("error {0}");
        item.Text.Value.Args.Should().BeEquivalentTo(new object[] { "fail" });
    }
}