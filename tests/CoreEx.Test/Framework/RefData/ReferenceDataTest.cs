using CoreEx.Entities;
using CoreEx.Entities.Extended;
using CoreEx.Mapping.Converters;
using CoreEx.RefData;
using CoreEx.RefData.Caching;
using CoreEx.RefData.Extended;
using CoreEx.Results;
using CoreEx.TestFunction;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnitTestEx.NUnit;
using Nsj = Newtonsoft.Json;
using Stj = System.Text.Json;
using CoreEx.AspNetCore.Http;

namespace CoreEx.Test.Framework.RefData
{
    [TestFixture]
    public class ReferenceDataTest
    {
        [Test]
        public void RefDataSimple()
        {
            var r = new RefDataSimple { Id = 1, Code = "X", Text = "XX" };
            r.Id = 1;
            r.Code = "X";
            r.Text = "XX";

            Assert.Multiple(() =>
            {
                Assert.That(r.Id, Is.EqualTo(1));
                Assert.That(((ReferenceDataBase<int>)r).Id, Is.EqualTo(1));
                Assert.That(((ReferenceDataBase)r).Id, Is.EqualTo(1));
                Assert.That(((IReferenceData)r).Id, Is.EqualTo(1));
                Assert.That(((IIdentifier)r).Id, Is.EqualTo(1));
                Assert.That(r.Code, Is.EqualTo("X"));
                Assert.That(r.Text, Is.EqualTo("XX"));

                Assert.That(((IIdentifier)r).IdType, Is.EqualTo(typeof(int)));
            });

            var ir = (IReferenceData)r;
            Assert.That(ir.IsValid, Is.True);

            ir.SetInvalid();
            Assert.Multiple(() =>
            {
                Assert.That(ir.IsValid, Is.False);
                Assert.That(typeof(int), Is.SameAs(ir.IdType));
            });

            Assert.That(new CoreEx.Text.Json.ReferenceDataContentJsonSerializer().Serialize(r), Is.EqualTo("{\"id\":1,\"code\":\"X\",\"text\":\"XX\",\"isActive\":true}"));
        }

        [Test]
        public void Exercise_RefData()
        {
            var r = new RefData { Id = 1, Code = "X", Text = "XX" };
            r.Id = 1;
            r.Code = "X";
            r.Text = "XX";

            Assert.Multiple(() =>
            {
                Assert.That(r.Id, Is.EqualTo(1));
                Assert.That(((IReferenceData)r).Id, Is.EqualTo(1));
                Assert.That(((ReferenceDataBaseEx<int, RefData>)r).Id, Is.EqualTo(1));
                Assert.That(((IIdentifier)r).Id, Is.EqualTo(1));

                Assert.That(((IIdentifier)r).IdType, Is.EqualTo(typeof(int)));
            });

            // Immutable.
            Assert.Throws<InvalidOperationException>(() => r.Id = 2);
            Assert.Throws<InvalidOperationException>(() => r.Code = "Y");
            r.Text = "YY";

            var r2 = r;
            Assert.That(r, Is.EqualTo(r2));

            r2 = (RefData)r.Clone();
            Assert.That(r, Is.EqualTo(r2));
            Assert.That(r, Is.EqualTo(r2));
            Assert.Multiple(() =>
            {
                Assert.That(r, Is.EqualTo((object)r2));
                Assert.That(r2.GetHashCode(), Is.EqualTo(r.GetHashCode()));
            });

            r2 = new RefData { Id = 1, Code = "X", Text = "XXXX" };
            Assert.That(r, Is.Not.EqualTo(r2));
            Assert.That(r, Is.Not.EqualTo(r2));
            Assert.Multiple(() =>
            {
                Assert.That(r, Is.Not.EqualTo((object)r2));
                Assert.That(r2.GetHashCode(), Is.Not.EqualTo(r.GetHashCode()));
            });

            r.MakeReadOnly();
            Assert.That(r.IsReadOnly, Is.True);
            Assert.Throws<InvalidOperationException>(() => r.Text = "XXXXX");
        }

        [Test]
        public void Exercise_RefDataEx()
        {
            var r = new RefDataEx { Id = "@", Code = "X", Text = "XX", Description = "XXX", IsActive = true, StartDate = new DateTime(2020, 01, 01), EndDate = new DateTime(2020, 12, 31) };
            r.Id = "@";
            r.Code = "X";
            r.Text = "XX";
            r.Description = "XXX";
            r.IsActive = true;
            r.StartDate = new DateTime(2020, 01, 01);
            r.EndDate = new DateTime(2020, 12, 31);

            // Immutable.
            Assert.Throws<InvalidOperationException>(() => r.Id = "Q");
            Assert.Throws<InvalidOperationException>(() => r.Code = "Y");
            r.Text = "YY";
            r.Description = "YYY";
            r.IsActive = false;
            r.StartDate = r.StartDate.Value.AddDays(1);
            r.EndDate = r.EndDate.Value.AddDays(1);

            var r2 = r;
            Assert.That(r, Is.EqualTo(r2));

            r2 = (RefDataEx)r.Clone();
            Assert.That(r, Is.EqualTo(r2));
            Assert.That(r, Is.EqualTo(r2));
            Assert.Multiple(() =>
            {
                Assert.That(r, Is.EqualTo((object)r2));
                Assert.That(r2.GetHashCode(), Is.EqualTo(r.GetHashCode()));
            });

            r2 = new RefDataEx { Id = "@", Code = "X", Text = "XX", Description = "XXX", IsActive = true, StartDate = new DateTime(2020, 01, 01), EndDate = new DateTime(2021, 12, 31) };
            Assert.That(r, Is.Not.EqualTo(r2));
            Assert.That(r, Is.Not.EqualTo(r2));
            Assert.Multiple(() =>
            {
                Assert.That(r, Is.Not.EqualTo((object)r2));
                Assert.That(r2.GetHashCode(), Is.Not.EqualTo(r.GetHashCode()));
            });

            r.MakeReadOnly();
            Assert.That(r.IsReadOnly, Is.True);
            Assert.Throws<InvalidOperationException>(() => r.Text = "Bananas");
            Assert.Throws<InvalidOperationException>(() => r.IsActive = true);
        }

