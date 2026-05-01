using CoreEx.Results;

namespace CoreEx.Test.Unit.Results;

public class ExtensionsAnyTests
{
    [Test]
    public void Result_Any_Action_Success()
    {
        var called = false;
        var result = new Result();
        var ret = result.Any(() => called = true);
        called.Should().BeTrue();
        ret.Should().Be(result);
    }

    [Test]
    public void Result_Any_Action_Failure()
    {
        var called = false;
        var result = new Result(new Exception("fail"));
        var ret = result.Any(() => called = true);
        called.Should().BeTrue();
        ret.Should().Be(result);
    }

    [Test]
    public void Result_Any_FuncResult_Success()
    {
        var called = false;
        var result = new Result();
        var ret = result.Any(() => { called = true; return Result.Success; });
        called.Should().BeTrue();
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Result_Any_FuncResult_Failure()
    {
        var called = false;
        var result = new Result(new Exception("fail"));
        var ret = result.Any(() => { called = true; return Result.Success; });
        called.Should().BeTrue();
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ResultT_Any_ActionT_Success()
    {
        int? value = null;
        var result = new Result<int>(42);
        var ret = result.Any(i => value = i);
        value.Should().Be(42);
        ret.Should().Be(result);
    }

    [Test]
    public void ResultT_Any_ActionT_Failure()
    {
        int? value = null;
        var result = new Result<int>(new Exception("fail"));
        var ret = result.Any(i => value = i);
        value.Should().Be(0); // default(int)
        ret.Should().Be(result);
    }

    [Test]
    public void ResultT_Any_FuncT_Success()
    {
        var result = new Result<int>(10);
        var ret = result.Any(i => i * 2);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(20);
    }

    [Test]
    public void ResultT_Any_FuncT_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = result.Any(i => i * 2);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(0); // default(int)
    }

    [Test]
    public void ResultT_Any_FuncResultT_Success()
    {
        var result = new Result<int>(5);
        var ret = result.Any(i => new Result<int>(i + 1));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(6);
    }

    [Test]
    public void ResultT_Any_FuncResultT_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = result.Any(i => new Result<int>(i + 1));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(1); // default(int) + 1
    }

    [Test]
    public void Result_AnyAsT_FuncT_Success()
    {
        var result = new Result();
        var ret = result.AnyAs(() => 123);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(123);
    }

