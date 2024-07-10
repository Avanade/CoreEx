using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.Validation.Clauses;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace CoreEx.Test.Framework.Mapping
{
    [TestFixture]
    public class MapperTest
    {
        [Test]
        public void Mapping()
        {
            var pm = new Mapper<PersonA, PersonB>()
                .Map((s, d) => d.ID = s.Id)
                .Map((s, d) => d.Text = s.Name);

            var m = new Mapper();
            m.Register(pm);

            var p2 = m.Map<PersonB>(new PersonA { Id = 1, Name = "Bob" });
            Assert.Multiple(() =>
            {
                Assert.That(p2.ID, Is.EqualTo(1));
                Assert.That(p2.Text, Is.EqualTo("Bob"));
            });
        }

        [Test]
        public void Mapping_Collection()
        {
            var pm = new Mapper<PersonA, PersonB>()
                .Map((s, d) => d.ID = s.Id)
                .Map((s, d) => d.Text = s.Name);

            var pmc = new CollectionMapper<PersonACollection, PersonA, PersonBCollection, PersonB>();

            var m = new Mapper();
            m.Register(pm);
            m.Register(pmc);

            var pac = new PersonACollection() { new PersonA { Id = 1, Name = "Bob" } };
            var pbc = m.Map<PersonBCollection>(pac);

            Assert.That(pbc, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(pbc[0].ID, Is.EqualTo(1));
                Assert.That(pbc[0].Text, Is.EqualTo("Bob"));
            });
        }

        [Test]
        public void Mapping_Collection_Auto()
        {
            var pm = new Mapper<PersonA, PersonB>()
                .Map((s, d) => d.ID = s.Id)
                .Map((s, d) => d.Text = s.Name);

            var m = new Mapper();
            m.Register(pm);

            var pac = new PersonACollection() { new PersonA { Id = 1, Name = "Bob" } };
            var pbc = m.Map<PersonBCollection>(pac);

            Assert.That(pbc, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(pbc[0].ID, Is.EqualTo(1));
                Assert.That(pbc[0].Text, Is.EqualTo("Bob"));
            });
        }

        [Test]
        public void Mapping_Collection_Auto_Into_Existing()
        {
            var pm = new Mapper<PersonA, PersonB>()
                .Map((s, d) => d.ID = s.Id)
                .Map((s, d) => d.Text = s.Name);

            var m = new Mapper();
            m.Register(pm);

            var pac = new PersonACollection() { new PersonA { Id = 1, Name = "Bob" } };
            var pbc = new PersonBCollection() { new PersonB { ID = 2, Text = "Carly" } };
            var pbc2 = m.Map(pac, pbc);

            Assert.That(pbc, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(pbc[0].ID, Is.EqualTo(1));
                Assert.That(pbc[0].Text, Is.EqualTo("Bob"));

                Assert.That(pbc2, Has.Count.EqualTo(1));
            });
            Assert.Multiple(() =>
            {
                Assert.That(pbc2[0].ID, Is.EqualTo(1));
                Assert.That(pbc2[0].Text, Is.EqualTo("Bob"));
            });
        }

        [Test]
        public void Mapping_Collection_Auto_Empty_To_Null()
        {
            var pm = new Mapper<PersonA, PersonB>()
                .Map((s, d) => d.ID = s.Id)
                .Map((s, d) => d.Text = s.Name);

            var m = new Mapper();
            m.Register(pm);

            var pac = new PersonACollection();
            var pbc = new PersonBCollection() { new PersonB { ID = 2, Text = "Carly" } };
            var pbc2 = m.Map(pac, pbc);

            Assert.That(pbc2, Is.Null);
        }

        [Test]
        public void ServicesMapper()
        {
            var sc = new ServiceCollection();
            sc.AddMappers<MapperTest>();
        }

        [Test]
        public void Mapping_Flatten()
        {
            var mc = new Mapper<Contact, Model>()
                .Map((s, d) => d.Id = s.Id)
                .Map((s, d) => d.Name = s.Name)
                .Flatten(s => s.Address);

            var ma = new Mapper<Address, Model>()
                .Map((s, d) => d.Street = s.Street)
                .Map((s, d) => d.City = s.City);

            var m = new Mapper();
            m.Register(mc);
            m.Register(ma);

            var c = new Contact { Id = 88, Name = "Brian", Address = new Address { Street = "Main", City = "Wellington" } };
            var r = m.Map<Model>(c);

            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.Id, Is.EqualTo(88));
                Assert.That(r.Name, Is.EqualTo("Brian"));
                Assert.That(r.Street, Is.EqualTo("Main"));
                Assert.That(r.City, Is.EqualTo("Wellington"));
            });

            // Try with Address of null - map into should null-ify!
            c.Address = null;
            m.Map(c, r);

            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.Id, Is.EqualTo(88));
                Assert.That(r.Name, Is.EqualTo("Brian"));
                Assert.That(r.Street, Is.Null);
                Assert.That(r.City, Is.Null);
            });
        }

        [Test]
        public void Mapping_Flatten_DoubleNest()
        {
            var mc = new Mapper<Contact, Model>()
                .Map((s, d) => d.Id = s.Id)
                .Map((s, d) => d.Name = s.Name)
                .Flatten(s => s.Address);

            var ma = new Mapper<Address, Model>()
                .Map((s, d) => d.Street = s.Street)
                .Map((s, d) => d.City = s.City)
                .Flatten(s => s.Other);

            var mo = new Mapper<Other, Model>()
                .Map((s, d) => d.Other = s.Value);

            var m = new Mapper();
            m.Register(mc);
            m.Register(ma);
            m.Register(mo);

            var c = new Contact { Id = 88, Name = "Brian", Address = new Address { Street = "Main", City = "Wellington" } };
            var r = m.Map<Model>(c);

            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.Id, Is.EqualTo(88));
                Assert.That(r.Name, Is.EqualTo("Brian"));
                Assert.That(r.Street, Is.EqualTo("Main"));
                Assert.That(r.City, Is.EqualTo("Wellington"));
                Assert.That(r.Other, Is.Null);
            });

            c = new Contact { Id = 88, Name = "Brian", Address = new Address { Street = "Main", City = "Wellington", Other = new Other { Value = "Blah" } } };
            r = m.Map<Model>(c);

            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.Id, Is.EqualTo(88));
                Assert.That(r.Name, Is.EqualTo("Brian"));
                Assert.That(r.Street, Is.EqualTo("Main"));
                Assert.That(r.City, Is.EqualTo("Wellington"));
                Assert.That(r.Other, Is.EqualTo("Blah"));
            });
        }

        [Test]
        public void Mapping_Flatten_DoubleNest_Perf()
        {
            var mc = new Mapper<Contact, Model>()
                .Map((s, d) => d.Id = s.Id)
                .Map((s, d) => d.Name = s.Name)
                .Flatten(s => s.Address);

            var ma = new Mapper<Address, Model>()
                .Map((s, d) => d.Street = s.Street)
                .Map((s, d) => d.City = s.City)
                .Flatten(s => s.Other);

            var mo = new Mapper<Other, Model>()
                .Map((s, d) => d.Other = s.Value);

            var m = new Mapper();
            m.Register(mc);
            m.Register(ma);
            m.Register(mo);

            var c = new Contact { Id = 88, Name = "Brian", Address = new Address { Street = "Main", City = "Wellington" } };
            for (int i = 0; i < 10000; i++)
            {
                _ = m.Map<Model>(c);
            }
        }

        [Test]
        public void Mapping_Flatten_Nullable_NonNullable_Inherit()
        {
            var m = new Mapper();
            m.Register(new EmployeeMapper());
            m.Register(new TerminationMapper());

            var e = new Employee { Name = "Tim" };
            var em = m.Map<EmployeeModel>(e);

            Assert.That(em, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(em.Name, Is.EqualTo("Tim"));
                Assert.That(em.Reason, Is.Null);
                Assert.That(em.Date, Is.Null);
            });

            em = new EmployeeModel { Name = "Tom", Reason = "Because", Date = DateTime.UtcNow };
            em = m.Map(e, em);

            Assert.That(em, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(em.Name, Is.EqualTo("Tim"));
                Assert.That(em.Reason, Is.Null);
                Assert.That(em.Date, Is.Null);
            });
        }

        [Test]
        public void Mapping_Flatten_Nullable_NonNullable_Fluent()
        {
            var me = new Mapper<Employee, EmployeeModel>()
                .Map((s, d) => d.Name = s.Name)
                .Flatten(s => s.Termination);

            var mt = new Mapper<Termination, EmployeeModel>()
                .Map((s, d) => d.Reason = s.Reason)
                .Map((s, d) => d.Date = s.Date)
                .InitializeDestination(d =>
                {
                    d.Reason = null;
                    d.Date = null;
                    return true;
                });

            var m = new Mapper();
            m.Register(me);
            m.Register(mt);

            var e = new Employee { Name = "Tim" };
            var em = m.Map<EmployeeModel>(e);

            Assert.That(em, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(em.Name, Is.EqualTo("Tim"));
                Assert.That(em.Reason, Is.Null);
                Assert.That(em.Date, Is.Null);
            });

            em = new EmployeeModel { Name = "Tom", Reason = "Because", Date = DateTime.UtcNow };
            em = m.Map(e, em);

            Assert.That(em, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(em.Name, Is.EqualTo("Tim"));
                Assert.That(em.Reason, Is.Null);
                Assert.That(em.Date, Is.Null);
            });
        }

        [Test]
        public void Mapping_Flatten_Nullable_NonNullable_InLine()
        {
            var me = new Mapper<Employee, EmployeeModel>()
                .Map((s, d) => d.Name = s.Name, isSourceInitial: s => s.Name == default, initializeDestination: d => d.Name = default)
                .Flatten(s => s.Termination, isSourceInitial: s => s.Termination == default);

            var mt = new Mapper<Termination, EmployeeModel>()
                .Map((s, d) => d.Reason = s.Reason, isSourceInitial: s => s.Reason == default, initializeDestination: d => d.Reason = default)
                .Map((s, d) => d.Date = s.Date, isSourceInitial: s => s.Date == default, initializeDestination: d => d.Date = default);

            var m = new Mapper();
            m.Register(me);
            m.Register(mt);

            var e = new Employee { Name = "Tim" };
            var em = m.Map<EmployeeModel>(e);

            Assert.That(em, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(em.Name, Is.EqualTo("Tim"));
                Assert.That(em.Reason, Is.Null);
                Assert.That(em.Date, Is.Null);
            });

            em = new EmployeeModel { Name = "Tom", Reason = "Because", Date = DateTime.UtcNow };
            em = m.Map(e, em);

            Assert.That(em, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(em.Name, Is.EqualTo("Tim"));
                Assert.That(em.Reason, Is.Null);
                Assert.That(em.Date, Is.Null);
            });
        }

        [Test]
        public void Mapping_Expand_Condition()
        {
            var mc = new Mapper<Model, Contact>()
                .Map((s, d) => d.Id = s.Id)
                .Map((s, d) => d.Name = s.Name)
                .Expand<Address>((d, v) => d.Address = v, (s, d) => d.Address != null || !(s.Street is null && s.City is null));

            var ma = new Mapper<Model, Address>()
                .Map((s, d) => d.Street = s.Street)
                .Map((s, d) => d.City = s.City);

            var m = new Mapper();
            m.Register(mc);
            m.Register(ma);

            var r = new Model { Id = 88, Name = "Brian", Street = "Main", City = "Wellington" };
            var c = m.Map<Contact>(r);

            Assert.That(c, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(c.Id, Is.EqualTo(88));
                Assert.That(c.Name, Is.EqualTo("Brian"));
                Assert.That(c.Address, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(c.Address!.Street, Is.EqualTo("Main"));
                Assert.That(c.Address!.City, Is.EqualTo("Wellington"));
            });

            // Try with all properties of Address are default - should not touch as condition not true.
            r.Street = null;
            r.City = null;
            c = m.Map<Contact>(r);

            Assert.That(c, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(c.Id, Is.EqualTo(88));
                Assert.That(c.Name, Is.EqualTo("Brian"));
                Assert.That(c.Address, Is.Null);
            });
        }

        [Test]
        public void Mapping_Expand_No_Condition()
        {
            var mc = new Mapper<Model, Contact>()
                .Map((s, d) => d.Id = s.Id)
                .Map((s, d) => d.Name = s.Name)
                .Expand<Address>((d, v) => d.Address = v);

            var ma = new Mapper<Model, Address>()
                .Map((s, d) => d.Street = s.Street)
                .Map((s, d) => d.City = s.City);

            var m = new Mapper();
            m.Register(mc);
            m.Register(ma);

            var r = new Model { Id = 88, Name = "Brian", Street = "Main", City = "Wellington" };
            var c = m.Map<Contact>(r);

            Assert.That(c, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(c.Id, Is.EqualTo(88));
                Assert.That(c.Name, Is.EqualTo("Brian"));
                Assert.That(c.Address, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(c.Address!.Street, Is.EqualTo("Main"));
                Assert.That(c.Address!.City, Is.EqualTo("Wellington"));
            });

            // Try with all properties of Address are default - should not touch as condition not true.
            r.Street = null;
            r.City = null;
            c = m.Map<Contact>(r);

            Assert.That(c, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(c.Id, Is.EqualTo(88));
                Assert.That(c.Name, Is.EqualTo("Brian"));
                Assert.That(c.Address, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(c.Address!.Street, Is.Null);
                Assert.That(c.Address!.City, Is.Null);
            });
        }

        [Test]
        public void Mapping_Expand_No_Condition_IsInitializedCheck()
        {
            var mc = new Mapper<Model, Contact>()
                .Map((s, d) => d.Id = s.Id)
                .Map((s, d) => d.Name = s.Name)
                .Expand<Address>((d, v) => d.Address = v, initializeDestination: d => d.Address = null);

            var ma = new Mapper<Model, Address>()
                .Map((s, d) => d.Street = s.Street, isSourceInitial: s => s.Street == default)
                .Map((s, d) => d.City = s.City, isSourceInitial: s => s.City == default);

            var m = new Mapper();
            m.Register(mc);
            m.Register(ma);

            var r = new Model { Id = 88, Name = "Brian", Street = "Main", City = "Wellington" };
            var c = m.Map<Contact>(r);

            Assert.That(c, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(c.Id, Is.EqualTo(88));
                Assert.That(c.Name, Is.EqualTo("Brian"));
                Assert.That(c.Address, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(c.Address!.Street, Is.EqualTo("Main"));
                Assert.That(c.Address!.City, Is.EqualTo("Wellington"));
            });

            // Try with all properties of Address are default - should nullify as all are initial.
            r.Street = null;
            r.City = null;
            c = m.Map<Contact>(r);

            Assert.That(c, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(c.Id, Is.EqualTo(88));
                Assert.That(c.Name, Is.EqualTo("Brian"));
                Assert.That(c.Address, Is.Null);
            });
        }

        [Test]
        public void Mapping_Expand_DoubleNest()
        {
            var mc = new Mapper<Model, Contact>()
                .Map((s, d) => d.Id = s.Id)
                .Map((s, d) => d.Name = s.Name)
                .Expand<Address>((d, v) => d.Address = v, (s, d) => d.Address != null || !(s.Street is null && s.City is null));

            var ma = new Mapper<Model, Address>()
                .Map((s, d) => d.Street = s.Street)
                .Map((s, d) => d.City = s.City)
                .Expand<Other>((d, v) => d.Other = v, (s, d) => !(s.Other == default));

            var mo = new Mapper<Model, Other>()
                .Map((s, d) => d.Value = s.Other);

            var m = new Mapper();
            m.Register(mc);
            m.Register(ma);
            m.Register(mo);

            // Other won't expand as no value specified.
            var r = new Model { Id = 88, Name = "Brian", Street = "Main", City = "Wellington" };
            var c = m.Map<Contact>(r);

            Assert.That(c, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(c.Id, Is.EqualTo(88));
                Assert.That(c.Name, Is.EqualTo("Brian"));
                Assert.That(c.Address, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(c.Address!.Street, Is.EqualTo("Main"));
                Assert.That(c.Address!.City, Is.EqualTo("Wellington"));
                Assert.That(c.Address!.Other, Is.Null);
            });

            // Other expands as it now has a value.
            r = new Model { Id = 88, Name = "Brian", Street = "Main", City = "Wellington", Other = "Blah" };
            c = m.Map<Contact>(r);

            Assert.That(c, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(c.Id, Is.EqualTo(88));
                Assert.That(c.Name, Is.EqualTo("Brian"));
                Assert.That(c.Address, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(c.Address!.Street, Is.EqualTo("Main"));
                Assert.That(c.Address!.City, Is.EqualTo("Wellington"));
                Assert.That(c.Address!.Other, Is.Not.Null);
            });
            Assert.That(c.Address!.Other!.Value, Is.EqualTo("Blah"));

            // Other will not expand as the Address is not specified; data must flow along expand branch.
            r = new Model { Id = 88, Name = "Brian", Street = null, City = null, Other = "Blah" };
            c = m.Map<Contact>(r);
            Assert.That(c, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(c.Id, Is.EqualTo(88));
                Assert.That(c.Name, Is.EqualTo("Brian"));
                Assert.That(c.Address, Is.Null);
            });
        }

        [Test]
        public void Mapping_Expand_DoubleNest_Perf()
        {
            var mc = new Mapper<Model, Contact>()
                .Map((s, d) => d.Id = s.Id)
                .Map((s, d) => d.Name = s.Name)
                .Expand<Address>((d, v) => d.Address = v, (s, d) => d.Address != null || !(s.Street is null && s.City is null));

            var ma = new Mapper<Model, Address>()
                .Map((s, d) => d.Street = s.Street)
                .Map((s, d) => d.City = s.City)
                .Expand<Other>((d, v) => d.Other = v, (s, d) => !(s.Other == default));

            var mo = new Mapper<Model, Other>()
                .Map((s, d) => d.Value = s.Other);

            var m = new Mapper();
            m.Register(mc);
            m.Register(ma);
            m.Register(mo);

            // Other won't expand as no value specified.
            var r = new Model { Id = 88, Name = "Brian", Street = "Main", City = "Wellington" };
            for (int i = 0; i < 10000; i++)
            {
                _ = m.Map<Contact>(r);
            }
        }

        [Test]
        public void Mapping_Expand_DoubleNext2()
        {
            var mc = new Mapper<Model, Contact>()
                .Map((s, d) => d.Id = s.Id)
                .Map((s, d) => d.Name = s.Name)
                .Expand<Address>((d, v) => d.Address = v, (s, d) => !(s.Street is null && s.City is null && s.Other is null));

            var ma = new Mapper<Model, Address>()
                .Map((s, d) => d.Street = s.Street)
                .Map((s, d) => d.City = s.City)
                .Expand<Other>((d, v) => d.Other = v, (s, d) => !(s.Other == default));

            var mo = new Mapper<Model, Other>()
                .Map((s, d) => d.Value = s.Other);

            var m = new Mapper();
            m.Register(mc);
            m.Register(ma);
            m.Register(mo);

            // Other won't expand as no value specified.
            var r = new Model { Id = 88, Name = "Brian", Street = "Main", City = "Wellington" };
            var c = m.Map<Contact>(r);

            Assert.That(c, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(c.Id, Is.EqualTo(88));
                Assert.That(c.Name, Is.EqualTo("Brian"));
                Assert.That(c.Address, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(c.Address!.Street, Is.EqualTo("Main"));
                Assert.That(c.Address!.City, Is.EqualTo("Wellington"));
                Assert.That(c.Address!.Other, Is.Null);
            });

            // Other expands as it now has a value.
            r = new Model { Id = 88, Name = "Brian", Street = "Main", City = "Wellington", Other = "Blah" };
            c = m.Map<Contact>(r);

            Assert.That(c, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(c.Id, Is.EqualTo(88));
                Assert.That(c.Name, Is.EqualTo("Brian"));
                Assert.That(c.Address, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(c.Address!.Street, Is.EqualTo("Main"));
                Assert.That(c.Address!.City, Is.EqualTo("Wellington"));
                Assert.That(c.Address!.Other, Is.Not.Null);
            });
            Assert.That(c.Address!.Other!.Value, Is.EqualTo("Blah"));

            // Other *will* expand as the Address has the Other in the condition.
            r = new Model { Id = 88, Name = "Brian", Street = null, City = null, Other = "Blah" };
            c = m.Map<Contact>(r);
            Assert.That(c, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(c.Id, Is.EqualTo(88));
                Assert.That(c.Name, Is.EqualTo("Brian"));
                Assert.That(c.Address, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(c.Address!.Street, Is.Null);
                Assert.That(c.Address!.City, Is.Null);
                Assert.That(c.Address!.Other, Is.Not.Null);
            });
            Assert.That(c.Address!.Other!.Value, Is.EqualTo("Blah"));
        }

        [Test]
        public void Test_Class_Property()
        {
            var mc = new Mapper<Contact, Contact>()
                .Map((s, d) => d.Id = s.Id)
                .Map((s, d) => d.Name = s.Name)
                .Map((o, s, d) => d.Address = o.Map(s.Address, d.Address));

            var ma = new Mapper<Address, Address>()
                .Map((s, d) => d.Street = s.Street)
                .Map((s, d) => d.City = s.City);

            var m = new Mapper();
            m.Register(mc);
            m.Register(ma);

            var r = new Contact { Id = 88, Name = "Brian", Address = new Address { Street = "Main", City = "Wellington" } };
            var c = m.Map<Contact>(r);

            Assert.That(c, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(c.Id, Is.EqualTo(88));
                Assert.That(c.Name, Is.EqualTo("Brian"));
                Assert.That(c.Address, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(c.Address!.Street, Is.EqualTo("Main"));
                Assert.That(c.Address!.City, Is.EqualTo("Wellington"));
                Assert.That(c.Address!.Other, Is.Null);
            });

            r = new Contact { Id = 88, Name = "Brian", Address = null };
            c = m.Map<Contact>(r);
            Assert.That(c, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(c.Id, Is.EqualTo(88));
                Assert.That(c.Name, Is.EqualTo("Brian"));
                Assert.That(c.Address, Is.Null);
            });
        }

        [Test]
        public void ChangeLog_OperationType()
        {
            var m = new Mapper();
            var cl = new ChangeLog();
            ChangeLog.PrepareCreated((IChangeLogAudit)cl);
            ChangeLog.PrepareUpdated((IChangeLogAudit)cl);

            var cl2 = m.Map<ChangeLog>(cl, OperationTypes.Create);
            Assert.That(cl2, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(cl2.CreatedBy, Is.Not.Null);
                Assert.That(cl2.CreatedDate, Is.Not.Null);
                Assert.That(cl2.UpdatedBy, Is.Null);
                Assert.That(cl2.UpdatedDate, Is.Null);
            });

            cl2 = m.Map<ChangeLog>(cl, OperationTypes.Update);
            Assert.That(cl2, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(cl2.CreatedBy, Is.Null);
                Assert.That(cl2.CreatedDate, Is.Null);
                Assert.That(cl2.UpdatedBy, Is.Not.Null);
                Assert.That(cl2.UpdatedDate, Is.Not.Null);
            });
        }

        [Test]
        public void Contact_ChangeLog_OperationType()
        {
            var mc = new Mapper<Contact, Contact>()
                .Map((s, d) => d.Id = s.Id)
                .Map((s, d) => d.Name = s.Name)
                .Map((o, s, d) => d.ChangeLog = o.Map(s.ChangeLog, d.ChangeLog));

            var m = new Mapper();
            m.Register(mc);

            var cl = new ChangeLog();
            ChangeLog.PrepareCreated((IChangeLogAudit)cl);
            ChangeLog.PrepareUpdated((IChangeLogAudit)cl);

            var c = new Contact { Id = 88, Name = "Dave", ChangeLog = cl };
            var c2 = m.Map<Contact>(c, OperationTypes.Create);

            Assert.That(c2, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(c2.Id, Is.EqualTo(88));
                Assert.That(c2.Name, Is.EqualTo("Dave"));
                Assert.That(c2.ChangeLog, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(c2.ChangeLog!.CreatedBy, Is.Not.Null);
                Assert.That(c2.ChangeLog.CreatedDate, Is.Not.Null);
                Assert.That(c2.ChangeLog.UpdatedBy, Is.Null);
                Assert.That(c2.ChangeLog.UpdatedDate, Is.Null);
            });
        }

        [Test]
        public void Register_WithBase()
        {
            var mc = new Mapper<Contact, Contact>()
                .Map((s, d) => d.Id = s.Id)
                .Map((s, d) => d.Name = s.Name)
                .Map((o, s, d) => d.ChangeLog = o.Map(s.ChangeLog, d.ChangeLog));

            var mce = new Mapper<ContactDetail, ContactDetail>()
                .Map((s, d) => d.ExtraDetail = s.ExtraDetail)
                .Base(mc);

            var m = new Mapper();
            m.Register(mc);
            m.Register(mce);

            var cd = new ContactDetail { Id = 88, Name = "Dave", ExtraDetail = "read all about it" };
            var cd2 = m.Map<ContactDetail>(cd);

            Assert.That(cd2, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(cd2.Id, Is.EqualTo(88));
                Assert.That(cd2.Name, Is.EqualTo("Dave"));
                Assert.That(cd2.ExtraDetail, Is.EqualTo("read all about it"));
            });
        }

        [Test]
        public void Register_WithBase2()
        {
            var mc = new Mapper<Contact, Contact>()
                .Map((s, d) => d.Id = s.Id)
                .Map((s, d) => d.Name = s.Name)
                .Map((o, s, d) => d.ChangeLog = o.Map(s.ChangeLog, d.ChangeLog));

            var mce = new Mapper<ContactDetail, ContactDetail>()
                .Map((s, d) => d.ExtraDetail = s.ExtraDetail)
                .Base<Contact, Contact>();

            var m = new Mapper();
            m.Register(mc);
            m.Register(mce);

            var cd = new ContactDetail { Id = 88, Name = "Dave", ExtraDetail = "read all about it" };
            var cd2 = m.Map<ContactDetail>(cd);

            Assert.That(cd2, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(cd2.Id, Is.EqualTo(88));
                Assert.That(cd2.Name, Is.EqualTo("Dave"));
                Assert.That(cd2.ExtraDetail, Is.EqualTo("read all about it"));
            });
        }

        [Test]
        public void Register_WithBase3()
        {
            var mce = new Mapper<ContactDetail, ContactDetail>()
                .Map((s, d) => d.ExtraDetail = s.ExtraDetail)
                .Base<ContactMapper>();

            var m = new Mapper();
            m.Register(new ContactMapper());
            m.Register(mce);

            var cd = new ContactDetail { Id = 88, Name = "Dave", ExtraDetail = "read all about it" };
            var cd2 = m.Map<ContactDetail>(cd);

            Assert.That(cd2, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(cd2.Id, Is.EqualTo(88));
                Assert.That(cd2.Name, Is.EqualTo("Dave"));
                Assert.That(cd2.ExtraDetail, Is.EqualTo("read all about it"));
            });
        }

        [Test]
        public void CustomMapper()
        {
            var m = new Mapper<PersonA, PersonB>((s, d, t) =>
            {
                d ??= new PersonB();
                Mapper.WhenCreate(t, () => d!.ID = s?.Id ?? 0);
                return d;
            });

            var d = m.Map(new PersonA { Id = 88, Name = "blah" }, null, OperationTypes.Create);
            Assert.That(d!.ID, Is.EqualTo(88));

            d = m.Map(new PersonA { Id = 88, Name = "blah" }, null, OperationTypes.Update);
            Assert.That(d!.ID, Is.EqualTo(0));
        }

        [Test]
        public void BidirectionalMapper()
        {
            var bm = new BidirectionalMapper<PersonA, PersonB>((s, d, _) =>
            {
                d ??= new PersonB();
                d.ID = s?.Id ?? 0;
                return d;
            }, (s, d, _) =>
            {
                d ??= new PersonA();
                d.Id = s?.ID ?? 0;
                return d;
            });

            var pa = new PersonA { Id = 99, Name = "blah" };
            var pb = bm.Map(pa);
            var pa2 = bm.Map(pb);
            Assert.That(pa2.Id, Is.EqualTo(99));

            var m = new Mapper();
            m.Register(bm);

            pa = new PersonA { Id = 88, Name = "blah" };
            pb = m.Map<PersonB>(pa);
            pa2 = m.Map<PersonA>(pb);
            Assert.That(pa2.Id, Is.EqualTo(88));
        }

        [Test]
        public void MapWithSameType_Allowed()
        {
            var m = new Mapper();
            var r = m.TryGetMapper<PersonA, PersonA>(out var sm);
            Assert.Multiple(() =>
            {
                Assert.That(r, Is.True);
                Assert.That(sm, Is.Not.Null);
            });

            var p = new PersonA { Id = 88, Name = "blah" };
            var d = sm!.Map(p);
            Assert.That(d, Is.Not.Null);
            Assert.That(d, Is.SameAs(p));
        }

        [Test]
        public void MapWithSameType_NotAllowed()
        {
            var m = new Mapper
            {
                MapSameTypeWithSourceValue = false
            };

            var r = m.TryGetMapper<PersonA, PersonA>(out var sm);

            Assert.Multiple(() =>
            {
                Assert.That(r, Is.False);
                Assert.That(sm, Is.Null);
            });
        }

        public class PersonAMapper : Mapper<PersonA, PersonB>
        {
            public PersonAMapper()
            {
                Map((s, d) => d.ID = s.Id);
                Map((s, d) => d.Text = s.Name);
            }
        }

        public class PersonA
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }

        public class PersonB
        {
            public int ID { get; set; }
            public string? Text { get; set; }
        }

        public class PersonACollection : List<PersonA> { }

        public class PersonBCollection : List<PersonB> { }

        public class PersonACollectionMapper : CollectionMapper<PersonACollection, PersonA, PersonBCollection, PersonB> { }

        public class Contact
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public Address? Address { get; set; }
            public ChangeLog? ChangeLog { get; set; }
        }

        public class ContactDetail : Contact
        {
            public string? ExtraDetail { get; set; }
        }

        public class ContactMapper : Mapper<Contact, Contact>
        {
            public ContactMapper()
            {
                Map((s, d) => d.Id = s.Id);
                Map((s, d) => d.Name = s.Name);
                Map((o, s, d) => d.ChangeLog = o.Map(s.ChangeLog, d.ChangeLog));
            }
        }

        public class Address
        {
            public string? Street { get; set; }
            public string? City { get; set; }
            public Other? Other { get; set; }
        }

        public class Other
        {
            public string? Value { get; set; }
        }

        public class Model
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public string? Street { get; set; }
            public string? City { get; set; }
            public string? Other { get; set; }
        }

        public class Employee
        {
            public string? Name { get; set; }
            public Termination? Termination { get; set; }
        }

        public class Termination
        {
            public string? Reason { get; set; }
            public DateTime Date { get; set; }
        }

        public class EmployeeModel
        {
            public string? Name { get; set; }
            public string? Reason { get; set; }
            public DateTime? Date { get; set; }
        }

        public class EmployeeMapper : Mapper<Employee, EmployeeModel>
        { 
            public EmployeeMapper()
            {
                Map((s, d) => d.Name = s.Name);
                Flatten(s => s.Termination);
            }
        }

        public class TerminationMapper : Mapper<Termination, EmployeeModel>
        { 
            public TerminationMapper()
            {
                Map((s, d) => d.Reason = s.Reason);
                Map((s, d) => d.Date = s.Date);
            }

            public override bool InitializeDestination(EmployeeModel d)
            {
                d.Reason = null;
                d.Date = null;
                return true;
            }
        }
    }
}