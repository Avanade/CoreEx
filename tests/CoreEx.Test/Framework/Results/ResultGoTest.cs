using CoreEx.Results;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class ResultGoTest
    {
        [Test]
        public void Go_No_Args()
        {
            var r = Result.Go();
            Assert.AreEqual(Result.Success, r);
        }

        [Test]
        public void Go_With_Action()
        {
            var r = Result.Go(() => { });
            Assert.AreEqual(Result.Success, r);
        }

        [Test]
        public void Go_With_Result_Func()
        {
            var r = Result.Go(() => Result.Fail("Test"));
            Assert.IsTrue(r.IsFailure);
        }

        [Test]
        public void Go_Value_With_Action()
        {
            var r = Result.Go<int>(() => { });
            Assert.AreEqual(Result<int>.None, r);
        }

        [Test]
        public void Go_Value_Func()
        {
            var r = Result.Go(() => 1);
            Assert.AreEqual(1, r.Value);
        }

        [Test]
        public void Go_Value_Result_Func()
        {
            var r = Result.Go(() => Result.Ok(1));
            Assert.AreEqual(1, r.Value);
        }

        [Test]
        public void Go_Value_No_Args()
        {
            var r = Result.Go<int>();
            Assert.AreEqual(Result<int>.None, r);
        }

        /* Go_Async */

        [Test]
        public async Task GoAsync_With_Action()
        {
            var r = await Result.GoAsync(() => Task.CompletedTask);
            Assert.AreEqual(Result.Success, r);
        }

        [Test]
        public async Task GoAsync_With_Result_Func()
        {
            var r = await Result.GoAsync(() => Task.FromResult(Result.Fail("Test")));
            Assert.IsTrue(r.IsFailure);
        }

        [Test]
        public async Task GoAsync_Value_With_Action()
        {
            var r = await Result.GoAsync<int>(() => Task.CompletedTask);
            Assert.AreEqual(Result<int>.None, r);
        }

        [Test]
        public async Task GoAsync_Value_Func()
        {
            var r = await Result.GoAsync<int>(() => Task.FromResult(1));
            Assert.AreEqual(1, r.Value);
        }

        [Test]
        public async Task GoAsync_Value_Result_Func()
        {
            var r = await Result.GoAsync(() => Task.FromResult(Result.Ok(1)));
            Assert.AreEqual(1, r.Value);
        }
    }
}