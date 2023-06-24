using CoreEx.Results;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class MatchExtensionsTest
    {
    //    [Test]
    //    public void Sync_Match_Result_Actions_Ok()
    //    {
    //        var r = Result.Success;
    //        var r2 = r.Match(
    //            ok: () => { Assert.Pass(); },
    //            fail: e => { Assert.Fail(); });

    //        Assert.IsTrue(r2.IsSuccess);
    //    }

    //    [Test]
    //    public void Sync_Match_Result_Actions_Fail()
    //    {
    //        var r = Result.Fail("bad");
    //        var r2 = r.Match(
    //            ok: () => { Assert.Fail(); },
    //            fail: e => { Assert.Pass(); });

    //        Assert.IsTrue(r2.IsFailure);
    //    }

    //    [Test]
    //    public void Sync_Match_Result_Func_Return_Result_Ok()
    //    {
    //        var r = Result.Success;
    //        var r2 = r.Match(
    //            ok: () => Result.Fail("ok"),
    //            fail: e => Result.Fail("bad"));

    //        Assert.That(r2.Error, Is.Not.Null.And.Message.EqualTo("ok"));
    //    }

    //    [Test]
    //    public void Sync_Match_Result_Func_Return_Result_Fail()
    //    {
    //        var r = Result.Fail("test");
    //        var r2 = r.Match(
    //            ok: () => Result.Fail("ok"),
    //            fail: e => Result.Fail("bad"));

    //        Assert.That(r2.Error, Is.Not.Null.And.Message.EqualTo("bad"));
    //    }

    //    [Test]
    //    public void Sync_Match_ResultT_Actions_Ok()
    //    {
    //        var r = Result.Ok(1);
    //        var r2 = r.Match(
    //            ok: v => { Assert.AreEqual(1, v); },
    //            fail: e => { Assert.Fail(); });

    //        Assert.IsTrue(r2.IsSuccess);
    //    }

    //    [Test]
    //    public void Sync_Match_ResultT_Actions_Fail()
    //    {
    //        var r = Result<int>.Fail("test");
    //        var r2 = r.Match(
    //            ok: v => { Assert.Fail(); },
    //            fail: e => { Assert.Pass(); });

    //        Assert.IsTrue(r2.IsFailure);
    //    }

    //    [Test]
    //    public void Sync_Match_ResultT_Func_Ok()
    //    {
    //        var r = Result.Ok(1);
    //        var r2 = r.Match(
    //            ok: v => Result.Ok(true),
    //            fail: e => false);

    //        Assert.IsTrue(r2.Value);
    //    }

    //    [Test]
    //    public void Sync_Match_ResultT_Func_Fail()
    //    {
    //        var r = Result<int>.Fail("test");
    //        var r2 = r.Match(
    //            ok: v => Result.Ok(true),
    //            fail: e => false);

    //        Assert.IsFalse(r2.Value);
    //    }

    //    /* AsyncResult */

    //    [Test]
    //    public async Task AsyncResult_Match_Result_Actions_Ok()
    //    {
    //        var r = Task.FromResult(Result.Success);
    //        var r2 = await r.Match(
    //            ok: () => { Assert.Pass(); },
    //            fail: e => { Assert.Fail(); });

    //        Assert.IsTrue(r2.IsSuccess);
    //    }

    //    [Test]
    //    public async Task AsyncResult_Match_Result_Actions_Fail()
    //    {
    //        var r = Task.FromResult(Result.Fail("bad"));
    //        var r2 = await r.Match(
    //            ok: () => { Assert.Fail(); },
    //            fail: e => { Assert.Pass(); });

    //        Assert.IsTrue(r2.IsFailure);
    //    }

    //    [Test]
    //    public async Task AsyncResult_Match_Result_Func_Return_Result_Ok()
    //    {
    //        var r = Task.FromResult(Result.Success);
    //        var r2 = await r.Match(
    //            ok: () => Result.Fail("ok"),
    //            fail: e => Result.Fail("bad"));

    //        Assert.That(r2.Error, Is.Not.Null.And.Message.EqualTo("ok"));
    //    }

    //    [Test]
    //    public async Task AsyncResult_Match_Result_Func_Return_Result_Fail()
    //    {
    //        var r = Task.FromResult(Result.Fail("test"));
    //        var r2 = await r.Match(
    //            ok: () => Result.Fail("ok"),
    //            fail: e => Result.Fail("bad"));

    //        Assert.That(r2.Error, Is.Not.Null.And.Message.EqualTo("bad"));
    //    }

    //    [Test]
    //    public async Task AsyncResult_Match_ResultT_Actions_Ok()
    //    {
    //        var r = Task.FromResult(Result.Ok(1));
    //        var r2 = await r.Match(
    //            ok: v => { Assert.AreEqual(1, v); },
    //            fail: e => { Assert.Fail(); });

    //        Assert.IsTrue(r2.IsSuccess);
    //    }

    //    [Test]
    //    public async Task AsyncResult_Match_ResultT_Actions_Fail()
    //    {
    //        var r = Task.FromResult(Result<int>.Fail("test"));
    //        var r2 = await r.Match(
    //            ok: v => { Assert.Fail(); },
    //            fail: e => { Assert.Pass(); });

    //        Assert.IsTrue(r2.IsFailure);
    //    }

    //    [Test]
    //    public async Task AsyncResult_Match_ResultT_Func_Ok()
    //    {
    //        var r = Task.FromResult(Result.Ok(1));
    //        var r2 = await r.Match(
    //            ok: v => Result.Ok(true),
    //            fail: e => false);

    //        Assert.IsTrue(r2.Value);
    //    }

    //    [Test]
    //    public async Task AsyncResult_Match_ResultT_Func_Fail()
    //    {
    //        var r = Task.FromResult(Result<int>.Fail("test"));
    //        var r2 = await r.Match(
    //            ok: v => Result.Ok(true),
    //            fail: e => false);

    //        Assert.IsFalse(r2.Value);
    //    }

    //    /* AsyncFunc */

    //    [Test]
    //    public async Task AsyncFunc_Match_Result_Actions_Ok()
    //    {
    //        var r = Result.Success;
    //        var r2 = await r.MatchAsync(
    //            ok: () => { Assert.Pass(); return Task.CompletedTask; },
    //            fail: e => { Assert.Fail(); return Task.CompletedTask; });

    //        Assert.IsTrue(r2.IsSuccess);
    //    }

    //    [Test]
    //    public async Task AsyncFunc_Match_Result_Actions_Fail()
    //    {
    //        var r = Result.Fail("bad");
    //        var r2 = await r.MatchAsync(
    //            ok: () => { Assert.Fail(); return Task.CompletedTask; },
    //            fail: e => { Assert.Pass(); return Task.CompletedTask; });

    //        Assert.IsTrue(r2.IsFailure);
    //    }

    //    [Test]
    //    public async Task AsyncFunc_Match_Result_Func_Return_Result_Ok()
    //    {
    //        var r = Result.Success;
    //        var r2 = await r.MatchAsync(
    //            ok: () => Task.FromResult(Result.Fail("ok")),
    //            fail: e => Task.FromResult(Result.Fail("bad")));

    //        Assert.That(r2.Error, Is.Not.Null.And.Message.EqualTo("ok"));
    //    }

    //    [Test]
    //    public async Task AsyncFunc_Match_Result_Func_Return_Result_Fail()
    //    {
    //        var r = Result.Fail("test");
    //        var r2 = await r.MatchAsync(
    //            ok: () => Task.FromResult(Result.Fail("ok")),
    //            fail: e => Task.FromResult(Result.Fail("bad")));

    //        Assert.That(r2.Error, Is.Not.Null.And.Message.EqualTo("bad"));
    //    }

    //    [Test]
    //    public async Task AsyncFunc_Match_ResultT_Actions_Ok()
    //    {
    //        var r = Result.Ok(1);
    //        var r2 = await r.MatchAsync(
    //            ok: v => { Assert.AreEqual(1, v); return Task.CompletedTask; },
    //            fail: e => { Assert.Fail(); return Task.CompletedTask; });

    //        Assert.IsTrue(r2.IsSuccess);
    //    }

    //    [Test]
    //    public async Task AsyncFunc_Match_ResultT_Actions_Fail()
    //    {
    //        var r = Result<int>.Fail("test");
    //        var r2 = await r.MatchAsync(
    //            ok: v => { Assert.Fail(); return Task.CompletedTask; },
    //            fail: e => { Assert.Pass(); return Task.CompletedTask; });

    //        Assert.IsTrue(r2.IsFailure);
    //    }

    //    [Test]
    //    public async Task AsyncFunc_Match_ResultT_Func_Ok()
    //    {
    //        var r = Result.Ok(1);
    //        var r2 = await r.MatchAsync(
    //            ok: v => Task.FromResult(Result.Ok(true)),
    //            fail: e => Task.FromResult(Result.Ok(false)));

    //        Assert.IsTrue(r2.Value);
    //    }

    //    [Test]
    //    public async Task AsyncFunc_Match_ResultT_Func_Fail()
    //    {
    //        var r = Result<int>.Fail("test");
    //        var r2 = await r.MatchAsync(
    //            ok: v => Task.FromResult(Result.Ok(true)),
    //            fail: e => Task.FromResult(Result.Ok(false)));

    //        Assert.IsFalse(r2.Value);
    //    }

    //    /* AsyncBoth */
    //    [Test]
    //    public async Task AsyncBoth_Match_Result_Actions_Ok()
    //    {
    //        var r = Task.FromResult(Result.Success);
    //        var r2 = await r.MatchAsync(
    //            ok: () => { Assert.Pass(); return Task.CompletedTask; },
    //            fail: e => { Assert.Fail(); return Task.CompletedTask; });

    //        Assert.IsTrue(r2.IsSuccess);
    //    }

    //    [Test]
    //    public async Task AsyncBoth_Match_Result_Actions_Fail()
    //    {
    //        var r = Task.FromResult(Result.Fail("bad"));
    //        var r2 = await r.MatchAsync(
    //            ok: () => { Assert.Fail(); return Task.CompletedTask; },
    //            fail: e => { Assert.Pass(); return Task.CompletedTask; });

    //        Assert.IsTrue(r2.IsFailure);
    //    }

    //    [Test]
    //    public async Task AsyncBoth_Match_Result_Func_Return_Result_Ok()
    //    {
    //        var r = Task.FromResult(Result.Success);
    //        var r2 = await r.MatchAsync(
    //            ok: () => Task.FromResult(Result.Fail("ok")),
    //            fail: e => Task.FromResult(Result.Fail("bad")));

    //        Assert.That(r2.Error, Is.Not.Null.And.Message.EqualTo("ok"));
    //    }

    //    [Test]
    //    public async Task AsyncBoth_Match_Result_Func_Return_Result_Fail()
    //    {
    //        var r = Task.FromResult(Result.Fail("test"));
    //        var r2 = await r.MatchAsync(
    //            ok: () => Task.FromResult(Result.Fail("ok")),
    //            fail: e => Task.FromResult(Result.Fail("bad")));

    //        Assert.That(r2.Error, Is.Not.Null.And.Message.EqualTo("bad"));
    //    }

    //    [Test]
    //    public async Task AsyncBoth_Match_ResultT_Actions_Ok()
    //    {
    //        var r = Task.FromResult(Result.Ok(1));
    //        var r2 = await r.MatchAsync(
    //            ok: v => { Assert.AreEqual(1, v); return Task.CompletedTask; },
    //            fail: e => { Assert.Fail(); return Task.CompletedTask; });

    //        Assert.IsTrue(r2.IsSuccess);
    //    }

    //    [Test]
    //    public async Task AsyncBoth_Match_ResultT_Actions_Fail()
    //    {
    //        var r = Task.FromResult(Result<int>.Fail("test"));
    //        var r2 = await r.MatchAsync(
    //            ok: v => { Assert.Fail(); return Task.CompletedTask; },
    //            fail: e => { Assert.Pass(); return Task.CompletedTask; });

    //        Assert.IsTrue(r2.IsFailure);
    //    }

    //    [Test]
    //    public async Task AsyncBoth_Match_ResultT_Func_Ok()
    //    {
    //        var r = Task.FromResult(Result.Ok(1));
    //        var r2 = await r.MatchAsync(
    //            ok: v => Task.FromResult(Result.Ok(true)),
    //            fail: e => Task.FromResult(Result.Ok(false)));

    //        Assert.IsTrue(r2.Value);
    //    }

    //    [Test]
    //    public async Task AsyncBoth_Match_ResultT_Func_Fail()
    //    {
    //        var r = Task.FromResult(Result<int>.Fail("test"));
    //        var r2 = await r.MatchAsync(
    //            ok: v => Task.FromResult(Result.Ok(true)),
    //            fail: e => Task.FromResult(Result.Ok(false)));

    //        Assert.IsFalse(r2.Value);
    //    }
    }
}