using CoreEx.Entities;
using CoreEx.Entities.Extended;
using CoreEx.Http;
using CoreEx.Mapping.Converters;
using CoreEx.RefData;
using CoreEx.RefData.Caching;
using CoreEx.RefData.Extended;
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

            Assert.AreEqual(1, r.Id);
            Assert.AreEqual(1, ((ReferenceDataBase<int>)r).Id);
            Assert.AreEqual(1, ((ReferenceDataBase)r).Id);
            Assert.AreEqual(1, ((IReferenceData)r).Id);
            Assert.AreEqual(1, ((IIdentifier)r).Id);
            Assert.AreEqual("X", r.Code);
            Assert.AreEqual("XX", r.Text);

            Assert.AreEqual(typeof(int), ((IIdentifier)r).IdType);
        }

        [Test]
        public void Exercise_RefData()
        {
            var r = new RefData { Id = 1, Code = "X", Text = "XX" };
            r.Id = 1;
            r.Code = "X";
            r.Text = "XX";

            Assert.AreEqual(1, r.Id);
            Assert.AreEqual(1, ((IReferenceData)r).Id);
            Assert.AreEqual(1, ((ReferenceDataBaseEx<int, RefData>)r).Id);
            Assert.AreEqual(1, ((IIdentifier)r).Id);

            Assert.AreEqual(typeof(int), ((IIdentifier)r).IdType);

            // Immutable.
            Assert.Throws<InvalidOperationException>(() => r.Id = 2);
            Assert.Throws<InvalidOperationException>(() => r.Code = "Y");
            r.Text = "YY";

            var r2 = r;
            Assert.IsTrue(r == r2);

            r2 = (RefData)r.Clone();
            Assert.IsTrue(r == r2);
            Assert.IsTrue(r.Equals(r2));
            Assert.IsTrue(r.Equals((object)r2));
            Assert.AreEqual(r.GetHashCode(), r2.GetHashCode());

            r2 = new RefData { Id = 1, Code = "X", Text = "XXXX" };
            Assert.IsFalse(r == r2);
            Assert.IsFalse(r.Equals(r2));
            Assert.IsFalse(r.Equals((object)r2));
            Assert.AreNotEqual(r.GetHashCode(), r2.GetHashCode());

            r.MakeReadOnly();
            Assert.IsTrue(r.IsReadOnly);
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
            Assert.IsTrue(r == r2);

            r2 = (RefDataEx)r.Clone();
            Assert.IsTrue(r == r2);
            Assert.IsTrue(r.Equals(r2));
            Assert.IsTrue(r.Equals((object)r2));
            Assert.AreEqual(r.GetHashCode(), r2.GetHashCode());

            r2 = new RefDataEx { Id = "@", Code = "X", Text = "XX", Description = "XXX", IsActive = true, StartDate = new DateTime(2020, 01, 01), EndDate = new DateTime(2021, 12, 31) };
            Assert.IsFalse(r == r2);
            Assert.IsFalse(r.Equals(r2));
            Assert.IsFalse(r.Equals((object)r2));
            Assert.AreNotEqual(r.GetHashCode(), r2.GetHashCode());

            r.MakeReadOnly();
            Assert.IsTrue(r.IsReadOnly);
            Assert.Throws<InvalidOperationException>(() => r.Text = "Bananas");
            Assert.Throws<InvalidOperationException>(() => r.IsActive = true);
        }

        [Test]
        public void Exercise_RefDataEx_Mappings()
        {
            var r = new RefDataEx();
            Assert.IsTrue(r.IsInitial);
            Assert.IsFalse(r.HasMappings);

            r.SetMapping("X", 1);
            Assert.IsFalse(r.IsInitial);
            Assert.IsTrue(r.HasMappings);

            Assert.IsTrue(r.TryGetMapping("X", out int val));
            Assert.AreEqual(1, val);
            Assert.AreEqual(1, r.GetMapping<int>("X"));

            var r2 = (RefDataEx)r.Clone();
            Assert.IsTrue(r2.HasMappings);
            Assert.IsTrue(r2.TryGetMapping("X", out val));
            Assert.AreEqual(1, val);
            Assert.AreEqual(1, r2.GetMapping<int>("X"));

            r2 = new RefDataEx();
            r2.SetMapping("X", 1);
            Assert.IsTrue(r == r2);

            r2.SetMapping("Y", 2);
            Assert.IsFalse(r == r2);

            r.MakeReadOnly();
            Assert.IsTrue(r.IsReadOnly);
            Assert.Throws<InvalidOperationException>(() => r.SetMapping("Y", 2));
        }

        [Test]
        public void IsValid_IsActive()
        {
            var rd = new RefDataEx();
            Assert.IsTrue(rd.IsActive);
            Assert.IsTrue(rd.IsValid);

            rd.IsActive = false;
            Assert.IsFalse(rd.IsActive);
            Assert.IsTrue(rd.IsValid);

            rd.IsActive = true;
            rd.EndDate = new DateTime(2000, 01, 01);
            Assert.IsFalse(rd.IsActive);
            Assert.IsTrue(rd.IsValid);

            rd.EndDate = null;
            Assert.IsTrue(rd.IsActive);
            Assert.IsTrue(rd.IsValid);

            rd.StartDate = DateTime.UtcNow.AddDays(20);
            Assert.IsFalse(rd.IsActive);
            Assert.IsTrue(rd.IsValid);

            rd.StartDate = DateTime.UtcNow.AddDays(-20);
            rd.EndDate = DateTime.UtcNow.AddDays(20);
            Assert.IsTrue(rd.IsActive);
            Assert.IsTrue(rd.IsValid);

            // Set invalid explicitly; makes it inactive; can not reset.
            ((IReferenceData)rd).SetInvalid();
            Assert.IsFalse(rd.IsActive);
            Assert.IsFalse(rd.IsValid);

            rd.IsActive = true;
            Assert.IsFalse(rd.IsActive);
            Assert.IsFalse(rd.IsValid);
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
            Assert.IsTrue(rd.IsValid);

            rd.StartDate = new DateTime(1999, 01, 01);
            rd.EndDate = new DateTime(1999, 12, 31);
            Assert.IsTrue(rd.IsValid);
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
            Assert.IsTrue(rd.IsValid);

            rd.StartDate = new DateTime(1999, 01, 01);
            rd.EndDate = new DateTime(1999, 12, 31);
            Assert.IsTrue(rd.IsValid);
        }

        [Test]
        public void Casting_FromRefData()
        {
            Assert.AreEqual(0, (int)(RefData)null!);
            Assert.AreEqual(null, (string?)(RefData)null!);
            Assert.AreEqual(0, (int)new RefData());
            Assert.AreEqual(null, (string?)new RefData());
            Assert.AreEqual(1, (int)new RefData { Id = 1, Code = "X", Text = "XX" });
            Assert.AreEqual("X", (string?)new RefData { Id = 1, Code = "X", Text = "XX" });
            Assert.AreEqual(0, (int)new RefDataEx { Id = "X", Code = "XX", Text = "XXX" });
            Assert.AreEqual("XX", (string?)new RefDataEx { Id = "X", Code = "XX", Text = "XXX" });

            Assert.AreEqual(null, (int?)(RefDataEx)null!);
            Assert.AreEqual(null, (string?)(RefDataEx)null!);
            Assert.AreEqual(null, (int?)new RefDataEx());
            Assert.AreEqual(null, (string?)new RefDataEx());
            Assert.AreEqual(1, (int?)new RefData { Id = 1, Code = "X", Text = "XX" });
            Assert.AreEqual("X", (string?)new RefData { Id = 1, Code = "X", Text = "XX" });
            Assert.AreEqual(null, (int?)new RefDataEx { Id = "X", Code = "XX", Text = "XXX" });
            Assert.AreEqual("XX", (string?)new RefDataEx { Id = "X", Code = "XX", Text = "XXX" });
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
            Assert.AreEqual(1, rc.Count);
            Assert.AreSame(r, rc["XX"]);
            Assert.AreSame(r, rc["xx"]);
            Assert.AreSame(r, rc.GetByCode("XX"));
            Assert.AreSame(r, rc.GetByCode("xx"));
            Assert.AreSame(r, rc.GetById("X"));
            Assert.IsTrue(r.IsReadOnly);

            Assert.Throws<ArgumentException>(() => rc.Add(r))!.Message.Should().StartWith("Item already exists within the collection.");
            Assert.Throws<ArgumentException>(() => rc.Add(new RefDataEx { Id = "X", Code = "YY" }))!.Message.Should().StartWith("Item with Id 'X' already exists within the collection.");
            Assert.Throws<ArgumentException>(() => rc.Add(new RefDataEx { Id = "Y", Code = "XX" }))!.Message.Should().StartWith("Item with Code 'XX' already exists within the collection.");
            Assert.Throws<ArgumentException>(() => rc.Add(new RefDataEx { Id = "Y", Code = "xx" }))!.Message.Should().StartWith("Item with Code 'xx' already exists within the collection.");

            r = new RefDataEx { Id = "Y", Code = "YY" };
            rc.Add(r);
            Assert.AreEqual(2, rc.Count);
            Assert.AreSame(r, rc["YY"]);
            Assert.AreSame(r, rc["yy"]);
            Assert.AreSame(r, rc.GetByCode("YY"));
            Assert.AreSame(r, rc.GetByCode("yy"));
            Assert.AreSame(r, rc.GetById("Y"));
            Assert.IsTrue(r.IsReadOnly);
        }

        [Test]
        public void Collection_Lists()
        {
            var rc = new RefDataCollection { new RefData { Id = 1, Code = "Z", Text = "A", SortOrder = 2 }, new RefData { Id = 2, Code = "A", Text = "B", IsActive = false, SortOrder = 4 }, new RefData { Id = 3, Code = "Y", Text = "D", SortOrder = 1 }, new RefData { Id = 4, Code = "B", Text = "C", SortOrder = 3 } };
            Assert.AreEqual(new int[] { 1, 2, 3, 4 }, rc.GetItems(ReferenceDataSortOrder.Id, null, null).Select(x => x.Id).ToArray());
            Assert.AreEqual(new int[] { 2, 4, 3, 1 }, rc.GetItems(ReferenceDataSortOrder.Code, null, null).Select(x => x.Id).ToArray());
            Assert.AreEqual(new int[] { 1, 2, 4, 3 }, rc.GetItems(ReferenceDataSortOrder.Text, null, null).Select(x => x.Id).ToArray());
            Assert.AreEqual(new int[] { 3, 1, 4, 2 }, rc.GetItems(ReferenceDataSortOrder.SortOrder, null, null).Select(x => x.Id).ToArray());

            rc.SortOrder = ReferenceDataSortOrder.Id;
            Assert.AreEqual(new int[] { 1, 2, 3, 4 }, rc.AllList.Select(x => x.Id).ToArray());
            rc.SortOrder = ReferenceDataSortOrder.Code;
            Assert.AreEqual(new int[] { 2, 4, 3, 1 }, rc.AllList.Select(x => x.Id).ToArray());
            rc.SortOrder = ReferenceDataSortOrder.Text;
            Assert.AreEqual(new int[] { 1, 2, 4, 3 }, rc.AllList.Select(x => x.Id).ToArray());
            rc.SortOrder = ReferenceDataSortOrder.SortOrder;
            Assert.AreEqual(new int[] { 3, 1, 4, 2 }, rc.AllList.Select(x => x.Id).ToArray());

            rc.SortOrder = ReferenceDataSortOrder.Id;
            Assert.AreEqual(new int[] { 1, 3, 4 }, rc.ActiveList.Select(x => x.Id).ToArray());
            rc.SortOrder = ReferenceDataSortOrder.Code;
            Assert.AreEqual(new int[] { 4, 3, 1 }, rc.ActiveList.Select(x => x.Id).ToArray());
            rc.SortOrder = ReferenceDataSortOrder.Text;
            Assert.AreEqual(new int[] { 1, 4, 3 }, rc.ActiveList.Select(x => x.Id).ToArray());
            rc.SortOrder = ReferenceDataSortOrder.SortOrder;
            Assert.AreEqual(new int[] { 3, 1, 4 }, rc.ActiveList.Select(x => x.Id).ToArray());

            ((IReferenceData)rc.GetById(1)!).SetInvalid();

            rc.SortOrder = ReferenceDataSortOrder.Id;
            Assert.AreEqual(new int[] { 2, 3, 4 }, rc.AllList.Select(x => x.Id).ToArray());
            rc.SortOrder = ReferenceDataSortOrder.Code;
            Assert.AreEqual(new int[] { 2, 4, 3 }, rc.AllList.Select(x => x.Id).ToArray());
            rc.SortOrder = ReferenceDataSortOrder.Text;
            Assert.AreEqual(new int[] { 2, 4, 3 }, rc.AllList.Select(x => x.Id).ToArray());
            rc.SortOrder = ReferenceDataSortOrder.SortOrder;
            Assert.AreEqual(new int[] { 3, 4, 2 }, rc.AllList.Select(x => x.Id).ToArray());

            rc.SortOrder = ReferenceDataSortOrder.Id;
            Assert.AreEqual(new int[] { 3, 4 }, rc.ActiveList.Select(x => x.Id).ToArray());
            rc.SortOrder = ReferenceDataSortOrder.Code;
            Assert.AreEqual(new int[] { 4, 3 }, rc.ActiveList.Select(x => x.Id).ToArray());
            rc.SortOrder = ReferenceDataSortOrder.Text;
            Assert.AreEqual(new int[] { 4, 3 }, rc.ActiveList.Select(x => x.Id).ToArray());
            rc.SortOrder = ReferenceDataSortOrder.SortOrder;
            Assert.AreEqual(new int[] { 3, 4 }, rc.ActiveList.Select(x => x.Id).ToArray());

            Assert.AreEqual(new int[] { 1, 2, 3, 4 }, rc.GetItems(ReferenceDataSortOrder.Id, null, null).Select(x => x.Id).ToArray());
            Assert.AreEqual(new int[] { 2, 4, 3, 1 }, rc.GetItems(ReferenceDataSortOrder.Code, null, null).Select(x => x.Id).ToArray());
            Assert.AreEqual(new int[] { 1, 2, 4, 3 }, rc.GetItems(ReferenceDataSortOrder.Text, null, null).Select(x => x.Id).ToArray());
            Assert.AreEqual(new int[] { 3, 1, 4, 2 }, rc.GetItems(ReferenceDataSortOrder.SortOrder, null, null).Select(x => x.Id).ToArray());
        }

        [Test]
        public void Collection_Mappings()
        {
            var rc = new RefDataCollection();
            var r = new RefData { Id = 1, Code = "A" };
            r.SetMapping("D365", "A-1");
            r.SetMapping("SAP", 4300);

            rc.Add(r);
            Assert.IsTrue(rc.ContainsMapping("D365", "A-1"));
            Assert.IsTrue(rc.ContainsMapping("SAP", 4300));
            Assert.IsFalse(rc.ContainsMapping("OTHER", Guid.NewGuid()));
            Assert.AreSame(r, rc.GetByMapping("D365", "A-1"));
            Assert.AreSame(r, rc.GetByMapping("SAP", 4300));
            Assert.IsNull(rc.GetByMapping("OTHER", Guid.NewGuid()));

            Assert.IsTrue(rc.TryGetByMapping("SAP", 4300, out RefData? r2));
            Assert.AreSame(r, r2);

            Assert.IsFalse(rc.TryGetByMapping("OTHER", Guid.NewGuid(), out r2));
            Assert.IsNull(r2);

            r2 = new RefData { Id = 2, Code = "B" };
            r2.SetMapping("D365", "A-2");
            r2.SetMapping("SAP", 4301);
            rc.Add(r2);

            Assert.IsTrue(rc.ContainsMapping("D365", "A-1"));
            Assert.IsTrue(rc.ContainsMapping("D365", "A-2"));
            Assert.AreSame(r, rc.GetByMapping("D365", "A-1"));
            Assert.AreSame(r2, rc.GetByMapping("D365", "A-2"));

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

            Assert.IsInstanceOf(typeof(RefDataCollection), o[typeof(RefData)]);
            Assert.IsInstanceOf(typeof(RefDataExCollection), o[typeof(RefDataEx)]);
            Assert.IsNull(o[typeof(string)]);

            Assert.IsInstanceOf(typeof(RefDataCollection), o["refdata"]);
            Assert.IsInstanceOf(typeof(RefDataExCollection), o[nameof(RefDataEx)]);
            Assert.IsNull(o["bananas"]);

            // Simulate access.
            Assert.AreEqual(1, o[typeof(RefData)]!.GetByCode("A")!.Id);
            Assert.AreEqual("BB", o[typeof(RefDataEx)]!.GetByCode("BBB")!.Id);

            // Provider not wired up to execution context should not throw an exception; all will be invalid.
            var r = (RefData)1;
            Assert.IsNotNull(r);
            Assert.AreEqual(1, r.Id);
            Assert.AreEqual(null, r.Code);
            Assert.IsFalse(r.IsValid);

            r = (RefData)"A";
            Assert.IsNotNull(r);
            Assert.AreEqual(0, r.Id);
            Assert.AreEqual("A", r.Code);
            Assert.IsFalse(r.IsValid);
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

            Assert.IsNotNull(r);
            Assert.AreEqual(1, r!.Id);
            Assert.AreEqual("A", r.Code);
            Assert.IsTrue(r.IsValid);

            Assert.IsNotNull(r1);
            Assert.AreEqual(2, r1!.Id);
            Assert.AreEqual("B", r1.Code);
            Assert.IsTrue(r1.IsValid);

            Assert.IsNotNull(r2);
            Assert.AreEqual(0, r2!.Id);
            Assert.AreEqual("C", r2.Code);
            Assert.IsFalse(r2.IsValid);
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
            Assert.AreEqual("A", sl[0].Code);
            Assert.AreEqual(0, sl[0].Id);
            Assert.AreEqual("B", sl[1].Code);
            Assert.AreEqual(0, sl[1].Id);

            var sids = new System.Collections.Generic.List<string?>() { "A" };
            sl = new ReferenceDataCodeList<RefData>(ref sids);
            Assert.AreEqual(1, sl.Count);
            Assert.AreEqual("A", sl[0].Code);
            Assert.AreEqual(0, sl[0].Id);

            sids!.Add("B");
            Assert.AreEqual(2, sl.Count);
            Assert.AreEqual("A", sl[0].Code);
            Assert.AreEqual(0, sl[0].Id);
            Assert.AreEqual("B", sl[1].Code);
            Assert.AreEqual(0, sl[1].Id);
            Assert.IsTrue(sl.HasInvalidItems);

            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProvider>();
            sc.AddReferenceDataOrchestrator(sp => new ReferenceDataOrchestrator(sp).Register<RefDataProvider>());
            var sp = sc.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            Assert.AreEqual(2, sl.Count);
            Assert.AreEqual("A", sl[0].Code);
            Assert.AreEqual(1, sl[0].Id);
            Assert.AreEqual("B", sl[1].Code);
            Assert.AreEqual(2, sl[1].Id);
            Assert.IsFalse(sl.HasInvalidItems);

            sids = new System.Collections.Generic.List<string?>() { "A" };
            sl = new ReferenceDataCodeList<RefData>(ref sids);
            Assert.AreEqual(1, sl.Count);

            sl.Add((RefData)"B");
            Assert.AreEqual(2, sl.Count);
            Assert.AreEqual(2, sids!.Count);
            Assert.AreEqual(new string?[] { "A", "B" }, sids);

            Assert.AreEqual(new int[] { 1, 2 }, sl.ToIdList<int>());
            Assert.AreEqual(new string?[] { "A", "B" }, sl.ToCodeList());
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

            Assert.AreEqual(new string[] { "AZ", "CO", "IL", "SC", "WA" }, o.GetWithFilterAsync<State>().GetAwaiter().GetResult().Select(x => x.Code));
            Assert.AreEqual(new string[] { "AZ", "CO", "IL", "SC", "WA" }, o.GetWithFilterAsync<State>(null, null, false).GetAwaiter().GetResult().Select(x => x.Code));

            Assert.AreEqual(new string[] { "AZ", "IL" }, o.GetWithFilterAsync<State>(new string[] { "AZ", "IL" }).GetAwaiter().GetResult().Select(x => x.Code));
            Assert.AreEqual(new string[] { "AZ", "IL" }, o.GetWithFilterAsync<State>(new string[] { "AZ", "IL", "XX" }).GetAwaiter().GetResult().Select(x => x.Code));
            Assert.AreEqual(new string[] { "AZ", "IL", "XX" }, o.GetWithFilterAsync<State>(new string[] { "AZ", "IL", "XX" }, includeInactive: true).GetAwaiter().GetResult().Select(x => x.Code));

            Assert.AreEqual(new string[] { "IL", "SC", "WA" }, o.GetWithFilterAsync<State>(text: "*IN*").GetAwaiter().GetResult().Select(x => x.Code));
            Assert.AreEqual(new string[] { "IL", "SC", "WA" }, o.GetWithFilterAsync<State>(text: "*in*").GetAwaiter().GetResult().Select(x => x.Code));
            Assert.AreEqual(new string[] { "WA" }, o.GetWithFilterAsync<State>(text: "*on").GetAwaiter().GetResult().Select(x => x.Code));
            Assert.AreEqual(Array.Empty<string>(), o.GetWithFilterAsync<State>(text: "pl*").GetAwaiter().GetResult().Select(x => x.Code));
            Assert.AreEqual(new string[] { "XX" }, o.GetWithFilterAsync<State>(text: "pl*", includeInactive: true).GetAwaiter().GetResult().Select(x => x.Code));

            Assert.AreEqual(new string[] { "IL", "WA" }, o.GetWithFilterAsync<State>(new string[] { "az", "il", "wa" }, text: "*in*").GetAwaiter().GetResult().Select(x => x.Code));
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
            Assert.NotNull(mc);
            Assert.AreEqual(2, mc.Count);
            Assert.AreEqual("State", mc[0].Name);
            Assert.AreEqual(new string[] { "AZ", "CO", "IL", "SC", "WA" }, mc[0].Items.Select(x => x.Code));
            Assert.AreEqual("Suburb", mc[1].Name);
            Assert.AreEqual(new string[] { "B", "H", "R" }, mc[1].Items.Select(x => x.Code));

            mc = o.GetNamedAsync(new string[] { "state", "bananas", "suburb" }, includeInactive: true).GetAwaiter().GetResult();
            Assert.NotNull(mc);
            Assert.AreEqual(2, mc.Count);
            Assert.AreEqual("State", mc[0].Name);
            Assert.AreEqual(new string[] { "AZ", "CO", "IL", "XX", "SC", "WA"}, mc[0].Items.Select(x => x.Code));
            Assert.AreEqual("Suburb", mc[1].Name);
            Assert.AreEqual(new string[] { "B", "H", "R" }, mc[1].Items.Select(x => x.Code));
        }

        [Test]
        public void GetNamed_HttpRequest()
        {
            using var test = FunctionTester.Create<Startup>().ReplaceScoped<RefDataProvider>().ReplaceSingleton(sp => new ReferenceDataOrchestrator(sp).Register<RefDataProvider>());
            var hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest/ref?names=state,bananas,suburb");
            var o = test.Services.GetRequiredService<ReferenceDataOrchestrator>();

            var mc = o.GetNamedAsync(hr.GetRequestOptions()).GetAwaiter().GetResult();
            Assert.NotNull(mc);
            Assert.AreEqual(2, mc.Count);
            Assert.AreEqual("State", mc[0].Name);
            Assert.AreEqual(new string[] { "AZ", "CO", "IL", "SC", "WA" }, mc[0].Items.Select(x => x.Code));
            Assert.AreEqual("Suburb", mc[1].Name);
            Assert.AreEqual(new string[] { "B", "H", "R" }, mc[1].Items.Select(x => x.Code));

            hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest/ref?state=co,il&suburb&state=xx");
            mc = o.GetNamedAsync(hr.GetRequestOptions()).GetAwaiter().GetResult();
            Assert.NotNull(mc);
            Assert.AreEqual(2, mc.Count);
            Assert.AreEqual("State", mc[0].Name);
            Assert.AreEqual(new string[] { "CO", "IL" }, mc[0].Items.Select(x => x.Code));
            Assert.AreEqual("Suburb", mc[1].Name);
            Assert.AreEqual(new string[] { "B", "H", "R" }, mc[1].Items.Select(x => x.Code));

            hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest/ref?state=co,il&suburb=h&state=xx&include-inactive&bananas");
            mc = o.GetNamedAsync(hr.GetRequestOptions()).GetAwaiter().GetResult();
            Assert.NotNull(mc);
            Assert.AreEqual(2, mc.Count);
            Assert.AreEqual("State", mc[0].Name);
            Assert.AreEqual(new string[] { "CO", "IL", "XX" }, mc[0].Items.Select(x => x.Code));
            Assert.AreEqual("Suburb", mc[1].Name);
            Assert.AreEqual(new string[] { "H" }, mc[1].Items.Select(x => x.Code));
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
            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 500);
            Assert.NotNull(c);
            Assert.IsTrue(c!.ContainsCode("A"));

            sw = Stopwatch.StartNew();
            c = ReferenceDataOrchestrator.Current.GetByTypeAsync<RefData>().GetAwaiter().GetResult();
            sw.Stop();
            Assert.Less(sw.ElapsedMilliseconds, 500);

            sw = Stopwatch.StartNew();
            c = ReferenceDataOrchestrator.Current.GetByTypeAsync<RefData>().GetAwaiter().GetResult();
            sw.Stop();
            Assert.Less(sw.ElapsedMilliseconds, 500);
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
            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 490);
            Assert.NotNull(c);
            Assert.IsTrue(c!.ContainsCode("A"));

            // 2nd time should be fast-as from cache.
            sw = Stopwatch.StartNew();
            c = ReferenceDataOrchestrator.Current.GetByTypeAsync<RefData>().GetAwaiter().GetResult();
            sw.Stop();
            Assert.Less(sw.ElapsedMilliseconds, 500);

            // Await longer than cache time.
            await Task.Delay(500).ConfigureAwait(false);

            // 3rd time should take some time to cache again.
            sw = Stopwatch.StartNew();
            c = await ReferenceDataOrchestrator.Current.GetByTypeAsync<RefData>().ConfigureAwait(false);
            sw.Stop();
            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 490);
            Assert.NotNull(c);
            Assert.IsTrue(c!.ContainsCode("A"));

            // 4th time should be fast-as from cache.
            sw = Stopwatch.StartNew();
            c = ReferenceDataOrchestrator.Current.GetByTypeAsync<RefData>().GetAwaiter().GetResult();
            sw.Stop();
            Assert.Less(sw.ElapsedMilliseconds, 500);
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
            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 500);

            sw = Stopwatch.StartNew();
            var c = await ReferenceDataOrchestrator.Current.GetByTypeAsync<RefData>();
            sw.Stop();
            Assert.Less(sw.ElapsedMilliseconds, 500);
            Assert.NotNull(c);
            Assert.IsTrue(c!.ContainsCode("A"));

            sw = Stopwatch.StartNew();
            c = await ReferenceDataOrchestrator.Current.GetByTypeAsync<RefDataEx>();
            sw.Stop();
            Assert.Less(sw.ElapsedMilliseconds, 500);
            Assert.NotNull(c);
            Assert.IsTrue(c!.ContainsId("BB"));
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
                Assert.IsNotNull(colls[i]);
                Assert.AreEqual(2, colls[i]!.Count);
            }

            Assert.AreSame(colls[0], colls[4]); // First and last should be same object ref.
        }

        [Test]
        public void Serialization_STJ_NoOrchestrator()
        {
            // Serialize.
            var td = new TestData { Id = 1, Name = "Bob" };
            Assert.AreEqual("{\"id\":1,\"name\":\"Bob\"}", new Text.Json.JsonSerializer().Serialize(td));

            td.RefData = new RefData { Code = "a" };
            Assert.AreEqual("{\"id\":1,\"name\":\"Bob\",\"refData\":\"a\"}", new Text.Json.JsonSerializer().Serialize(td));

            // Deserialize.
            td = new Text.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\"}");
            Assert.NotNull(td);
            Assert.AreEqual(1, td!.Id);
            Assert.AreEqual("Bob", td.Name);
            Assert.Null(td.RefData);

            td = new Text.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\",\"refData\":\"a\"}");
            Assert.NotNull(td);
            Assert.AreEqual(1, td!.Id);
            Assert.AreEqual("Bob", td.Name);
            Assert.NotNull(td.RefData);
            Assert.AreEqual("a", td.RefData!.Code);
            Assert.IsFalse(td.RefData.IsValid);

            var ex = Assert.Throws<Stj.JsonException>(() => new Text.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\",\"refData\":1}"));
            Assert.AreEqual("The JSON value could not be converted to CoreEx.Test.Framework.RefData.RefData. Path: $.refData | LineNumber: 0 | BytePositionInLine: 32.", ex!.Message);
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
            Assert.AreEqual("{\"id\":1,\"name\":\"Bob\"}", new Text.Json.JsonSerializer().Serialize(td));

            td.RefData = "a";
            Assert.AreEqual("{\"id\":1,\"name\":\"Bob\",\"refData\":\"A\"}", new Text.Json.JsonSerializer().Serialize(td));

            // Deserialize.
            td = new Text.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\"}");
            Assert.NotNull(td);
            Assert.AreEqual(1, td!.Id);
            Assert.AreEqual("Bob", td.Name);
            Assert.Null(td.RefData);

            td = new Text.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\",\"refData\":\"a\"}");
            Assert.NotNull(td);
            Assert.AreEqual(1, td!.Id);
            Assert.AreEqual("Bob", td.Name);
            Assert.NotNull(td.RefData);
            Assert.AreEqual("A", td.RefData!.Code);
            Assert.IsTrue(td.RefData.IsValid);
        }

        [Test]
        public void Serialization_NSJ_NoOrchestrator()
        {
            // Serialize.
            var td = new TestData { Id = 1, Name = "Bob" };
            Assert.AreEqual("{\"id\":1,\"name\":\"Bob\"}", new Newtonsoft.Json.JsonSerializer().Serialize(td));

            td.RefData = new RefData { Code = "a" };
            Assert.AreEqual("{\"id\":1,\"name\":\"Bob\",\"refData\":\"a\"}", new Newtonsoft.Json.JsonSerializer().Serialize(td));

            // Deserialize.
            td = new Newtonsoft.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\"}");
            Assert.NotNull(td);
            Assert.AreEqual(1, td!.Id);
            Assert.AreEqual("Bob", td.Name);
            Assert.Null(td.RefData);

            td = new Newtonsoft.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\",\"refData\":\"a\"}");
            Assert.NotNull(td);
            Assert.AreEqual(1, td!.Id);
            Assert.AreEqual("Bob", td.Name);
            Assert.NotNull(td.RefData);
            Assert.AreEqual("a", td.RefData!.Code);
            Assert.IsFalse(td.RefData.IsValid);

            var ex = Assert.Throws<Nsj.JsonSerializationException>(() => new Newtonsoft.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\",\"refData\":1}"));
            Assert.AreEqual("Reference data value must be a string.", ex!.Message);
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
            Assert.AreEqual("{\"id\":1,\"name\":\"Bob\"}", new Newtonsoft.Json.JsonSerializer().Serialize(td));

            td.RefData = "a";
            Assert.AreEqual("{\"id\":1,\"name\":\"Bob\",\"refData\":\"A\"}", new Newtonsoft.Json.JsonSerializer().Serialize(td));

            // Deserialize.
            td = new Newtonsoft.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\"}");
            Assert.NotNull(td);
            Assert.AreEqual(1, td!.Id);
            Assert.AreEqual("Bob", td.Name);
            Assert.Null(td.RefData);

            td = new Newtonsoft.Json.JsonSerializer().Deserialize<TestData>("{\"id\":1,\"name\":\"Bob\",\"refData\":\"a\"}");
            Assert.NotNull(td);
            Assert.AreEqual(1, td!.Id);
            Assert.AreEqual("Bob", td.Name);
            Assert.NotNull(td.RefData);
            Assert.AreEqual("A", td.RefData!.Code);
            Assert.IsTrue(td.RefData.IsValid);
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

        public Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default)
        {
            IReferenceDataCollection coll = type switch
            {
                Type _ when type == typeof(RefData) => _refData,
                Type _ when type == typeof(RefDataEx) => _refDataEx,
                Type _ when type == typeof(State) => _state,
                Type _ when type == typeof(Suburb) => _suburb,
                _ => throw new InvalidOperationException()
            };

            return Task.FromResult(coll);
        }
    }

    public class RefDataProviderSlow : IReferenceDataProvider
    {
        private readonly RefDataCollection _refData = new() { new RefData { Id = 1, Code = "A" }, new RefData { Id = 2, Code = "B" } };
        private readonly RefDataExCollection _refDataEx = new() { new RefDataEx { Id = "AA", Code = "AAA" }, new RefDataEx { Id = "BB", Code = "BBB" } };

        public Type[] Types => new Type[] { typeof(RefData), typeof(RefDataEx) };

        public async Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default)
        {
            IReferenceDataCollection coll = type switch
            {
                Type _ when type == typeof(RefData) => _refData,
                Type _ when type == typeof(RefDataEx) => _refDataEx,
                _ => throw new InvalidOperationException()
            };

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);

            return coll;
        }
    }

    public class RefDataConcurrencyProvider : IReferenceDataProvider
    {
        private int _count;

        public Type[] Types => new Type[] { typeof(RefData) };

        public async Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default)
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