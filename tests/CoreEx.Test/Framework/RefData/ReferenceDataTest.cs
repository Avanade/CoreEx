using CoreEx.RefData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.RefData
{
    [TestFixture]
    public class ReferenceDataTest
    {
        [Test]
        public void Exercise_RefData()
        {
            var r = new RefData { Id = 1, Code = "X", Text = "XX" };
            r.Id = 1;
            r.Code = "X";
            r.Text = "XX";

            Assert.AreEqual(1, r.Id);

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
            Assert.AreEqual(new int[] { 1, 2, 3, 4 }, rc.GetList(ReferenceDataSortOrder.Id, null, null).Select(x => x.Id).ToArray());
            Assert.AreEqual(new int[] { 2, 4, 3, 1 }, rc.GetList(ReferenceDataSortOrder.Code, null, null).Select(x => x.Id).ToArray());
            Assert.AreEqual(new int[] { 1, 2, 4, 3 }, rc.GetList(ReferenceDataSortOrder.Text, null, null).Select(x => x.Id).ToArray());
            Assert.AreEqual(new int[] { 3, 1, 4, 2 }, rc.GetList(ReferenceDataSortOrder.SortOrder, null, null).Select(x => x.Id).ToArray());

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

            Assert.AreEqual(new int[] { 1, 2, 3, 4 }, rc.GetList(ReferenceDataSortOrder.Id, null, null).Select(x => x.Id).ToArray());
            Assert.AreEqual(new int[] { 2, 4, 3, 1 }, rc.GetList(ReferenceDataSortOrder.Code, null, null).Select(x => x.Id).ToArray());
            Assert.AreEqual(new int[] { 1, 2, 4, 3 }, rc.GetList(ReferenceDataSortOrder.Text, null, null).Select(x => x.Id).ToArray());
            Assert.AreEqual(new int[] { 3, 1, 4, 2 }, rc.GetList(ReferenceDataSortOrder.SortOrder, null, null).Select(x => x.Id).ToArray());
        }

        [Test]
        public void Collection_Mappings()
        {
            var rc = new RefDataCollection();
            var r = new RefData { Id = 1, Code = "A" };
            r.SetMapping("D365", "A-1");
            r.SetMapping("SAP", 4300);

            rc.Add(r);
            Assert.IsTrue(rc.ContainsMappingValue("D365", "A-1"));
            Assert.IsTrue(rc.ContainsMappingValue("SAP", 4300));
            Assert.IsFalse(rc.ContainsMappingValue("OTHER", Guid.NewGuid()));
            Assert.AreSame(r, rc.GetByMappingValue("D365", "A-1"));
            Assert.AreSame(r, rc.GetByMappingValue("SAP", 4300));
            Assert.IsNull(rc.GetByMappingValue("OTHER", Guid.NewGuid()));

            Assert.IsTrue(rc.TryGetByMappingValue("SAP", 4300, out RefData? r2));
            Assert.AreSame(r, r2);

            Assert.IsFalse(rc.TryGetByMappingValue("OTHER", Guid.NewGuid(), out r2));
            Assert.IsNull(r2);

            r2 = new RefData { Id = 2, Code = "B" };
            r2.SetMapping("D365", "A-2");
            r2.SetMapping("SAP", 4301);
            rc.Add(r2);

            Assert.IsTrue(rc.ContainsMappingValue("D365", "A-1"));
            Assert.IsTrue(rc.ContainsMappingValue("D365", "A-2"));
            Assert.AreSame(r, rc.GetByMappingValue("D365", "A-1"));
            Assert.AreSame(r2, rc.GetByMappingValue("D365", "A-2"));

            var r3 = new RefData { Id = 3, Code = "C" };
            r3.SetMapping("D365", "A-2");
            Assert.Throws<ArgumentException>(() => rc.Add(r3))!.Message.Should().StartWith("Item with Mapping Key 'D365' and Value 'A-2' already exists within the collection.");
        }

        [Test]
        public void Manager_Providers()
        {
            var m = new ReferenceDataManager();
            m.Register(new RefDataProvider());

            Assert.Throws<InvalidOperationException>(() => m.Register(new RefDataProvider()))!.Message.Should().StartWith("Type 'CoreEx.Test.Framework.RefData.RefData' cannot be added as already associated with previously added Provider 'CoreEx.Test.Framework.RefData.RefDataProvider'.");

            Assert.NotNull(m[typeof(RefData)]);
            Assert.NotNull(m[typeof(RefDataEx)]);
            Assert.IsNull(m[typeof(string)]);

            // Simulate access.
            Assert.AreEqual(1, m[typeof(RefData)]!.GetByCode("A")!.Id);
            Assert.AreEqual("BB", m[typeof(RefDataEx)]!.GetByCode("BBB")!.Id);

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
            var m = new ReferenceDataManager();
            m.Register(new RefDataProvider());

            IServiceCollection sc = new ServiceCollection();
            sc.AddExecutionContext();
            sc.AddSingleton(_ => m);
            var sb = sc.BuildServiceProvider();
            using var scope = sb.CreateScope();
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
    }

    public class RefData : ReferenceDataBase<int, RefData> 
    {
        public static implicit operator RefData(int id) => ConvertFromId(id);

        public static implicit operator RefData(string code) => ConvertFromCode(code);
    }

    public class RefDataCollection : ReferenceDataCollection<int, RefData> { }

    public class RefDataEx : ReferenceDataBase<string, RefDataEx>
    {
        public static implicit operator RefDataEx(string code) => ConvertFromCode(code);
    }

    public class RefDataExCollection : ReferenceDataCollection<string, RefDataEx> { }

    public class RefDataProvider : IReferenceDataProvider
    {
        private readonly RefDataCollection _refData = new RefDataCollection() { new RefData { Id = 1, Code = "A" }, new RefData { Id = 2, Code = "B" } };
        private readonly RefDataExCollection _refDataEx = new RefDataExCollection() { new RefDataEx { Id = "AA", Code = "AAA" }, new RefDataEx { Id = "BB", Code = "BBB" } };

        public IReferenceDataCollection this[Type type] => type switch
        {
            Type _ when type == typeof(RefData) => _refData,
            Type _ when type == typeof(RefDataEx) => _refDataEx,
            _ => throw new InvalidOperationException(),
        };
            
        public Type[] Types => new Type[] { typeof(RefData), typeof(RefDataEx) };

        public Task PrefetchAsync(params string[] names) => throw new NotImplementedException();
    }
}