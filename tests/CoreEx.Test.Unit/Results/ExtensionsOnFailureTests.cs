using CoreEx.Results;

namespace CoreEx.Test.Unit.Results;

public class ExtensionsOnFailureTests
{
    [Test]
    public void Result_OnFailure_Action_Success()
    {
        var called = false;
        var result = new Result();
        var ret = result.OnFailure(e => called = true);
        called.Should().BeFalse();
        ret.Should().Be(result);
    }

    [Test]
    public void Result_OnFailure_Action_Failure()
    {
        var called = false;
        var ex = new Exception("fail");
        var result = new Result(ex);
        var ret = result.OnFailure(e => { called = true; e.Error.Should().Be(ex); });
        called.Should().BeTrue();
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Result_OnFailure_Func_Success()
    {
        var result = new Result();
        var ret = result.OnFailure(_ => Result.Fail(new Exception("fail2")));
        ret.Should().Be(result);
    }

    [Test]
    public void Result_OnFailure_Func_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = result.OnFailure(_ => Result.Success);
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ResultT_OnFailure_Action_Success()
    {
        var called = false;
        var result = new Result<int>(42);
        var ret = result.OnFailure(e => called = true);
        called.Should().BeFalse();
        ret.Should().Be(result);
    }

    [Test]
    public void ResultT_OnFailure_Action_Failure()
    {
        var called = false;
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = result.OnFailure(e => { called = true; e.Error.Should().Be(ex); });
        called.Should().BeTrue();
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ResultT_OnFailure_FuncT_Success()
    {
        var result = new Result<int>(42);
        var ret = result.OnFailure(e => 99);
        ret.Should().Be(result);
    }

    [Test]
    public void ResultT_OnFailure_FuncT_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = result.OnFailure(e => 99);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(99);
    }

    [Test]
    public void ResultT_OnFailure_FuncResultT_Success()
    {
        var result = new Result<int>(42);
        var ret = result.OnFailure(e => new Result<int>(99));
        ret.Should().Be(result);
    }

    [Test]
    public void ResultT_OnFailure_FuncResultT_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = result.OnFailure(e => new Result<int>(99));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(99);
    }

    [Test]
    public void Result_OnFailureAsT_FuncT_Success()
    {
        var result = new Result();
        var ret = result.OnFailureAs(_ => 123);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(default);
    }

