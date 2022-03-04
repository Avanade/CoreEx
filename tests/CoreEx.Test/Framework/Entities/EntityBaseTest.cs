using CoreEx.Entities;
using NUnit.Framework;
using System;

namespace CoreEx.Test.Framework.Entities
{
    [TestFixture]
    public class EntityBaseTest
    {
        [Test]
        public void ChangeLog_Clone()
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
        public void ChangeLog_CopyFrom()
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
        public void ChangeLog_Equals()
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
        public void ChangeLog_Equals2()
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
        public void ChangeLog_HashCode()
        {
            var cl1 = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() };
            var cl2 = cl1.Clone();
            Assert.AreEqual(cl1.GetHashCode(), cl2.GetHashCode());

            cl2.CreatedBy = "username2";
            Assert.AreNotEqual(cl1.GetHashCode(), cl2.GetHashCode());
        }

        [Test]
        public void ChangeLog_IsInitial()
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
        public void ChangleLog_AcceptChanges()
        {
            var cl = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() };
            Assert.IsTrue(cl.IsChanged);

            cl.AcceptChanges();
            Assert.IsFalse(cl.IsChanged);

            cl.UpdatedBy = "username";
            Assert.IsTrue(cl.IsChanged);
        }

        [Test]
        public void ChangeLog_MakeReadonly()
        {
            var cl = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() };
            Assert.IsFalse(cl.IsReadOnly);

            cl.MakeReadOnly();
            Assert.IsTrue(cl.IsReadOnly);

            Assert.Throws<InvalidOperationException>(() => cl.UpdatedBy = "username");
        }

        [Test]
        public void Person_Clone()
        {
            var p = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var po = p.Clone();

            Assert.IsNotNull(po);
            Assert.AreEqual("dave", po.Name);
            Assert.AreEqual(30, po.Age);
            Assert.AreEqual("username", po.ChangeLog.CreatedBy);
            Assert.AreEqual(CreateDateTime(), po.ChangeLog.CreatedDate);
            Assert.IsNull(po.ChangeLog.UpdatedBy);
            Assert.IsNull(po.ChangeLog.UpdatedDate);
        }

        [Test]
        public void Person_CopyFrom()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "sarah", Age = 29, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime(), UpdatedBy = "username2", UpdatedDate = CreateDateTime().AddDays(1) } };

            p1.CopyFrom(p2);

            Assert.AreEqual("sarah", p1.Name);
            Assert.AreEqual(29, p1.Age); Assert.AreEqual("username", p1.ChangeLog.CreatedBy);
            Assert.AreEqual(CreateDateTime(), p1.ChangeLog.CreatedDate);
            Assert.AreEqual("username2", p1.ChangeLog.UpdatedBy);
            Assert.AreEqual(CreateDateTime().AddDays(1), p1.ChangeLog.UpdatedDate);
        }

        [Test]
        public void Person_Equals()
        {
            Person p2 = null;

            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            Assert.IsFalse(p1.Equals(p2));

            p2 = p1.Clone();
            Assert.IsTrue(p1.Equals(p2));

            p2.ChangeLog.CreatedBy = "username2";
            Assert.IsFalse(p1.Equals(p2));

            p2.ChangeLog.CreatedBy = "username";
            Assert.IsTrue(p1.Equals(p2));

            p2.Name = "mike";
            Assert.IsFalse(p1.Equals(p2));

            Person p3 = p1;
            Assert.IsTrue(p3.Equals(p1));
        }

        [Test]
        public void Person_Equals2()
        {
            Person p2 = null;

            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            Assert.IsFalse(p1 == p2);

            p2 = p1.Clone();
            Assert.IsTrue(p1 == p2);

            p2.ChangeLog.CreatedBy = "username2";
            Assert.IsFalse(p1 == p2);

            p2.ChangeLog.CreatedBy = "username";
            Assert.IsTrue(p1 == p2);

            p2.Name = "mike";
            Assert.IsFalse(p1 == p2);

            Person p3 = p1;
            Assert.IsTrue(p3 == p1);
        }

        [Test]
        public void Person_HashCode()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = p1.Clone();
            Assert.AreEqual(p1.GetHashCode(), p2.GetHashCode());

            p1.Name = "mike";
            Assert.AreNotEqual(p1.GetHashCode(), p2.GetHashCode());

            p1.Name = "dave";
            Assert.AreEqual(p1.GetHashCode(), p2.GetHashCode());

            p1.ChangeLog.CreatedBy = "username2";
            Assert.AreNotEqual(p1.GetHashCode(), p2.GetHashCode());
        }

        [Test]
        public void Person_IsInitial()
        {
            var p = new Person();
            Assert.IsTrue(p.IsInitial);

            p.Name = "mike";
            Assert.IsFalse(p.IsInitial);

            p.Name = null;
            Assert.IsTrue(p.IsInitial);

            p.ChangeLog = new ChangeLog { UpdatedDate = CreateDateTime() };
            Assert.IsFalse(p.IsInitial);

            p.ChangeLog.UpdatedDate = null;
            Assert.IsFalse(p.IsInitial);

            p.CleanUp();
            Assert.IsTrue(p.IsInitial);
        }

        [Test]
        public void Person_AcceptChanges()
        {
            var p = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            Assert.IsTrue(p.IsChanged);
            Assert.IsTrue(p.ChangeLog.IsChanged);

            p.AcceptChanges();
            Assert.IsFalse(p.IsChanged);

            p.Name = "julie";
            Assert.IsTrue(p.IsChanged);
            Assert.IsFalse(p.ChangeLog.IsChanged);

            p.AcceptChanges();
            Assert.IsFalse(p.IsChanged);
            Assert.IsFalse(p.ChangeLog.IsChanged);

            p.ChangeLog.CreatedBy = "username2";
            Assert.IsTrue(p.ChangeLog.IsChanged);
            Assert.IsTrue(p.IsChanged);
        }

        [Test]
        public void Person_Bubbling()
        {
            var p = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            p.AcceptChanges();
            var cl1 = p.ChangeLog;
            var cl2 = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() };
            Assert.IsFalse(p.IsChanged);
            Assert.IsFalse(p.ChangeLog.IsChanged);
            Assert.IsFalse(cl1.IsChanged);
            Assert.IsTrue(cl2.IsChanged);

            p.ChangeLog = cl2;
            Assert.IsTrue(p.IsChanged);
            Assert.IsTrue(p.ChangeLog.IsChanged);
            Assert.IsFalse(cl1.IsChanged);
            Assert.IsTrue(cl2.IsChanged);

            p.AcceptChanges();
            Assert.IsFalse(p.IsChanged);
            Assert.IsFalse(p.ChangeLog.IsChanged);
            Assert.IsFalse(cl1.IsChanged);
            Assert.IsFalse(cl2.IsChanged);

            cl1.UpdatedBy = "username";
            Assert.IsFalse(p.IsChanged);
            Assert.IsFalse(p.ChangeLog.IsChanged);
            Assert.IsTrue(cl1.IsChanged);
            Assert.IsFalse(cl2.IsChanged);

            cl2.UpdatedBy = "username";
            Assert.IsTrue(p.IsChanged);
            Assert.IsTrue(p.ChangeLog.IsChanged);
            Assert.IsTrue(cl1.IsChanged);
            Assert.IsTrue(cl2.IsChanged);
        }

        [Test]
        public void Person_MakeReadonly()
        {
            var cl = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() };
            Assert.IsFalse(cl.IsReadOnly);

            cl.MakeReadOnly();
            Assert.IsTrue(cl.IsReadOnly);

            Assert.Throws<InvalidOperationException>(() => cl.UpdatedBy = "username");
        }

        [Test]
        public void PersonEx_Clone()
        {
            var p = new PersonEx { Name = "dave", Age = 30, Salary = 1m, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var po = p.Clone();

            Assert.IsNotNull(po);
            Assert.AreEqual("dave", po.Name);
            Assert.AreEqual(30, po.Age);
            Assert.AreEqual(1m, po.Salary);
            Assert.AreEqual("username", po.ChangeLog.CreatedBy);
            Assert.AreEqual(CreateDateTime(), po.ChangeLog.CreatedDate);
            Assert.IsNull(po.ChangeLog.UpdatedBy);
            Assert.IsNull(po.ChangeLog.UpdatedDate);
        }

        [Test]
        public void PersonEx_CopyFrom()
        {
            var p1 = new PersonEx { Name = "dave", Age = 30, Salary = 1m, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new PersonEx { Name = "sarah", Age = 29, Salary = 2m, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime(), UpdatedBy = "username2", UpdatedDate = CreateDateTime().AddDays(1) } };

            p1.CopyFrom(p2);

            Assert.AreEqual("sarah", p1.Name);
            Assert.AreEqual(29, p1.Age);
            Assert.AreEqual(2m, p1.Salary);
            Assert.AreEqual("username", p1.ChangeLog.CreatedBy);
            Assert.AreEqual(CreateDateTime(), p1.ChangeLog.CreatedDate);
            Assert.AreEqual("username2", p1.ChangeLog.UpdatedBy);
            Assert.AreEqual(CreateDateTime().AddDays(1), p1.ChangeLog.UpdatedDate);
        }

        [Test]
        public void PersonEx_Equals()
        {
            PersonEx p2 = null;

            var p1 = new PersonEx { Name = "dave", Age = 30, Salary = 1m, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            Assert.IsFalse(p1.Equals(p2));

            p2 = p1.Clone();
            Assert.IsTrue(p1.Equals(p2));

            p2.ChangeLog.CreatedBy = "username2";
            Assert.IsFalse(p1.Equals(p2));

            p2.ChangeLog.CreatedBy = "username";
            Assert.IsTrue(p1.Equals(p2));

            p2.Name = "mike";
            Assert.IsFalse(p1.Equals(p2));

            p2.Name = "dave";
            Assert.IsTrue(p1.Equals(p2));

            p2.Salary = 2m;
            Assert.IsFalse(p1.Equals(p2));

            Person p3 = p1;
            Assert.IsTrue(p3.Equals(p1));
        }

        [Test]
        public void PersonEx_Equals2()
        {
            PersonEx p2 = null;

            var p1 = new PersonEx { Name = "dave", Age = 30, Salary = 1m, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            Assert.IsFalse(p1 == p2);

            p2 = p1.Clone();
            Assert.IsTrue(p1 == p2);

            p2.ChangeLog.CreatedBy = "username2";
            Assert.IsFalse(p1 == p2);

            p2.ChangeLog.CreatedBy = "username";
            Assert.IsTrue(p1 == p2);

            p2.Name = "mike";
            Assert.IsFalse(p1 == p2);

            p2.Name = "dave";
            Assert.IsTrue(p1 == p2);

            p2.Salary = 2m;
            Assert.IsFalse(p1 == p2);

            Person p3 = p1;
            Assert.IsTrue(p3 == p1);
        }

        [Test]
        public void PersonEx_HashCode()
        {
            var p1 = new PersonEx { Name = "dave", Age = 30, Salary = 1m, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = p1.Clone();
            Assert.AreEqual(p1.GetHashCode(), p2.GetHashCode());

            p1.Name = "mike";
            Assert.AreNotEqual(p1.GetHashCode(), p2.GetHashCode());

            p1.Name = "dave";
            Assert.AreEqual(p1.GetHashCode(), p2.GetHashCode());

            p1.Salary = 2m;
            Assert.AreNotEqual(p1.GetHashCode(), p2.GetHashCode());

            p1.Salary = 1m;
            Assert.AreEqual(p1.GetHashCode(), p2.GetHashCode());

            p1.ChangeLog.CreatedBy = "username2";
            Assert.AreNotEqual(p1.GetHashCode(), p2.GetHashCode());
        }

        [Test]
        public void PersonEx_IsInitial()
        {
            var p = new PersonEx();
            Assert.IsTrue(p.IsInitial);

            p.Salary = 0m;
            Assert.IsFalse(p.IsInitial);

            p.Salary = null;
            Assert.IsTrue(p.IsInitial);

            p.Name = "mike";
            Assert.IsFalse(p.IsInitial);

            p.Name = null;
            Assert.IsTrue(p.IsInitial);

            p.ChangeLog = new ChangeLog { UpdatedDate = CreateDateTime() };
            Assert.IsFalse(p.IsInitial);

            p.ChangeLog.UpdatedDate = null;
            Assert.IsFalse(p.IsInitial);

            p.CleanUp();
            Assert.IsTrue(p.IsInitial);
        }

        private DateTime CreateDateTime() => new DateTime(2000, 01, 01, 12, 45, 59);

        internal class Person : EntityBase<Person>
        {
            private string _name;
            private int _age;
            private ChangeLog _changeLog;

            public string Name { get => _name; set => SetValue(ref _name, value); }
            public int Age { get => _age; set => SetValue(ref _age, value); }
            public ChangeLog ChangeLog { get => _changeLog; set => SetValue(ref _changeLog, value); }

            public override bool Equals(Person other) => ReferenceEquals(this, other) || (other != null && base.Equals(other)
                && Equals(Name, other!.Name)
                && Equals(Age, other.Age)
                && Equals(ChangeLog, other.ChangeLog));

            public override int GetHashCode()
            {
                var hash = new HashCode();
                hash.Add(Name);
                hash.Add(Age);
                hash.Add(ChangeLog);
                return base.GetHashCode() ^ hash.ToHashCode();
            }

            public override void CopyFrom(Person from)
            {
                base.CopyFrom(from);
                Name = from.Name;
                Age = from.Age;
                ChangeLog = CopyOrClone(from.ChangeLog, ChangeLog);
            }

            protected override void OnApplyAction(EntityAction action)
            {
                base.OnApplyAction(action);
                Name = ApplyAction(Name, action);
                Age = ApplyAction(Age, action);
                ChangeLog = ApplyAction(ChangeLog, action);
            }

            public override bool IsInitial => base.IsInitial
                && Cleaner.IsDefault(Name)
                && Cleaner.IsDefault(Age)
                && Cleaner.IsDefault(ChangeLog);
        }

        internal class PersonEx : Person, ICloneable<PersonEx>, ICopyFrom<PersonEx>, IEquatable<PersonEx>
        {
            private decimal? _salary;

            public decimal? Salary { get => _salary; set => SetValue(ref _salary, value); }

            public bool Equals(PersonEx other) => ReferenceEquals(this, other) || (other != null && base.Equals(other)
                && Equals(Salary, other!.Salary));

            public override bool Equals(object obj) => (obj is PersonEx other) && Equals(other);

            public static bool operator ==(PersonEx a, PersonEx b) => Equals(a, b);

            public static bool operator !=(PersonEx a, PersonEx b) => !Equals(a, b);

            public override int GetHashCode()
            {
                var hash = new HashCode();
                hash.Add(Salary);
                return base.GetHashCode() ^ hash.ToHashCode();
            }

            public new PersonEx Clone()
            {
                var clone = new PersonEx();
                clone.CopyFrom(this);
                return clone;
            }

            public void CopyFrom(PersonEx from)
            {
                base.CopyFrom(from);
                Salary = from.Salary;
                ChangeLog = CopyOrClone(from.ChangeLog, ChangeLog);
            }

            protected override void OnApplyAction(EntityAction action)
            {
                base.OnApplyAction(action);
                Salary = ApplyAction(Salary, action);
            }

            public override bool IsInitial => base.IsInitial
                && Cleaner.IsDefault(Salary);
        }
    }
}