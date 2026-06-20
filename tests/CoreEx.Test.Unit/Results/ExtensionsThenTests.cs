using CoreEx.Results;

namespace CoreEx.Test.Unit.Results;

public class ExtensionsThenTests
{
    [Test]
    public void Result_Then_Action_Success()
    {
        var called = false;
        var result = new Result();
        var ret = result.Then(() => called = true);
        called.Should().BeTrue();
        ret.Should().Be(result);
    }

    [Test]
    public void Result_Then_Action_Failure()
    {
        var called = false;
        var result = new Result(new Exception("fail"));
        var ret = result.Then(() => called = true);
        called.Should().BeFalse();
        ret.Should().Be(result);
    }

    [Test]
    public void Result_Then_FuncResult_Success()
    {
        var called = false;
        var result = new Result();
        var ret = result.Then(() => { called = true; return Result.Success; });
        called.Should().BeTrue();
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Result_Then_FuncResult_Failure()
    {
        var called = false;
        var result = new Result(new Exception("fail"));
        var ret = result.Then(() => { called = true; return Result.Success; });
        called.Should().BeFalse();
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ResultT_Then_ActionT_Success()
    {
        int? value = null;
        var result = new Result<int>(42);
        var ret = result.Then(i => value = i);
        value.Should().Be(42);
        ret.Should().Be(result);
    }

    [Test]
    public void ResultT_Then_ActionT_Failure()
    {
        int? value = null;
        var result = new Result<int>(new Exception("fail"));
        var ret = result.Then(i => value = i);
        value.Should().BeNull();
        ret.Should().Be(result);
    }

    [Test]
    public void ResultT_Then_FuncT_Success()
    {
        var result = new Result<int>(10);
        var ret = result.Then(i => i * 2);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(20);
    }

    [Test]
    public void ResultT_Then_FuncT_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = result.Then(i => i * 2);
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ResultT_Then_FuncResultT_Success()
    {
        var result = new Result<int>(5);
        var ret = result.Then(i => new Result<int>(i + 1));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(6);
    }

    [Test]
    public void ResultT_Then_FuncResultT_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = result.Then(i => new Result<int>(99));
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ResultT_Then_FuncTResult_Success()
    {
        var result = new Result<int>(5);
        var ret = result.Then(i => Result.Success);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(5);
    }

    [Test]
    public void ResultT_Then_FuncTResult_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = result.Then(i => Result.Success);
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Result_ThenAsT_FuncT_Success()
    {
        var result = new Result();
        var ret = result.ThenAs(() => 123);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(123);
    }

    [Test]
    public void Result_ThenAsT_FuncT_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = result.ThenAs(() => 123);
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Result_ThenAsT_FuncResultT_Success()
    {
        var result = new Result();
        var ret = result.ThenAs(() => new Result<string>("abc"));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }

    [Test]
    public void Result_ThenAsT_FuncResultT_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = result.ThenAs(() => new Result<string>("abc"));
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ResultT_ThenAs_ActionT_Success()
    {
        int? captured = null;
        var result = new Result<int>(42);
        var ret = result.ThenAs(i => captured = i);
        captured.Should().Be(42);
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ResultT_ThenAs_ActionT_Failure()
    {
        int? captured = null;
        var result = new Result<int>(new Exception("fail"));
        var ret = result.ThenAs(i => captured = i);
        captured.Should().BeNull();
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ResultT_ThenAs_FuncTResult_Success()
    {
        var result = new Result<int>(5);
        var ret = result.ThenAs(i => new Result());
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ResultT_ThenAs_FuncTResult_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = result.ThenAs(i => new Result());
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ResultT_ThenAs_FuncTU_Success()
    {
        var result = new Result<int>(5);
        var ret = result.ThenAs<int, string>(i => (i * 2).ToString());
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("10");
    }

    [Test]
    public void ResultT_ThenAs_FuncTU_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = result.ThenAs<int, string>(i => (i * 2).ToString());
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ResultT_ThenAs_FuncTResultU_Success()
    {
        var result = new Result<int>(5);
        var ret = result.ThenAs<int, string>(i => new Result<string>((i * 2).ToString()));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("10");
    }

    [Test]
    public void ResultT_ThenAs_FuncTResultU_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = result.ThenAs<int, string>(i => new Result<string>((i * 2).ToString()));
        ret.IsFailure.Should().BeTrue();
    }

    // --- ASYNC ---

    [Test]
    public async Task Result_ThenAsync_Action_Success()
    {
        var called = false;
        var result = new Result();
        var ret = await result.ThenAsync(async () => { called = true; await Task.Yield(); });
        called.Should().BeTrue();
        ret.Should().Be(result);
    }

    [Test]
    public async Task Result_ThenAsync_Action_Failure()
    {
        var called = false;
        var result = new Result(new Exception("fail"));
        var ret = await result.ThenAsync(async () => { called = true; await Task.Yield(); });
        called.Should().BeFalse();
        ret.Should().Be(result);
    }

    [Test]
    public async Task Result_ThenAsync_FuncResult_Success()
    {
        var called = false;
        var result = new Result();
        var ret = await result.ThenAsync(async () => { called = true; await Task.Yield(); return Result.Success; });
        called.Should().BeTrue();
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Result_ThenAsync_FuncResult_Failure()
    {
        var called = false;
        var result = new Result(new Exception("fail"));
        var ret = await result.ThenAsync(async () => { called = true; await Task.Yield(); return Result.Success; });
        called.Should().BeFalse();
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_ThenAsync_ActionT_Success()
    {
        int? value = null;
        var result = new Result<int>(42);
        var ret = await result.ThenAsync(async i => { value = i; await Task.Yield(); });
        value.Should().Be(42);
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_ThenAsync_ActionT_Failure()
    {
        int? value = null;
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.ThenAsync(async i => { value = i; await Task.Yield(); });
        value.Should().BeNull();
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_ThenAsync_FuncT_Success()
    {
        var result = new Result<int>(10);
        var ret = await result.ThenAsync(async i => { await Task.Yield(); return i * 2; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(20);
    }

    [Test]
    public async Task ResultT_ThenAsync_FuncT_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.ThenAsync(async i => { await Task.Yield(); return i * 2; });
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_ThenAsync_FuncResultT_Success()
    {
        var result = new Result<int>(5);
        var ret = await result.ThenAsync(async i => { await Task.Yield(); return new Result<int>(i + 1); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(6);
    }

    [Test]
    public async Task ResultT_ThenAsync_FuncResultT_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.ThenAsync(async i => { await Task.Yield(); return new Result<int>(99); });
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_ThenAsync_FuncTResult_Success()
    {
        var result = new Result<int>(5);
        var ret = await result.ThenAsync(async i => { await Task.Yield(); return Result.Success; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(5);
    }

    [Test]
    public async Task ResultT_ThenAsync_FuncTResult_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.ThenAsync(async i => { await Task.Yield(); return Result.Success; });
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Result_ThenAsAsyncT_FuncT_Success()
    {
        var result = new Result();
        var ret = await result.ThenAsAsync(async () => { await Task.Yield(); return 123; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(123);
    }

    [Test]
    public async Task Result_ThenAsAsyncT_FuncT_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = await result.ThenAsAsync(async () => { await Task.Yield(); return 123; });
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Result_ThenAsAsyncT_FuncResultT_Success()
    {
        var result = new Result();
        var ret = await result.ThenAsAsync(async () => { await Task.Yield(); return new Result<string>("abc"); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }

    [Test]
    public async Task Result_ThenAsAsyncT_FuncResultT_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = await result.ThenAsAsync(async () => { await Task.Yield(); return new Result<string>("abc"); });
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_ThenAsAsync_ActionT_Success()
    {
        int? captured = null;
        var result = new Result<int>(42);
        var ret = await result.AsTask().ThenAsAsync(async i => { captured = i; await Task.Yield(); });
        captured.Should().Be(42);
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_ThenAsAsync_ActionT_Failure()
    {
        int? captured = null;
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.AsTask().ThenAsAsync(async i => { captured = i; await Task.Yield(); });
        captured.Should().BeNull();
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_ThenAsAsync_FuncTResult_Success()
    {
        var result = new Result<int>(5);
        var ret = await result.AsTask().ThenAsAsync(async i => { await Task.Yield(); return new Result(); });
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_ThenAsAsync_FuncTResult_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.AsTask().ThenAsAsync(async i => { await Task.Yield(); return new Result(); });
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_ThenAsAsync_FuncTU_Success()
    {
        var result = new Result<int>(5);
        var ret = await result.AsTask().ThenAsAsync<int, string>(async i => { await Task.Yield(); return (i * 2).ToString(); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("10");
    }

    [Test]
    public async Task ResultT_ThenAsAsync_FuncTU_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.AsTask().ThenAsAsync<int, string>(async i => { await Task.Yield(); return (i * 2).ToString(); });
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_ThenAsAsync_FuncTResultU_Success()
    {
        var result = new Result<int>(5);
        var ret = await result.AsTask().ThenAsAsync<int, string>(async i => { await Task.Yield(); return new Result<string>((i * 2).ToString()); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("10");
    }

    [Test]
    public async Task ResultT_ThenAsAsync_FuncTResultU_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.AsTask().ThenAsAsync<int, string>(async i => { await Task.Yield(); return new Result<string>((i * 2).ToString()); });
        ret.IsFailure.Should().BeTrue();
    }
}
