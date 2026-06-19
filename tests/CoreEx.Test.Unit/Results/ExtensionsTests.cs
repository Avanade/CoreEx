using CoreEx.Results;

namespace CoreEx.Test.Unit.Results;

public class ExtensionsTests
{
    [Test]
    public void Bind_T_U_Success()
    {
        var result = new Result<int>(5);
        var ret = result.Bind<int, string>(i => new Result<string>(i.ToString()));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("5");
    }

    [Test]
    public void Bind_T_U_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = result.Bind<int, string>(i => new Result<string>(i.ToString()));
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public void Bind_T_U_Success_TAssignableToU()
    {
        var result = new Result<string>("abc");
        var ret = result.Bind<string, object>();
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }

    [Test]
    public void Bind_T_U_Success_TNotAssignableToU()
    {
        var result = new Result<int>(42);
        var ret = result.Bind<int, string>();
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(default);
    }

    [Test]
    public void Bind_T_U_Failure_TAssignableToU()
    {
        var ex = new Exception("fail");
        var result = new Result<string>(ex);
        var ret = result.Bind<string, object>();
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public void Bind_T_U_Failure_TNotAssignableToU()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = result.Bind<int, string>();
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public void Bind_T_Func_Success()
    {
        var result = new Result();
        var ret = result.Bind(() => new Result<int>(123));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(123);
    }

    [Test]
    public void Bind_T_Func_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result(ex);
        var ret = result.Bind(() => new Result<int>(123));
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public void Bind_T_Success()
    {
        var result = new Result();
        var ret = result.Bind<int>();
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(default);
    }

    [Test]
    public void Bind_T_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result(ex);
        var ret = result.Bind<int>();
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public void Bind_T_ResultT_Success()
    {
        var result = new Result<int>(42);
        var ret = result.Bind();
        ret.IsSuccess.Should().BeTrue();
        ret.IsFailure.Should().BeFalse();
    }

    [Test]
    public void Bind_T_ResultT_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = result.Bind();
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public void Combine_Result_Result_BothSuccess()
    {
        var r1 = new Result();
        var r2 = new Result();
        var ret = r1.Combine(r2);
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Combine_Result_Result_ThisFailure()
    {
        var ex = new Exception("fail1");
        var r1 = new Result(ex);
        var r2 = new Result();
        var ret = r1.Combine(r2);
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public void Combine_Result_Result_OtherFailure()
    {
        var ex = new Exception("fail2");
        var r1 = new Result();
        var r2 = new Result(ex);
        var ret = r1.Combine(r2);
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public void Combine_Result_Result_BothFailure()
    {
        var ex1 = new Exception("fail1");
        var ex2 = new Exception("fail2");
        var r1 = new Result(ex1);
        var r2 = new Result(ex2);
        var ret = r1.Combine(r2);
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().BeOfType<AggregateException>();
        var agg = (AggregateException)ret.Error;
        agg.InnerExceptions.Should().Contain([ex1, ex2]);
    }

    [Test]
    public void Combine_Result_ResultT_BothSuccess()
    {
        var r1 = new Result();
        var r2 = new Result<int>(42);
        var ret = r1.Combine(r2);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(42);
    }

    [Test]
    public void Combine_Result_ResultT_ThisFailure()
    {
        var ex = new Exception("fail1");
        var r1 = new Result(ex);
        var r2 = new Result<int>(42);
        var ret = r1.Combine(r2);
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public void Combine_Result_ResultT_OtherFailure()
    {
        var ex = new Exception("fail2");
        var r1 = new Result();
        var r2 = new Result<int>(ex);
        var ret = r1.Combine(r2);
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public void Combine_Result_ResultT_BothFailure()
    {
        var ex1 = new Exception("fail1");
        var ex2 = new Exception("fail2");
        var r1 = new Result(ex1);
        var r2 = new Result<int>(ex2);
        var ret = r1.Combine(r2);
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().BeOfType<AggregateException>();
        var agg = (AggregateException)ret.Error;
        agg.InnerExceptions.Should().Contain([ex1, ex2]);
    }

    [Test]
    public void Combine_ResultT_ResultU_BothSuccess_TAssignableToU()
    {
        var r1 = new Result<string>("abc");
        var r2 = new Result<object>(new object());
        var ret = r1.Combine<string, object>(r2);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }

    [Test]
    public void Combine_ResultT_ResultU_BothSuccess_TNotAssignableToU()
    {
        var r1 = new Result<int>(42);
        var r2 = new Result<string>("abc");
        var ret = r1.Combine<int, string>(r2);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }

    [Test]
    public void Combine_ResultT_ResultU_ThisFailure()
    {
        var ex = new Exception("fail1");
        var r1 = new Result<int>(ex);
        var r2 = new Result<string>("abc");
        var ret = r1.Combine<int, string>(r2);
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public void Combine_ResultT_ResultU_OtherFailure()
    {
        var ex = new Exception("fail2");
        var r1 = new Result<int>(42);
        var r2 = new Result<string>(ex);
        var ret = r1.Combine<int, string>(r2);
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public void Combine_ResultT_ResultU_BothFailure()
    {
        var ex1 = new Exception("fail1");
        var ex2 = new Exception("fail2");
        var r1 = new Result<int>(ex1);
        var r2 = new Result<string>(ex2);
        var ret = r1.Combine<int, string>(r2);
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().BeOfType<AggregateException>();
        var agg = (AggregateException)ret.Error;
        agg.InnerExceptions.Should().Contain([ex1, ex2]);
    }

    [Test]
    public void AsResult_ResultT_Success()
    {
        var result = new Result<int>(42);
        var ret = result.AsResult();
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void AsResult_ResultT_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = result.AsResult();
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public async Task AsResult_ValueTask_ResultT_Success()
    {
        var result = new Result<int>(42);
        var ret = await result.AsTask().AsResultAsync();
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task AsResult_ValueTask_ResultT_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = await result.AsTask().AsResultAsync();
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public void AsResult_T_U_Success_TAssignableToU()
    {
        var result = new Result<string>("abc");
        var ret = result.AsResult<string, object>();
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }

    [Test]
    public void AsResult_T_U_Success_TNotAssignableToU()
    {
        var result = new Result<int>(42);
        var ret = result.AsResult<int, string>();
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(default);
    }

    [Test]
    public void AsResult_T_U_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = result.AsResult<int, string>();
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public async Task AsResult_ValueTask_T_U_Success_TAssignableToU()
    {
        var result = new Result<string>("abc");
        var ret = await result.AsTask().AsResultAsync<string, object>();
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }

    [Test]
    public async Task AsResult_ValueTask_T_U_Success_TNotAssignableToU()
    {
        var result = new Result<int>(42);
        var ret = await result.AsTask().AsResultAsync<int, string>();
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(default);
    }

    [Test]
    public async Task AsResult_ValueTask_T_U_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = await result.AsTask().AsResultAsync<int, string>();
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }
}