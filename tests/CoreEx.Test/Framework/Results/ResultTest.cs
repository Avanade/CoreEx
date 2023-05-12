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
            var r = Result.Successful;
            Assert.IsTrue(r.IsSuccessful);
            Assert.IsFalse(r.IsFailure);
        }

        [Test]
        public void Result_Success_Ctor()
        {
            var r = new Result();
            Assert.AreEqual(Result.Successful, r);
        }

        [Test]
        public void Success_Is_Successful()
        {
            var r = Result.Success();
            Assert.AreEqual(Result.Successful, r);
        }

        [Test]
        public void Success_ThrowOnError()
        {
            Result.Success().ThrowOnError();
        }

        [Test]
        public void Failure_Ctor()
        {
            var r = new Result(new BusinessException());
            Assert.IsFalse(r.IsSuccessful);
            Assert.IsTrue(r.IsFailure);
            Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public void Failure_Is_Failure()
        {
            var r = Result.Failure(new BusinessException());
            Assert.IsFalse(r.IsSuccessful);
            Assert.IsTrue(r.IsFailure);
            Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public void Failure_Is_Failure_With_Message()
        {
            var r = Result.Failure("Test");
            Assert.IsFalse(r.IsSuccessful);
            Assert.IsTrue(r.IsFailure);
            Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>().And.Message.EqualTo("Test"));
        }

        [Test]
        public void Failure_Explicit_Conversion()
        {
            var r = (Result) new BusinessException();
            Assert.IsFalse(r.IsSuccessful);
            Assert.IsTrue(r.IsFailure);
            Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public void Failure_ThrowOnError()
        {
            Assert.Throws<BusinessException>(() => Result.Failure(new BusinessException()).ThrowOnError());
        }

        [Test]
        public void Compare_Success_And_Failure()
        {
            Assert.AreNotEqual(Result.Success(), Result.Failure(new BusinessException()));
        }

        [Test]
        public void Compare_Two_Same_Failures()
        {
            var r1 = Result.Failure(new BusinessException());
            var r2 = Result.Failure(new BusinessException());
            Assert.AreNotEqual(r1, r2);
        }

        [Test]
        public void Compare_Two_Different_Failures()
        {
            var r1 = Result.Failure(new BusinessException());
            var r2 = Result.Failure(new BusinessException("Test"));
            Assert.AreNotEqual(r1, r2);
        }

        [Test]
        public void Success_ToString()
        {
            Assert.AreEqual("Successful", Result.Success().ToString());
        }

        [Test]
        public void Failure_ToString()
        {
            Assert.AreEqual("Failure: A business error occurred.", Result.Failure(new BusinessException()).ToString());
        }
    }
}