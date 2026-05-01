using CoreEx.Entities;
using CoreEx.Localization;
using System.Net;

namespace CoreEx.Test.Unit;

[TestFixture]
public class CoreExExceptionTests
{
    [Test]
    public void BusinessException_DefaultAndMessage()
    {
        var ltext = new LText("business");
        var ex1 = new BusinessException(ltext);
        ex1.Message.Should().Contain("business");
    }

    [Test]
    public void ValidationException_DefaultAndMessage()
    {
        var ex1 = new ValidationException();
        ex1.Message.Should().NotBeNull();
        ex1.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex1.ErrorType.Should().Be("validation");

        var ltext = new LText("validation");
        var ex2 = new ValidationException(ltext);
        ex2.Message.Should().Contain("validation");

        var item = new MessageItem(MessageType.Error, new LText("msg"));
        var ex3 = new ValidationException(item);
        ex3.Messages.Should().ContainSingle();

        var items = new List<MessageItem> { item };
        var ex4 = new ValidationException(items);
        ex4.Messages.Should().ContainSingle();
    }

    [Test]
    public void ConflictException_DefaultAndMessage()
    {
        var ex1 = new ConflictException();
        ex1.Message.Should().NotBeNull();
        ex1.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex1.ErrorType.Should().Be("conflict");

        var ltext = new LText("conflict");
        var ex2 = new ConflictException(ltext);
        ex2.Message.Should().Contain("conflict");
    }

    [Test]
    public void ConcurrencyException_DefaultAndMessage()
    {
        var ex1 = new ConcurrencyException();
        ex1.Message.Should().NotBeNull();
        ex1.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        ex1.ErrorType.Should().Be("concurrency");

        var ltext = new LText("concurrency");
        var ex2 = new ConcurrencyException(ltext);
        ex2.Message.Should().Contain("concurrency");
    }

    [Test]
    public void DataConsistencyException_DefaultAndMessage()
    {
        var ex1 = new DataConsistencyException();
        ex1.Message.Should().NotBeNull();
        ex1.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        ex1.ErrorType.Should().Be("data-consistency");

        var ltext = new LText("dataconsistency");
        var ex2 = new DataConsistencyException(ltext);
        ex2.Message.Should().Contain("dataconsistency");
    }

    [Test]
    public void DuplicateException_DefaultAndMessage()
    {
        var ex1 = new DuplicateException();
        ex1.Message.Should().NotBeNull();
        ex1.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex1.ErrorType.Should().Be("duplicate");

        var ltext = new LText("duplicate");
        var ex2 = new DuplicateException(ltext);
        ex2.Message.Should().Contain("duplicate");
    }

    [Test]
    public void NotFoundException_DefaultAndMessage()
    {
        var ex1 = new NotFoundException();
        ex1.Message.Should().NotBeNull();
        ex1.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex1.ErrorType.Should().Be("not-found");

        var ltext = new LText("notfound");
        var ex2 = new NotFoundException(ltext);
        ex2.Message.Should().Contain("notfound");
    }

    [Test]
    public void TransientException_DefaultAndMessage()
    {
        var ex1 = new TransientException();
        ex1.Message.Should().NotBeNull();
        ex1.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        ex1.ErrorType.Should().Be("transient");

        var ltext = new LText("transient");
        var ex2 = new TransientException(ltext);
        ex2.Message.Should().Contain("transient");
    }

    [Test]
    public void AuthenticationException_DefaultAndMessage()
    {
        var ex1 = new AuthenticationException();
        ex1.Message.Should().NotBeNull();
        ex1.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex1.ErrorType.Should().Be("authentication");

        var ltext = new LText("auth");
        var ex2 = new AuthenticationException(ltext);
        ex2.Message.Should().Contain("auth");
    }

    [Test]
    public void AuthorizationException_DefaultAndMessage()
    {
        var ex1 = new AuthorizationException();
        ex1.Message.Should().NotBeNull();
        ex1.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex1.ErrorType.Should().Be("authorization");

        var ltext = new LText("author");
        var ex2 = new AuthorizationException(ltext);
        ex2.Message.Should().Contain("author");
    }
}