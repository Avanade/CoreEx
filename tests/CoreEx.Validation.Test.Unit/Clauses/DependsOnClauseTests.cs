namespace CoreEx.Validation.Test.Unit.Clauses;

[TestFixture]
public class DependsOnClauseTests
{
    [Test]
    public void DependsOn()
    {
        var v = Validator.Create<Person>().HasProperty(p => p.FirstName, c => c.Length(4).DependsOn(p => p.LastName));

        v.ValidateAsSuccess(new Person());
        v.ValidateAsSuccess(new Person { FirstName = "Joh" });
        v.ValidateAsSuccess(new Person { FirstName = "John", LastName = "Doe" });

        v.ValidateAsError(new Person { FirstName = "Joh", LastName = "Doe" }, "firstName", "First name must be exactly 4 character(s) in length.");
    }

    private class Person
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}