using CoreEx.Entities;
using CoreEx.Entities.Extended;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace CoreEx.Test.Framework.Entities.Extended
{
    [TestFixture]
    public class EntityBaseTest
    {
        [Test]
        public void ChangeLog_Clone()
        {
            var cl = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() };
            var co = (ChangeLog)cl.Clone();

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
            ChangeLog? cl2 = null;

            var cl1 = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() };
            Assert.IsFalse(cl1.Equals(cl2));

            cl2 = (ChangeLog)cl1.Clone();
            Assert.IsTrue(cl1.Equals(cl2));

            cl2.CreatedBy = "username2";
            Assert.IsFalse(cl1.Equals(cl2));

            ChangeLog cl3 = cl1;
            Assert.IsTrue(cl3.Equals(cl1));
        }

        [Test]
        public void ChangeLog_Equals2()
        {
            ChangeLog? cl1 = null;
            ChangeLog? cl2 = null;

            Assert.IsTrue(cl1 == cl2);

            cl1 = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() };
            Assert.IsFalse(cl1 == cl2);

            cl2 = (ChangeLog)cl1.Clone();
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
            var cl2 = (ChangeLog)cl1.Clone();
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
            var po = (Person)p.Clone();

            Assert.IsNotNull(po);
            Assert.AreEqual("dave", po.Name);
            Assert.AreEqual(30, po.Age);
            Assert.AreEqual("username", po.ChangeLog!.CreatedBy);
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
            Person? p2 = null;

            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            Assert.IsFalse(p1.Equals(p2));

            p2 = (Person)p1.Clone();
            Assert.IsTrue(p1.Equals(p2));

            p2.ChangeLog!.CreatedBy = "username2";
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
            Person? p2 = null;

            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            Assert.IsFalse(p1 == p2);

            p2 = (Person)p1.Clone();
            Assert.IsTrue(p1 == p2);

            p2.ChangeLog!.CreatedBy = "username2";
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
            var p2 = (Person)p1.Clone();
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
        public void Person_Load()
        {
            for (int i = 0; i < 1000; i++)
            {
                var p1 = new Person { Name = "dave", Age = i, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
                var p2 = new Person { Name = "dave", Age = i, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };

                p1.CleanUp();
                p1.Clone();
                p1.AcceptChanges();

                if (p1 == p2 && p1.GetHashCode() == p2.GetHashCode())
                {
                    if (!p1.IsReadOnly)
                        p1.MakeReadOnly();
                }
                else
                    throw new InvalidOperationException("Should not get here!");
            }
        }

        [Test]
        public void PersonEx_Clone()
        {
            var p = new PersonEx { Name = "dave", Age = 30, Salary = 1m, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var po = (PersonEx)p.Clone();

            Assert.IsNotNull(po);
            Assert.AreEqual("dave", po.Name);
            Assert.AreEqual(30, po.Age);
            Assert.AreEqual(1m, po.Salary);
            Assert.AreEqual("username", po.ChangeLog!.CreatedBy);
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
            PersonEx? p2 = null;

            var p1 = new PersonEx { Name = "dave", Age = 30, Salary = 1m, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            Assert.IsFalse(p1.Equals(p2));

            p2 = (PersonEx)p1.Clone();
            Assert.IsTrue(p1.Equals(p2));

            p2.ChangeLog!.CreatedBy = "username2";
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
            PersonEx? p2 = null;

            var p1 = new PersonEx { Name = "dave", Age = 30, Salary = 1m, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            Assert.IsFalse(p1 == p2);

            p2 = (PersonEx)p1.Clone();
            Assert.IsTrue(p1 == p2);

            p2.ChangeLog!.CreatedBy = "username2";
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
            var p2 = (PersonEx)p1.Clone();
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

        [Test]
        public void Collection_Person_Clone()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1, p2 };

            var pc2 = (PersonCollection)pc.Clone();
            Assert.IsNotNull(pc2);
            Assert.AreEqual(2, pc2.Count);
            Assert.IsFalse(ReferenceEquals(pc2[0], p1));
            Assert.IsFalse(ReferenceEquals(pc2[1], p2));
            Assert.AreEqual(pc2[0], p1);
            Assert.AreEqual(pc2[1], p2);
        }

        [Test]
        public void Collection_Person_Equals()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1, p2 };

            Assert.IsFalse(pc.Equals(null));
            Assert.IsTrue(pc!.Equals(pc));

            var pc2 = (PersonCollection)pc.Clone();
            Assert.IsTrue(pc.Equals(pc2));
            pc2.Add(new Person { Name = "john", Age = 35 });
            Assert.IsFalse(pc.Equals(pc2));

            pc2 = (PersonCollection)pc.Clone();
            Assert.IsTrue(pc.Equals(pc2));
            pc2[1].Name = "jenny";
            Assert.IsFalse(pc.Equals(pc2));
        }

        [Test]
        public void Collection_Person_Equals2()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1, p2 };
            var pc2 = pc;

            Assert.IsFalse(pc == null);
            Assert.IsTrue(pc == pc2);

            pc2 = (PersonCollection)pc!.Clone();
            Assert.IsTrue(pc == pc2);
            pc2.Add(new Person { Name = "john", Age = 35 });
            Assert.IsFalse(pc == pc2);

            pc2 = (PersonCollection)pc.Clone();
            Assert.IsTrue(pc == pc2);
            pc2[1].Name = "jenny";
            Assert.IsFalse(pc == pc2);
        }

        [Test]
        public void Collection_Person_HashCode()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1, p2 };
            var pc2 = (PersonCollection)pc.Clone();
            Assert.AreEqual(pc.GetHashCode(), pc2.GetHashCode());

            pc2[0].ChangeLog!.CreatedBy = "username2";
            Assert.AreNotEqual(pc.GetHashCode(), pc2.GetHashCode());
        }

        [Test]
        public void Collection_Person_AcceptChanges()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1 };
            Assert.IsTrue(pc.IsChanged);
            Assert.IsTrue(pc[0].IsChanged);
            Assert.IsTrue(pc[0].ChangeLog!.IsChanged);

            pc.AcceptChanges();
            Assert.IsFalse(pc.IsChanged);
            Assert.IsFalse(pc[0].IsChanged);
            Assert.IsFalse(pc[0].ChangeLog!.IsChanged);

            pc.Add(p2);
            Assert.IsTrue(pc.IsChanged);
            Assert.IsFalse(pc[0].IsChanged);
            Assert.IsFalse(pc[0].ChangeLog!.IsChanged);
            Assert.IsTrue(pc[1].IsChanged);

            pc.AcceptChanges();
            Assert.IsFalse(pc.IsChanged);
            Assert.IsFalse(pc[0].IsChanged);
            Assert.IsFalse(pc[0].ChangeLog!.IsChanged);
            Assert.IsFalse(pc[1].IsChanged);

            pc[0].ChangeLog!.CreatedBy = "username2";
            Assert.IsTrue(pc.IsChanged);
            Assert.IsTrue(pc[0].IsChanged);
            Assert.IsTrue(pc[0].ChangeLog!.IsChanged);
            Assert.IsFalse(pc[1].IsChanged);
        }

        [Test]
        public void Collect_Person_Clear()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1, p2 };
            Assert.IsTrue(pc.IsChanged);

            pc.AcceptChanges();
            Assert.IsFalse(pc.IsChanged);

            pc.Clear();
            Assert.IsTrue(pc.IsChanged);
        }

        [Test]
        public void Collection_Person_MakeReadOnly()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1 };
            pc.MakeReadOnly();
            Assert.IsTrue(pc.IsReadOnly);
            Assert.IsTrue(pc[0].IsReadOnly);
            Assert.IsTrue(pc[0].ChangeLog!.IsReadOnly);
            Assert.Throws<InvalidOperationException>(() => pc.Add(p2));
            Assert.AreEqual(1, pc.Count);

            Assert.Throws<InvalidOperationException>(() => pc.Clear());
            Assert.AreEqual(1, pc.Count);

            Assert.Throws<InvalidOperationException>(() => pc.Remove(p1));
            Assert.AreEqual(1, pc.Count);

            Assert.Throws<InvalidOperationException>(() => pc.RemoveAt(0));
            Assert.AreEqual(1, pc.Count);

            Assert.Throws<InvalidOperationException>(() => pc[0] = p2);
            Assert.AreEqual(1, pc.Count);
        }

        [Test]
        public void Collection_Person_GetByPrimaryKey()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1, p2 };

            var pi = pc.GetByKey();
            Assert.IsNull(pi);

            pi = pc.GetByKey("dave");
            Assert.AreEqual(pi, p1);

            pi = pc.GetByKey("bazza");
            Assert.IsNull(pi);

            pi = pc.GetByKey(p1.PrimaryKey);
            Assert.AreEqual(pi, p1);

            pi = pc.GetByKey(new CompositeKey("bazza"));
            Assert.IsNull(pi);
        }

        [Test]
        public void Collection_Person_DeleteByPrimaryKey()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var p3 = new Person { Name = "dave", Age = 40 };
            var pc = new PersonCollection { p1, p2, p3 };

            pc.RemoveByKey("rebecca");
            Assert.AreEqual(3, pc.Count);
            Assert.AreEqual("dave", pc[0].Name);

            pc.RemoveByKey("dave");
            Assert.AreEqual(1, pc.Count);
            Assert.AreEqual("mary", pc[0].Name);

            pc.RemoveByKey("mary");
            Assert.AreEqual(0, pc.Count);
        }

        [Test]
        public void Collection_Person_ItemsAreAllUnique()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var p3 = new Person { Name = "dave", Age = 40 };
            var pc = new PersonCollection { p1, p2, p3 };

            Assert.IsTrue(pc.IsAnyDuplicates());

            pc.Remove(p3);
            Assert.IsFalse(pc.IsAnyDuplicates());

            pc = new PersonCollection { null!, null! };
            Assert.IsTrue(pc.IsAnyDuplicates());

            pc.RemoveAt(0);
            Assert.IsFalse(pc.IsAnyDuplicates());
        }

        [Test]
        public void CollectionResult_Person()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1, p2 };
            var pcr = new PersonCollectionResult(pc);
            Assert.AreSame(pcr.Collection, pc);

            var pc2 = (PersonCollection)pcr;
            Assert.AreSame(pc, pc2);

            var pcr2 = (PersonCollectionResult)pcr.Clone();
            Assert.IsFalse(ReferenceEquals(pcr2, pcr));
            Assert.IsTrue(pcr2.Equals(pcr));
            Assert.IsTrue(pcr2 == pcr);
        }

        [Test]
        public void Dictionary_Person_IsChanged()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pd = new PersonDictionary { { "dave", p1 }, { "mary", p2 } };
            Assert.IsTrue(pd.IsChanged);

            pd.AcceptChanges();
            Assert.IsFalse(pd.IsChanged);
            Assert.IsFalse(p1.IsChanged);
            Assert.IsFalse(p2.IsChanged);

            p1.ChangeLog.CreatedDate = p1.ChangeLog.CreatedDate.Value.AddMinutes(1);
            Assert.IsTrue(pd.IsChanged);
            Assert.IsTrue(p1.IsChanged);
            Assert.IsFalse(p2.IsChanged);

            pd.AcceptChanges();
            Assert.IsFalse(pd.IsChanged);
            Assert.IsFalse(p1.IsChanged);
            Assert.IsFalse(p2.IsChanged);

            pd.Remove("mary");
            Assert.IsTrue(pd.IsChanged);

            pd.AcceptChanges();
            Assert.IsFalse(pd.IsChanged);

            pd.Remove("mary");
            Assert.IsFalse(pd.IsChanged);
        }

        [Test]
        public void Dictionary_Person_ReadOnly()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pd = new PersonDictionary { { "dave", p1 }, { "mary", p2 } };
            pd.MakeReadOnly();
            Assert.IsTrue(pd.IsReadOnly);

            Assert.Throws<InvalidOperationException>(() => pd.Clear());
            Assert.Throws<InvalidOperationException>(() => pd.Remove("mary"));
            Assert.Throws<InvalidOperationException>(() => pd.Add("donna", new Person { Name = "Donna" }));
            Assert.Throws<InvalidOperationException>(() => pd["donna"] = new Person { Name = "Donna" });
        }

        [Test]
        public void Dictionary_Person_Equals()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pd = new PersonDictionary { { "dave", p1 }, { "mary", p2 } };

            var px1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLog { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var px2 = new Person { Name = "mary", Age = 25 };
            var pxd = new PersonDictionary { { "dave", px1 }, { "mary", px2 } };

            Assert.IsTrue(pd == pxd);
            Assert.AreEqual(pd.GetHashCode(), pxd.GetHashCode());

            px2.Name += "X";
            Assert.IsFalse(pd == pxd);
            Assert.AreNotEqual(pd.GetHashCode(), pxd.GetHashCode());
        }

        private DateTime CreateDateTime() => new DateTime(2000, 01, 01, 12, 45, 59);

        public class Person : EntityBase<Person>, IPrimaryKey
        {
            private string? _name;
            private int _age;
            private ChangeLog? _changeLog;

            public string? Name { get => _name; set => SetValue(ref _name, value); }
            public int Age { get => _age; set => SetValue(ref _age, value); }
            public ChangeLog? ChangeLog { get => _changeLog; set => SetValue(ref _changeLog, value); }

            public CompositeKey PrimaryKey => new CompositeKey(Name);

            protected override IEnumerable<IPropertyValue> GetPropertyValues()
            {
                yield return CreateProperty(Name, v => Name = v);
                yield return CreateProperty(Age, v => Age = v);
                yield return CreateProperty(ChangeLog, v => ChangeLog = v);
            }
        }

        public class PersonCollection : PrimaryKeyBaseCollection<Person, PersonCollection>
        {
            public PersonCollection() { }

            public PersonCollection(IEnumerable<Person> entities) : base(entities) { }

            public static implicit operator PersonCollection(PersonCollectionResult result) => result?.Collection!;
        }

        public class PersonCollectionResult : EntityCollectionResult<PersonCollection, Person, PersonCollectionResult>
        {
            public PersonCollectionResult() { }

            public PersonCollectionResult(PagingArgs paging) : base(paging) { }

            public PersonCollectionResult(PersonCollection collection, PagingArgs? paging = null) : base(paging) => Collection = collection;
        }

        public class PersonEx : Person
        {
            private decimal? _salary;

            public decimal? Salary { get => _salary; set => SetValue(ref _salary, value); }

            protected override IEnumerable<IPropertyValue> GetPropertyValues()
            {
                foreach (var pv in base.GetPropertyValues())
                    yield return pv;

                yield return CreateProperty(Salary, v => Salary = v);
            }

            public override bool Equals(object? other) => base.Equals(other);

            public static bool operator ==(PersonEx? a, PersonEx? b) => Equals(a, b);

            public static bool operator !=(PersonEx? a, PersonEx? b) => !Equals(a, b);

            public override int GetHashCode() => base.GetHashCode();

            public override object Clone() => CreateClone<PersonEx>(this);
        }

        public class PersonExCollection : EntityBaseCollection<PersonEx, PersonExCollection>
        {
            public PersonExCollection() { }

            public PersonExCollection(IEnumerable<PersonEx> entities) : base(entities) { }
        }

        public class PersonDictionary : EntityBaseDictionary<Person, PersonDictionary> { }
    }
}