    [Test]
    public void Result_OnFailureAsT_FuncT_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = result.OnFailureAs(_ => 123);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(123);
    }

    [Test]
    public void Result_OnFailureAsT_FuncResultT_Success()
    {
        var result = new Result();
        var ret = result.OnFailureAs(_ => new Result<string>("abc"));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(default);
    }

    [Test]
    public void Result_OnFailureAsT_FuncResultT_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = result.OnFailureAs(_ => new Result<string>("abc"));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }

    [Test]
    public void ResultT_OnFailureAs_ActionT_Success()
    {
        var called = false;
        var result = new Result<int>(42);
        var ret = result.OnFailureAs(e => called = true);
        called.Should().BeFalse();
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ResultT_OnFailureAs_ActionT_Failure()
    {
        var called = false;
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = result.OnFailureAs(e => { called = true; e.Error.Should().Be(ex); });
        called.Should().BeTrue();
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ResultT_OnFailureAs_FuncTResult_Success()
    {
        var result = new Result<int>(42);
        var ret = result.OnFailureAs(e => new Result());
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ResultT_OnFailureAs_FuncTResult_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = result.OnFailureAs(e => new Result());
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ResultT_OnFailureAs_FuncTU_Success()
    {
        var result = new Result<int>(42);
        var ret = result.OnFailureAs<int, string>(e => "abc");
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(default);
    }

    [Test]
    public void ResultT_OnFailureAs_FuncTU_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = result.OnFailureAs<int, string>(e => "abc");
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }

    [Test]
    public void ResultT_OnFailureAs_FuncTResultU_Success()
    {
        var result = new Result<int>(42);
        var ret = result.OnFailureAs<int, string>(e => new Result<string>("abc"));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(default);
    }

    [Test]
    public void ResultT_OnFailureAs_FuncTResultU_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = result.OnFailureAs<int, string>(e => new Result<string>("abc"));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }

    // --- ASYNC ---

    [Test]
    public async Task Result_OnFailureAsync_Func_Success()
    {
        var called = false;
        var result = new Result();
        var ret = await result.OnFailureAsync(async _ => { called = true; await Task.Yield(); });
        called.Should().BeFalse();
        ret.Should().Be(result);
    }

    [Test]
    public async Task Result_OnFailureAsync_Func_Failure()
    {
        var called = false;
        var result = new Result(new Exception("fail"));
        var ret = await result.OnFailureAsync(async _ => { called = true; await Task.Yield(); });
        called.Should().BeTrue();
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Result_OnFailureAsync_FuncResult_Success()
    {
        var result = new Result();
        var ret = await result.OnFailureAsync(async _ => { await Task.Yield(); return Result.Fail(new Exception("fail2")); });
        ret.Should().Be(result);
    }

    [Test]
    public async Task Result_OnFailureAsync_FuncResult_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = await result.OnFailureAsync(async _ => { await Task.Yield(); return Result.Success; });
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_OnFailureAsync_ActionT_Success()
    {
        var called = false;
        var result = new Result<int>(42);
        var ret = await result.OnFailureAsync(async e => { called = true; await Task.Yield(); });
        called.Should().BeFalse();
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_OnFailureAsync_ActionT_Failure()
    {
        var called = false;
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = await result.OnFailureAsync(async e => { called = true; e.Error.Should().Be(ex); await Task.Yield(); });
        called.Should().BeTrue();
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_OnFailureAsync_FuncT_Success()
    {
        var result = new Result<int>(42);
        var ret = await result.OnFailureAsync(async e => { await Task.Yield(); return 99; });
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_OnFailureAsync_FuncT_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = await result.OnFailureAsync(async e => { await Task.Yield(); return 99; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(99);
    }

    [Test]
    public async Task ResultT_OnFailureAsync_FuncResultT_Success()
    {
        var result = new Result<int>(42);
        var ret = await result.OnFailureAsync(async e => { await Task.Yield(); return new Result<int>(99); });
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_OnFailureAsync_FuncResultT_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = await result.OnFailureAsync(async e => { await Task.Yield(); return new Result<int>(99); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(99);
    }

    [Test]
    public async Task Result_OnFailureAsAsyncT_FuncT_Success()
    {
        var result = new Result();
        var ret = await result.OnFailureAsAsync(async _ => { await Task.Yield(); return 123; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(default);
    }

    [Test]
    public async Task Result_OnFailureAsAsyncT_FuncT_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = await result.OnFailureAsAsync(async _ => { await Task.Yield(); return 123; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(123);
    }

    [Test]
    public async Task Result_OnFailureAsAsyncT_FuncResultT_Success()
    {
        var result = new Result();
        var ret = await result.OnFailureAsAsync(async _ => { await Task.Yield(); return new Result<string>("abc"); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(default);
    }

    [Test]
    public async Task Result_OnFailureAsAsyncT_FuncResultT_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = await result.OnFailureAsAsync(async _ => { await Task.Yield(); return new Result<string>("abc"); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }

    [Test]
    public async Task ResultT_OnFailureAsAsync_ActionT_Success()
    {
        var called = false;
        var result = new Result<int>(42);
        var ret = await result.AsTask().OnFailureAsAsync(async e => { called = true; await Task.Yield(); });
        called.Should().BeFalse();
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_OnFailureAsAsync_ActionT_Failure()
    {
        var called = false;
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = await result.AsTask().OnFailureAsAsync(async e => { called = true; e.Error.Should().Be(ex); await Task.Yield(); });
        called.Should().BeTrue();
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_OnFailureAsAsync_FuncTResult_Success()
    {
        var result = new Result<int>(42);
        var ret = await result.AsTask().OnFailureAsAsync(async e => { await Task.Yield(); return new Result(); });
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_OnFailureAsAsync_FuncTResult_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = await result.AsTask().OnFailureAsAsync(async e => { await Task.Yield(); return new Result(); });
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_OnFailureAsAsync_FuncTU_Success()
    {
        var result = new Result<int>(42);
        var ret = await result.AsTask().OnFailureAsAsync<int, string>(async e => { await Task.Yield(); return "abc"; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(default);
    }

    [Test]
    public async Task ResultT_OnFailureAsAsync_FuncTU_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = await result.AsTask().OnFailureAsAsync<int, string>(async e => { await Task.Yield(); return "abc"; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }

    [Test]
    public async Task ResultT_OnFailureAsAsync_FuncTResultU_Success()
    {
        var result = new Result<int>(42);
        var ret = await result.AsTask().OnFailureAsAsync<int, string>(async e => { await Task.Yield(); return new Result<string>("abc"); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(default);
    }

    [Test]
    public async Task ResultT_OnFailureAsAsync_FuncTResultU_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = await result.AsTask().OnFailureAsAsync<int, string>(async e => { await Task.Yield(); return new Result<string>("abc"); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }
}