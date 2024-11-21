using CoreEx.Database;
using Microsoft.Extensions.DependencyInjection;
using My.Hr.Api;
using My.Hr.Business.Data;
using My.Hr.Business.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
    }
}