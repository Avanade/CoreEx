using CoreEx.Results;

namespace CoreEx.Test.Unit.Results;

public class ExtensionsWhenTests
{
    [Test]
    public void Result_When_Action_ConditionTrue_Success()
    {
        var called = false;
        var result = new Result();
        var ret = result.When(() => true, () => called = true);
        called.Should().BeTrue();
        ret.Should().Be(result);
    }

    [Test]
    public void Result_When_Action_ConditionFalse_Success()
    {
        var called = false;
        var otherwiseCalled = false;
        var result = new Result();
        var ret = result.When(() => false, () => called = true, () => otherwiseCalled = true);
        called.Should().BeFalse();
        otherwiseCalled.Should().BeTrue();
        ret.Should().Be(result);
    }

    [Test]
    public void Result_When_Action_Failure()
    {
        var called = false;
        var result = new Result(new Exception("fail"));
        var ret = result.When(() => true, () => called = true);
        called.Should().BeFalse();
        ret.Should().Be(result);
    }

    [Test]
    public void Result_When_Func_ConditionTrue_Success()
    {
        var result = new Result();
        var ret = result.When(() => true, () => Result.Success);
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Result_When_Func_ConditionFalse_Success()
    {
        var result = new Result();
        var ret = result.When(() => false, () => Result.Fail(new Exception("fail")), () => Result.Success);
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Result_When_Func_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = result.When(() => true, () => Result.Success);
        ret.Should().Be(result);
    }

    [Test]
    public void ResultT_When_ActionT_ConditionTrue_Success()
    {
        int? value = null;
        var result = new Result<int>(42);
        var ret = result.When(i => i == 42, i => value = i);
        value.Should().Be(42);
        ret.Should().Be(result);
    }

    [Test]
    public void ResultT_When_ActionT_ConditionFalse_Success()
    {
        int? value = null;
        int? otherwise = null;
        var result = new Result<int>(42);
        var ret = result.When(i => i == 0, i => value = i, i => otherwise = i);
        value.Should().BeNull();
        otherwise.Should().Be(42);
        ret.Should().Be(result);
    }

    [Test]
    public void ResultT_When_ActionT_Failure()
    {
        int? value = null;
        var result = new Result<int>(new Exception("fail"));
        var ret = result.When(i => true, i => value = i);
        value.Should().BeNull();
        ret.Should().Be(result);
    }

    [Test]
    public void ResultT_When_FuncT_ConditionTrue_Success()
    {
        var result = new Result<int>(10);
        var ret = result.When(i => i == 10, i => i * 2);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(20);
    }

    [Test]
    public void ResultT_When_FuncT_ConditionFalse_Success()
    {
        var result = new Result<int>(10);
        var ret = result.When(i => i == 0, i => i * 2, i => 99);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(99);
    }

    [Test]
    public void ResultT_When_FuncT_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = result.When(i => true, i => i * 2);
        ret.Should().Be(result);
    }

    [Test]
    public void ResultT_When_FuncResultT_ConditionTrue_Success()
    {
        var result = new Result<int>(5);
        var ret = result.When(i => i == 5, i => new Result<int>(i + 1));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(6);
    }

    [Test]
    public void ResultT_When_FuncResultT_ConditionFalse_Success()
    {
        var result = new Result<int>(5);
        var ret = result.When(i => i == 0, i => new Result<int>(i + 1), i => new Result<int>(99));
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(99);
    }

