﻿using NUnit.Framework;
using CoreEx.Results;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class ResultTTest
    {
        [Test]
        public void Success_Property()
        {
            var r = Result<int>.None;
            Assert.Multiple(() =>
            {
                Assert.That(r.IsSuccess, Is.True);
                Assert.That(r.IsFailure, Is.False);
            });
        }

        [Test]
        public void Result_Success_Ctor()
        {
            var r = new Result<int>();
            Assert.That(r, Is.EqualTo(Result<int>.None));
        }

        [Test]
        public void Success_Is_Success()
        {
            var r = Result<int>.Ok();
            Assert.That(r, Is.EqualTo(Result<int>.None));
        }

        [Test]
        public void Success_Implicit_Conversion_From_Value()
        {
            var r = (Result<int>) 1;
            Assert.That(r.Value, Is.EqualTo(1));
        }

        [Test]
        public void Success_Implicit_Conversion_From_Result()
        {
            int i = Result<int>.Ok(1);
            Assert.That(i, Is.EqualTo(1));
        }

        [Test]
        public void Success_ThrowOnError()
        {
            Result<int>.Ok().ThrowOnError();
        }

        [Test]
        public void Failure_Ctor()
        {
            var r = new Result<int>(new BusinessException());
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
            var r = Result<int>.Fail(new BusinessException());
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
            var r = Result<int>.Fail("Test");
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
            var r = (Result<int>) new BusinessException();
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
            Assert.Throws<BusinessException>(() => Result<int>.Fail(new BusinessException()).ThrowOnError());
        }

        [Test]
        public void Compare_Success_And_Failure()
        {
            Assert.That(Result<int>.Fail(new BusinessException()), Is.Not.EqualTo(Result<int>.Ok()));
        }

        [Test]
        public void Compare_Two_Same_Successes()
        {
            var r1 = Result<int>.Ok();
            var r2 = Result<int>.Ok();
            Assert.That(r2, Is.EqualTo(r1));
        }

        [Test]
        public void Compare_Two_Same_Successes_With_Value()
        {
            var r1 = Result<int>.Ok(1);
            var r2 = Result<int>.Ok(1);
            Assert.That(r2, Is.EqualTo(r1));
        }

        [Test]
        public void Compare_Two_Different_Successes()
        {
            var r1 = Result<int>.Ok();
            var r2 = Result<int>.Ok(1);
            Assert.That(r2, Is.Not.EqualTo(r1));
        }

        [Test]
        public void Compare_Two_Different_Successes_With_Value()
        {
            var r1 = Result<int>.Ok(1);
            var r2 = Result<int>.Ok(2);
            Assert.That(r2, Is.Not.EqualTo(r1));
        }

        [Test]
        public void Compare_Two_Same_Failures()
        {
            var r1 = Result<int>.Fail(new BusinessException());
            var r2 = Result<int>.Fail(new BusinessException());
            Assert.That(r2, Is.EqualTo(r1));
        }

        [Test]
        public void Compare_Two_Different_Failures()
        {
            var r1 = Result<int>.Fail(new BusinessException());
            var r2 = Result<int>.Fail(new BusinessException("Test"));
            Assert.That(r2, Is.Not.EqualTo(r1));
        }

        [Test]
        public void Compare_Two_Different_Type_Failures()
        {
            var r1 = Result<int>.Fail(new ValidationException("Test"));
            var r2 = Result<int>.Fail(new BusinessException("Test"));
            Assert.That(r2, Is.Not.EqualTo(r1));
        }

        [Test]
        public void Result_Success_Explicit_Convert_To_Result()
        {
            Result<int> r = Result<int>.Ok(1);
            Result r2 = (Result)r;
            Assert.That(r2, Is.EqualTo(Result.Success));
        }

        [Test]
        public void Result_Failure_Explicit_Convert_To_Result()
        {
            Result<int> r = Result<int>.Fail(new BusinessException());
            Result r2 = (Result)r;
            Assert.Multiple(() =>
            {
                Assert.That(r2.IsFailure, Is.True);
                Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
            });
        }

        [Test]
        public void Success_Value_ToString()
        {
            var r = Result<int>.Ok(1);
            Assert.That(r.ToString(), Is.EqualTo("Success: 1"));
        }

        [Test]
        public void Success_Null_Value_ToString()
        {
            var r = Result<string?>.Ok(null);
            Assert.That(r.ToString(), Is.EqualTo("Success: null"));
        }

        [Test]
        public void Failure_ToString()
        {
            var r = Result<int>.Fail(new BusinessException("Test"));
            Assert.That(r.ToString(), Is.EqualTo("Failure: Test"));
        }

        [Test]
        public async Task AsTask()
        {
            var r = await Result.Go(1).AsTask();
            Assert.Multiple(() =>
            {
                Assert.That(r.IsSuccess, Is.True);
                Assert.That(r.Value, Is.EqualTo(1));
            });
        }

        [Test]
        public void Failure_Value()
        {
            var ir = (IResult)Result<int>.Fail("On no!");
            Assert.Throws<BusinessException>(() => _ = ir.Value);
        }

        [Test]
        public void Adjusts()
        {
            var r = Result<Person>.Ok(new Person());
            Assert.Multiple(() =>
            {
                Assert.That(r.IsSuccess, Is.True);
                Assert.That(r.Value.Id, Is.EqualTo(0));
            });

            var r2 = r.Adjusts(v => v.Id = 2);
            Assert.Multiple(() =>
            {
                Assert.That(r2.IsSuccess, Is.True);
                Assert.That(r2.Value.Id, Is.EqualTo(2));
            });

            r = Result<Person>.Fail(new BusinessException());
            r2 = r.Adjusts(v => v.Id = 2);
            Assert.That(r2.IsSuccess, Is.False);
        }

        [Test]
        public async Task AdjustsAsync()
        {
            var r = Result<Person>.Ok(new Person());
            var r2 = await r.AdjustsAsync(async v =>
            {
                await Task.CompletedTask;
                v.Id = 2;
            });

            Assert.Multiple(() =>
            {
                Assert.That(r2.IsSuccess, Is.True);
                Assert.That(r2.Value.Id, Is.EqualTo(2));
            });
        }

        [Test]
        public async Task Adjusts2Async()
        {
            var r = Result.GoAsync(async () =>
            {
                await Task.CompletedTask;
                return new Person();
            });

            var r2 = await r.AdjustsAsync(async v =>
            {
                await Task.CompletedTask;
                v.Id = 2;
            });

            Assert.Multiple(() =>
            {
                Assert.That(r2.IsSuccess, Is.True);
                Assert.That(r2.Value.Id, Is.EqualTo(2));
            });
        }

        [Test]
        public async Task Adjusts2AsyncTightLoop()
        {
            for (int i = 0; i < 10000; i++)
            {
                var r = Result.GoAsync(async () =>
                {
                    await Task.CompletedTask;
                    return new Person();
                });

                var r2 = await r.AdjustsAsync(async v =>
                {
                    await Task.CompletedTask;
                    v.Id = 2;
                });
            }
        }

        public class Person
        {
            public int Id { get; set; }
        }
    }
}