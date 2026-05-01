using CoreEx.Results;

namespace CoreEx.Test.Unit.Results;

public class ResultTTests
{
    [Test]
    public void DefaultCtor_ShouldBeSuccess()
    {
        var result = new Result<int>();
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(0);
    }

    [Test]
    public void ValueCtor_ShouldBeSuccess()
    {
        var result = new Result<string>("abc");
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("abc");
    }

    [Test]
    public void CtorWithException_ShouldBeFailure()
    {
        var ex = new InvalidOperationException("fail");
        var result = new Result<int>(ex);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ex);
    }

    [Test]
    public void Value_ShouldThrowOnFailure()
    {
        var ex = new Exception("fail");
        var result = new Result<string>(ex);
        Action act = () => { var _ = result.Value; };
        act.Should().Throw<Exception>().WithMessage("fail");
    }

    [Test]
    public void ThrowOnError_ShouldThrowOnFailure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        Action act = () => result.ThrowOnError();
        act.Should().Throw<Exception>().WithMessage("fail");
    }

    [Test]
    public void ThrowOnError_ShouldNotThrowOnSuccess()
    {
        var result = new Result<int>(123);
        var returned = result.ThrowOnError();
        returned.IsSuccess.Should().BeTrue();
        returned.Value.Should().Be(123);
    }

    [Test]
    public void IsFailureOfType_ShouldReturnTrueForExactType()
    {
        var ex = new InvalidOperationException();
        var result = new Result<int>(ex);
        result.IsFailureOfType<InvalidOperationException>().Should().BeTrue();
        result.IsFailureOfType<ArgumentException>().Should().BeFalse();
    }

    [Test]
    public void Required_ShouldReturnValidationErrorOnDefault()
    {
        var result = new Result<int>(0);
        var required = result.Required("Test");
        required.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Required_ShouldReturnSelfOnNonDefault()
    {
        var result = new Result<int>(5);
        var required = result.Required("Test");
        required.IsSuccess.Should().BeTrue();
        required.Value.Should().Be(5);
    }

    [Test]
    public void ImplicitOperator_FromException()
    {
        Exception ex = new("fail");
        Result<int> result = ex;
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ex);
    }

    [Test]
    public void ImplicitOperator_FromValue()
    {
        Result<string> result = "abc";
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("abc");
    }

    [Test]
    public void ImplicitOperator_ToValue()
    {
        Result<string> result = "abc";
        string value = result;
        value.Should().Be("abc");
    }

    [Test]
    public void Equals_And_Operators()
    {
        var r1 = new Result();
        var r2 = new Result();
        var r3 = new Result(new Exception("fail"));
        var r4 = new Result(new Exception("fail"));
        var r5 = new Result(new Exception("fail2"));

        (r1 == r2).Should().BeTrue();
        (r1 != r2).Should().BeFalse();
        (r1 == r3).Should().BeFalse();
        (r3 != r1).Should().BeTrue();
        (r3 == r4).Should().BeTrue(); // Different exception instances but same message.
        (r4 == r5).Should().BeFalse(); // Different exception instances and different message.
    }

    [Test]
    public void ToString_And_DebuggerString()
    {
        var r1 = new Result<string>("abc");
        var r2 = new Result<string>(new Exception("fail"));
        r1.ToString().Should().StartWith("Success: abc");
        r2.ToString().Should().StartWith("Failure: fail");
    }

    [Test]
    public async Task AsValueTask_ShouldReturnSelf()
    {
        var result = new Result<int>(42);
        var valueTask = result.AsTask();
        var awaited = await valueTask;
        awaited.Should().Be(result);
    }
}