using CoreEx.Entities;
using CoreEx.Results;
using NUnit.Framework;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class ResultStateTest
    {
        [Test]
        public void Result_Success_No_Value()
        {
            var r = Result<int>.Ok();
            Assert.That(r, Is.EqualTo(Result<int>.None));
        }

        [Test]
        public void Result_Success_With_Value()
        {
            var r = Result.Ok(1);
            Assert.That(r, Is.EqualTo(Result<int>.Ok(1)));
        }

        [Test]
        public void Result_Failure_With_Exception()
        {
            var r = Result<int>.Fail(new BusinessException());
            Assert.Multiple(() =>
            {
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
            });
        }

        [Test]
        public void Result_Failure_With_Message()
        {
            var r = Result<int>.Fail("Test");
            Assert.Multiple(() =>
            {
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>().And.Message.EqualTo("Test"));
            });
        }

        [Test]
        public void Result_Success()
        {
            var r = Result.Success;
            Assert.That(r, Is.EqualTo(Result.Success));
        }

        [Test]
        public void Result_Ok_With_Value()
        {
            var r = Result.Ok(1);
            Assert.That(r, Is.EqualTo(Result<int>.Ok(1)));
        }

        [Test]
        public void Result_Fail_With_Exception()
        {
            var r = Result.Fail(new BusinessException());
            Assert.Multiple(() =>
            {
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
            });
        }

        [Test]
        public void Result_Fail_With_Message()
        {
            var r = Result.Fail("Test");
            Assert.Multiple(() =>
            {
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<BusinessException>().And.Message.EqualTo("Test"));
            });
        }

        [Test]
        public void Result_ValidationError_With_Message()
        {
            var r = Result.ValidationError("Test");
            Assert.Multiple(() =>
            {
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<ValidationException>().And.Message.EqualTo("Test"));
            });
        }

        [Test]
        public void Result_ValidationError_With_MessageItem()
        {
            var r = Result.ValidationError(new MessageItem { Type = MessageType.Error, Text = "Error" });
            Assert.Multiple(() =>
            {
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<ValidationException>());
            });
            var ve = (ValidationException)r.Error;
            Assert.That(ve.Messages, Is.Not.Null.And.Count.EqualTo(1));
        }

        [Test]
        public void Result_ValidationError_With_MessageItems()
        {
            var r = Result.ValidationError(new MessageItemCollection { new MessageItem { Type = MessageType.Error, Text = "Error" }, new MessageItem { Type = MessageType.Error, Text = "Error2" } });
            Assert.Multiple(() =>
            {
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<ValidationException>());
            });
            var ve = (ValidationException)r.Error;
            Assert.That(ve.Messages, Is.Not.Null.And.Count.EqualTo(2));
        }

        [Test]
        public void Result_ConflictError()
        {
            var r = Result.ConflictError();
            Assert.Multiple(() =>
            {
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<ConflictException>());
            });
        }

        [Test]
        public void Result_ConcurrencyError()
        {
            var r = Result.ConcurrencyError();
            Assert.Multiple(() =>
            {
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<ConcurrencyException>());
            });
        }

        [Test]
        public void Result_DuplicateError()
        {
            var r = Result.DuplicateError();
            Assert.Multiple(() =>
            {
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<DuplicateException>());
            });
        }

        [Test]
        public void Result_NotFoundError()
        {
            var r = Result.NotFoundError();
            Assert.Multiple(() =>
            {
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
            });
        }

        [Test]
        public void Result_TransientError()
        {
            var r = Result.TransientError();
            Assert.Multiple(() =>
            {
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<TransientException>());
            });
        }

        [Test]
        public void Result_AuthenticationError()
        {
            var r = Result.AuthenticationError();
            Assert.Multiple(() =>
            {
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<AuthenticationException>());
            });
        }

        [Test]
        public void Result_AuthorizationError()
        {
            var r = Result.AuthorizationError();
            Assert.Multiple(() =>
            {
                Assert.That(r.IsFailure, Is.True);
                Assert.That(r.Error, Is.Not.Null.And.InstanceOf<AuthorizationException>());
            });
        }
    }
}