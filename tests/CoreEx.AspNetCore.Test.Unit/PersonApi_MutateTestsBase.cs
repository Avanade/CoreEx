using CoreEx.AspNetCore.Test.Api.Entities;
using CoreEx.AspNetCore.Test.Api.Services;

namespace CoreEx.AspNetCore.Test.Unit;

[Parallelizable]
public abstract class PersonApi_MutateTestsBase : WithApiTester<Api.Program>
{
    public abstract string Route { get; }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        PersonService.Reset();
        PersonService2.Reset();
    }

    [Test]
    public void Create_NoValue()
    {
        Test.Http<Person>()
            .Run(HttpMethod.Post, $"{Route}")
            .AssertBadRequest()
            .AssertProblemDetailsTitle("Request body is invalid: Unable to read the request as JSON because the request content type '' is not a known JSON content type.");
    }

    [Test]
    public void Create_Success()
    {
        // Create a new entity.
        var v = Test.Http<Person>()
            .Run(HttpMethod.Post, $"{Route}", new Person
            {
                Id = "5",
                FirstName = "Charlie",
                LastName = "Brown",
                Birthday = new DateOnly(2000, 1, 1),
                GenderSid = "M"
            })
            .AssertCreated()
            .AssertValue(new Person
            {
                Id = "5",
                FirstName = "Charlie",
                LastName = "Brown",
                Birthday = new DateOnly(2000, 1, 1),
                GenderSid = "M",
            }, "etag")
            .AssertETagHeader()
            .AssertLocationHeader(p => new Uri($"{Route}/{p!.Id}", UriKind.Relative))
            .Value;

        // Verify the entity was created.
        Test.Http<Person>()
            .Run(HttpMethod.Get, $"{Route}/5")
            .AssertOK()
            .AssertValue(v);
    }

    [Test]
    public void Create_Duplicate()
    {
        Test.Http<Person>()
            .Run(HttpMethod.Post, $"{Route}", new Person
            {
                Id = "1",
                FirstName = "Charlie",
                LastName = "Brown",
                Birthday = new DateOnly(2000, 1, 1),
                GenderSid = "M",
            })
            .AssertConflict();
    }

    [Test]
    public void Update_Success()
    {
        // Pre-read the entity to get the etag.
        var v = Test.Http<Person>()
            .Run(HttpMethod.Get, $"{Route}/1")
            .AssertOK()
            .Value;

        v.Should().NotBeNull();

        // Update the entity.
        v.FirstName = "Jane";
        v.LastName = "Doe";
        v = Test.Http<Person>()
            .Run(HttpMethod.Put, $"{Route}/1", v, r => r.WithIfMatch(v.ETag))
            .AssertOK()
            .AssertValue(v, "etag")
            .Value;

        // Verify the update.
        Test.Http<Person?>()
            .Run(HttpMethod.Get, $"{Route}/1")
            .AssertOK()
            .AssertValue(v);
    }

    [Test]
    public void Update_NotFound()
    {
        Test.Http<Person>()
            .Run(HttpMethod.Put, $"{Route}/0", new Person
            {
                Id = "0",
                FirstName = "Charlie",
                LastName = "Brown",
                Birthday = new DateOnly(2000, 1, 1),
                GenderSid = "M",
                ETag = "oops"
            })
            .AssertNotFound();
    }

    [Test]
    public void Update_Concurrency()
    {
        // Pre-read the entity to get the etag.
        var v = Test.Http<Person>()
            .Run(HttpMethod.Get, $"{Route}/1")
            .AssertOK()
            .Value!;

        // Attempt to update the entity with an invalid etag.
        Test.Http<Person>()
            .Run(HttpMethod.Put, $"{Route}/1", v, r => r.WithIfMatch("oops"))
            .AssertPreconditionFailed();
    }

    [Test]
    public void Patch_Success()
    {
        // Pre-read the entity to get the etag.
        var v = Test.Http<Person>()
            .Run(HttpMethod.Get, $"{Route}/1")
            .AssertOK()
            .Value;

        v.Should().NotBeNull();
        v.LastName += " Jr.";

        // Patch the entity.
        v = Test.Http<Person>()
            .Run(HttpMethod.Patch, $"{Route}/1", new { v.LastName }, r => r.WithMergePatchJsonContentType().WithIfMatch(v.ETag))
            .AssertOK()
            .AssertValue(v, "etag")
            .Value;

        // Verify the patch.
        Test.Http<Person?>()
            .Run(HttpMethod.Get, $"{Route}/1")
            .AssertOK()
            .AssertValue(v);
    }

    [Test]
    public void Patch_NotFound()
    {
        Test.Http<Person>()
            .Run(HttpMethod.Patch, $"{Route}/0", new { FirstName = "Charlie" }, r => r.WithMergePatchJsonContentType())
            .AssertNotFound();
    }

    [Test]
    public void Patch_Concurrency()
    {
        // Pre-read the entity to get the etag.
        var v = Test.Http<Person>()
            .Run(HttpMethod.Get, $"{Route}/1")
            .AssertOK()
            .Value!;

        v.LastName += " Jr.";

        // Attempt to patch the entity with an invalid etag.
        Test.Http<Person>()
            .Run(HttpMethod.Patch, $"{Route}/1", new { v.LastName }, r => r.WithMergePatchJsonContentType().WithIfMatch("oops"))
            .AssertPreconditionFailed();
    }

    [Test]
    public void Delete_Success()
    {
        // Pre-read the entity to verify it exists.
        Test.Http()
            .Run(HttpMethod.Get, $"{Route}/4")
            .AssertOK();

        // Delete the entity.
        Test.Http()
            .Run(HttpMethod.Delete, $"{Route}/4")
            .AssertNoContent();

        // Verify the entity is deleted.
        Test.Http()
            .Run(HttpMethod.Get, $"{Route}/4")
            .AssertNotFound();
    }

    [Test]
    public void Delete_NotFound()
    {
        // Verify the entity does not exist.
        Test.Http()
            .Run(HttpMethod.Get, $"{Route}/0")
            .AssertNotFound();

        // Delete the entity - should be idempotent.
        Test.Http()
            .Run(HttpMethod.Delete, $"{Route}/0")
            .AssertNoContent();
    }
}