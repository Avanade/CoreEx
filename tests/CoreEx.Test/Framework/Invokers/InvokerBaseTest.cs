using CoreEx.Invokers;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Invokers
{
    [TestFixture]
    public class InvokerBaseTest
    {
        [Test]
        public async Task Invoke_AsyncNoResult()
        {
            var i = new TestInvoker();
            await i.InvokeAsync(this, async (_, ct) => { await Task.Delay(100, ct); });
            Assert.Multiple(() =>
            {
                Assert.That(i.Before, Is.True);
                Assert.That(i.After, Is.True);
            });
        }

        [Test]
        public void Invoke_AsyncWithResult()
        {
            var i = new TestInvoker();
            Assert.Multiple(async () =>
            {
                Assert.That(await i.InvokeAsync(this, async (_, ct) => { await Task.Delay(100, ct); return 88; }), Is.EqualTo(88));
                Assert.That(i.Before, Is.True);
                Assert.That(i.After, Is.True);
            });
        }

        [Test]
        public async Task Invoke_AsyncWithResult_Load()
        {
            var i = new TestInvoker();
            for (var j = 0; j < 100000; j++)
            {
                await i.InvokeAsync(this, async (_, ct) => { await Task.Delay(0, ct); return 88; });
            }
        }

        [Test]
        public void Invoke_WithException()
        {
            var i = new TestInvoker();
            Assert.ThrowsAsync<DivideByZeroException>(async () => await i.InvokeAsync(this, async (_, ct) => { await Task.Delay(0, ct); throw new DivideByZeroException(); }));
        }

        public class TestInvoker : InvokerBase<InvokerBaseTest, object?>
        {
            public bool Before { get; set; }

            public bool After { get; set; }

            protected async override Task<TResult> OnInvokeAsync<TResult>(InvokeArgs invokeArgs, InvokerBaseTest invoker, Func<InvokeArgs, System.Threading.CancellationToken, Task<TResult>> func, object? param, System.Threading.CancellationToken ct)
            {
                Before = true;
                var r = await base.OnInvokeAsync(invokeArgs, invoker, func, param, ct).ConfigureAwait(false);
                After = true;
                return r;
            }
        }
    }
}