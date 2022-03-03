using CoreEx.Entities;
using NUnit.Framework;
using System;

namespace CoreEx.Test.Framework.Entities
{
    [TestFixture]
    public class EntityBaseTest
    {
        [Test]
        public void Clone()
        {
            var cl = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() };
            var co = cl.Clone();

            Assert.IsNotNull(co);
            Assert.AreEqual("username", co.CreatedBy);
            Assert.AreEqual(CreateDateTime(), co.CreatedDate);
            Assert.IsNull(co.UpdatedBy);
            Assert.IsNull(co.UpdatedDate);
        }

        [Test]
        public void CopyFrom()
        {
            var cl1 = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() };
            var cl2 = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime(), UpdatedBy = "username2", UpdatedDate = CreateDateTime().AddDays(1) };
            cl1.CopyFrom(cl2);

            Assert.AreEqual("username", cl1.CreatedBy);
            Assert.AreEqual(CreateDateTime(), cl1.CreatedDate);
            Assert.AreEqual("username2", cl1.UpdatedBy);
            Assert.AreEqual(CreateDateTime().AddDays(1), cl1.UpdatedDate);
        }

        [Test]
        public void Equals()
        {
            ChangeLog cl2 = null;

            var cl1 = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() };
            Assert.IsFalse(cl1.Equals(cl2));

            cl2 = cl1.Clone();
            Assert.IsTrue(cl1.Equals(cl2));

            cl2.CreatedBy = "username2";
            Assert.IsFalse(cl1.Equals(cl2));

            ChangeLog cl3 = cl1;
            Assert.IsTrue(cl3.Equals(cl1));
        }

        [Test]
        public void Equals2()
        {
            ChangeLog cl1 = null;
            ChangeLog cl2 = null;

            Assert.IsTrue(cl1 == cl2);

            cl1 = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() };
            Assert.IsFalse(cl1 == cl2);

            cl2 = cl1.Clone();
            Assert.IsTrue(cl1 == cl2);

            cl2.CreatedBy = "username2";
            Assert.IsFalse(cl1 == cl2);

            ChangeLog cl3 = cl1;
            Assert.IsTrue(cl3 == cl1);
        }

        [Test]
        public void HashCode()
        {
            var cl1 = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() };
            var cl2 = cl1.Clone();
            Assert.AreEqual(cl1.GetHashCode(), cl2.GetHashCode());

            cl2.CreatedBy = "username2";
            Assert.AreNotEqual(cl1.GetHashCode(), cl2.GetHashCode());
        }

        [Test]
        public void IsInitial()
        {
            var cl = new ChangeLog();
            Assert.IsTrue(cl.IsInitial);

            cl.UpdatedBy = "username";
            Assert.IsFalse(cl.IsInitial);

            cl.UpdatedBy = null;
            Assert.IsTrue(cl.IsInitial);

            cl.UpdatedDate = CreateDateTime();
            Assert.IsFalse(cl.IsInitial);
        }

        [Test]
        public void TrackChanges()
        {
            var cl = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() };
            Assert.IsTrue(cl.IsChanged);
            Assert.IsFalse(cl.IsChangeTracking);
            Assert.IsNull(cl.ChangeTracking);

            cl.AcceptChanges();
            Assert.IsFalse(cl.IsChanged);
            Assert.IsNull(cl.ChangeTracking);

            cl.TrackChanges();
            Assert.IsFalse(cl.IsChanged);
            Assert.IsTrue(cl.IsChangeTracking);
            Assert.IsNotNull(cl.ChangeTracking);

            cl.UpdatedBy = "username2";
            Assert.IsTrue(cl.IsChanged);
            Assert.IsTrue(cl.IsChangeTracking);
            Assert.AreEqual(1, cl.ChangeTracking.Count);
            Assert.AreEqual("UpdatedBy", cl.ChangeTracking[0]);

            cl.AcceptChanges();
            Assert.IsFalse(cl.IsChanged);
            Assert.IsFalse(cl.IsChangeTracking);
            Assert.IsNull(cl.ChangeTracking);

            Assert.AreEqual("username2", cl.UpdatedBy);
        }

        [Test]
        public void Editable()
        {
            var cl = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() };

            cl.BeginEdit();
            Assert.IsFalse(cl.IsChanged);

            cl.CreatedBy = "username2";
            Assert.IsTrue(cl.IsChanged);
            Assert.AreEqual("username2", cl.CreatedBy);

            cl.CancelEdit();
            Assert.IsFalse(cl.IsChanged);
            Assert.AreEqual("username", cl.CreatedBy);

            cl.BeginEdit();
            cl.CreatedBy = "username2";
            Assert.IsTrue(cl.IsChanged);
            Assert.AreEqual("username2", cl.CreatedBy);

            cl.EndEdit();
            Assert.IsFalse(cl.IsChanged);
            Assert.AreEqual("username2", cl.CreatedBy);
        }

        private DateTime CreateDateTime() => new DateTime(2000, 01, 01, 12, 45, 59);
    }
}