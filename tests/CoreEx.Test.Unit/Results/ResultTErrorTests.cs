using CoreEx.Entities;
using CoreEx.Localization;
using CoreEx.Results;

namespace CoreEx.Test.Unit.Results;

public class ResultTErrorTests
{
    [Test]
    public void ValidationError_LText_Null()
    {
        var result = Result<int>.ValidationError((LText?)null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        ((ValidationException)result.Error).Message.Should().NotBeNull();
    }

    [Test]
    public void ValidationError_LText_Value()
    {
        var ltext = new LText("Validation failed");
        var result = Result<int>.ValidationError(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        ((ValidationException)result.Error).Message.Should().Contain("Validation failed");
    }

    [Test]
    public void ValidationError_IEnumerable_Null()
    {
        var result = Result<int>.ValidationError((IEnumerable<MessageItem>?)null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
    }

    [Test]
    public void ValidationError_IEnumerable_List()
    {
        var items = new List<MessageItem> { new(MessageType.Error, new LText("msg")) };
        var result = Result<int>.ValidationError(items);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        ((ValidationException)result.Error).Messages.Should().ContainSingle();
    }

    [Test]
    public void ValidationError_MessageItem()
    {
        var item = new MessageItem(MessageType.Error, new LText("msg"));
        var result = Result<int>.ValidationError(item);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        ((ValidationException)result.Error).Messages.Should().ContainSingle();
    }

    [Test]
    public void ConflictError_Null()
    {
        var result = Result<int>.ConflictError(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictException>();
    }

    [Test]
    public void ConflictError_Value()
    {
        var ltext = new LText("conflict");
        var result = Result<int>.ConflictError(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictException>();
        ((ConflictException)result.Error).Message.Should().Contain("conflict");
    }

    [Test]
    public void ConcurrencyError_Null()
    {
        var result = Result<int>.ConcurrencyError(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConcurrencyException>();
    }

    [Test]
    public void ConcurrencyError_Value()
    {
        var ltext = new LText("concurrency");
        var result = Result<int>.ConcurrencyError(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConcurrencyException>();
        ((ConcurrencyException)result.Error).Message.Should().Contain("concurrency");
    }

    [Test]
    public void DuplicateError_Null()
    {
        var result = Result<int>.DuplicateError(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<DuplicateException>();
    }

    [Test]
    public void DuplicateError_Value()
    {
        var ltext = new LText("duplicate");
        var result = Result<int>.DuplicateError(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<DuplicateException>();
        ((DuplicateException)result.Error).Message.Should().Contain("duplicate");
    }

    [Test]
    public void NotFoundError_Null()
    {
        var result = Result<int>.NotFoundError(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
    }

    [Test]
    public void NotFoundError_Value()
    {
        var ltext = new LText("notfound");
        var result = Result<int>.NotFoundError(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        ((NotFoundException)result.Error).Message.Should().Contain("notfound");
    }

    [Test]
    public void TransientError_Null()
    {
        var result = Result<int>.TransientError(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<TransientException>();
    }

    [Test]
    public void TransientError_Value()
    {
        var ltext = new LText("transient");
        var result = Result<int>.TransientError(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<TransientException>();
        ((TransientException)result.Error).Message.Should().Contain("transient");
    }

    [Test]
    public void AuthenticationError_Null()
    {
        var result = Result<int>.AuthenticationError(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<AuthenticationException>();
    }

    [Test]
    public void AuthenticationError_Value()
    {
        var ltext = new LText("auth");
        var result = Result<int>.AuthenticationError(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<AuthenticationException>();
        ((AuthenticationException)result.Error).Message.Should().Contain("auth");
    }

    [Test]
    public void AuthorizationError_Null()
    {
        var result = Result<int>.AuthorizationError(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<AuthorizationException>();
    }

    [Test]
    public void AuthorizationError_Value()
    {
        var ltext = new LText("author");
        var result = Result<int>.AuthorizationError(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<AuthorizationException>();
        ((AuthorizationException)result.Error).Message.Should().Contain("author");
    }
}