    [Test]
    public void Result_AnyAsT_FuncT_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = result.AnyAs(() => 123);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(123);
    }

    [Test]
    public void Result_AnyAsT_FuncResultT_Success()
    {
        var result = new Result();
        var ret = result.AnyAs(() => new Result<string>("abc"));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }

    [Test]
    public void Result_AnyAsT_FuncResultT_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = result.AnyAs(() => new Result<string>("abc"));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }

    [Test]
    public void ResultT_AnyAs_ActionT_Success()
    {
        int? captured = null;
        var result = new Result<int>(42);
        var ret = result.AnyAs(i => captured = i);
        captured.Should().Be(42);
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ResultT_AnyAs_ActionT_Failure()
    {
        int? captured = null;
        var result = new Result<int>(new Exception("fail"));
        var ret = result.AnyAs(i => captured = i);
        captured.Should().Be(0); // default(int)
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ResultT_AnyAs_FuncTResult_Success()
    {
        var result = new Result<int>(5);
        var ret = result.AnyAs(i => new Result());
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ResultT_AnyAs_FuncTResult_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = result.AnyAs(i => new Result());
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ResultT_AnyAs_FuncTU_Success()
    {
        var result = new Result<int>(5);
        var ret = result.AnyAs<int, string>(i => (i * 2).ToString());
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("10");
    }

    [Test]
    public void ResultT_AnyAs_FuncTU_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = result.AnyAs<int, string>(i => (i * 2).ToString());
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("0");
    }

    [Test]
    public void ResultT_AnyAs_FuncTResultU_Success()
    {
        var result = new Result<int>(5);
        var ret = result.AnyAs<int, string>(i => new Result<string>((i * 2).ToString()));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("10");
    }

    [Test]
    public void ResultT_AnyAs_FuncTResultU_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = result.AnyAs<int, string>(i => new Result<string>((i * 2).ToString()));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("0");
    }

    // --- ASYNC ---

    [Test]
    public async Task Result_AnyAsync_Action_Success()
    {
        var called = false;
        var result = new Result();
        var ret = await result.AsTask().Any(async () => { called = true; await Task.Yield(); });
        called.Should().BeTrue();
        ret.Should().Be(result);
    }

    [Test]
    public async Task Result_AnyAsync_Action_Failure()
    {
        var called = false;
        var result = new Result(new Exception("fail"));
        var ret = await result.AsTask().Any(async () => { called = true; await Task.Yield(); });
        called.Should().BeTrue();
        ret.Should().Be(result);
    }

    [Test]
    public async Task Result_AnyAsync_FuncResult_Success()
    {
        var called = false;
        var result = new Result();
        var ret = await result.AsTask().AnyAsync(async () => { called = true; await Task.Yield(); return Result.Success; });
        called.Should().BeTrue();
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Result_AnyAsync_FuncResult_Failure()
    {
        var called = false;
        var result = new Result(new Exception("fail"));
        var ret = await result.AsTask().AnyAsync(async () => { called = true; await Task.Yield(); return Result.Success; });
        called.Should().BeTrue();
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_AnyAsync_ActionT_Success()
    {
        int? value = null;
        var result = new Result<int>(42);
        var ret = await result.AsTask().Any(async i => { value = i; await Task.Yield(); });
        value.Should().Be(42);
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_AnyAsync_ActionT_Failure()
    {
        int? value = null;
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.AsTask().Any(async i => { value = i; await Task.Yield(); });
        value.Should().Be(0);
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_AnyAsync_FuncT_Success()
    {
        var result = new Result<int>(10);
        var ret = await result.AsTask().Any(i => { return i * 2; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(20);
    }

    [Test]
    public async Task ResultT_AnyAsync_FuncT_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.AsTask().Any(i => { return i * 2; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(0);
    }

    [Test]
    public async Task ResultT_AnyAsync_FuncResultT_Success()
    {
        var result = new Result<int>(5);
        var ret = await result.AsTask().Any(i => { return new Result<int>(i + 1); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(6);
    }

    [Test]
    public async Task ResultT_AnyAsync_FuncResultT_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.AsTask().Any(i => { return new Result<int>(i + 1); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(1);
    }

    [Test]
    public async Task Result_AnyAsAsyncT_FuncT_Success()
    {
        var result = new Result();
        var ret = await result.AsTask().AnyAsAsync(async () => { await Task.Yield(); return 123; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(123);
    }

    [Test]
    public async Task Result_AnyAsAsyncT_FuncT_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = await result.AsTask().AnyAsAsync(async () => { await Task.Yield(); return 123; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(123);
    }

    [Test]
    public async Task Result_AnyAsAsyncT_FuncResultT_Success()
    {
        var result = new Result();
        var ret = await result.AsTask().AnyAsAsync(async () => { await Task.Yield(); return new Result<string>("abc"); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }

    [Test]
    public async Task Result_AnyAsAsyncT_FuncResultT_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = await result.AsTask().AnyAsAsync(async () => { await Task.Yield(); return new Result<string>("abc"); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("abc");
    }

    [Test]
    public async Task ResultT_AnyAsAsync_ActionT_Success()
    {
        int? captured = null;
        var result = new Result<int>(42);
        var ret = await result.AsTask().AnyAsAsync(async i => { captured = i; await Task.Yield(); });
        captured.Should().Be(42);
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_AnyAsAsync_ActionT_Failure()
    {
        int? captured = null;
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.AsTask().AnyAsAsync(async i => { captured = i; await Task.Yield(); });
        captured.Should().Be(0);
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_AnyAsAsync_FuncTResult_Success()
    {
        var result = new Result<int>(5);
        var ret = await result.AsTask().AnyAsAsync(async i => { await Task.Yield(); return new Result(); });
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_AnyAsAsync_FuncTResult_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.AsTask().AnyAsAsync(async i => { await Task.Yield(); return new Result(); });
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task ResultT_AnyAsAsync_FuncTU_Success()
    {
        var result = new Result<int>(5);
        var ret = await result.AsTask().AnyAsAsync<int, string>(async i => { await Task.Yield(); return (i * 2).ToString(); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("10");
    }

    [Test]
    public async Task ResultT_AnyAsAsync_FuncTU_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.AsTask().AnyAsAsync<int, string>(async i => { await Task.Yield(); return (i * 2).ToString(); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("0");
    }

    [Test]
    public async Task ResultT_AnyAsAsync_FuncTResultU_Success()
    {
        var result = new Result<int>(5);
        var ret = await result.AsTask().AnyAsAsync<int, string>(async i => { await Task.Yield(); return new Result<string>((i * 2).ToString()); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("10");
    }

    [Test]
    public async Task ResultT_AnyAsAsync_FuncTResultU_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.AsTask().AnyAsAsync<int, string>(async i => { await Task.Yield(); return new Result<string>((i * 2).ToString()); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be("0");
    }
}
