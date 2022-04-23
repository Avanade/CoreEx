using CoreEx.RefData;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;

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
            Assert.IsTrue(rd.IsValid);

            rd.IsActive = false;
            Assert.IsFalse(rd.IsValid);

            rd.IsActive = true;
            rd.EndDate = new DateTime(2000, 01, 01);
            Assert.IsFalse(rd.IsValid);

            rd.EndDate = null;
            Assert.IsTrue(rd.IsValid);

            rd.StartDate = DateTime.UtcNow.AddDays(20);
            Assert.IsFalse(rd.IsValid);

            rd.StartDate = DateTime.UtcNow.AddDays(-20);
            rd.EndDate = DateTime.UtcNow.AddDays(20);
            Assert.IsTrue(rd.IsValid);

            // Set invalid explicitly; makes it inactive; can not reset.
            ((IReferenceData)rd).SetInvalid();
            Assert.IsFalse(rd.IsValid);
            Assert.IsFalse(rd.IsActive);

            rd.IsActive = true;
            Assert.IsFalse(rd.IsActive);
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
    }

    public class RefData : ReferenceDataBase<int, RefData> { }

    public class RefDataCollection : ReferenceDataCollection<int, RefData> { }

    public class RefDataEx : ReferenceDataBase<string, RefDataEx> { }
}