        [Test]
        public void Exercise_RefDataEx_Mappings()
        {
            var r = new RefDataEx();
            Assert.Multiple(() =>
            {
                Assert.That(r.IsInitial, Is.True);
                Assert.That(r.HasMappings, Is.False);
            });

            r.SetMapping("X", 1);
            Assert.Multiple(() =>
            {
                Assert.That(r.IsInitial, Is.False);
                Assert.That(r.HasMappings, Is.True);

                Assert.That(r.TryGetMapping("X", out int val), Is.True);
                Assert.That(val, Is.EqualTo(1));
                Assert.That(r.GetMapping<int>("X"), Is.EqualTo(1));
            });

            var r2 = (RefDataEx)r.Clone();
            Assert.Multiple(() =>
            {
                Assert.That(r2.HasMappings, Is.True);
                Assert.That(r2.TryGetMapping("X", out int val), Is.True);
                Assert.That(val, Is.EqualTo(1));
                Assert.That(r2.GetMapping<int>("X"), Is.EqualTo(1));
            });

            r2 = new RefDataEx();
            r2.SetMapping("X", 1);
            Assert.That(r, Is.EqualTo(r2));

            r2.SetMapping("Y", 2);
            Assert.That(r, Is.Not.EqualTo(r2));

            r.MakeReadOnly();
            Assert.That(r.IsReadOnly, Is.True);
            Assert.Throws<InvalidOperationException>(() => r.SetMapping("Y", 2));
        }

        [Test]
        public void IsValid_IsActive()
        {
            var rd = new RefDataEx();
            Assert.Multiple(() =>
            {
                Assert.That(rd.IsActive, Is.True);
                Assert.That(rd.IsValid, Is.True);
            });

            rd.IsActive = false;
            Assert.Multiple(() =>
            {
                Assert.That(rd.IsActive, Is.False);
                Assert.That(rd.IsValid, Is.True);
            });

            rd.IsActive = true;
            rd.EndDate = new DateTime(2000, 01, 01);
            Assert.Multiple(() =>
            {
                Assert.That(rd.IsActive, Is.False);
                Assert.That(rd.IsValid, Is.True);
            });

            rd.EndDate = null;
            Assert.Multiple(() =>
            {
                Assert.That(rd.IsActive, Is.True);
                Assert.That(rd.IsValid, Is.True);
            });

            rd.StartDate = DateTime.UtcNow.AddDays(20);
            Assert.Multiple(() =>
            {
                Assert.That(rd.IsActive, Is.False);
                Assert.That(rd.IsValid, Is.True);
            });

            rd.StartDate = DateTime.UtcNow.AddDays(-20);
            rd.EndDate = DateTime.UtcNow.AddDays(20);
            Assert.Multiple(() =>
            {
                Assert.That(rd.IsActive, Is.True);
                Assert.That(rd.IsValid, Is.True);
            });

            // Set invalid explicitly; makes it inactive; can not reset.
            ((IReferenceData)rd).SetInvalid();
            Assert.Multiple(() =>
            {
                Assert.That(rd.IsActive, Is.False);
                Assert.That(rd.IsValid, Is.False);
            });

            rd.IsActive = true;
            Assert.Multiple(() =>
            {
                Assert.That(rd.IsActive, Is.False);
                Assert.That(rd.IsValid, Is.False);
            });
        }

        [Test]
        public void IsValid_IsActive_SystemTime()
        {
            // Set the system time to the past.
            IServiceCollection sc = new ServiceCollection();
            sc.AddExecutionContext();
            sc.AddScoped<ISystemTime>(_ => SystemTime.CreateFixed(new DateTime(1999, 06, 01)));
            var sb = sc.BuildServiceProvider();
            using var scope = sb.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            var rd = new RefDataEx { IsActive = true };
            Assert.That(rd.IsValid, Is.True);

            rd.StartDate = new DateTime(1999, 01, 01);
            rd.EndDate = new DateTime(1999, 12, 31);
            Assert.That(rd.IsValid, Is.True);
        }

        [Test]
        public void IsValid_IsActive_ReferenceDataContext()
        {
            // Set the reference data context back in time.
            IServiceCollection sc = new ServiceCollection();
            sc.AddExecutionContext();
            sc.AddScoped<IReferenceDataContext>(_ => new ReferenceDataContext { Date = new DateTime(1999, 06, 01) });
            var sb = sc.BuildServiceProvider();
            using var scope = sb.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            var rd = new RefDataEx { IsActive = true};
            Assert.That(rd.IsValid, Is.True);

            rd.StartDate = new DateTime(1999, 01, 01);
            rd.EndDate = new DateTime(1999, 12, 31);
            Assert.That(rd.IsValid, Is.True);
        }

        [Test]
        public void Casting_FromRefData()
        {
            Assert.Multiple(() =>
            {
                Assert.That((int)(RefData)null!, Is.EqualTo(0));
                Assert.That((string?)(RefData)null!, Is.EqualTo(null));
                Assert.That((int)new RefData(), Is.EqualTo(0));
                Assert.That((string?)new RefData(), Is.EqualTo(null));
                Assert.That((int)new RefData { Id = 1, Code = "X", Text = "XX" }, Is.EqualTo(1));
                Assert.That((string?)new RefData { Id = 1, Code = "X", Text = "XX" }, Is.EqualTo("X"));
                Assert.That((int)new RefDataEx { Id = "X", Code = "XX", Text = "XXX" }, Is.EqualTo(0));
                Assert.That((string?)new RefDataEx { Id = "X", Code = "XX", Text = "XXX" }, Is.EqualTo("XX"));

                Assert.That((int?)(RefDataEx)null!, Is.EqualTo(null));
                Assert.That((string?)(RefDataEx)null!, Is.EqualTo(null));
                Assert.That((int?)new RefDataEx(), Is.EqualTo(null));
                Assert.That((string?)new RefDataEx(), Is.EqualTo(null));
                Assert.That((int?)new RefData { Id = 1, Code = "X", Text = "XX" }, Is.EqualTo(1));
                Assert.That((string?)new RefData { Id = 1, Code = "X", Text = "XX" }, Is.EqualTo("X"));
                Assert.That((int?)new RefDataEx { Id = "X", Code = "XX", Text = "XXX" }, Is.EqualTo(null));
                Assert.That((string?)new RefDataEx { Id = "X", Code = "XX", Text = "XXX" }, Is.EqualTo("XX"));
            });
        }

