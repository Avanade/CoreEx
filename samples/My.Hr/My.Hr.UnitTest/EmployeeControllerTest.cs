using CoreEx.Entities;
using CoreEx.Events;
using CoreEx.Http;
using DbEx.Migration;
using DbEx.Migration.Data;
using Microsoft.Extensions.Configuration;
using My.Hr.Api;
using My.Hr.Api.Controllers;
using My.Hr.Business.Models;
using My.Hr.Business.External.Contracts;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnitTestEx;
using UnitTestEx.NUnit;

namespace My.Hr.UnitTest
{
    [TestFixture]
    [Category("WithDB")]
    public class EmployeeControllerTest
    {
        [OneTimeSetUp]
        public static async Task Init()
        {
            HttpConsts.IncludeFieldsQueryStringName = "include-fields";

            using var test = ApiTester.Create<Startup>();
            var cs = test.Configuration.GetConnectionString("Database");
            if (await Database.Program.RunMigrator(cs, typeof(EmployeeControllerTest).Assembly, MigrationCommand.ResetAndAll.ToString()).ConfigureAwait(false) != 0)
                Assert.Fail("Database migration failed.");
        }

        [Test]
        public void A100_Get_NotFound()
        {
            using var test = ApiTester.Create<Startup>();

            test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(404.ToGuid()))
                .AssertNotFound();
        }

