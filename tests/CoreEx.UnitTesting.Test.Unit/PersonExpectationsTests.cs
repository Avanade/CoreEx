using CoreEx.AspNetCore.Test.Api.Entities;
using CoreEx.Entities;
using UnitTestEx.Expectations;

namespace CoreEx.UnitTesting.Test.Unit;

// Reuses the existing CoreEx.AspNetCore.Test.Api host (its Person/PersonController fixtures) rather than building
// a second companion host — matches the pattern CoreEx.AspNetCore.Test.Unit itself already uses for the same host.
public class PersonExpectationsTests : WithApiTester<CoreEx.AspNetCore.Test.Api.Program>
{
    private static Person NewPerson() => new() { Id = Guid.NewGuid().ToString(), FirstName = "Test", LastName = "Person", GenderSid = "M" };

    private Person CreatePerson(Person person) => Test.Http<Person>().Run(HttpMethod.Post, "api/people", person).AssertCreated().Value!;

    // ---------------------------------------------------------------------------------------------------------------
    // ExpectETag
    // ---------------------------------------------------------------------------------------------------------------

    [Test]
    public void Post_ExpectETag_Succeeds()
    {
        Test.Http<Person>()
            .ExpectETag()
            .Run(HttpMethod.Post, "api/people", NewPerson())
            .AssertCreated();
    }

    [Test]
    public void Get_NotFound_ExpectETag_Fails()
    {
        // No value is returned for a 404, so ExpectETag's "must have a non-null ETag" check has nothing to check against.
        // NUnit's Assert.Throws (not AwesomeAssertions' Should().Throw()) is required here — UnitTestEx's Implementor.AssertFail
        // records the failure via NUnit's own TestExecutionContext the moment it fires, independent of whether the resulting
        // exception is later caught; only Assert.Throws is recognized by NUnit as "this failure was the expected outcome".
        Assert.Throws<AssertionException>(() => Test.Http<Person>()
            .ExpectETag()
            .Run(HttpMethod.Get, $"api/people/{Guid.NewGuid()}"));
    }

    // ---------------------------------------------------------------------------------------------------------------
    // ExpectIdentifier
    // ---------------------------------------------------------------------------------------------------------------

    [Test]
    public void Post_ExpectIdentifier_NoArgs_Succeeds()
    {
        Test.Http<Person>()
            .ExpectIdentifier()
            .Run(HttpMethod.Post, "api/people", NewPerson())
            .AssertCreated();
    }

    [Test]
    public void Post_ExpectIdentifier_MatchingValue_Succeeds()
    {
        var person = NewPerson();

        Test.Http<Person>()
            .ExpectIdentifier(person.Id)
            .Run(HttpMethod.Post, "api/people", person)
            .AssertCreated();
    }

    [Test]
    public void Post_ExpectIdentifier_MismatchedValue_Fails()
    {
        Assert.Throws<AssertionException>(() => Test.Http<Person>()
            .ExpectIdentifier("a-completely-different-id")
            .Run(HttpMethod.Post, "api/people", NewPerson()));
    }

    // ---------------------------------------------------------------------------------------------------------------
    // ExpectChangeLogCreated / ExpectChangeLogUpdated
    //
    // PersonService does not itself stamp ChangeLog (it stores whatever the request supplied), so these drive the
    // ChangeLog explicitly via the request body rather than relying on server-side auto-stamping.
    // ---------------------------------------------------------------------------------------------------------------

    [Test]
    public void Post_ExpectChangeLogCreated_Succeeds()
    {
        var person = NewPerson();
        person.ChangeLog = new ChangeLog { CreatedBy = "test-user", CreatedOn = DateTimeOffset.UtcNow };

        Test.Http<Person>()
            .ExpectChangeLogCreated(createdBy: "test-user")
            .Run(HttpMethod.Post, "api/people", person)
            .AssertCreated();
    }

    [Test]
    public void Post_ExpectChangeLogCreated_Fails_WhenChangeLogMissing()
    {
        // NewPerson() has no ChangeLog set at all.
        Assert.Throws<AssertionException>(() => Test.Http<Person>()
            .ExpectChangeLogCreated()
            .Run(HttpMethod.Post, "api/people", NewPerson()));
    }

    [Test]
    public void Post_ExpectChangeLogCreated_Fails_WhenExplicitEmptyCreatedByDoesNotMatchActual()
    {
        // Regression: an explicitly-empty createdBy must not fall back to SubscribedBase.IsMatch's "null/empty pattern
        // matches anything" glob-routing semantics - it must be treated as an explicit expectation of an empty actual value.
        var person = NewPerson();
        person.ChangeLog = new ChangeLog { CreatedBy = "test-user", CreatedOn = DateTimeOffset.UtcNow };

        Assert.Throws<AssertionException>(() => Test.Http<Person>()
            .ExpectChangeLogCreated(createdBy: string.Empty)
            .Run(HttpMethod.Post, "api/people", person));
    }

    [Test]
    public void Put_ExpectChangeLogUpdated_Succeeds()
    {
        var created = CreatePerson(NewPerson());
        created.LastName = "Updated";
        created.ChangeLog = new ChangeLog { UpdatedBy = "test-user", UpdatedOn = DateTimeOffset.UtcNow };

        Test.Http<Person>()
            .ExpectChangeLogUpdated(updatedBy: "test-user")
            .Run(HttpMethod.Put, $"api/people/{created.Id}", created)
            .AssertOK();
    }

    [Test]
    public void Put_ExpectChangeLogUpdated_Fails_WhenChangeLogMissing()
    {
        var created = CreatePerson(NewPerson());
        created.LastName = "Updated";

        // created.ChangeLog is left untouched (null) — no UpdatedBy/UpdatedOn supplied.
        Assert.Throws<AssertionException>(() => Test.Http<Person>()
            .ExpectChangeLogUpdated()
            .Run(HttpMethod.Put, $"api/people/{created.Id}", created));
    }
}