        [Test]
        public void Collection_Add()
        {
            var rc = new RefDataExCollection();
            Assert.Throws<ArgumentNullException>(() => rc.Add(null!));
            Assert.Throws<ArgumentException>(() => rc.Add(new RefDataEx()))!.Message.Should().StartWith("Id must not be null.");
            Assert.Throws<ArgumentException>(() => rc.Add(new RefDataEx{ Id = "X" }))!.Message.Should().StartWith("Code must not be null.");

            var r = new RefDataEx { Id = "X", Code = "XX" };
            rc.Add(r);
            Assert.That(rc, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(rc["XX"], Is.SameAs(r));
                Assert.That(rc["xx"], Is.SameAs(r));
                Assert.That(rc.GetByCode("XX"), Is.SameAs(r));
                Assert.That(rc.GetByCode("xx"), Is.SameAs(r));
                Assert.That(rc.GetById("X"), Is.SameAs(r));
                Assert.That(r.IsReadOnly, Is.True);
            });

            Assert.Throws<ArgumentException>(() => rc.Add(r))!.Message.Should().StartWith("Item already exists within the collection.");
            Assert.Throws<ArgumentException>(() => rc.Add(new RefDataEx { Id = "X", Code = "YY" }))!.Message.Should().StartWith("Item with Id 'X' already exists within the collection.");
            Assert.Throws<ArgumentException>(() => rc.Add(new RefDataEx { Id = "Y", Code = "XX" }))!.Message.Should().StartWith("Item with Code 'XX' already exists within the collection.");
            Assert.Throws<ArgumentException>(() => rc.Add(new RefDataEx { Id = "Y", Code = "xx" }))!.Message.Should().StartWith("Item with Code 'xx' already exists within the collection.");

