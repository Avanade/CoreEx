using NUnit.Framework;
using CoreEx.Results;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class ResultTest
    {
        [Test]
        public void Success_Property()
        {
            var r = Result.Success;
            Assert.Multiple(() =>
            {
                Assert.That(r.IsSuccess, Is.True);
                Assert.That(r.IsFailure, Is.False);
            });
        }

        [Test]
        public void Result_Success_Ctor()
        {
            var r = new Result();
            Assert.That(r, Is.EqualTo(Result.Success));
            Assert.That(r.IsSuccess, Is.True);
        }

        [Test]
        public void Success_Is_Success()
        {
            var r = Result.Success;
            Assert.That(r.IsSuccess, Is.True);
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
            Assert.Multiple(() =>
            {
                Assert.That(r.IsSuccess, Is.False);
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
            });
        }

        [Test]
        public void Failure_Is_Failure()
        {
            var r = Result.Fail(new BusinessException());
            Assert.Multiple(() =>
            {
                Assert.That(r.IsSuccess, Is.False);
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
            });
        }

        [Test]
        public void Failure_Is_Failure_With_Message()
        {
            var r = Result.Fail("Test");
            Assert.Multiple(() =>
            {
                Assert.That(r.IsSuccess, Is.False);
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>().And.Message.EqualTo("Test"));
            });
        }

        [Test]
        public void Failure_Explicit_Conversion()
        {
            var r = (Result) new BusinessException();
            Assert.Multiple(() =>
            {
                Assert.That(r.IsSuccess, Is.False);
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
            });
        }

        [Test]
        public void Failure_ThrowOnError()
        {
            Assert.Throws<BusinessException>(() => Result.Fail(new BusinessException()).ThrowOnError());
        }

        [Test]
        public void Compare_Success_And_Failure()
        {
            Assert.That(Result.Fail(new BusinessException()), Is.Not.EqualTo(Result.Success));
        }

        [Test]
        public void Compare_Two_Same_Failures()
        {
            var r1 = Result.Fail(new BusinessException());
            var r2 = Result.Fail(new BusinessException());
            Assert.That(r2, Is.EqualTo(r1));
        }

        [Test]
        public void Compare_Two_Different_Failures()
        {
            var r1 = Result.Fail(new BusinessException());
            var r2 = Result.Fail(new BusinessException("Test"));
            Assert.That(r2, Is.Not.EqualTo(r1));
        }

        [Test]
        public void Compare_Two_Different_Types_Failures()
        {
            var r1 = Result.Fail(new ValidationException("Test"));
            var r2 = Result.Fail(new BusinessException("Test"));
            Assert.That(r2, Is.Not.EqualTo(r1));
        }

        [Test]
        public void Success_ToString()
        {
            Assert.That(Result.Success.ToString(), Is.EqualTo("Success."));
        }

        [Test]
        public void Failure_ToString()
        {
            Assert.That(Result.Fail(new BusinessException()).ToString(), Is.EqualTo("Failure: A business error occurred."));
        }

        [Test]
        public async Task AsTask()
        {
            var r = await Result.Go().AsTask();
            Assert.That(r, Is.EqualTo(Result.Success));
        }

        [Test]
        public void Success_Value()
        {
            var ir = (IResult)Result.Success;
            Assert.That(ir.Value, Is.Null);
        }

        [Test]
        public void Failure_Value()
        {
            var ir = (IResult)Result.Fail("On no!");
            Assert.Throws<BusinessException>(() => _ = ir.Value);
        }
    }
}