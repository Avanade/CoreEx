using CoreEx.Results;
using CoreEx.Results.Abstractions;

namespace CoreEx.Test.Unit.Results;

public class ResultTests
{
    [Test]
    public void DefaultCtor_ShouldBeSuccess()
    {
        var result = new Result();
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Test]
    public void CtorWithException_ShouldBeFailure()
    {
        var ex = new InvalidOperationException("fail");
        var result = new Result(ex);
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ex);
    }

    [Test]
    public void Error_ShouldThrowOnSuccess()
    {
        var result = new Result();
        Action act = () => { var _ = result.Error; };
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void IResult_Value_ShouldThrowOnFailure()
    {
        var ex = new Exception("fail");
        IResult result = new Result(ex);
        Action act = () => { var _ = result.Value; };
        act.Should().Throw<Exception>().WithMessage("fail");
    }

    [Test]
    public void ThrowOnError_ShouldThrowOnFailure()
    {
        var ex = new Exception("fail");
        var result = new Result(ex);
        Action act = () => result.ThrowOnError();
        act.Should().Throw<Exception>().WithMessage("fail");
    }

    [Test]
    public void ThrowOnError_ShouldNotThrowOnSuccess()
    {
        var result = new Result();
        var returned = result.ThrowOnError();
        returned.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void IsFailureOfType_ShouldReturnTrueForExactType()
    {
        var ex = new InvalidOperationException();
        var result = new Result(ex);
        result.IsFailureOfType<InvalidOperationException>().Should().BeTrue();
        result.IsFailureOfType<ArgumentException>().Should().BeFalse();
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
        var r1 = new Result();
        var r2 = new Result(new Exception("fail"));
        r1.ToString().Should().Be("Success.");
        r2.ToString().Should().StartWith("Failure: fail");
    }

    [Test]
    public void ImplicitOperator_FromException()
    {
        Exception ex = new("fail");
        Result result = ex;
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ex);
    }

    [Test]
    public async Task AsValueTask_ShouldReturnSelf()
    {
        var result = new Result();
        var valueTask = result.AsTask();
        var awaited = await valueTask;
        awaited.Should().Be(result);
    }
}