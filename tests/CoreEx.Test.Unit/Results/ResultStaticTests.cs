using CoreEx.Localization;
using CoreEx.Results;

namespace CoreEx.Test.Unit.Results;

public class ResultStaticTests
{
    [Test]
    public void Success_ShouldBeSuccess()
    {
        var result = Result.Success;
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Test]
    public async Task SuccessValueTask_ShouldBeSuccess()
    {
        var result = await Result.SuccessTask;
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Test]
    public void Done_Action_CallsActionAndReturnsSuccess()
    {
        var called = false;
        var result = Result.Done(() => called = true);
        called.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Done_Action_Null_Throws()
    {
        Action act = () => Result.Done(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void OkT_ReturnsSuccessWithValue()
    {
        var result = Result.Ok(123);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(123);
    }

    [Test]
    public void Fail_Exception_ReturnsFailureWithError()
    {
        var ex = new Exception("fail");
        var result = Result.Fail(ex);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ex);
    }

    [Test]
    public void Fail_LText_Null_ReturnsFailureWithValidationException()
    {
        var result = Result.Fail((LText?)null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
    }

    [Test]
    public void Fail_LText_Value_ReturnsFailureWithValidationException()
    {
        var ltext = new LText("fail error");
        var result = Result.Fail(ltext);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error.Message.Should().Contain("fail error");
    }
}