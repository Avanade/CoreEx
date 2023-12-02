using CoreEx.Entities;
using CoreEx.Http;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnitTestEx.Expectations;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.UnitTesting
{
    [TestFixture]
    public class ExpectationsTest
    {
        [Test]
        public void ExpectIdentifier()
        {
            var gt = GenericTester.CreateFor<Entity<string>>().ExpectIdentifier();
            var ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string>())))!;
            Assert.IsTrue(ex.Message.Contains("Expected IIdentifier.Id to have a non-null value."));

            ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { Id = "x" }));

            gt = GenericTester.CreateFor<Entity<string>>().ExpectIdentifier("y");
            ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { Id = "x" })))!;
            Assert.IsTrue(ex.Message.Contains("Expected IIdentifier.Id value of 'y'; actual 'x'."));

            ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { Id = "y" }));
        }

        [Test]
        public void ExpectPrimaryKey()
        {
            var gt = GenericTester.CreateFor<Entity2<string>>().ExpectPrimaryKey();
            var ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity2<string>())))!;
            Assert.IsTrue(ex.Message.Contains("Expected IPrimaryKey.PrimaryKey.Args to have one or more non-default values."));

            ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity2<string> { Id = "x" }));

            gt = GenericTester.CreateFor<Entity2<string>>().ExpectPrimaryKey("y");
            ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity2<string> { Id = "x" })))!;
            Assert.IsTrue(ex.Message.Contains("Expected IPrimaryKey.PrimaryKey value of 'y'; actual 'x'."));

            ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity2<string> { Id = "y" }));
        }

        [Test]
        public void ExpectETag()
        {
            var gt = GenericTester.CreateFor<Entity<string>>().ExpectETag();
            var ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string>())))!;
            Assert.IsTrue(ex.Message.Contains("Expected IETag.ETag to have a non-null value."));

            ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ETag = "xxx" }));

            gt = GenericTester.CreateFor<Entity<string>>().ExpectETag("yyy");
            ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ETag = "yyy" })))!;
            Assert.IsTrue(ex.Message.Contains("Expected IETag.ETag value of 'yyy' to be different to actual."));

            ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ETag = "xxx" }));
        }

        [Test]
        public void ExpectChangeLogCreated()
        {
            var gt = GenericTester.CreateFor<Entity<string>>().ExpectChangeLogCreated();
            ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ChangeLog = new ChangeLog { CreatedBy = "Anonymous", CreatedDate = DateTime.UtcNow } }));

            var ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string>())))!;
            Assert.IsTrue(ex.Message.Contains("Expected Change Log (IChangeLogAuditLog.ChangeLogAudit) to have a non-null value."));

            ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ChangeLog = new ChangeLog() })))!;
            Assert.IsTrue(ex.Message.Contains("Expected Change Log (IChangeLogAuditLog.ChangeLogAudit).CreatedBy value of 'Anonymous'; actual was null."));

            ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ChangeLog = new ChangeLog { CreatedBy = "Anonymous" } })))!;
            Assert.IsTrue(ex.Message.Contains("Expected Change Log (IChangeLogAuditLog.ChangeLogAudit).CreatedDate to have a non-null value."));

            ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ChangeLog = new ChangeLog { CreatedBy = "Anonymous", CreatedDate = DateTime.UtcNow.AddMinutes(-1) } })))!;
            Assert.IsTrue(ex.Message.Contains("Expected Change Log (IChangeLogAuditLog.ChangeLogAudit).CreatedDate value of '") && ex.Message.Contains("' must be greater than or equal to expected."));

            gt = GenericTester.CreateFor<Entity<string>>().ExpectChangeLogCreated("Banana");
            ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ChangeLog = new ChangeLog { CreatedBy = "Banana", CreatedDate = DateTime.UtcNow } }));

            gt = GenericTester.CreateFor<Entity<string>>().ExpectChangeLogCreated("b*");
            ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ChangeLog = new ChangeLog { CreatedBy = "Banana", CreatedDate = DateTime.UtcNow } }));

            gt = GenericTester.CreateFor<Entity<string>>().ExpectChangeLogCreated("*");
            ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ChangeLog = new ChangeLog { CreatedBy = "Banana", CreatedDate = DateTime.UtcNow } }));
        }

        [Test]
        public void ExpectChangeLogUpdated()
        {
            var gt = GenericTester.CreateFor<Entity<string>>().ExpectChangeLogUpdated();
            ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ChangeLog = new ChangeLog { UpdatedBy = "Anonymous", UpdatedDate = DateTime.UtcNow } }));

            var ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string>())))!;
            Assert.IsTrue(ex.Message.Contains("Expected Change Log (IChangeLogAuditLog.ChangeLogAudit) to have a non-null value."));

            ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ChangeLog = new ChangeLog() })))!;
            Assert.IsTrue(ex.Message.Contains("Expected Change Log (IChangeLogAuditLog.ChangeLogAudit).UpdatedBy value of 'Anonymous'; actual was null."));

            ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ChangeLog = new ChangeLog { UpdatedBy = "Anonymous" } })))!;
            Assert.IsTrue(ex.Message.Contains("Expected Change Log (IChangeLogAuditLog.ChangeLogAudit).UpdatedDate to have a non-null value."));

            ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ChangeLog = new ChangeLog { UpdatedBy = "Anonymous", UpdatedDate = DateTime.UtcNow.AddMinutes(-1) } })))!;
            Assert.IsTrue(ex.Message.Contains("Expected Change Log (IChangeLogAuditLog.ChangeLogAudit).UpdatedDate value of '") && ex.Message.Contains("' must be greater than or equal to expected."));

            gt = GenericTester.CreateFor<Entity<string>>().ExpectChangeLogUpdated("Banana");
            ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ChangeLog = new ChangeLog { UpdatedBy = "Banana", UpdatedDate = DateTime.UtcNow } }));

            gt = GenericTester.CreateFor<Entity<string>>().ExpectChangeLogUpdated("b*");
            ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ChangeLog = new ChangeLog { UpdatedBy = "Banana", UpdatedDate = DateTime.UtcNow } }));

            gt = GenericTester.CreateFor<Entity<string>>().ExpectChangeLogUpdated("*");
            ArrangerAssert(async () => await gt.ExpectationsArranger.AssertValueAsync(null, new Entity<string> { ChangeLog = new ChangeLog { UpdatedBy = "Banana", UpdatedDate = DateTime.UtcNow } }));
        }

        [Test]
        public void ExpectErrorType()
        {
            var gt = GenericTester.CreateFor<Entity<string>>().ExpectErrorType(CoreEx.Abstractions.ErrorType.ValidationError);
            ArrangerAssert(async () => await gt.ExpectationsArranger.AssertAsync(null, new ValidationException()));

            var ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertAsync(null, new BusinessException())))!;
            Assert.IsTrue(ex.Message.Contains("Expected error type of 'ValidationError' but actual was 'BusinessError'."));

            ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertAsync(null, new Exception())))!;
            Assert.IsTrue(ex.Message.Contains("Expected error type of 'ValidationError' but none was returned."));

            var hr = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertAsync(gt.ExpectationsArranger.CreateArgs(null, null).AddExtra(hr))))!;
            Assert.IsTrue(ex.Message.Contains("Expected error type of 'ValidationError' but none was returned."));

            hr.Headers.Add(HttpConsts.ErrorTypeHeaderName, "BusinessError");
            ex = Assert.Throws<Exception>(() => ArrangerAssert(async () => await gt.ExpectationsArranger.AssertAsync(gt.ExpectationsArranger.CreateArgs(null, null).AddExtra(hr))))!;
            Assert.IsTrue(ex.Message.Contains("Expected error type of 'ValidationError' but actual was 'BusinessError'."));

            hr.Headers.Add(HttpConsts.ErrorTypeHeaderName, "ValidationError");
            ArrangerAssert(async () => await gt.ExpectationsArranger.AssertAsync(gt.ExpectationsArranger.CreateArgs(null, null).AddExtra(hr)));
        }

        private static void ArrangerAssert(Func<Task> func)
        {
            try
            {
                var t = Task.Run(() => func());
                t.Wait();
            }
            catch (AggregateException agex) when (agex.InnerException is not null && agex.InnerException is AssertionException aex)
            {
                throw new Exception(aex.Message, aex);
            }
            catch (AssertionException aex)
            {
                throw new Exception(aex.Message, aex);
            }
        }

        public class Entity<TId> : IIdentifier<TId>, IChangeLog, IETag where TId : IComparable<TId>, IEquatable<TId>
        {
            public TId? Id { get; set; }

            public string? Name { get; set; }

            public ChangeLog? ChangeLog { get; set; }

            public string? ETag { get; set; }
        }

        public class Entity2<TId> : IPrimaryKey, IChangeLog, IETag where TId : IComparable<TId>, IEquatable<TId>
        {
            public TId? Id { get; set; }

            public string? Name { get; set; }

            public CompositeKey PrimaryKey => new(Id);

            public ChangeLog? ChangeLog { get; set; }

            public string? ETag { get; set; }
        }
    }
}