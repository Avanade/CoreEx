using CoreEx;
using CoreEx.Database;
using CoreEx.Entities;
using CoreEx.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using My.Hr.Api;
using My.Hr.Business.Data;
using My.Hr.Business.Models;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using UnitTestEx;

namespace My.Hr.UnitTest
{
    [TestFixture]
    [Category("WithDB")]
    public class DatabaseTest
    {
        [OneTimeSetUp]
        public static Task Init() => EmployeeControllerTest.Init();

        [Test]
        public async Task DatabaseParameters_JsonParameter()
        {
            using var test = ApiTester.Create<Startup>();
            using var scope = test.Services.CreateScope();

            var hrdb = scope.ServiceProvider.GetRequiredService<IDatabase>();

            var ids = new List<Guid>();
            await hrdb.SqlStatement("SELECT * FROM [Hr].[Employee]").SelectAsync(dr =>
            {
                ids.Add(dr.GetValue<Guid>("EmployeeId"));
                return true;
            });

            var ids2 = new List<Guid>();
            var c = hrdb.StoredProcedure("[Hr].[spGetEmployees]").JsonParamWith("", "ids", () => ids);

            await c.SelectAsync(dr =>
            {
                ids2.Add(dr.GetValue<Guid>("EmployeeId"));
                return true;
            });

            Assert.That(ids, Is.EquivalentTo(ids2));
        }

        [Test]
        public async Task EF_00A_Query_SelectSingle()
        {
            using var test = ApiTester.Create<Startup>();
            using var scope = test.Services.CreateScope();
            var ef = scope.ServiceProvider.GetRequiredService<IHrEfDb>();

            var r = await ef.Employees2.Query(q => q.Where(x => x.Id == 1.ToGuid())).SelectSingleAsync();
            Assert.That(r, Is.Not.Null);

            await Assert.ThatAsync(async () => await ef.Employees2.Query(q => q.Where(x => x.Id == 404.ToGuid())).SelectSingleAsync(), Throws.Exception.TypeOf<InvalidOperationException>());

            var r2 = await ef.Employees2.Query(q => q.Where(x => x.Id == 404.ToGuid())).SelectSingleOrDefaultAsync();
            Assert.That(r2, Is.Null);
        }

        [Test]
        public async Task EF_00B_Query_SelectFirst()
        {
            using var test = ApiTester.Create<Startup>();
            using var scope = test.Services.CreateScope();
            var ef = scope.ServiceProvider.GetRequiredService<IHrEfDb>();

            var r = await ef.Employees2.Query().SelectFirstAsync();
            Assert.That(r, Is.Not.Null);

            await Assert.ThatAsync(async () => await ef.Employees2.Query(q => q.Where(x => x.Id == 404.ToGuid())).SelectFirstAsync(), Throws.Exception.TypeOf<InvalidOperationException>());

            var r2 = await ef.Employees2.Query(q => q.Where(x => x.Id == 404.ToGuid())).SelectFirstOrDefaultAsync();
            Assert.That(r2, Is.Null);
        }

        [Test]
        public async Task EF_00C_Query_SelectQuery()
        {
            using var test = ApiTester.Create<Startup>();
            using var scope = test.Services.CreateScope();
            var ef = scope.ServiceProvider.GetRequiredService<IHrEfDb>();

            var c = await ef.Employees2.Query().WithPaging(1).SelectQueryAsync<List<Employee2>>();

            Assert.That(c, Is.Not.Null);
            Assert.That(c, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task EF_00D_Query_SelectResult()
        {
            using var test = ApiTester.Create<Startup>();
            using var scope = test.Services.CreateScope();
            var ef = scope.ServiceProvider.GetRequiredService<IHrEfDb>();

            var r = await ef.Employees2.Query().WithPaging(PagingArgs.CreateSkipAndTake(1, null, true)).SelectResultAsync<Employee2CollectionResult, Employee2Collection>();

            Assert.That(r, Is.Not.Null);
            Assert.That(r.Items, Has.Count.EqualTo(2));
            Assert.That(r.Paging, Is.Not.Null);
            Assert.That(r.Paging!.TotalCount, Is.EqualTo(3));
        }

        [Test]
        public async Task EF_00E_Query_SelectQuery_Item()
        {
            using var test = ApiTester.Create<Startup>();
            using var scope = test.Services.CreateScope();
            var ef = scope.ServiceProvider.GetRequiredService<IHrEfDb>();

            var count = 0;

            await ef.Query<Employee2>().SelectQueryAsync(e =>
            {
                count++;
                Assert.That(e, Is.Not.Null);
                Assert.That(e.Id, Is.Not.EqualTo(Guid.Empty));
                Assert.That(e.FirstName, Is.Not.Null);
                return count <= 1;
            });

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public async Task EF_01_Query_IsDeleted()
        {
            using var test = ApiTester.Create<Startup>();
            using var scope = test.Services.CreateScope();
            var ef = scope.ServiceProvider.GetRequiredService<IHrEfDb>();

            var r = await ef.Employees2.Query().SelectResultAsync<Employee2CollectionResult, Employee2Collection>();

            Assert.That(r, Is.Not.Null);
            Assert.That(r.Items, Has.Count.EqualTo(3));
        }

        [Test]
        public async Task EF_02_Get_IsDeleted()
        {
            using var test = ApiTester.Create<Startup>();
            using var scope = test.Services.CreateScope();
            var ef = scope.ServiceProvider.GetRequiredService<IHrEfDb>();

            var e = await ef.Employees2.GetAsync(1.ToGuid());
            Assert.That(e, Is.Not.Null);

            e = await ef.Employees2.GetAsync(2.ToGuid());
            Assert.That(e, Is.Not.Null);

            e = await ef.Employees2.GetAsync(3.ToGuid());
            Assert.That(e, Is.Null);
        }

        [Test]
        public async Task EF_03_Update_IsDeleted()
        {
            using var test = ApiTester.Create<Startup>();
            using var scope = test.Services.CreateScope();
            var ef = scope.ServiceProvider.GetRequiredService<IHrEfDb>();

            var e = await ef.Employees2.GetAsync(1.ToGuid());
            Assert.That(e, Is.Not.Null);

            ef.DbContext.ChangeTracker.Clear();

            e!.Id = 3.ToGuid();
            await Assert.ThatAsync(async () => await ef.Employees2.UpdateAsync(e), Throws.Exception.TypeOf<NotFoundException>());
        }

        [Test]
        public async Task EF_04_Delete_IsDeleted()
        {
            using var test = ApiTester.Create<Startup>();
            using var scope = test.Services.CreateScope();
            var ef = scope.ServiceProvider.GetRequiredService<IHrEfDb>();

            await ef.Employees2.DeleteAsync(1.ToGuid());

            var e = await ef.Employees2.GetAsync(1.ToGuid());
            Assert.That(e, Is.Null);

            await Assert.ThatAsync(async () => await ef.Employees2.DeleteAsync(1.ToGuid()), Throws.Exception.TypeOf<NotFoundException>());
        }
    }
}