using CoreEx.Results;

namespace CoreEx.Test.Unit.Results;

public class ExtensionsMatchTests
{
    [Test]
    public void Result_Match_Action_Success()
    {
        var okCalled = false;
        var failCalled = false;
        var result = new Result();
        var ret = result.Match(() => okCalled = true, e => failCalled = true);
        okCalled.Should().BeTrue();
        failCalled.Should().BeFalse();
        ret.Should().Be(result);
    }

    [Test]
    public void Result_Match_Action_Failure()
    {
        var okCalled = false;
        var failCalled = false;
        var ex = new Exception("fail");
        var result = new Result(ex);
        var ret = result.Match(() => okCalled = true, e => { failCalled = true; e.Should().Be(ex); });
        okCalled.Should().BeFalse();
        failCalled.Should().BeTrue();
        ret.Should().Be(result);
    }

    [Test]
    public void Result_Match_Func_Success()
    {
        var okCalled = false;
        var failCalled = false;
        var result = new Result();
        var ret = result.Match(
            () => { okCalled = true; return Result.Success; },
            e => { failCalled = true; return Result.Fail(e); });
        okCalled.Should().BeTrue();
        failCalled.Should().BeFalse();
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Result_Match_Func_Failure()
    {
        var okCalled = false;
        var failCalled = false;
        var ex = new Exception("fail");
        var result = new Result(ex);
        var ret = result.Match(
            () => { okCalled = true; return Result.Success; },
            e => { failCalled = true; e.Should().Be(ex); return Result.Fail(e); });
        okCalled.Should().BeFalse();
        failCalled.Should().BeTrue();
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public void Result_MatchAsT_Success()
    {
        var okCalled = false;
        var failCalled = false;
        var result = new Result();
        var ret = result.MatchAs(
            () => { okCalled = true; return Result<int>.Ok(123); },
            e => { failCalled = true; return Result<int>.Fail(e); });
        okCalled.Should().BeTrue();
        failCalled.Should().BeFalse();
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(123);
    }

    [Test]
    public void Result_MatchAsT_Failure()
    {
        var okCalled = false;
        var failCalled = false;
        var ex = new Exception("fail");
        var result = new Result(ex);
        var ret = result.MatchAs(
            () => { okCalled = true; return Result<int>.Ok(123); },
            e => { failCalled = true; e.Should().Be(ex); return Result<int>.Fail(e); });
        okCalled.Should().BeFalse();
        failCalled.Should().BeTrue();
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public void ResultT_Match_ActionT_Success()
    {
        int? okValue = null;
        Exception? failEx = null;
        var result = new Result<int>(42);
        var ret = result.Match(i => okValue = i, e => failEx = e);
        okValue.Should().Be(42);
        failEx.Should().BeNull();
        ret.Should().Be(result);
    }

    [Test]
    public void ResultT_Match_ActionT_Failure()
    {
        int? okValue = null;
        Exception? failEx = null;
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = result.Match(i => okValue = i, e => failEx = e);
        okValue.Should().BeNull();
        failEx.Should().Be(ex);
        ret.Should().Be(result);
    }

    [Test]
    public void ResultT_Match_FuncT_Success()
    {
        var result = new Result<int>(10);
        var ret = result.Match(
            i => new Result<int>(i * 2),
            e => new Result<int>(-1));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(20);
    }

    [Test]
    public void ResultT_Match_FuncT_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = result.Match(
            i => new Result<int>(i * 2),
            e => { e.Should().Be(ex); return new Result<int>(-1); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(-1);
    }

    [Test]
    public void ResultT_MatchAsTU_Success()
    {
        var result = new Result<int>(5);
        var ret = result.MatchAs<int, string>(
            i => new Result<string>((i * 2).ToString()),
            e => new Result<string>("fail"));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("10");
    }

    [Test]
    public void ResultT_MatchAsTU_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = result.MatchAs<int, string>(
            i => new Result<string>((i * 2).ToString()),
            e => { e.Should().Be(ex); return new Result<string>("fail"); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("fail");
    }

    // --- ASYNC ---

    [Test]
    public async Task Result_MatchAsync_Action_Success()
    {
        var okCalled = false;
        var failCalled = false;
        var result = new Result();
        var ret = await result.AsTask().Match(() => okCalled = true, e => failCalled = true);
        okCalled.Should().BeTrue();
        failCalled.Should().BeFalse();
        ret.Should().Be(result);
    }

    [Test]
    public async Task Result_MatchAsync_Action_Failure()
    {
        var okCalled = false;
        var failCalled = false;
        var ex = new Exception("fail");
        var result = new Result(ex);
        var ret = await result.AsTask().Match(() => okCalled = true, e => { failCalled = true; e.Should().Be(ex); });
        okCalled.Should().BeFalse();
        failCalled.Should().BeTrue();
        ret.Should().Be(result);
    }

    [Test]
    public async Task Result_MatchAsync_Func_Success()
    {
        var okCalled = false;
        var failCalled = false;
        var result = new Result();
        var ret = await result.AsTask().Match(
            () => { okCalled = true; return Result.Success; },
            e => { failCalled = true; return Result.Fail(e); });
        okCalled.Should().BeTrue();
        failCalled.Should().BeFalse();
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Result_MatchAsync_Func_Failure()
    {
        var okCalled = false;
        var failCalled = false;
        var ex = new Exception("fail");
        var result = new Result(ex);
        var ret = await result.AsTask().Match(
            () => { okCalled = true; return Result.Success; },
            e => { failCalled = true; e.Should().Be(ex); return Result.Fail(e); });
        okCalled.Should().BeFalse();
        failCalled.Should().BeTrue();
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public async Task Result_MatchAsAsyncT_Success()
    {
        var okCalled = false;
        var failCalled = false;
        var result = new Result();
        var ret = await result.AsTask().MatchAs(
            () => { okCalled = true; return Result<int>.Ok(123); },
            e => { failCalled = true; return Result<int>.Fail(e); });
        okCalled.Should().BeTrue();
        failCalled.Should().BeFalse();
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(123);
    }

    [Test]
    public async Task Result_MatchAsAsyncT_Failure()
    {
        var okCalled = false;
        var failCalled = false;
        var ex = new Exception("fail");
        var result = new Result(ex);
        var ret = await result.AsTask().MatchAs(
            () => { okCalled = true; return Result<int>.Ok(123); },
            e => { failCalled = true; e.Should().Be(ex); return Result<int>.Fail(e); });
        okCalled.Should().BeFalse();
        failCalled.Should().BeTrue();
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public async Task ResultT_MatchAsync_ActionT_Success()
    {
        int? okValue = null;
        Exception? failEx = null;
        var result = new Result<int>(42);
        var ret = await result.AsTask().Match(i => okValue = i, e => failEx = e);
        okValue.Should().Be(42);
        failEx.Should().BeNull();
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_MatchAsync_ActionT_Failure()
    {
        int? okValue = null;
        Exception? failEx = null;
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = await result.AsTask().Match(i => okValue = i, e => failEx = e);
        okValue.Should().BeNull();
        failEx.Should().Be(ex);
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_MatchAsync_FuncT_Success()
    {
        var result = new Result<int>(10);
        var ret = await result.AsTask().Match(
            i => new Result<int>(i * 2),
            e => new Result<int>(-1));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(20);
    }

    [Test]
    public async Task ResultT_MatchAsync_FuncT_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = await result.AsTask().Match(
            i => new Result<int>(i * 2),
            e => { e.Should().Be(ex); return new Result<int>(-1); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(-1);
    }

    [Test]
    public async Task ResultT_MatchAsAsyncTU_Success()
    {
        var result = new Result<int>(5);
        var ret = await result.AsTask().MatchAs<int, string>(
            i => new Result<string>((i * 2).ToString()),
            e => new Result<string>("fail"));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("10");
    }

    [Test]
    public async Task ResultT_MatchAsAsyncTU_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = await result.AsTask().MatchAs<int, string>(
            i => new Result<string>((i * 2).ToString()),
            e => { e.Should().Be(ex); return new Result<string>("fail"); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("fail");
    }

    // --- AsyncFunc ---

    [Test]
    public async Task Result_MatchAsyncFunc_Success()
    {
        var okCalled = false;
        var failCalled = false;
        var result = new Result();
        var ret = await result.MatchAsync(
            async () => { okCalled = true; await Task.Yield(); },
            async e => { failCalled = true; await Task.Yield(); });
        okCalled.Should().BeTrue();
        failCalled.Should().BeFalse();
        ret.Should().Be(result);
    }

    [Test]
    public async Task Result_MatchAsyncFunc_Failure()
    {
        var okCalled = false;
        var failCalled = false;
        var ex = new Exception("fail");
        var result = new Result(ex);
        var ret = await result.MatchAsync(
            async () => { okCalled = true; await Task.Yield(); },
            async e => { failCalled = true; e.Should().Be(ex); await Task.Yield(); });
        okCalled.Should().BeFalse();
        failCalled.Should().BeTrue();
        ret.Should().Be(result);
    }

    [Test]
    public async Task Result_MatchAsyncFuncResult_Success()
    {
        var okCalled = false;
        var failCalled = false;
        var result = new Result();
        var ret = await result.MatchAsync(
            async () => { okCalled = true; await Task.Yield(); return Result.Success; },
            async e => { failCalled = true; await Task.Yield(); return Result.Fail(e); });
        okCalled.Should().BeTrue();
        failCalled.Should().BeFalse();
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Result_MatchAsyncFuncResult_Failure()
    {
        var okCalled = false;
        var failCalled = false;
        var ex = new Exception("fail");
        var result = new Result(ex);
        var ret = await result.MatchAsync(
            async () => { okCalled = true; await Task.Yield(); return Result.Success; },
            async e => { failCalled = true; e.Should().Be(ex); await Task.Yield(); return Result.Fail(e); });
        okCalled.Should().BeFalse();
        failCalled.Should().BeTrue();
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public async Task Result_MatchAsAsyncFuncT_Success()
    {
        var okCalled = false;
        var failCalled = false;
        var result = new Result();
        var ret = await result.MatchAsAsync(
            async () => { okCalled = true; await Task.Yield(); return Result<int>.Ok(123); },
            async e => { failCalled = true; await Task.Yield(); return Result<int>.Fail(e); });
        okCalled.Should().BeTrue();
        failCalled.Should().BeFalse();
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(123);
    }

    [Test]
    public async Task Result_MatchAsAsyncFuncT_Failure()
    {
        var okCalled = false;
        var failCalled = false;
        var ex = new Exception("fail");
        var result = new Result(ex);
        var ret = await result.MatchAsAsync(
            async () => { okCalled = true; await Task.Yield(); return Result<int>.Ok(123); },
            async e => { failCalled = true; e.Should().Be(ex); await Task.Yield(); return Result<int>.Fail(e); });
        okCalled.Should().BeFalse();
        failCalled.Should().BeTrue();
        ret.IsFailure.Should().BeTrue();
        ret.Error.Should().Be(ex);
    }

    [Test]
    public async Task ResultT_MatchAsyncFuncT_Success()
    {
        int? okValue = null;
        Exception? failEx = null;
        var result = new Result<int>(42);
        var ret = await result.MatchAsync(
            async i => { okValue = i; await Task.Yield(); },
            async e => { failEx = e; await Task.Yield(); });
        okValue.Should().Be(42);
        failEx.Should().BeNull();
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_MatchAsyncFuncT_Failure()
    {
        int? okValue = null;
        Exception? failEx = null;
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = await result.MatchAsync(
            async i => { okValue = i; await Task.Yield(); },
            async e => { failEx = e; await Task.Yield(); });
        okValue.Should().BeNull();
        failEx.Should().Be(ex);
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_MatchAsyncFuncResultT_Success()
    {
        var result = new Result<int>(10);
        var ret = await result.MatchAsync(
            async i => { await Task.Yield(); return new Result<int>(i * 2); },
            async e => { await Task.Yield(); return new Result<int>(-1); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(20);
    }

    [Test]
    public async Task ResultT_MatchAsyncFuncResultT_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = await result.MatchAsync(
            async i => { await Task.Yield(); return new Result<int>(i * 2); },
            async e => { e.Should().Be(ex); await Task.Yield(); return new Result<int>(-1); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(-1);
    }

    [Test]
    public async Task ResultT_MatchAsAsyncFuncTU_Success()
    {
        var result = new Result<int>(5);
        var ret = await result.MatchAsAsync<int, string>(
            async i => { await Task.Yield(); return new Result<string>((i * 2).ToString()); },
            async e => { await Task.Yield(); return new Result<string>("fail"); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("10");
    }

    [Test]
    public async Task ResultT_MatchAsAsyncFuncTU_Failure()
    {
        var ex = new Exception("fail");
        var result = new Result<int>(ex);
        var ret = await result.MatchAsAsync<int, string>(
            async i => { await Task.Yield(); return new Result<string>((i * 2).ToString()); },
            async e => { e.Should().Be(ex); await Task.Yield(); return new Result<string>("fail"); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("fail");
    }
}