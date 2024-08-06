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
            var cl = new ChangeLogEx { CreatedBy = "username  ", CreatedDate = CreateDateTime() };
            var co = cl.Clone();

            Assert.That(co, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(co.CreatedBy, Is.EqualTo("username"));
                Assert.That(co.CreatedDate, Is.EqualTo(CreateDateTime()));
                Assert.That(co.UpdatedBy, Is.Null);
                Assert.That(co.UpdatedDate, Is.Null);
            });
        }

        [Test]
        public void ChangeLog_CopyFrom()
        {
            var cl1 = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() };
            var cl2 = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime(), UpdatedBy = "username2", UpdatedDate = CreateDateTime().AddDays(1) };
            cl1.CopyFrom(cl2);

            Assert.Multiple(() =>
            {
                Assert.That(cl1.CreatedBy, Is.EqualTo("username"));
                Assert.That(cl1.CreatedDate, Is.EqualTo(CreateDateTime()));
                Assert.That(cl1.UpdatedBy, Is.EqualTo("username2"));
                Assert.That(cl1.UpdatedDate, Is.EqualTo(CreateDateTime().AddDays(1)));
            });
        }

        [Test]
        public void ChangeLog_Equals()
        {
            ChangeLogEx? cl2 = null;

            var cl1 = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() };
            Assert.That(cl1, Is.Not.EqualTo(cl2));

            cl2 = (ChangeLogEx)cl1.Clone();
            Assert.That(cl1, Is.EqualTo(cl2));

            cl2.CreatedBy = "username2";
            Assert.That(cl1, Is.Not.EqualTo(cl2));

            ChangeLogEx cl3 = cl1;
            Assert.That(cl3, Is.EqualTo(cl1));
        }

        [Test]
        public void ChangeLog_Equals2()
        {
            ChangeLogEx? cl1 = null;
            ChangeLogEx? cl2 = null;

            Assert.That(cl1, Is.EqualTo(cl2));

            cl1 = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() };
            Assert.That(cl1, Is.Not.EqualTo(cl2));

            cl2 = (ChangeLogEx)cl1.Clone();
            Assert.That(cl1, Is.EqualTo(cl2));

            cl2.CreatedBy = "username2";
            Assert.That(cl1, Is.Not.EqualTo(cl2));

            ChangeLogEx cl3 = cl1;
            Assert.That(cl3, Is.EqualTo(cl1));
        }

        [Test]
        public void ChangeLog_HashCode()
        {
            var cl1 = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() };
            var cl2 = (ChangeLogEx)cl1.Clone();
            Assert.That(cl2.GetHashCode(), Is.EqualTo(cl1.GetHashCode()));

            cl2.CreatedBy = "username2";
            Assert.That(cl2.GetHashCode(), Is.Not.EqualTo(cl1.GetHashCode()));
        }

        [Test]
        public void ChangeLog_IsInitial()
        {
            var cl = new ChangeLogEx();
            Assert.That(cl.IsInitial, Is.True);

            cl.UpdatedBy = "username";
            Assert.That(cl.IsInitial, Is.False);

            cl.UpdatedBy = null;
            Assert.That(cl.IsInitial, Is.True);

            cl.UpdatedDate = CreateDateTime();
            Assert.That(cl.IsInitial, Is.False);
        }

        [Test]
        public void ChangleLog_AcceptChanges()
        {
            var cl = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() };
            Assert.That(cl.IsChanged, Is.True);

            cl.AcceptChanges();
            Assert.That(cl.IsChanged, Is.False);

            cl.UpdatedBy = "username";
            Assert.That(cl.IsChanged, Is.True);
        }

        [Test]
        public void ChangeLog_MakeReadonly()
        {
            var cl = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() };
            Assert.That(cl.IsReadOnly, Is.False);

            cl.MakeReadOnly();
            Assert.That(cl.IsReadOnly, Is.True);

            Assert.Throws<InvalidOperationException>(() => cl.UpdatedBy = "username");
        }

        [Test]
        public void Person_Clone()
        {
            var p = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var po = (Person)p.Clone();

            Assert.That(po, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(po.Name, Is.EqualTo("dave"));
                Assert.That(po.Age, Is.EqualTo(30));
                Assert.That(po.ChangeLog!.CreatedBy, Is.EqualTo("username"));
                Assert.That(po.ChangeLog.CreatedDate, Is.EqualTo(CreateDateTime()));
                Assert.That(po.ChangeLog.UpdatedBy, Is.Null);
                Assert.That(po.ChangeLog.UpdatedDate, Is.Null);
            });
        }

        [Test]
        public void Person_CopyFrom()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "sarah", Age = 29, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime(), UpdatedBy = "username2", UpdatedDate = CreateDateTime().AddDays(1) } };

            p1.CopyFrom(p2);

            Assert.Multiple(() =>
            {
                Assert.That(p1.Name, Is.EqualTo("sarah"));
                Assert.That(p1.Age, Is.EqualTo(29));
                Assert.That(p1.ChangeLog.CreatedBy, Is.EqualTo("username"));
                Assert.That(p1.ChangeLog.CreatedDate, Is.EqualTo(CreateDateTime()));
                Assert.That(p1.ChangeLog.UpdatedBy, Is.EqualTo("username2"));
                Assert.That(p1.ChangeLog.UpdatedDate, Is.EqualTo(CreateDateTime().AddDays(1)));
            });
        }

        [Test]
        public void Person_CreateFrom()
        {
            var p2 = new Person { Name = "sarah", Age = 29, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime(), UpdatedBy = "username2", UpdatedDate = CreateDateTime().AddDays(1) } };
            var p1 = p2.CreateFromAs<Person>();

            Assert.Multiple(() =>
            {
                Assert.That(p1.Name, Is.EqualTo("sarah"));
                Assert.That(p1.Age, Is.EqualTo(29));
                Assert.That(p1.ChangeLog!.CreatedBy, Is.EqualTo("username"));
                Assert.That(p1.ChangeLog.CreatedDate, Is.EqualTo(CreateDateTime()));
                Assert.That(p1.ChangeLog.UpdatedBy, Is.EqualTo("username2"));
                Assert.That(p1.ChangeLog.UpdatedDate, Is.EqualTo(CreateDateTime().AddDays(1)));
            });
        }

        [Test]
        public void Person_CopyFrom_Hierarchy()
        {
            var p1 = new Person { Name = "dave", Age = 30 };
            var p2 = new PersonEx { Name = "sarah", Age = 29, Salary = 100000 };

            p1.CopyFrom(p2);

            Assert.Multiple(() =>
            {
                Assert.That(p1.Name, Is.EqualTo("sarah"));
                Assert.That(p1.Age, Is.EqualTo(29));
            });

            p1.Name = "ivan";
            p1.Age = 55;

            p2.CopyFrom(p1);
            Assert.Multiple(() =>
            {
                Assert.That(p2.Name, Is.EqualTo("ivan"));
                Assert.That(p2.Age, Is.EqualTo(55));
                Assert.That(p2.Salary, Is.EqualTo(100000));
            });
        }

        [Test]
        public void Person_Equals()
        {
            Person? p2 = null;

            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            Assert.That(p1, Is.Not.EqualTo(p2));

            p2 = (Person)p1.Clone();
            Assert.That(p1, Is.EqualTo(p2));

            p2.ChangeLog!.CreatedBy = "username2";
            Assert.That(p1, Is.Not.EqualTo(p2));

            p2.ChangeLog.CreatedBy = "username";
            Assert.That(p1, Is.EqualTo(p2));

            p2.Name = "mike";
            Assert.That(p1, Is.Not.EqualTo(p2));

            Person p3 = p1;
            Assert.That(p3, Is.EqualTo(p1));
        }

        [Test]
        public void Person_Equals2()
        {
            Person? p2 = null;

            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            Assert.That(p1, Is.Not.EqualTo(p2));

            p2 = (Person)p1.Clone();
            Assert.That(p1, Is.EqualTo(p2));

            p2.ChangeLog!.CreatedBy = "username2";
            Assert.That(p1, Is.Not.EqualTo(p2));

            p2.ChangeLog.CreatedBy = "username";
            Assert.That(p1, Is.EqualTo(p2));

            p2.Name = "mike";
            Assert.That(p1, Is.Not.EqualTo(p2));

            Person p3 = p1;
            Assert.That(p3, Is.EqualTo(p1));
        }

        [Test]
        public void Person_HashCode()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = (Person)p1.Clone();
            Assert.That(p2.GetHashCode(), Is.EqualTo(p1.GetHashCode()));

            p1.Name = "mike";
            Assert.That(p2.GetHashCode(), Is.Not.EqualTo(p1.GetHashCode()));

            p1.Name = "dave";
            Assert.That(p2.GetHashCode(), Is.EqualTo(p1.GetHashCode()));

            p1.ChangeLog.CreatedBy = "username2";
            Assert.That(p2.GetHashCode(), Is.Not.EqualTo(p1.GetHashCode()));
        }

        [Test]
        public void Person_IsInitial()
        {
            var p = new Person();
            Assert.That(p.IsInitial, Is.True);

            p.Name = "mike";
            Assert.That(p.IsInitial, Is.False);

            p.Name = null;
            Assert.That(p.IsInitial, Is.True);

            p.ChangeLog = new ChangeLogEx { UpdatedDate = CreateDateTime() };
            Assert.That(p.IsInitial, Is.False);

            p.ChangeLog.UpdatedDate = null;
            Assert.That(p.IsInitial, Is.False);

            p.CleanUp();
            Assert.That(p.IsInitial, Is.True);
        }

        [Test]
        public void Person_AcceptChanges()
        {
            var p = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            Assert.Multiple(() =>
            {
                Assert.That(p.IsChanged, Is.True);
                Assert.That(p.ChangeLog.IsChanged, Is.True);
            });

            p.AcceptChanges();
            Assert.That(p.IsChanged, Is.False);

            p.Name = "julie";
            Assert.Multiple(() =>
            {
                Assert.That(p.IsChanged, Is.True);
                Assert.That(p.ChangeLog.IsChanged, Is.False);
            });

            p.AcceptChanges();
            Assert.Multiple(() =>
            {
                Assert.That(p.IsChanged, Is.False);
                Assert.That(p.ChangeLog.IsChanged, Is.False);
            });

            p.ChangeLog.CreatedBy = "username2";
            Assert.Multiple(() =>
            {
                Assert.That(p.ChangeLog.IsChanged, Is.True);
                Assert.That(p.IsChanged, Is.True);
            });
        }

        [Test]
        public void Person_Bubbling()
        {
            var p = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            p.AcceptChanges();
            var cl1 = p.ChangeLog;
            var cl2 = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() };
            Assert.Multiple(() =>
            {
                Assert.That(p.IsChanged, Is.False);
                Assert.That(p.ChangeLog.IsChanged, Is.False);
                Assert.That(cl1.IsChanged, Is.False);
                Assert.That(cl2.IsChanged, Is.True);
            });

            p.ChangeLog = cl2;
            Assert.Multiple(() =>
            {
                Assert.That(p.IsChanged, Is.True);
                Assert.That(p.ChangeLog.IsChanged, Is.True);
                Assert.That(cl1.IsChanged, Is.False);
                Assert.That(cl2.IsChanged, Is.True);
            });

            p.AcceptChanges();
            Assert.Multiple(() =>
            {
                Assert.That(p.IsChanged, Is.False);
                Assert.That(p.ChangeLog.IsChanged, Is.False);
                Assert.That(cl1.IsChanged, Is.False);
                Assert.That(cl2.IsChanged, Is.False);
            });

            cl1.UpdatedBy = "username";
            Assert.Multiple(() =>
            {
                Assert.That(p.IsChanged, Is.False);
                Assert.That(p.ChangeLog.IsChanged, Is.False);
                Assert.That(cl1.IsChanged, Is.True);
                Assert.That(cl2.IsChanged, Is.False);
            });

            cl2.UpdatedBy = "username";
            Assert.Multiple(() =>
            {
                Assert.That(p.IsChanged, Is.True);
                Assert.That(p.ChangeLog.IsChanged, Is.True);
                Assert.That(cl1.IsChanged, Is.True);
                Assert.That(cl2.IsChanged, Is.True);
            });
        }

        [Test]
        public void Person_MakeReadonly()
        {
            var cl = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() };
            Assert.That(cl.IsReadOnly, Is.False);

            cl.MakeReadOnly();
            Assert.That(cl.IsReadOnly, Is.True);

            Assert.Throws<InvalidOperationException>(() => cl.UpdatedBy = "username");
        }

        [Test]
        public void Person_Load()
        {
            for (int i = 0; i < 1000; i++)
            {
                var p1 = new Person { Name = "dave", Age = i, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
                var p2 = new Person { Name = "dave", Age = i, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };

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
            var p = new PersonEx { Name = "dave", Age = 30, Salary = 1m, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var po = (PersonEx)p.Clone();

            Assert.That(po, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(po.Name, Is.EqualTo("dave"));
                Assert.That(po.Age, Is.EqualTo(30));
                Assert.That(po.Salary, Is.EqualTo(1m));
                Assert.That(po.ChangeLog!.CreatedBy, Is.EqualTo("username"));
                Assert.That(po.ChangeLog.CreatedDate, Is.EqualTo(CreateDateTime()));
                Assert.That(po.ChangeLog.UpdatedBy, Is.Null);
                Assert.That(po.ChangeLog.UpdatedDate, Is.Null);
            });
        }

        [Test]
        public void PersonEx_CopyFrom()
        {
            var p1 = new PersonEx { Name = "dave", Age = 30, Salary = 1m, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new PersonEx { Name = "sarah", Age = 29, Salary = 2m, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime(), UpdatedBy = "username2", UpdatedDate = CreateDateTime().AddDays(1) } };

            p1.CopyFrom(p2);

            Assert.Multiple(() =>
            {
                Assert.That(p1.Name, Is.EqualTo("sarah"));
                Assert.That(p1.Age, Is.EqualTo(29));
                Assert.That(p1.Salary, Is.EqualTo(2m));
                Assert.That(p1.ChangeLog.CreatedBy, Is.EqualTo("username"));
                Assert.That(p1.ChangeLog.CreatedDate, Is.EqualTo(CreateDateTime()));
                Assert.That(p1.ChangeLog.UpdatedBy, Is.EqualTo("username2"));
                Assert.That(p1.ChangeLog.UpdatedDate, Is.EqualTo(CreateDateTime().AddDays(1)));
            });
        }

        [Test]
        public void PersonEx_Equals()
        {
            PersonEx? p2 = null;

            var p1 = new PersonEx { Name = "dave", Age = 30, Salary = 1m, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            Assert.That(p1, Is.Not.EqualTo(p2));

            p2 = (PersonEx)p1.Clone();
            Assert.That(p1, Is.EqualTo(p2));

            p2.ChangeLog!.CreatedBy = "username2";
            Assert.That(p1, Is.Not.EqualTo(p2));

            p2.ChangeLog.CreatedBy = "username";
            Assert.That(p1, Is.EqualTo(p2));

            p2.Name = "mike";
            Assert.That(p1, Is.Not.EqualTo(p2));

            p2.Name = "dave";
            Assert.That(p1, Is.EqualTo(p2));

            p2.Salary = 2m;
            Assert.That(p1, Is.Not.EqualTo(p2));

            Person p3 = p1;
            Assert.That(p3, Is.EqualTo(p1));
        }

        [Test]
        public void PersonEx_Equals2()
        {
            PersonEx? p2 = null;

            var p1 = new PersonEx { Name = "dave", Age = 30, Salary = 1m, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            Assert.That(p1, Is.Not.EqualTo(p2));

            p2 = (PersonEx)p1.Clone();
            Assert.That(p1, Is.EqualTo(p2));

            p2.ChangeLog!.CreatedBy = "username2";
            Assert.That(p1, Is.Not.EqualTo(p2));

            p2.ChangeLog.CreatedBy = "username";
            Assert.That(p1, Is.EqualTo(p2));

            p2.Name = "mike";
            Assert.That(p1, Is.Not.EqualTo(p2));

            p2.Name = "dave";
            Assert.That(p1, Is.EqualTo(p2));

            p2.Salary = 2m;
            Assert.That(p1, Is.Not.EqualTo(p2));

            Person p3 = p1;
            Assert.That(p3, Is.EqualTo(p1));
        }

        [Test]
        public void PersonEx_HashCode()
        {
            var p1 = new PersonEx { Name = "dave", Age = 30, Salary = 1m, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = (PersonEx)p1.Clone();
            Assert.That(p2.GetHashCode(), Is.EqualTo(p1.GetHashCode()));

            p1.Name = "mike";
            Assert.That(p2.GetHashCode(), Is.Not.EqualTo(p1.GetHashCode()));

            p1.Name = "dave";
            Assert.That(p2.GetHashCode(), Is.EqualTo(p1.GetHashCode()));

            p1.Salary = 2m;
            Assert.That(p2.GetHashCode(), Is.Not.EqualTo(p1.GetHashCode()));

            p1.Salary = 1m;
            Assert.That(p2.GetHashCode(), Is.EqualTo(p1.GetHashCode()));

            p1.ChangeLog.CreatedBy = "username2";
            Assert.That(p2.GetHashCode(), Is.Not.EqualTo(p1.GetHashCode()));
        }

        [Test]
        public void PersonEx_IsInitial()
        {
            var p = new PersonEx();
            Assert.That(p.IsInitial, Is.True);

            p.Salary = 0m;
            Assert.That(p.IsInitial, Is.False);

            p.Salary = null;
            Assert.That(p.IsInitial, Is.True);

            p.Name = "mike";
            Assert.That(p.IsInitial, Is.False);

            p.Name = null;
            Assert.That(p.IsInitial, Is.True);

            p.ChangeLog = new ChangeLogEx { UpdatedDate = CreateDateTime() };
            Assert.That(p.IsInitial, Is.False);

            p.ChangeLog.UpdatedDate = null;
            Assert.That(p.IsInitial, Is.False);

            p.CleanUp();
            Assert.That(p.IsInitial, Is.True);
        }

        [Test]
        public void Collection_Person_Clone()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1, p2 };

            var pc2 = (PersonCollection)pc.Clone();
            Assert.That(pc2, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(pc2, Has.Count.EqualTo(2));
                Assert.That(ReferenceEquals(pc2[0], p1), Is.False);
                Assert.That(ReferenceEquals(pc2[1], p2), Is.False);
                Assert.That(p1, Is.EqualTo(pc2[0]));
                Assert.That(p2, Is.EqualTo(pc2[1]));
            });
        }

        [Test]
        public void Collection_Person_Equals()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1, p2 };

            Assert.That(pc, Is.Not.EqualTo(null));

            var pc2 = (PersonCollection)pc.Clone();
            Assert.That(pc, Is.EqualTo(pc2));
            pc2.Add(new Person { Name = "john", Age = 35 });
            Assert.That(pc, Is.Not.EqualTo(pc2));

            pc2 = (PersonCollection)pc.Clone();
            Assert.That(pc, Is.EqualTo(pc2));
            pc2[1].Name = "jenny";
            Assert.That(pc, Is.Not.EqualTo(pc2));
        }

        [Test]
        public void Collection_Person_Equals2()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1, p2 };
            var pc2 = pc;

            Assert.That(pc, Is.Not.EqualTo(null));
            Assert.That(pc, Is.EqualTo(pc2));

            pc2 = (PersonCollection)pc!.Clone();
            Assert.That(pc, Is.EqualTo(pc2));
            pc2.Add(new Person { Name = "john", Age = 35 });
            Assert.That(pc, Is.Not.EqualTo(pc2));

            pc2 = (PersonCollection)pc.Clone();
            Assert.That(pc, Is.EqualTo(pc2));
            pc2[1].Name = "jenny";
            Assert.That(pc, Is.Not.EqualTo(pc2));
        }

        [Test]
        public void Collection_Person_HashCode()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1, p2 };
            var pc2 = (PersonCollection)pc.Clone();
            Assert.That(pc2.GetHashCode(), Is.EqualTo(pc.GetHashCode()));

            pc2[0].ChangeLog!.CreatedBy = "username2";
            Assert.That(pc2.GetHashCode(), Is.Not.EqualTo(pc.GetHashCode()));
        }

        [Test]
        public void Collection_Person_AcceptChanges()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1 };
            Assert.Multiple(() =>
            {
                Assert.That(pc.IsChanged, Is.True);
                Assert.That(pc[0].IsChanged, Is.True);
                Assert.That(pc[0].ChangeLog!.IsChanged, Is.True);
            });

            pc.AcceptChanges();
            Assert.Multiple(() =>
            {
                Assert.That(pc.IsChanged, Is.False);
                Assert.That(pc[0].IsChanged, Is.False);
                Assert.That(pc[0].ChangeLog!.IsChanged, Is.False);
            });

            pc.Add(p2);
            Assert.Multiple(() =>
            {
                Assert.That(pc.IsChanged, Is.True);
                Assert.That(pc[0].IsChanged, Is.False);
                Assert.That(pc[0].ChangeLog!.IsChanged, Is.False);
                Assert.That(pc[1].IsChanged, Is.True);
            });

            pc.AcceptChanges();
            Assert.Multiple(() =>
            {
                Assert.That(pc.IsChanged, Is.False);
                Assert.That(pc[0].IsChanged, Is.False);
                Assert.That(pc[0].ChangeLog!.IsChanged, Is.False);
                Assert.That(pc[1].IsChanged, Is.False);
            });

            pc[0].ChangeLog!.CreatedBy = "username2";
            Assert.Multiple(() =>
            {
                Assert.That(pc.IsChanged, Is.True);
                Assert.That(pc[0].IsChanged, Is.True);
                Assert.That(pc[0].ChangeLog!.IsChanged, Is.True);
                Assert.That(pc[1].IsChanged, Is.False);
            });
        }

        [Test]
        public void Collect_Person_Clear()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1, p2 };
            Assert.That(pc.IsChanged, Is.True);

            pc.AcceptChanges();
            Assert.That(pc.IsChanged, Is.False);

            pc.Clear();
            Assert.That(pc.IsChanged, Is.True);
        }

        [Test]
        public void Collection_Person_MakeReadOnly()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1 };
            pc.MakeReadOnly();
            Assert.Multiple(() =>
            {
                Assert.That(pc.IsReadOnly, Is.True);
                Assert.That(pc[0].IsReadOnly, Is.True);
                Assert.That(pc[0].ChangeLog!.IsReadOnly, Is.True);
            });
            Assert.Throws<InvalidOperationException>(() => pc.Add(p2));
            Assert.That(pc, Has.Count.EqualTo(1));

            Assert.Throws<InvalidOperationException>(() => pc.Clear());
            Assert.That(pc, Has.Count.EqualTo(1));

            Assert.Throws<InvalidOperationException>(() => pc.Remove(p1));
            Assert.That(pc, Has.Count.EqualTo(1));

            Assert.Throws<InvalidOperationException>(() => pc.RemoveAt(0));
            Assert.That(pc, Has.Count.EqualTo(1));

            Assert.Throws<InvalidOperationException>(() => pc[0] = p2);
            Assert.That(pc, Has.Count.EqualTo(1));
        }

        [Test]
        public void Collection_Person_GetByPrimaryKey()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1, p2 };

            var pi = pc.GetByKey();
            Assert.That(pi, Is.Null);

            pi = pc.GetByKey("dave");
            Assert.That(p1, Is.EqualTo(pi));

            pi = pc.GetByKey("bazza");
            Assert.That(pi, Is.Null);

            pi = pc.GetByKey(p1.PrimaryKey);
            Assert.That(p1, Is.EqualTo(pi));

            pi = pc.GetByKey(new CompositeKey("bazza"));
            Assert.That(pi, Is.Null);
        }

        [Test]
        public void Collection_Person_DeleteByPrimaryKey()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var p3 = new Person { Name = "dave", Age = 40 };
            var pc = new PersonCollection { p1, p2, p3 };

            pc.RemoveByKey("rebecca");
            Assert.That(pc, Has.Count.EqualTo(3));
            Assert.That(pc[0].Name, Is.EqualTo("dave"));

            pc.RemoveByKey("dave");
            Assert.That(pc, Has.Count.EqualTo(1));
            Assert.That(pc[0].Name, Is.EqualTo("mary"));

            pc.RemoveByKey("mary");
            Assert.That(pc, Is.Empty);
        }

        [Test]
        public void Collection_Person_ItemsAreAllUnique()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var p3 = new Person { Name = "dave", Age = 40 };
            var pc = new PersonCollection { p1, p2, p3 };

            Assert.That(pc.IsAnyDuplicates(), Is.True);

            pc.Remove(p3);
            Assert.That(pc.IsAnyDuplicates(), Is.False);

            pc = new PersonCollection { null!, null! };
            Assert.That(pc.IsAnyDuplicates(), Is.True);

            pc.RemoveAt(0);
            Assert.That(pc.IsAnyDuplicates(), Is.False);
        }

        [Test]
        public void CollectionResult_Person()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pc = new PersonCollection { p1, p2 };
            var pcr = new PersonCollectionResult(pc);
            Assert.That(pc, Is.SameAs(pcr.Items));

            var pc2 = (PersonCollection)pcr;
            Assert.That(pc2, Is.SameAs(pc));

            var pcr2 = (PersonCollectionResult)pcr.Clone();
            Assert.Multiple(() =>
            {
                Assert.That(ReferenceEquals(pcr2, pcr), Is.False);
                Assert.That(pcr2, Is.EqualTo(pcr));
            });
            Assert.That(pcr2, Is.EqualTo(pcr));
        }

        [Test]
        public void Dictionary_Person_IsChanged()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pd = new PersonDictionary { { "dave", p1 }, { "mary", p2 } };
            Assert.That(pd.IsChanged, Is.True);

            pd.AcceptChanges();
            Assert.Multiple(() =>
            {
                Assert.That(pd.IsChanged, Is.False);
                Assert.That(p1.IsChanged, Is.False);
                Assert.That(p2.IsChanged, Is.False);
            });

            p1.ChangeLog.CreatedDate = p1.ChangeLog.CreatedDate.Value.AddMinutes(1);
            Assert.Multiple(() =>
            {
                Assert.That(pd.IsChanged, Is.True);
                Assert.That(p1.IsChanged, Is.True);
                Assert.That(p2.IsChanged, Is.False);
            });

            pd.AcceptChanges();
            Assert.Multiple(() =>
            {
                Assert.That(pd.IsChanged, Is.False);
                Assert.That(p1.IsChanged, Is.False);
                Assert.That(p2.IsChanged, Is.False);
            });

            pd.Remove("mary");
            Assert.That(pd.IsChanged, Is.True);

            pd.AcceptChanges();
            Assert.That(pd.IsChanged, Is.False);

            pd.Remove("mary");
            Assert.That(pd.IsChanged, Is.False);
        }

        [Test]
        public void Dictionary_Person_ReadOnly()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pd = new PersonDictionary { { "dave", p1 }, { "mary", p2 } };
            pd.MakeReadOnly();
            Assert.That(pd.IsReadOnly, Is.True);

            Assert.Throws<InvalidOperationException>(() => pd.Clear());
            Assert.Throws<InvalidOperationException>(() => pd.Remove("mary"));
            Assert.Throws<InvalidOperationException>(() => pd.Add("donna", new Person { Name = "Donna" }));
            Assert.Throws<InvalidOperationException>(() => pd["donna"] = new Person { Name = "Donna" });
        }

        [Test]
        public void Dictionary_Person_Equals()
        {
            var p1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var p2 = new Person { Name = "mary", Age = 25 };
            var pd = new PersonDictionary { { "dave", p1 }, { "mary", p2 } };

            var px1 = new Person { Name = "dave", Age = 30, ChangeLog = new ChangeLogEx { CreatedBy = "username", CreatedDate = CreateDateTime() } };
            var px2 = new Person { Name = "mary", Age = 25 };
            var pxd = new PersonDictionary { { "dave", px1 }, { "mary", px2 } };

            Assert.Multiple(() =>
            {
                Assert.That(pd, Is.EqualTo(pxd));
                Assert.That(pxd.GetHashCode(), Is.EqualTo(pd.GetHashCode()));
            });

            px2.Name += "X";
            Assert.Multiple(() =>
            {
                Assert.That(pd, Is.Not.EqualTo(pxd));
                Assert.That(pxd.GetHashCode(), Is.Not.EqualTo(pd.GetHashCode()));
            });
        }

        [Test]
        public void Dictionary_Person_Add_Infer_Key()
        {
            var pd = new PersonDictionary();
            var p2 = new Person { Name = "mary", Age = 25 };
            pd.AddRange([p2]);

            Assert.That(pd, Has.Count.EqualTo(1));
            Assert.That(pd.ContainsKey("mary"), Is.True);
        }

        private static DateTime CreateDateTime() => new(2000, 01, 01, 12, 45, 59);

        public class Person : EntityBase, CoreEx.Entities.IPrimaryKey
        {
            private string? _name;
            private int _age;
            private ChangeLogEx? _changeLog;

            public string? Name { get => _name; set => SetValue(ref _name, value); }
            public int Age { get => _age; set => SetValue(ref _age, value); }
            public ChangeLogEx? ChangeLog { get => _changeLog; set => SetValue(ref _changeLog, value); }

            public CoreEx.Entities.CompositeKey PrimaryKey => new(Name);

            protected override IEnumerable<IPropertyValue> GetPropertyValues()
            {
                yield return CreateProperty(nameof(Name), Name, v => Name = v);
                yield return CreateProperty(nameof(Age), Age, v => Age = v);
                yield return CreateProperty(nameof(ChangeLog), ChangeLog, v => ChangeLog = v);
            }
        }

        public class PersonCollection : EntityKeyBaseCollection<Person, PersonCollection>
        {
            public PersonCollection() { }

            public PersonCollection(IEnumerable<Person> entities) : base(entities) { }

            public static implicit operator PersonCollection(PersonCollectionResult result) => result?.Items!;
        }

        public class PersonCollectionResult : EntityCollectionResult<PersonCollection, Person, PersonCollectionResult>
        {
            public PersonCollectionResult() { }

            public PersonCollectionResult(PagingArgs paging) : base(paging) { }

            public PersonCollectionResult(PersonCollection collection, PagingArgs? paging = null) : base(paging) => Items = collection;
        }

        public class PersonEx : Person
        {
            private decimal? _salary;

            public decimal? Salary { get => _salary; set => SetValue(ref _salary, value); }

            protected override IEnumerable<IPropertyValue> GetPropertyValues()
            {
                foreach (var pv in base.GetPropertyValues())
                    yield return pv;

                yield return CreateProperty(nameof(Salary), Salary, v => Salary = v);
            }
        }

        public class PersonExCollection : EntityBaseCollection<PersonEx, PersonExCollection>
        {
            public PersonExCollection() { }

            public PersonExCollection(IEnumerable<PersonEx> entities) : base(entities) { }
        }

        public class PersonDictionary : EntityBaseDictionary<Person, PersonDictionary> { }
    }
}