    [Test]
    public void ResultT_When_FuncResultT_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = result.When(i => true, i => new Result<int>(i + 1));
        ret.Should().Be(result);
    }

    [Test]
    public void ResultT_WhenAs_FuncT_ConditionTrue_Success()
    {
        var result = new Result<int>(5);
        var ret = result.WhenAs(i => i == 5, i => i * 2);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(10);
    }

    [Test]
    public void ResultT_WhenAs_FuncT_ConditionFalse_Success()
    {
        var result = new Result<int>(5);
        var ret = result.WhenAs(i => i == 0, i => i * 2, i => 99);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(99);
    }

    [Test]
    public void ResultT_WhenAs_FuncT_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = result.WhenAs(i => true, i => i * 2);
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Result_WhenAsT_ConditionTrue_Success()
    {
        var result = new Result();
        var ret = result.WhenAs(() => true, () => 123);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(123);
    }

    [Test]
    public void Result_WhenAsT_ConditionFalse_Success()
    {
        var result = new Result();
        var ret = result.WhenAs(() => false, () => 123, () => 456);
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(456);
    }

    [Test]
    public void Result_WhenAsT_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = result.WhenAs(() => true, () => 123);
        ret.IsFailure.Should().BeTrue();
    }

    // --- ASYNC ---

    [Test]
    public async Task Result_WhenAsync_Action_ConditionTrue_Success()
    {
        var called = false;
        var result = new Result();
        var ret = await result.WhenAsync(() => true, async () => { called = true; await Task.Yield(); });
        called.Should().BeTrue();
        ret.Should().Be(result);
    }

    [Test]
    public async Task Result_WhenAsync_Action_ConditionFalse_Success()
    {
        var called = false;
        var otherwiseCalled = false;
        var result = new Result();
        var ret = await result.WhenAsync(() => false, async () => { called = true; await Task.Yield(); }, async () => { otherwiseCalled = true; await Task.Yield(); });
        called.Should().BeFalse();
        otherwiseCalled.Should().BeTrue();
        ret.Should().Be(result);
    }

    [Test]
    public async Task Result_WhenAsync_Action_Failure()
    {
        var called = false;
        var result = new Result(new Exception("fail"));
        var ret = await result.WhenAsync(() => true, async () => { called = true; await Task.Yield(); });
        called.Should().BeFalse();
        ret.Should().Be(result);
    }

    [Test]
    public async Task Result_WhenAsync_Func_ConditionTrue_Success()
    {
        var result = new Result();
        var ret = await result.WhenAsync(() => true, async () => { await Task.Yield(); return Result.Success; });
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Result_WhenAsync_Func_ConditionFalse_Success()
    {
        var result = new Result();
        var ret = await result.WhenAsync(() => false, async () => { await Task.Yield(); return Result.Fail(new Exception("fail")); }, async () => { await Task.Yield(); return Result.Success; });
        ret.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Result_WhenAsync_Func_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = await result.WhenAsync(() => true, async () => { await Task.Yield(); return Result.Success; });
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_WhenAsync_ActionT_ConditionTrue_Success()
    {
        int? value = null;
        var result = new Result<int>(42);
        var ret = await result.WhenAsync(i => i == 42, async i => { value = i; await Task.Yield(); });
        value.Should().Be(42);
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_WhenAsync_ActionT_ConditionFalse_Success()
    {
        int? value = null;
        int? otherwise = null;
        var result = new Result<int>(42);
        var ret = await result.WhenAsync(i => i == 0, async i => { value = i; await Task.Yield(); }, async i => { otherwise = i; await Task.Yield(); });
        value.Should().BeNull();
        otherwise.Should().Be(42);
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_WhenAsync_ActionT_Failure()
    {
        int? value = null;
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.WhenAsync(i => true, async i => { value = i; await Task.Yield(); });
        value.Should().BeNull();
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_WhenAsync_FuncT_ConditionTrue_Success()
    {
        var result = new Result<int>(10);
        var ret = await result.WhenAsync(i => i == 10, async i => { await Task.Yield(); return i * 2; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(20);
    }

    [Test]
    public async Task ResultT_WhenAsync_FuncT_ConditionFalse_Success()
    {
        var result = new Result<int>(10);
        var ret = await result.WhenAsync(i => i == 0, async i => { await Task.Yield(); return i * 2; }, async i => { await Task.Yield(); return 99; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(99);
    }

    [Test]
    public async Task ResultT_WhenAsync_FuncT_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.WhenAsync(i => true, async i => { await Task.Yield(); return i * 2; });
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_WhenAsync_FuncResultT_ConditionTrue_Success()
    {
        var result = new Result<int>(5);
        var ret = await result.WhenAsync(i => i == 5, async i => { await Task.Yield(); return new Result<int>(i + 1); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(6);
    }

    [Test]
    public async Task ResultT_WhenAsync_FuncResultT_ConditionFalse_Success()
    {
        var result = new Result<int>(5);
        var ret = await result.WhenAsync(i => i == 0, async i => { await Task.Yield(); return new Result<int>(i + 1); }, async i => { await Task.Yield(); return new Result<int>(99); });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(99);
    }

    [Test]
    public async Task ResultT_WhenAsync_FuncResultT_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.WhenAsync(i => true, async i => { await Task.Yield(); return new Result<int>(i + 1); });
        ret.Should().Be(result);
    }

    [Test]
    public async Task ResultT_WhenAsAsync_FuncT_ConditionTrue_Success()
    {
        var result = new Result<int>(5);
        var ret = await result.WhenAsAsync(i => i == 5, async i => { await Task.Yield(); return i * 2; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(10);
    }

    [Test]
    public async Task ResultT_WhenAsAsync_FuncT_ConditionFalse_Success()
    {
        var result = new Result<int>(5);
        var ret = await result.WhenAsAsync(i => i == 0, async i => { await Task.Yield(); return i * 2; }, async i => { await Task.Yield(); return 99; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(99);
    }

    [Test]
    public async Task ResultT_WhenAsAsync_FuncT_Failure()
    {
        var result = new Result<int>(new Exception("fail"));
        var ret = await result.WhenAsAsync(i => true, async i => { await Task.Yield(); return i * 2; });
        ret.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Result_WhenAsAsyncT_ConditionTrue_Success()
    {
        var result = new Result();
        var ret = await result.WhenAsAsync(() => true, async () => { await Task.Yield(); return 123; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(123);
    }

    [Test]
    public async Task Result_WhenAsAsyncT_ConditionFalse_Success()
    {
        var result = new Result();
        var ret = await result.WhenAsAsync(() => false, async () => { await Task.Yield(); return 123; }, async () => { await Task.Yield(); return 456; });
        ret.IsSuccess.Should().BeTrue();
        ret.Value.Should().Be(456);
    }

    [Test]
    public async Task Result_WhenAsAsyncT_Failure()
    {
        var result = new Result(new Exception("fail"));
        var ret = await result.WhenAsAsync(() => true, async () => { await Task.Yield(); return 123; });
        ret.IsFailure.Should().BeTrue();
    }
}