            r = new RefDataEx { Id = "Y", Code = "YY" };
            rc.Add(r);
            Assert.That(rc, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(rc["YY"], Is.SameAs(r));
                Assert.That(rc["yy"], Is.SameAs(r));
                Assert.That(rc.GetByCode("YY"), Is.SameAs(r));
                Assert.That(rc.GetByCode("yy"), Is.SameAs(r));
                Assert.That(rc.GetById("Y"), Is.SameAs(r));
                Assert.That(r.IsReadOnly, Is.True);
            });
        }

        [Test]
        public void Collection_Lists()
        {
            var rc = new RefDataCollection { new RefData { Id = 1, Code = "Z", Text = "A", SortOrder = 2 }, new RefData { Id = 2, Code = "A", Text = "B", IsActive = false, SortOrder = 4 }, new RefData { Id = 3, Code = "Y", Text = "D", SortOrder = 1 }, new RefData { Id = 4, Code = "B", Text = "C", SortOrder = 3 } };
            Assert.Multiple(() =>
            {
                Assert.That(rc.GetItems(ReferenceDataSortOrder.Id, null, null).Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 1, 2, 3, 4 }));
                Assert.That(rc.GetItems(ReferenceDataSortOrder.Code, null, null).Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 2, 4, 3, 1 }));
                Assert.That(rc.GetItems(ReferenceDataSortOrder.Text, null, null).Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 1, 2, 4, 3 }));
                Assert.That(rc.GetItems(ReferenceDataSortOrder.SortOrder, null, null).Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 3, 1, 4, 2 }));
            });

            rc.SortOrder = ReferenceDataSortOrder.Id;
            Assert.That(rc.AllList.Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 1, 2, 3, 4 }));
            rc.SortOrder = ReferenceDataSortOrder.Code;
            Assert.That(rc.AllList.Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 2, 4, 3, 1 }));
            rc.SortOrder = ReferenceDataSortOrder.Text;
            Assert.That(rc.AllList.Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 1, 2, 4, 3 }));
            rc.SortOrder = ReferenceDataSortOrder.SortOrder;
            Assert.That(rc.AllList.Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 3, 1, 4, 2 }));

            rc.SortOrder = ReferenceDataSortOrder.Id;
            Assert.That(rc.ActiveList.Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 1, 3, 4 }));
            rc.SortOrder = ReferenceDataSortOrder.Code;
            Assert.That(rc.ActiveList.Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 4, 3, 1 }));
            rc.SortOrder = ReferenceDataSortOrder.Text;
            Assert.That(rc.ActiveList.Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 1, 4, 3 }));
            rc.SortOrder = ReferenceDataSortOrder.SortOrder;
            Assert.That(rc.ActiveList.Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 3, 1, 4 }));

            ((IReferenceData)rc.GetById(1)!).SetInvalid();

            rc.SortOrder = ReferenceDataSortOrder.Id;
            Assert.That(rc.AllList.Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 2, 3, 4 }));
            rc.SortOrder = ReferenceDataSortOrder.Code;
            Assert.That(rc.AllList.Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 2, 4, 3 }));
            rc.SortOrder = ReferenceDataSortOrder.Text;
            Assert.That(rc.AllList.Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 2, 4, 3 }));
            rc.SortOrder = ReferenceDataSortOrder.SortOrder;
            Assert.That(rc.AllList.Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 3, 4, 2 }));

            rc.SortOrder = ReferenceDataSortOrder.Id;
            Assert.That(rc.ActiveList.Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 3, 4 }));
            rc.SortOrder = ReferenceDataSortOrder.Code;
            Assert.That(rc.ActiveList.Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 4, 3 }));
            rc.SortOrder = ReferenceDataSortOrder.Text;
            Assert.That(rc.ActiveList.Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 4, 3 }));
            rc.SortOrder = ReferenceDataSortOrder.SortOrder;
            Assert.Multiple(() =>
            {
                Assert.That(rc.ActiveList.Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 3, 4 }));

                Assert.That(rc.GetItems(ReferenceDataSortOrder.Id, null, null).Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 1, 2, 3, 4 }));
                Assert.That(rc.GetItems(ReferenceDataSortOrder.Code, null, null).Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 2, 4, 3, 1 }));
                Assert.That(rc.GetItems(ReferenceDataSortOrder.Text, null, null).Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 1, 2, 4, 3 }));
                Assert.That(rc.GetItems(ReferenceDataSortOrder.SortOrder, null, null).Select(x => x.Id).ToArray(), Is.EqualTo(new int[] { 3, 1, 4, 2 }));
            });
        }

        [Test]
        public void Collection_Mappings()
        {
            var rc = new RefDataCollection();
            var r = new RefData { Id = 1, Code = "A" };
            r.SetMapping("D365", "A-1");
            r.SetMapping("SAP", 4300);

            rc.Add(r);
            Assert.Multiple(() =>
            {
                Assert.That(rc.ContainsMapping("D365", "A-1"), Is.True);
                Assert.That(rc.ContainsMapping("SAP", 4300), Is.True);
                Assert.That(rc.ContainsMapping("OTHER", Guid.NewGuid()), Is.False);
                Assert.That(rc.GetByMapping("D365", "A-1"), Is.SameAs(r));
                Assert.That(rc.GetByMapping("SAP", 4300), Is.SameAs(r));
                Assert.That(rc.GetByMapping("OTHER", Guid.NewGuid()), Is.Null);
                Assert.That(rc.TryGetByMapping("SAP", 4300, out RefData? r2), Is.True);
                Assert.That(r2, Is.SameAs(r));
            });

            Assert.Multiple(() =>
            {
                Assert.That(rc.TryGetByMapping("OTHER", Guid.NewGuid(), out RefData? r2), Is.False);
                Assert.That(r2, Is.Null);
            });

            var r2 = new RefData { Id = 2, Code = "B" };
            Assert.Multiple(() =>
            {
                r2.SetMapping("D365", "A-2");
                r2.SetMapping("SAP", 4301);
                rc.Add(r2);
            });

            Assert.Multiple(() =>
            {
                Assert.That(rc.ContainsMapping("D365", "A-1"), Is.True);
                Assert.That(rc.ContainsMapping("D365", "A-2"), Is.True);

                Assert.That(rc.GetByMapping("D365", "A-1"), Is.SameAs(r));
                Assert.That(rc.GetByMapping("D365", "A-2"), Is.SameAs(r2));
            });

            var r3 = new RefData { Id = 3, Code = "C" };
            r3.SetMapping("D365", "A-2");
            Assert.Throws<ArgumentException>(() => rc.Add(r3))!.Message.Should().StartWith("Item with Mapping Key 'D365' and Value 'A-2' already exists within the collection.");
        }

        [Test]
        public void OrchestratorProviders()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProvider>();
            var sp = sc.BuildServiceProvider();
            var o = new ReferenceDataOrchestrator(sp);
            o.Register<RefDataProvider>();

            Assert.Throws<InvalidOperationException>(() => o.Register<RefDataProvider>())!.Message.Should().StartWith("Type 'CoreEx.Test.Framework.RefData.RefData' cannot be added as name 'RefData' already associated with previously added Type 'CoreEx.Test.Framework.RefData.RefData'.");

            Assert.Multiple(() =>
            {
                Assert.That(o[typeof(RefData)], Is.InstanceOf(typeof(RefDataCollection)));
                Assert.That(o[typeof(RefDataEx)], Is.InstanceOf(typeof(RefDataExCollection)));
                Assert.That(o[typeof(string)], Is.Null);

                Assert.That(o["refdata"], Is.InstanceOf(typeof(RefDataCollection)));
                Assert.That(o[nameof(RefDataEx)], Is.InstanceOf(typeof(RefDataExCollection)));
                Assert.That(o["bananas"], Is.Null);
            });

            // Simulate access.
            Assert.Multiple(() =>
            {
                Assert.That(o[typeof(RefData)]!.GetByCode("A")!.Id, Is.EqualTo(1));
                Assert.That(o[typeof(RefDataEx)]!.GetByCode("BBB")!.Id, Is.EqualTo("BB"));
            });

            // Provider not wired up to execution context should not throw an exception; all will be invalid.
            var r = (RefData)1;
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.Id, Is.EqualTo(1));
                Assert.That(r.Code, Is.EqualTo(null));
                Assert.That(r.IsValid, Is.False);
            });

            r = (RefData)"A";
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.Id, Is.EqualTo(0));
                Assert.That(r.Code, Is.EqualTo("A"));
                Assert.That(r.IsValid, Is.False);
            });
        }

        [Test]
        public void RefData_ImplicitCast_Load()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProvider>();
            sc.AddReferenceDataOrchestrator(sp => new ReferenceDataOrchestrator(sp).Register<RefDataProvider>());
            var sp = sc.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            RefData? r = null;
            RefData? r1 = null;
            RefData? r2 = null;
            for (int i = 0; i < 1000; i++)
            {
                r = (RefData)1;
                r1 = (RefData)"B";
                r2 = (RefData)"C";
            }

            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r!.Id, Is.EqualTo(1));
                Assert.That(r.Code, Is.EqualTo("A"));
                Assert.That(r.IsValid, Is.True);

                Assert.That(r1, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(r1!.Id, Is.EqualTo(2));
                Assert.That(r1.Code, Is.EqualTo("B"));
                Assert.That(r1.IsValid, Is.True);

                Assert.That(r2, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(r2!.Id, Is.EqualTo(0));
                Assert.That(r2.Code, Is.EqualTo("C"));
                Assert.That(r2.IsValid, Is.False);
            });
        }

        [Test]
        public void RefenceDataIdConverter()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProvider>();
            sc.AddReferenceDataOrchestrator(sp => new ReferenceDataOrchestrator(sp).Register<RefDataProvider>());
            var sp = sc.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            var rd = (RefData)1;
            new ReferenceDataIdConverter<RefData, int>().ToDestination.Convert(rd).Should().Be(1);
            new ReferenceDataIdConverter<RefData, int>().ToDestination.Convert(null).Should().Be(0);
            new ReferenceDataIdConverter<RefData, int?>().ToDestination.Convert(rd).Should().Be(1);
            new ReferenceDataIdConverter<RefData, int?>().ToDestination.Convert(null).Should().BeNull();

            new ReferenceDataIdConverter<RefData, int>().ToSource.Convert(1).Should().NotBeNull().And.BeOfType<RefData>().Which.Id.Should().Be(1);
            new ReferenceDataIdConverter<RefData, int>().ToSource.Convert(0).Should().NotBeNull().And.BeOfType<RefData>().Which.Id.Should().Be(0);
            new ReferenceDataIdConverter<RefData, int?>().ToSource.Convert(1).Should().NotBeNull().And.BeOfType<RefData>().Which.Id.Should().Be(1);
            new ReferenceDataIdConverter<RefData, int?>().ToSource.Convert(null).Should().BeNull();

            Assert.Throws<InvalidCastException>(() => new ReferenceDataIdConverter<RefData, Guid?>().ToSource.Convert(Guid.Empty));
        }

        [Test]
        public void SidList()
        {
            var sl = new ReferenceDataCodeList<RefData>("A", "B");
            Assert.Multiple(() =>
            {
                Assert.That(sl[0].Code, Is.EqualTo("A"));
                Assert.That(sl[0].Id, Is.EqualTo(0));
                Assert.That(sl[1].Code, Is.EqualTo("B"));
                Assert.That(sl[1].Id, Is.EqualTo(0));
            });

            var sids = new System.Collections.Generic.List<string?>() { "A" };
            sl = new ReferenceDataCodeList<RefData>(ref sids);
            Assert.That(sl, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(sl[0].Code, Is.EqualTo("A"));
                Assert.That(sl[0].Id, Is.EqualTo(0));
            });

            sids!.Add("B");
            Assert.That(sl, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(sl[0].Code, Is.EqualTo("A"));
                Assert.That(sl[0].Id, Is.EqualTo(0));
                Assert.That(sl[1].Code, Is.EqualTo("B"));
                Assert.That(sl[1].Id, Is.EqualTo(0));
                Assert.That(sl.HasInvalidItems, Is.True);
            });

            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProvider>();
            sc.AddReferenceDataOrchestrator(sp => new ReferenceDataOrchestrator(sp).Register<RefDataProvider>());
            var sp = sc.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            Assert.That(sl, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(sl[0].Code, Is.EqualTo("A"));
                Assert.That(sl[0].Id, Is.EqualTo(1));
                Assert.That(sl[1].Code, Is.EqualTo("B"));
                Assert.That(sl[1].Id, Is.EqualTo(2));
                Assert.That(sl.HasInvalidItems, Is.False);
            });

            sids = new System.Collections.Generic.List<string?>() { "A" };
            sl = new ReferenceDataCodeList<RefData>(ref sids);
            Assert.That(sl, Has.Count.EqualTo(1));

            sl.Add((RefData)"B");
            Assert.Multiple(() =>
            {
                Assert.That(sl, Has.Count.EqualTo(2));
                Assert.That(sids!, Has.Count.EqualTo(2));
                Assert.Multiple(() =>
            {
                Assert.That(sids, Is.EqualTo(new string?[] { "A", "B" }));
            });

                Assert.That(sl.ToIdList<int>(), Is.EqualTo(new int[] { 1, 2 }));
                Assert.That(sl.ToCodeList(), Is.EqualTo(new string?[] { "A", "B" }));
            });
        }

        [Test]
        public void GetWithFilter()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProvider>();
            sc.AddReferenceDataOrchestrator<RefDataProvider>();
            var sp = sc.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();
            var o = scope.ServiceProvider.GetRequiredService<ReferenceDataOrchestrator>();

            Assert.Multiple(() =>
            {
                Assert.That(o.GetWithFilterAsync<State>().GetAwaiter().GetResult().Select(x => x.Code), Is.EqualTo(new string[] { "AZ", "CO", "IL", "SC", "WA" }));
                Assert.That(o.GetWithFilterAsync<State>(null, null, false).GetAwaiter().GetResult().Select(x => x.Code), Is.EqualTo(new string[] { "AZ", "CO", "IL", "SC", "WA" }));

                Assert.That(o.GetWithFilterAsync<State>(new string[] { "AZ", "IL" }).GetAwaiter().GetResult().Select(x => x.Code), Is.EqualTo(new string[] { "AZ", "IL" }));
                Assert.That(o.GetWithFilterAsync<State>(new string[] { "AZ", "IL", "XX" }).GetAwaiter().GetResult().Select(x => x.Code), Is.EqualTo(new string[] { "AZ", "IL" }));
                Assert.That(o.GetWithFilterAsync<State>(new string[] { "AZ", "IL", "XX" }, includeInactive: true).GetAwaiter().GetResult().Select(x => x.Code), Is.EqualTo(new string[] { "AZ", "IL", "XX" }));

                Assert.That(o.GetWithFilterAsync<State>(text: "*IN*").GetAwaiter().GetResult().Select(x => x.Code), Is.EqualTo(new string[] { "IL", "SC", "WA" }));
                Assert.That(o.GetWithFilterAsync<State>(text: "*in*").GetAwaiter().GetResult().Select(x => x.Code), Is.EqualTo(new string[] { "IL", "SC", "WA" }));
                Assert.That(o.GetWithFilterAsync<State>(text: "*on").GetAwaiter().GetResult().Select(x => x.Code), Is.EqualTo(new string[] { "WA" }));
                Assert.That(o.GetWithFilterAsync<State>(text: "pl*").GetAwaiter().GetResult().Select(x => x.Code), Is.EqualTo(Array.Empty<string>()));
                Assert.That(o.GetWithFilterAsync<State>(text: "pl*", includeInactive: true).GetAwaiter().GetResult().Select(x => x.Code), Is.EqualTo(new string[] { "XX" }));

                Assert.That(o.GetWithFilterAsync<State>(new string[] { "az", "il", "wa" }, text: "*in*").GetAwaiter().GetResult().Select(x => x.Code), Is.EqualTo(new string[] { "IL", "WA" }));
            });
        }

        [Test]
        public void GetNamed()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProvider>();
            sc.AddReferenceDataOrchestrator<RefDataProvider>();
            var sp = sc.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();
            var o = scope.ServiceProvider.GetRequiredService<ReferenceDataOrchestrator>();

            var mc = o.GetNamedAsync(new string[] { "state", "bananas", "suburb" }).GetAwaiter().GetResult();
            Assert.That(mc, Is.Not.Null);
            Assert.That(mc, Has.Count.EqualTo(2));
            var mc1 = mc["State"];
            Assert.That(mc1.Select(x => x.Code), Is.EqualTo(new string[] { "AZ", "CO", "IL", "SC", "WA" }));
            var mc2 = mc["Suburb"];
            Assert.That(mc2.Select(x => x.Code), Is.EqualTo(new string[] { "B", "H", "R" }));

            mc = o.GetNamedAsync(new string[] { "state", "bananas", "suburb" }, includeInactive: true).GetAwaiter().GetResult();
            Assert.That(mc, Is.Not.Null);
            Assert.That(mc, Has.Count.EqualTo(2));
            mc1 = mc["State"];
            Assert.That(mc1.Select(x => x.Code), Is.EqualTo(new string[] { "AZ", "CO", "IL", "XX", "SC", "WA"}));
            mc2 = mc["Suburb"];
            Assert.That(mc2.Select(x => x.Code), Is.EqualTo(new string[] { "B", "H", "R" }));
        }

        [Test]
        public void GetNamed_HttpRequest()
        {
            using var test = FunctionTester.Create<Startup>().ReplaceScoped<RefDataProvider>().ReplaceSingleton(sp => new ReferenceDataOrchestrator(sp).Register<RefDataProvider>());
            var hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest/ref?names=state,bananas,suburb");
            var o = test.Services.GetRequiredService<ReferenceDataOrchestrator>();

            var mc = o.GetNamedAsync(hr.GetRequestOptions()).GetAwaiter().GetResult();
            Assert.That(mc, Is.Not.Null);
            Assert.That(mc, Has.Count.EqualTo(2));
            var mc1 = mc["State"];
            Assert.That(mc1.Select(x => x.Code), Is.EqualTo(new string[] { "AZ", "CO", "IL", "SC", "WA" }));
            var mc2 = mc["Suburb"];
            Assert.That(mc2.Select(x => x.Code), Is.EqualTo(new string[] { "B", "H", "R" }));

            hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest/ref?state=co,il&suburb&state=xx");
            mc = o.GetNamedAsync(hr.GetRequestOptions()).GetAwaiter().GetResult();
            Assert.That(mc, Is.Not.Null);
            Assert.That(mc, Has.Count.EqualTo(2));
            mc1 = mc["State"];
            Assert.That(mc1.Select(x => x.Code), Is.EqualTo(new string[] { "CO", "IL" }));
            mc2 = mc["Suburb"];
            Assert.That(mc2.Select(x => x.Code), Is.EqualTo(new string[] { "B", "H", "R" }));

            hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest/ref?state=co,il&suburb=h&state=xx&include-inactive&bananas");
            mc = o.GetNamedAsync(hr.GetRequestOptions()).GetAwaiter().GetResult();
            Assert.That(mc, Is.Not.Null);
            Assert.That(mc, Has.Count.EqualTo(2));
            mc1 = mc["State"];
            Assert.That(mc1.Select(x => x.Code), Is.EqualTo(new string[] { "CO", "IL", "XX" }));
            mc2 = mc["Suburb"];
            Assert.That(mc2.Select(x => x.Code), Is.EqualTo(new string[] { "H" }));
        }

        [Test]
        public async Task Caching()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProviderSlow>();
            sc.AddReferenceDataOrchestrator<RefDataProviderSlow>();
            var sp = sc.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            ReferenceDataOrchestrator.SetCurrent(sp.GetRequiredService<ReferenceDataOrchestrator>());

            // 1st time should take time and get cached.
            var sw = Stopwatch.StartNew();
            IReferenceDataCollection?  c = await ReferenceDataOrchestrator.Current.GetByTypeAsync<RefData>().ConfigureAwait(false);
            sw.Stop();
            Assert.Multiple(() =>
            {
                Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(500));
                Assert.That(c, Is.Not.Null);
                Assert.That(c!.ContainsCode("A"), Is.True);
            });

            sw = Stopwatch.StartNew();
            c = ReferenceDataOrchestrator.Current.GetByTypeAsync<RefData>().GetAwaiter().GetResult();
            sw.Stop();
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(500));

            sw = Stopwatch.StartNew();
            c = ReferenceDataOrchestrator.Current.GetByTypeAsync<RefData>().GetAwaiter().GetResult();
            sw.Stop();
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(500));
        }

        [Test]
        public async Task Caching_LoadA()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProviderSlow>();
            sc.AddReferenceDataOrchestrator<RefDataProviderSlow>();
            var sp = sc.BuildServiceProvider();

            ReferenceDataOrchestrator.SetCurrent(sp.GetRequiredService<ReferenceDataOrchestrator>());

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            for (int i = 0; i < 1000; i++)
            {
                _ = await ReferenceDataOrchestrator.Current.GetByTypeAsync<RefData>().ConfigureAwait(false);
            }
        }

        [Test]
        public async Task Caching_Refresh()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProviderSlow>();
            sc.AddReferenceDataOrchestrator(sp => new ReferenceDataOrchestrator(sp, cacheEntryConfig: new FixedExpirationCacheEntry(TimeSpan.FromMilliseconds(250))).Register<RefDataProviderSlow>());
            var sp = sc.BuildServiceProvider();

            var rdo = sp.GetRequiredService<ReferenceDataOrchestrator>();
            ReferenceDataOrchestrator.SetCurrent(rdo);

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            // 1st time should take time and get cached.
            var sw = Stopwatch.StartNew();
            IReferenceDataCollection? c = await ReferenceDataOrchestrator.Current.GetByTypeAsync<RefData>().ConfigureAwait(false);
            sw.Stop();
            Assert.Multiple(() =>
            {
                Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(490));
                Assert.That(c, Is.Not.Null);
            });
            Assert.That(c!.ContainsCode("A"), Is.True);

            // 2nd time should be fast-as from cache.
            sw = Stopwatch.StartNew();
            c = ReferenceDataOrchestrator.Current.GetByTypeAsync<RefData>().GetAwaiter().GetResult();
            sw.Stop();
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(500));

            // Await longer than cache time.
            await Task.Delay(500).ConfigureAwait(false);

            // 3rd time should take some time to cache again.
            sw = Stopwatch.StartNew();
            c = await ReferenceDataOrchestrator.Current.GetByTypeAsync<RefData>().ConfigureAwait(false);
            sw.Stop();
            Assert.Multiple(() =>
            {
                Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(490));
                Assert.That(c, Is.Not.Null);
                Assert.That(c!.ContainsCode("A"), Is.True);
            });

            // 4th time should be fast-as from cache.
            sw = Stopwatch.StartNew();
            c = ReferenceDataOrchestrator.Current.GetByTypeAsync<RefData>().GetAwaiter().GetResult();
            sw.Stop();
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(500));
        }

        [Test]
        public void Caching_LoadB()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProviderSlow>();
            sc.AddReferenceDataOrchestrator<RefDataProviderSlow>();
            var sp = sc.BuildServiceProvider();

            ReferenceDataOrchestrator.SetCurrent(sp.GetRequiredService<ReferenceDataOrchestrator>());

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            for (int i = 0; i < 1000; i++)
            {
                _ = ReferenceDataOrchestrator.Current.GetByTypeAsync<RefData>().GetAwaiter().GetResult();
            }
        }

        [Test]
        public async Task Caching_Prefetch()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProviderSlow>();
            sc.AddReferenceDataOrchestrator<RefDataProviderSlow>();
            var sp = sc.BuildServiceProvider();

            ReferenceDataOrchestrator.SetCurrent(sp.GetRequiredService<ReferenceDataOrchestrator>());

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            // Should load both in parallel.
            var sw = Stopwatch.StartNew();
            await ReferenceDataOrchestrator.Current.PrefetchAsync(new string[] { "RefData", "RefDataEx" }).ConfigureAwait(false);
            sw.Stop();
            Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(480));

            sw = Stopwatch.StartNew();
            var c = await ReferenceDataOrchestrator.Current.GetByTypeAsync<RefData>();
            sw.Stop();
            Assert.Multiple(() =>
            {
                Assert.That(sw.ElapsedMilliseconds, Is.LessThan(520));
                Assert.That(c, Is.Not.Null);
                Assert.That(c!.ContainsCode("A"), Is.True);
            });

            sw = Stopwatch.StartNew();
            c = await ReferenceDataOrchestrator.Current.GetByTypeAsync<RefDataEx>();
            sw.Stop();
            Assert.Multiple(() =>
            {
                Assert.That(sw.ElapsedMilliseconds, Is.LessThan(520));
                Assert.That(c, Is.Not.Null);
                Assert.That(c!.ContainsId("BB"), Is.True);
            });
        }

        [Test]
        public async Task Caching_Concurrency()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataConcurrencyProvider>();
            sc.AddReferenceDataOrchestrator<RefDataConcurrencyProvider>();
            var sp = sc.BuildServiceProvider();

            ReferenceDataOrchestrator.SetCurrent(sp.GetRequiredService<ReferenceDataOrchestrator>());

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            var colls = new IReferenceDataCollection?[5];

            var tasks = new Task[5];
            tasks[0] = Task.Run(() => colls[0] = ReferenceDataOrchestrator.Current.GetByType<RefData>());
            tasks[1] = Task.Run(() => colls[1] = ReferenceDataOrchestrator.Current.GetByType<RefData>());
            tasks[2] = Task.Run(() => colls[2] = ReferenceDataOrchestrator.Current.GetByType<RefData>());
            tasks[3] = Task.Run(() => colls[3] = ReferenceDataOrchestrator.Current.GetByType<RefData>());
            tasks[4] = Task.Run(() => colls[4] = ReferenceDataOrchestrator.Current.GetByType<RefData>());

            await Task.WhenAll(tasks).ConfigureAwait(false);

            for (int i = 0; i < colls.Length; i++)
            {
                Assert.That(colls[i], Is.Not.Null);
                Assert.That(colls[i]!.Count, Is.EqualTo(2));
            }

            Assert.That(colls[4], Is.SameAs(colls[0])); // First and last should be same object ref.
        }

        [Test]
        public void Serialization_STJ_NoOrchestrator()
        {
            // Serialize.
            var td = new TestData { Id = 1, Name = "Bob" };
            Assert.That(new Text.Json.JsonSerializer().Serialize(td), Is.EqualTo("{\"id\":1,\"name\":\"Bob\"}"));

            td.RefData = new RefData { Code = "a" };
            Assert.That(new Text.Json.JsonSerializer().Serialize(td), Is.EqualTo("{\"id\":1,\"name\":\"Bob\",\"refData\":\"a\"}"));

            // Deserialize.
            td = new Text.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\"}");
            Assert.That(td, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(td!.Id, Is.EqualTo(1));
                Assert.That(td.Name, Is.EqualTo("Bob"));
            });
            Assert.That(td.RefData, Is.Null);

            td = new Text.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\",\"refData\":\"a\"}");
            Assert.That(td, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(td!.Id, Is.EqualTo(1));
                Assert.That(td.Name, Is.EqualTo("Bob"));
                Assert.That(td.RefData, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(td.RefData!.Code, Is.EqualTo("a"));
                Assert.That(td.RefData.IsValid, Is.False);
            });

            var ex = Assert.Throws<Stj.JsonException>(() => new Text.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\",\"refData\":1}"));
            Assert.That(ex!.Message, Is.EqualTo("The JSON value could not be converted to CoreEx.Test.Framework.RefData.RefData. Path: $.refData | LineNumber: 0 | BytePositionInLine: 32."));
        }

        [Test]
        public void Serialization_STJ_WithOrchestrator()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProvider>();
            sc.AddReferenceDataOrchestrator<RefDataProvider>();
            var sp = sc.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            // Serialize.
            var td = new TestData { Id = 1, Name = "Bob" };
            Assert.That(new Text.Json.JsonSerializer().Serialize(td), Is.EqualTo("{\"id\":1,\"name\":\"Bob\"}"));

            td.RefData = "a";
            Assert.That(new Text.Json.JsonSerializer().Serialize(td), Is.EqualTo("{\"id\":1,\"name\":\"Bob\",\"refData\":\"A\"}"));

            // Deserialize.
            td = new Text.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\"}");
            Assert.That(td, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(td!.Id, Is.EqualTo(1));
                Assert.That(td.Name, Is.EqualTo("Bob"));
            });
            Assert.That(td.RefData, Is.Null);

            td = new Text.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\",\"refData\":\"a\"}");
            Assert.That(td, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(td!.Id, Is.EqualTo(1));
                Assert.That(td.Name, Is.EqualTo("Bob"));
                Assert.That(td.RefData, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(td.RefData!.Code, Is.EqualTo("A"));
                Assert.That(td.RefData.IsValid, Is.True);
            });
        }

        [Test]
        public void Serialization_NSJ_NoOrchestrator()
        {
            // Serialize.
            var td = new TestData { Id = 1, Name = "Bob" };
            Assert.That(new Newtonsoft.Json.JsonSerializer().Serialize(td), Is.EqualTo("{\"id\":1,\"name\":\"Bob\"}"));

            td.RefData = new RefData { Code = "a" };
            Assert.That(new Newtonsoft.Json.JsonSerializer().Serialize(td), Is.EqualTo("{\"id\":1,\"name\":\"Bob\",\"refData\":\"a\"}"));

            // Deserialize.
            td = new Newtonsoft.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\"}");
            Assert.That(td, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(td!.Id, Is.EqualTo(1));
                Assert.That(td.Name, Is.EqualTo("Bob"));
            });
            Assert.That(td.RefData, Is.Null);

            td = new Newtonsoft.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\",\"refData\":\"a\"}");
            Assert.That(td, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(td!.Id, Is.EqualTo(1));
                Assert.That(td.Name, Is.EqualTo("Bob"));
                Assert.That(td.RefData, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(td.RefData!.Code, Is.EqualTo("a"));
                Assert.That(td.RefData.IsValid, Is.False);
            });

            var ex = Assert.Throws<Nsj.JsonSerializationException>(() => new Newtonsoft.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\",\"refData\":1}"));
            Assert.That(ex!.Message, Is.EqualTo("Reference data value must be a string."));
        }

        [Test]
        public void Serialization_NSJ_WithOrchestrator()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProvider>();
            sc.AddReferenceDataOrchestrator<RefDataProvider>();
            var sp = sc.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            // Serialize.
            var td = new TestData { Id = 1, Name = "Bob" };
            Assert.That(new Newtonsoft.Json.JsonSerializer().Serialize(td), Is.EqualTo("{\"id\":1,\"name\":\"Bob\"}"));

            td.RefData = "a";
            Assert.That(new Newtonsoft.Json.JsonSerializer().Serialize(td), Is.EqualTo("{\"id\":1,\"name\":\"Bob\",\"refData\":\"A\"}"));

            // Deserialize.
            td = new Newtonsoft.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\"}");
            Assert.That(td, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(td!.Id, Is.EqualTo(1));
                Assert.That(td.Name, Is.EqualTo("Bob"));
            });
            Assert.That(td.RefData, Is.Null);

            td = new Newtonsoft.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\",\"refData\":\"a\"}");
            Assert.That(td, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(td!.Id, Is.EqualTo(1));
                Assert.That(td.Name, Is.EqualTo("Bob"));
                Assert.That(td.RefData, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(td.RefData!.Code, Is.EqualTo("A"));
                Assert.That(td.RefData.IsValid, Is.True);
            });
        }
    }

    public class RefData : ReferenceDataBaseEx<int, RefData> 
    {
        public static implicit operator RefData(int id) => ConvertFromId(id);

        [return: NotNullIfNotNull("code")]
        public static implicit operator RefData?(string? code) => ConvertFromCode(code);
    }

    public class RefDataSimple : ReferenceDataBase<int> { }

    public class RefDataCollection : ReferenceDataCollection<int, RefData> { }

    public class RefDataEx : ReferenceDataBaseEx<string, RefDataEx>
    {
        [return: NotNullIfNotNull("code")]
        public static implicit operator RefDataEx?(string? code) => ConvertFromCode(code);
    }

    public class RefDataExCollection : ReferenceDataCollection<string, RefDataEx> { }

    public class State : ReferenceDataBaseEx<int, State> { }

    public class StateCollection : ReferenceDataCollection<int, State> { }

    public class Suburb : ReferenceDataBaseEx<string, Suburb> { }

    public class SuburbCollection : ReferenceDataCollection<string, Suburb> { }

    public class RefDataProvider : IReferenceDataProvider
    {
        private readonly RefDataCollection _refData = new() { new RefData { Id = 1, Code = "A" }, new RefData { Id = 2, Code = "B" } };
        private readonly RefDataExCollection _refDataEx = new() { new RefDataEx { Id = "AA", Code = "AAA" }, new RefDataEx { Id = "BB", Code = "BBB" } };
        private readonly StateCollection _state = new() 
        {
            new State { Id = 1, Code = "IL", Text = "Illinois" },
            new State { Id = 2, Code = "SC", Text = "South Carolina" },
            new State { Id = 3, Code = "AZ", Text = "Arizona" },
            new State { Id = 4, Code = "CO", Text = "Colorado" },
            new State { Id = 5, Code = "XX", Text = "Placeholder", IsActive = false },
            new State { Id = 6, Code = "WA", Text = "Washington" }
        };
        private readonly SuburbCollection _suburb = new()
        {
            new Suburb { Id = "BB", Code = "B", Text = "Bardon" },
            new Suburb { Id = "RR", Code = "R", Text = "Redmond" },
            new Suburb { Id = "HH", Code = "H", Text = "Hataitai" }
        };

        public Type[] Types => new Type[] { typeof(RefData), typeof(RefDataEx), typeof(State), typeof(Suburb) };

        public Task<Result<IReferenceDataCollection>> GetAsync(Type type, CancellationToken cancellationToken = default)
        {
            IReferenceDataCollection coll = type switch
            {
                Type _ when type == typeof(RefData) => _refData,
                Type _ when type == typeof(RefDataEx) => _refDataEx,
                Type _ when type == typeof(State) => _state,
                Type _ when type == typeof(Suburb) => _suburb,
                _ => throw new InvalidOperationException()
            };

            return Task.FromResult(Result.Ok(coll));
        }
    }

    public class RefDataProviderSlow : IReferenceDataProvider
    {
        private readonly RefDataCollection _refData = new() { new RefData { Id = 1, Code = "A" }, new RefData { Id = 2, Code = "B" } };
        private readonly RefDataExCollection _refDataEx = new() { new RefDataEx { Id = "AA", Code = "AAA" }, new RefDataEx { Id = "BB", Code = "BBB" } };

        public Type[] Types => new Type[] { typeof(RefData), typeof(RefDataEx) };

        public async Task<Result<IReferenceDataCollection>> GetAsync(Type type, CancellationToken cancellationToken = default)
        {
            IReferenceDataCollection coll = type switch
            {
                Type _ when type == typeof(RefData) => _refData,
                Type _ when type == typeof(RefDataEx) => _refDataEx,
                _ => throw new InvalidOperationException()
            };

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);

            return Result.Ok(coll);
        }
    }

    public class RefDataConcurrencyProvider : IReferenceDataProvider
    {
        private int _count;

        public Type[] Types => new Type[] { typeof(RefData) };

        public async Task<Result<IReferenceDataCollection>> GetAsync(Type type, CancellationToken cancellationToken = default)
        {
            System.Diagnostics.Debug.WriteLine($"GetAsync=>Enter({_count})");
            Interlocked.Increment(ref _count);
            if (_count > 1)
                //throw new InvalidOperationException("ReferenceData has loaded already; this should not occur as the ReferenceDataOrchestrator should ensure multi-load under concurrency does not occur.");
                Assert.Fail("ReferenceData has loaded already; this should not occur as the ReferenceDataOrchestrator should ensure multi-load under concurrency does not occur.");

            var coll = new RefDataCollection() { new RefData { Id = 1, Code = "A" }, new RefData { Id = 2, Code = "B" } };
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"GetAsync=>Exit({_count})");
            return coll;
        }
    }

    public class TestData : CoreEx.Entities.Extended.EntityBase
    {
        private int _id;
        private string? _name;
        private string? _gender;

        public int Id { get => _id; set => SetValue(ref _id, value); }

        public string? Name { get => _name; set => SetValue(ref _name, value); }

        public RefData? RefData { get => _gender; set => SetValue(ref _gender, value); }

        protected override IEnumerable<IPropertyValue> GetPropertyValues()
        {
            yield return CreateProperty(nameof(Id), Id, v => Id = v);
            yield return CreateProperty(nameof(Name), Name, v => Name = v);
            yield return CreateProperty(nameof(RefData), RefData, v => RefData = v);
        }
    }
}