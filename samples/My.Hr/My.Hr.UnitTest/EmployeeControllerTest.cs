using CoreEx.Entities;
using CoreEx.Events;
using CoreEx.Http;
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
using UnitTestEx.Expectations;
using UnitTestEx.NUnit;
using DbEx;
using DbEx.Migration;
using DbEx.SqlServer.Migration;

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
            var args = Database.Program.ConfigureMigrationArgs(new MigrationArgs(MigrationCommand.ResetAndDatabase, cs)).AddAssembly<EmployeeControllerTest>();
            var (Success, Output) = await new SqlServerMigration(args).MigrateAndLogAsync().ConfigureAwait(false);
            if (!Success)
                Assert.Fail(Output);
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

            var resp = test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(1.ToGuid()))
                .AssertOK()
                .AssertValue(new Employee
                {
                    Id = 1.ToGuid(),
                    Email = "w.jones@org.com",
                    FirstName = "Wendy",
                    LastName = "Jones",
                    Gender = "F",
                    Birthday = new DateTime(1985, 03, 18, 0, 0, 0, DateTimeKind.Unspecified),
                    StartDate = new DateTime(2000, 12, 11, 0, 0, 0, DateTimeKind.Unspecified),
                    PhoneNo = "(425) 612 8113"
                }, nameof(Employee.ETag))
                .Response;

            // Also, validate the context header messages.
            var result = HttpResult.CreateAsync(resp).GetAwaiter().GetResult();
            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Messages, Is.Not.Null);
            });
            Assert.That(result.Messages, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(result.Messages[0].Type, Is.EqualTo(MessageType.Warning));
                Assert.That(result.Messages[0].Text, Is.EqualTo("Employee is considered old."));
            });
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

            Assert.That(v?.Items, Is.Not.Null);
            Assert.That(v!.Items, Has.Count.EqualTo(4));
            Assert.That(v.Items.Select(x => x.LastName).ToArray(), Is.EqualTo(new string[] { "Browne", "Jones", "Smith", "Smithers" }));
        }

        [Test]
        public void B110_GetAll_Paging()
        {
            using var test = ApiTester.Create<Startup>();

            var x = TestSetUp.Extensions;

            var v = test.Controller<EmployeeController>()
                .Run(c => c.GetAllAsync(), requestOptions: HttpRequestOptions.Create(PagingArgs.CreateSkipAndTake(1, 2, true)))
                .AssertOK()
                .GetValue<EmployeeCollectionResult>();

            Assert.That(v?.Items, Is.Not.Null);
            Assert.That(v!.Items, Has.Count.EqualTo(2));
            Assert.That(v.Items.Select(x => x.LastName).ToArray(), Is.EqualTo(new string[] { "Jones", "Smith" }));
            Assert.That(v.Paging, Is.Not.Null);
            Assert.That(v.Paging!.TotalCount, Is.EqualTo(4));
        }

        [Test]
        public void B120_GetAll_PagingAndIncludeFields()
        {
            using var test = ApiTester.Create<Startup>();

            var v = test.Controller<EmployeeController>()
                .Run(c => c.GetAllAsync(), requestOptions: HttpRequestOptions.Create(PagingArgs.CreateSkipAndTake(1, 2)).Include("lastname"))
                .AssertOK()
                .AssertJson("[ { \"lastName\": \"Jones\" }, { \"lastName\": \"Smith\" } ]")
                .GetValue<EmployeeCollectionResult>();

            Assert.That(v!.Paging!.TotalCount, Is.Null); // No count requested.
        }

        [Test]
        public void B120_GetAll_Filter_LastName()
        {
            using var test = ApiTester.Create<Startup>();

            var v = test.Controller<EmployeeController>()
                .Run(c => c.GetAllAsync(), requestOptions: HttpRequestOptions.Create().Filter("startswith(lastname, 's')"))
                .AssertOK()
                .GetValue<EmployeeCollectionResult>();

            Assert.That(v?.Items, Is.Not.Null);
            Assert.That(v!.Items, Has.Count.EqualTo(2));
            Assert.That(v.Items.Select(x => x.LastName).ToArray(), Is.EqualTo(new string[] { "Smith", "Smithers" }));
        }

        [Test]
        public void B130_GetAll_Filter_StartDateAndGenders_OrderBy_FirstName()
        {
            using var test = ApiTester.Create<Startup>();

            var v = test.Controller<EmployeeController>()
                .Run(c => c.GetAllAsync(), requestOptions: HttpRequestOptions.Create().Filter("startdate ge 2010-01-01 and gender in ('m','f')").OrderBy("lastname desc"))
                .AssertOK()
                .GetValue<EmployeeCollectionResult>();

            Assert.That(v?.Items, Is.Not.Null);
            Assert.That(v!.Items, Has.Count.EqualTo(2));
            Assert.That(v.Items.Select(x => x.LastName).ToArray(), Is.EqualTo(new string[] { "Smith", "Browne" }));
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
                .AssertValue(e, "Id", "ETag")
                .AssertLocationHeader<Employee>(v => new Uri($"api/employees/{v!.Id}", UriKind.Relative))
                .AssertLocationHeaderContains("api/employees") // Just for kicks testing both types work
                .GetValue<Employee>();

            // Do a GET to make sure it is in the database and all fields equal.
            test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(v!.Id))
                .AssertOK()
                .AssertValue(v);
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
                .AssertValue(v, "ETag")
                .GetValue<Employee>()!;

            // Get again and check all.
            test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(v.Id))
                .AssertOK()
                .AssertValue(v);
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
                .RunContent(c => c.PatchAsync(v.Id, null!), $"{{ \"firstName\": \"{v.FirstName}\" }}", HttpConsts.MergePatchMediaTypeName, new HttpRequestOptions { ETag = "ZZZZZZZZZZZZ" })
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
                .RunContent(c => c.PatchAsync(v.Id, null!), $"{{ \"firstName\": \"{v.FirstName}\" }}", HttpConsts.MergePatchMediaTypeName, new HttpRequestOptions { ETag = v.ETag })
                .AssertOK()
                .AssertValue(v, "ETag")
                .GetValue<Employee>()!;

            // Get again and check all.
            test.Controller<EmployeeController>()
                .Run(c => c.GetAsync(v.Id))
                .AssertOK()
                .AssertValue(v);
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

            Assert.That(imp.GetNames(), Has.Length.EqualTo(1));
            var e = imp.GetEvents("pendingVerifications");
            Assert.That(e, Has.Length.EqualTo(1));
            ObjectComparer.Assert(new EmployeeVerificationRequest { Name = "Wendy", Age = 39, Gender = "F" }, e[0].Value);
        }

        [Test]
        public void G100_Verify_Publish_WithExpectations()
        {
            using var test = ApiTester.Create<Startup>();
            test.UseExpectedEvents()
                .Controller<EmployeeController>()
                .ExpectDestinationEvent("pendingVerifications", new EventData<EmployeeVerificationRequest> { Value = new EmployeeVerificationRequest { Name = "Wendy", Age = 39, Gender = "F" } })
                .ExpectStatusCode(System.Net.HttpStatusCode.Accepted)
                .Run(c => c.VerifyAsync(1.ToGuid()));
        }
    }
}