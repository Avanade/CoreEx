using NUnit.Framework;
using CoreEx.Results;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class ResultTest
    {
        [Test]
        public void Successful_Property()
        {
            var r = Result.Success;
            Assert.IsTrue(r.IsSuccess);
            Assert.IsFalse(r.IsFailure);
        }

        [Test]
        public void Result_Success_Ctor()
        {
            var r = new Result();
            Assert.AreEqual(Result.Success, r);
            Assert.IsTrue(r.IsSuccess);
        }

        [Test]
        public void Success_Is_Successful()
        {
            var r = Result.Success;
            Assert.IsTrue(r.IsSuccess);
        }

        [Test]
        public void Success_ThrowOnError()
        {
            Result.Success.ThrowOnError();
        }

        [Test]
        public void Failure_Ctor()
        {
            var r = new Result(new BusinessException());
            Assert.IsFalse(r.IsSuccess);
            Assert.IsTrue(r.IsFailure);
            Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public void Failure_Is_Failure()
        {
            var r = Result.Fail(new BusinessException());
            Assert.IsFalse(r.IsSuccess);
            Assert.IsTrue(r.IsFailure);
            Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public void Failure_Is_Failure_With_Message()
        {
            var r = Result.Fail("Test");
            Assert.IsFalse(r.IsSuccess);
            Assert.IsTrue(r.IsFailure);
            Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>().And.Message.EqualTo("Test"));
        }

        [Test]
        public void Failure_Explicit_Conversion()
        {
            var r = (Result) new BusinessException();
            Assert.IsFalse(r.IsSuccess);
            Assert.IsTrue(r.IsFailure);
            Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public void Failure_ThrowOnError()
        {
            Assert.Throws<BusinessException>(() => Result.Fail(new BusinessException()).ThrowOnError());
        }

        [Test]
        public void Compare_Success_And_Failure()
        {
            Assert.AreNotEqual(Result.Success, Result.Fail(new BusinessException()));
        }

        [Test]
        public void Compare_Two_Same_Failures()
        {
            var r1 = Result.Fail(new BusinessException());
            var r2 = Result.Fail(new BusinessException());
            Assert.AreNotEqual(r1, r2);
        }

        [Test]
        public void Compare_Two_Different_Failures()
        {
            var r1 = Result.Fail(new BusinessException());
            var r2 = Result.Fail(new BusinessException("Test"));
            Assert.AreNotEqual(r1, r2);
        }

        [Test]
        public void Success_ToString()
        {
            Assert.AreEqual("Success.", Result.Success.ToString());
        }

        [Test]
        public void Failure_ToString()
        {
            Assert.AreEqual("Failure: A business error occurred.", Result.Fail(new BusinessException()).ToString());
        }
    }
}