        [Test]
        public void A110_Get_Found()
        {
            using var test = ApiTester.Create<Startup>();

            test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(1.ToGuid()))
                .AssertOK()
                .Assert(new Employee
                {
                    Id = 1.ToGuid(),
                    Email = "w.jones@org.com",
                    FirstName = "Wendy",
                    LastName = "Jones",
                    Gender = "F",
                    Birthday = new DateTime(1985, 03, 18, 0, 0, 0, DateTimeKind.Unspecified),
                    StartDate = new DateTime(2000, 12, 11, 0, 0, 0, DateTimeKind.Unspecified),
                    PhoneNo = "(425) 612 8113"
                }, nameof(Employee.ETag));
        }

        [Test]
        public void A120_Get_NotModifed()
        {
            using var test = ApiTester.Create<Startup>();

            var e = test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(1.ToGuid()))
                .AssertOK()
                .GetValue<Employee>()!;

            test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(e.Id), requestOptions: new HttpRequestOptions { ETag = e.ETag })
                .AssertNotModified();
        }

        [Test]
        public void A130_Get_IncludeFields()
        {
            using var test = ApiTester.Create<Startup>();

            test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(1.ToGuid()), requestOptions: new HttpRequestOptions().Include("FirstName", "LastName"))
                .AssertOK()
                .AssertJson("{\"firstName\":\"Wendy\",\"lastName\":\"Jones\"}");
        }

        [Test]
        public void B100_GetAll_All()
        {
            using var test = ApiTester.Create<Startup>();

            var v = test.Controller<EmployeeController>()
                .Run(c => c.GetAllAsync())
                .AssertOK()
                .GetValue<EmployeeCollectionResult>();

            Assert.IsNotNull(v?.Collection);
            Assert.AreEqual(4, v!.Collection.Count);
            Assert.AreEqual(new string[] { "Browne", "Jones", "Smith", "Smithers" }, v.Collection.Select(x => x.LastName).ToArray());
        }

        [Test]
        public void B110_GetAll_Paging()
        {
            using var test = ApiTester.Create<Startup>();

            var v = test.Controller<EmployeeController>()
                .Run(c => c.GetAllAsync(), requestOptions: new HttpRequestOptions { Paging = PagingArgs.CreateSkipAndTake(1, 2, true) })
                .AssertOK()
                .GetValue<EmployeeCollectionResult>();

            Assert.IsNotNull(v?.Collection);
            Assert.AreEqual(2, v!.Collection.Count);
            Assert.AreEqual(new string[] { "Jones", "Smith" }, v.Collection.Select(x => x.LastName).ToArray());
            Assert.IsNotNull(v.Paging);
            Assert.AreEqual(4, v.Paging!.TotalCount);
        }

        [Test]
        public void B120_GetAll_PagingAndIncludeFields()
        {
            using var test = ApiTester.Create<Startup>();

            var v = test.Controller<EmployeeController>()
                .Run(c => c.GetAllAsync(), requestOptions: new HttpRequestOptions { Paging = PagingArgs.CreateSkipAndTake(1, 2) }.Include("lastname"))
                .AssertOK()
                .AssertJson("[ { \"lastName\": \"Jones\" }, { \"lastName\": \"Smith\" } ]")
                .GetValue<EmployeeCollectionResult>();

            Assert.IsNull(v!.Paging!.TotalCount); // No count requested.
        }

        [Test]
        public void C100_Create_Error()
        {
            using var test = ApiTester.Create<Startup>();

            var e = new Employee
            {
                FirstName = "Rebecca",
                LastName = "Smythe",
                Birthday = new DateTime(2000, 01, 01, 0, 0, 0, DateTimeKind.Unspecified),
                Gender = "Z",
                PhoneNo = "555 123 4567",
                StartDate = new DateTime(2020, 01, 08, 0, 0, 0, DateTimeKind.Unspecified)
            };

            test.Controller<EmployeeController>()
                .Run(c => c.CreateAsync(null!), e)
                .AssertErrors(
                    new ApiError("Email", "'Email' must not be empty."),
                    new ApiError("Gender", "'Gender' is invalid."));
        }

        [Test]
        public void C110_Create_Success()
        {
            using var test = ApiTester.Create<Startup>();

            var e = new Employee
            {
                FirstName = "Rebecca",
                LastName = "Smythe",
                Birthday = new DateTime(2000, 01, 01, 0, 0, 0, DateTimeKind.Unspecified),
                Gender = "M",
                PhoneNo = "555 123 4567",
                Email = "rs@email.com",
                StartDate = new DateTime(2020, 01, 08, 0, 0, 0, DateTimeKind.Unspecified)
            };

            var v = test.Controller<EmployeeController>()
                .Run(c => c.CreateAsync(null!), e)
                .AssertCreated()
                .Assert(e, "Id", "ETag")
                .AssertLocationHeader<Employee>(v => new Uri($"api/employees/{v!.Id}", UriKind.Relative))
                .GetValue<Employee>();

            // Do a GET to make sure it is in the database and all fields equal.
            test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(v!.Id))
                .AssertOK()
                .Assert(v);
        }

        [Test]
        public void D100_Update_Error()
        {
            using var test = ApiTester.Create<Startup>();

            var e = new Employee
            {
                FirstName = "Rebecca",
                LastName = "Smythe",
                Birthday = new DateTime(2000, 01, 01, 0, 0, 0, DateTimeKind.Unspecified),
                Gender = "M",
                PhoneNo = "555 123 4567",
                StartDate = new DateTime(2020, 01, 08, 0, 0, 0, DateTimeKind.Unspecified)
            };

            test.Controller<EmployeeController>()
                .Run(c => c.UpdateAsync(404.ToGuid(), null!), e)
                .AssertErrors(
                    new ApiError("Email", "'Email' must not be empty."));
        }

        [Test]
        public void D110_Update_NotFound()
        {
            using var test = ApiTester.Create<Startup>();

            var e = new Employee
            {
                FirstName = "Rebecca",
                LastName = "Smythe",
                Birthday = new DateTime(2000, 01, 01, 0, 0, 0, DateTimeKind.Unspecified),
                Gender = "M",
                PhoneNo = "555 123 4567",
                Email = "rs@email.com",
                StartDate = new DateTime(2020, 01, 08, 0, 0, 0, DateTimeKind.Unspecified)
            };

            test.Controller<EmployeeController>()
                .Run(c => c.UpdateAsync(404.ToGuid(), null!), e)
                .AssertNotFound();
        }

        [Test]
        public void D120_Update_Success()
        {
            using var test = ApiTester.Create<Startup>();

            // Get current.
            var v = test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(2.ToGuid()))
                .AssertOK()
                .GetValue<Employee>()!;

            // Update it.
            v.FirstName += "X";

            v = test.Controller<EmployeeController>()
                .Run(c => c.UpdateAsync(v.Id, null!), v)
                .AssertOK()
                .Assert(v, "ETag")
                .GetValue<Employee>()!;

            // Get again and check all.
            test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(v.Id))
                .AssertOK()
                .Assert(v);
        }

        [Test]
        public void D130_Update_ConcurrencyError()
        {
            using var test = ApiTester.Create<Startup>();

            // Get current.
            var v = test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(2.ToGuid()))
                .AssertOK()
                .GetValue<Employee>()!;

            // Update it with errant etag.
            v.FirstName += "X";
            v.ETag = "ZZZZZZZZZZZZ";

            test.Controller<EmployeeController>()
                .Run(c => c.UpdateAsync(v.Id, null!), v)
                .AssertPreconditionFailed();
        }

        [Test]
        public void E100_Delete()
        {
            using var test = ApiTester.Create<Startup>();

            // Get current.
            test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(2.ToGuid()))
                .AssertOK();

            // Delete it.
            test.Controller<EmployeeController>()
                .Run(c => c.DeleteAsync(2.ToGuid()))
                .AssertNoContent();

            // Must not exist.
            test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(2.ToGuid()))
                .AssertNotFound();

            // Delete it again; should appear as if deleted as operation is considered idempotent.
            test.Controller<EmployeeController>()
                .Run(c => c.DeleteAsync(2.ToGuid()))
                .AssertNoContent();
        }

        [Test]
        public void F100_Patch_NotFound()
        {
            using var test = ApiTester.Create<Startup>();

            test.Controller<EmployeeController>()
                .RunContent(c => c.PatchAsync(404.ToGuid(), null!), "{}", HttpConsts.MergePatchMediaTypeName)
                .AssertNotFound();
        }

        [Test]
        public void F110_Patch_Concurrency()
        {
            using var test = ApiTester.Create<Startup>();

            // Get current.
            var v = test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(4.ToGuid()))
                .AssertOK()
                .GetValue<Employee>()!;

            // Patch it with errant etag.
            v.FirstName += "X";

            test.Controller<EmployeeController>()
                .RunContent(c => c.PatchAsync(v.Id, null!), $"{{ \"firstName\": \"{v.FirstName}\" }}", new HttpRequestOptions { ETag = "ZZZZZZZZZZZZ" }, HttpConsts.MergePatchMediaTypeName)
                .AssertPreconditionFailed();
        }

        [Test]
        public void F120_Patch()
        {
            using var test = ApiTester.Create<Startup>();

            // Get current.
            var v = test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(4.ToGuid()))
                .AssertOK()
                .GetValue<Employee>()!;

            // Patch it with errant etag.
            v.FirstName += "X";

            v = test.Controller<EmployeeController>()
                .RunContent(c => c.PatchAsync(v.Id, null!), $"{{ \"firstName\": \"{v.FirstName}\" }}", new HttpRequestOptions { ETag = v.ETag }, HttpConsts.MergePatchMediaTypeName)
                .AssertOK()
                .Assert(v, "ETag")
                .GetValue<Employee>()!;

            // Get again and check all.
            test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(v.Id))
                .AssertOK()
                .Assert(v);
        }

        [Test]
        public void G100_Verify_NotFound()
        {
            using var test = ApiTester.Create<Startup>();

            test.Controller<EmployeeController>()
                .Run(c => c.VerifyAsync(404.ToGuid()))
                .AssertNotFound();
        }

        [Test]
        public void G100_Verify_Publish()
        {
            using var test = ApiTester.Create<Startup>();
            var imp = new InMemoryPublisher(test.Logger);

            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .Controller<EmployeeController>()
                .Run(c => c.VerifyAsync(1.ToGuid()))
                .AssertAccepted();

            Assert.AreEqual(1, imp.GetNames().Length);
            var e = imp.GetEvents("pendingVerifications");
            Assert.AreEqual(1, e.Length);
            ObjectComparer.Assert(new EmployeeVerificationRequest { Name = "Wendy", Age = 37, Gender = "F" }, e[0].Value);
        }
    }
}