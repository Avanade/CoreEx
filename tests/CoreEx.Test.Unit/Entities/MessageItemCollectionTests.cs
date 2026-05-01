using CoreEx.Entities;

namespace CoreEx.Test.Unit.Entities;

[TestFixture]
public class MessageItemCollectionTests
{
    [Test]
    public void Ctor_Default_Empty()
    {
        var coll = new MessageItemCollection();
        coll.Should().BeEmpty();
    }

    [Test]
    public void Ctor_WithMessages()
    {
        var msgs = new List<MessageItem>
        {
            MessageItem.CreateMessage(MessageType.Info, "info"),
            MessageItem.CreateMessage(MessageType.Error, "error")
        };
        var coll = new MessageItemCollection(msgs);
        coll.Should().BeEquivalentTo(msgs);
    }

    [Test]
    public void ContainsType_TrueAndFalse()
    {
        var coll = new MessageItemCollection
        {
            MessageItem.CreateMessage(MessageType.Info, "info"),
            MessageItem.CreateMessage(MessageType.Error, "error"),
            MessageItem.CreateMessage(MessageType.Warning, "warn")
        };
        coll.ContainsType(MessageType.Info).Should().BeTrue();
        coll.ContainsType(MessageType.Error).Should().BeTrue();
        coll.ContainsType(MessageType.Warning).Should().BeTrue();
        coll.ContainsType((MessageType)99).Should().BeFalse();
    }

    [Test]
    public void GetMessagesForType_ReturnsFiltered()
    {
        var coll = new MessageItemCollection
        {
            MessageItem.CreateMessage(MessageType.Info, "info1"),
            MessageItem.CreateMessage(MessageType.Error, "error1"),
            MessageItem.CreateMessage(MessageType.Info, "info2"),
            MessageItem.CreateMessage(MessageType.Error, "error2")
        };
        var infos = coll.GetMessagesForType(MessageType.Info);
        infos.Should().HaveCount(2);
        infos.Should().OnlyContain(x => x.Type == MessageType.Info);
        infos.Should().BeEquivalentTo([MessageItem.CreateMessage(MessageType.Info, "info1"), MessageItem.CreateMessage(MessageType.Info, "info2")]);

        var errors = coll.GetMessagesForType(MessageType.Error);
        errors.Should().HaveCount(2);
        errors.Should().OnlyContain(x => x.Type == MessageType.Error);
    }

    [Test]
    public void GetMessagesForType_EmptyResult()
    {
        var coll = new MessageItemCollection
        {
            MessageItem.CreateMessage(MessageType.Info, "info")
        };
        var warnings = coll.GetMessagesForType(MessageType.Warning);
        warnings.Should().BeEmpty();
    }
}