using NUnit.Framework;
using CoreEx.Results;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class ResultTTest
    {
        [Test]
        public void Successful_Property()
        {
            var r = Result<int>.Successful;
            Assert.IsTrue(r.IsSuccessful);
            Assert.IsFalse(r.IsFailure);
        }

        [Test]
        public void Result_Success_Ctor()
        {
            var r = new Result<int>();
            Assert.AreEqual(Result<int>.Successful, r);
        }

        [Test]
        public void Success_Is_Successful()
        {
            var r = Result.Success<int>();
            Assert.AreEqual(Result<int>.Successful, r);
        }

        [Test]
        public void Success_Implicit_Conversion_From_Value()
        {
            var r = (Result<int>) 1;
            Assert.AreEqual(1, r.Value);
        }

        [Test]
        public void Success_Implicit_Conversion_From_Result()
        {
            int i = Result.Success<int>(1);
            Assert.AreEqual(1, i);
        }

        [Test]
        public void Success_ThrowOnError()
        {
            Result.Success<int>().ThrowOnError();
        }

        [Test]
        public void Failure_Ctor()
        {
            var r = new Result<int>(new BusinessException());
            Assert.IsFalse(r.IsSuccessful);
            Assert.IsTrue(r.IsFailure);
            Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public void Failure_Is_Failure()
        {
            var r = Result.Failure<int>(new BusinessException());
            Assert.IsFalse(r.IsSuccessful);
            Assert.IsTrue(r.IsFailure);
            Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public void Failure_Is_Failure_With_Message()
        {
            var r = Result.Failure<int>("Test");
            Assert.IsFalse(r.IsSuccessful);
            Assert.IsTrue(r.IsFailure);
            Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>().And.Message.EqualTo("Test"));
        }

        [Test]
        public void Failure_Explicit_Conversion()
        {
            var r = (Result<int>) new BusinessException();
            Assert.IsFalse(r.IsSuccessful);
            Assert.IsTrue(r.IsFailure);
            Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public void Failure_ThrowOnError()
        {
            Assert.Throws<BusinessException>(() => Result.Failure<int>(new BusinessException()).ThrowOnError());
        }

        [Test]
        public void Compare_Success_And_Failure()
        {
            Assert.AreNotEqual(Result.Success<int>(), Result.Failure<int>(new BusinessException()));
        }

        [Test]
        public void Compare_Two_Same_Successes()
        {
            var r1 = Result.Success<int>();
            var r2 = Result.Success<int>();
            Assert.AreEqual(r1, r2);
        }

        [Test]
        public void Compare_Two_Same_Successes_With_Value()
        {
            var r1 = Result.Success(1);
            var r2 = Result.Success(1);
            Assert.AreEqual(r1, r2);
        }

        [Test]
        public void Compare_Two_Different_Successes()
        {
            var r1 = Result.Success<int>();
            var r2 = Result.Success(1);
            Assert.AreNotEqual(r1, r2);
        }

        [Test]
        public void Compare_Two_Different_Successes_With_Value()
        {
            var r1 = Result.Success(1);
            var r2 = Result.Success(2);
            Assert.AreNotEqual(r1, r2);
        }

        [Test]
        public void Compare_Two_Same_Failures()
        {
            var r1 = Result.Failure<int>(new BusinessException());
            var r2 = Result.Failure<int>(new BusinessException());
            Assert.AreNotEqual(r1, r2);
        }

        [Test]
        public void Compare_Two_Different_Failures()
        {
            var r1 = Result.Failure<int>(new BusinessException());
            var r2 = Result.Failure<int>(new BusinessException("Test"));
            Assert.AreNotEqual(r1, r2);
        }

        [Test]
        public void Result_Success_Implicit_Convert_To_Result()
        {
            Result<int> r = Result.Success(1);
            Result r2 = r;
            Assert.AreEqual(Result.Successful, r2);
        }

        [Test]
        public void Result_Failure_Implicit_Convert_To_Result()
        {
            Result<int> r = Result.Failure<int>(new BusinessException());
            Result r2 = r;
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public void Success_Value_ToString()
        {
            var r = Result.Success(1);
            Assert.AreEqual("Successful: 1", r.ToString());
        }

        [Test]
        public void Success_Null_Value_ToString()
        {
            var r = Result.Success<string?>(null);
            Assert.AreEqual("Successful: null", r.ToString());
        }

        [Test]
        public void Failure_ToString()
        {
            var r = Result.Failure<int>(new BusinessException("Test"));
            Assert.AreEqual("Failure: Test", r.ToString());
        }
    }
}