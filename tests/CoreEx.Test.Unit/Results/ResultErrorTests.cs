using CoreEx.Entities;
using CoreEx.Localization;
using CoreEx.Results;

namespace CoreEx.Test.Unit.Results;

public class ResultErrorTests
{
    [Test]
    public void ValidationError_LText_Null()
    {
        var result = Result.ValidationError((LText?)null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        ((ValidationException)result.Error).Message.Should().NotBeNull();
    }

    [Test]
    public void ValidationError_LText_Value()
    {
        var ltext = new LText("Validation failed");
        var result = Result.ValidationError(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        ((ValidationException)result.Error).Message.Should().Contain("Validation failed");
    }

    [Test]
    public void ValidationError_IEnumerable_Null()
    {
        var result = Result.ValidationError((IEnumerable<MessageItem>?)null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
    }

    [Test]
    public void ValidationError_IEnumerable_List()
    {
        var items = new List<MessageItem> { new(MessageType.Error, new LText("msg")) };
        var result = Result.ValidationError(items);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        ((ValidationException)result.Error).Messages.Should().ContainSingle();
    }

    [Test]
    public void ValidationError_MessageItem()
    {
        var item = new MessageItem(MessageType.Error, new LText("msg"));
        var result = Result.ValidationError(item);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        ((ValidationException)result.Error).Messages.Should().ContainSingle();
    }

    [Test]
    public void ConflictError_Null()
    {
        var result = Result.ConflictError(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictException>();
    }

    [Test]
    public void ConflictError_Value()
    {
        var ltext = new LText("conflict");
        var result = Result.ConflictError(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictException>();
        ((ConflictException)result.Error).Message.Should().Contain("conflict");
    }

    [Test]
    public void ConcurrencyError_Null()
    {
        var result = Result.ConcurrencyError(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConcurrencyException>();
    }

    [Test]
    public void ConcurrencyError_Value()
    {
        var ltext = new LText("concurrency");
        var result = Result.ConcurrencyError(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConcurrencyException>();
        ((ConcurrencyException)result.Error).Message.Should().Contain("concurrency");
    }

    [Test]
    public void DuplicateError_Null()
    {
        var result = Result.DuplicateError(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<DuplicateException>();
    }

    [Test]
    public void DuplicateError_Value()
    {
        var ltext = new LText("duplicate");
        var result = Result.DuplicateError(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<DuplicateException>();
        ((DuplicateException)result.Error).Message.Should().Contain("duplicate");
    }

    [Test]
    public void NotFoundError_Null()
    {
        var result = Result.NotFoundError(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
    }

    [Test]
    public void NotFoundError_Value()
    {
        var ltext = new LText("notfound");
        var result = Result.NotFoundError(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        ((NotFoundException)result.Error).Message.Should().Contain("notfound");
    }

    [Test]
    public void TransientError_Null()
    {
        var result = Result.TransientError(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<TransientException>();
    }

    [Test]
    public void TransientError_Value()
    {
        var ltext = new LText("transient");
        var result = Result.TransientError(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<TransientException>();
        ((TransientException)result.Error).Message.Should().Contain("transient");
    }

    [Test]
    public void AuthenticationError_Null()
    {
        var result = Result.AuthenticationError(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<AuthenticationException>();
    }

    [Test]
    public void AuthenticationError_Value()
    {
        var ltext = new LText("auth");
        var result = Result.AuthenticationError(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<AuthenticationException>();
        ((AuthenticationException)result.Error).Message.Should().Contain("auth");
    }

    [Test]
    public void AuthorizationError_Null()
    {
        var result = Result.AuthorizationError(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<AuthorizationException>();
    }

    [Test]
    public void AuthorizationError_Value()
    {
        var ltext = new LText("author");
        var result = Result.AuthorizationError(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<AuthorizationException>();
        ((AuthorizationException)result.Error).Message.Should().Contain("author");
    }
}