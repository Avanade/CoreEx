using CoreEx.Results;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class TryCatchWithTest
    {
        [Test]
        public void Sync()
        {
            var r = Result.Success.Try().Catch<DivideByZeroException>().With(r => r);
            Assert.That(r.IsSuccess, Is.True);

            r = Result.Success.Try().With(r => throw new DivideByZeroException());
            Assert.That(r.Error, Is.TypeOf<BusinessException>().And.Message.EqualTo("Attempted to divide by zero."));

            r = Result.Success.Try(_ => "boo hoo").With(r => throw new DivideByZeroException());
            Assert.That(r.Error, Is.TypeOf<BusinessException>().And.Message.EqualTo("boo hoo"));

            r = Result.Success.Try().Catch<DivideByZeroException>().With(r => throw new DivideByZeroException());
            Assert.That(r.Error, Is.TypeOf<BusinessException>().And.Message.EqualTo("Attempted to divide by zero."));

            r = Result.Success.Try().Catch<DivideByZeroException>(_ => "dang it").With(r => throw new DivideByZeroException());
            Assert.That(r.Error, Is.TypeOf<BusinessException>().And.Message.EqualTo("dang it"));

            r = Result.Success.Try(_ => "boo hoo").Catch<DivideByZeroException>().With(r => throw new DivideByZeroException());
            Assert.That(r.Error, Is.TypeOf<BusinessException>().And.Message.EqualTo("boo hoo"));

            Assert.Throws<InvalidCastException>(() => Result.Success.Try(_ => "boo hoo").Catch<DivideByZeroException>().With(r => throw new InvalidCastException()));

            r = Result.Fail("bad").Try().With(r => throw new DivideByZeroException());
            Assert.That(r.Error, Is.TypeOf<BusinessException>().And.Message.EqualTo("bad"));

            r = Result.Fail("bad").TryAny().With(r => throw new DivideByZeroException());
            Assert.That(r.Error, Is.TypeOf<BusinessException>().And.Message.EqualTo("Attempted to divide by zero."));

            var r2 = Result.Success.Try().WithAs(r => r.ThenAs(() => 1));
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public async Task Async()
        {
            var r = await Result.Success.Try().Catch<DivideByZeroException>().WithAsync(r => Task.FromResult(r));
            Assert.That(r.IsSuccess, Is.True);

            r = await Result.Success.Try().WithAsync(r => throw new DivideByZeroException());
            Assert.That(r.Error, Is.TypeOf<BusinessException>().And.Message.EqualTo("Attempted to divide by zero."));

            var r2 = await Result.Success.Try().Catch<DivideByZeroException>().WithAsAsync(r => Task.FromResult(r.ThenAs(() => 1)));
            Assert.That(r2.IsSuccess, Is.True);

            r2 = await Result.Success.Try().WithAsAsync<Result<int>>(r => throw new DivideByZeroException());
            Assert.That(r.Error, Is.TypeOf<BusinessException>().And.Message.EqualTo("Attempted to divide by zero."));
        }
    }
}