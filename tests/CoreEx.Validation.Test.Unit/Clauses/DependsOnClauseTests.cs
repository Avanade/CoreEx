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

    [Test]
    public void DependsOn_WhenPredicateIsTrue_ValidatesTheProperty()
    {
        var v = Validator.Create<Person>()
            .HasProperty(p => p.FareClass, c => c.Mandatory().DependsOn(p => p.SegmentType, s => s!.IsFlight));

        v.ValidateAsError(new Person { SegmentType = new SegmentType { IsFlight = true } }, "fareClass", "Fare class is required.");
        v.ValidateAsSuccess(new Person { SegmentType = new SegmentType { IsFlight = true }, FareClass = "Y" });
    }

    [Test]
    public void DependsOn_WhenPredicateIsFalse_SkipsThePropertyRules()
    {
        var v = Validator.Create<Person>()
            .HasProperty(p => p.FareClass, c => c.Mandatory().DependsOn(p => p.SegmentType, s => s!.IsFlight));

        v.ValidateAsSuccess(new Person { SegmentType = new SegmentType { IsFlight = false } });
    }

    [Test]
    public void DependsOn_AndWhenEntityClausesAreCombined()
    {
        var v = Validator.Create<Person>()
            .HasProperty(p => p.FareClass, c => c.Mandatory()
                .WhenEntity(p => p.IsEnabled)
                .DependsOn(p => p.SegmentType, s => s!.IsFlight));

        v.ValidateAsError(new Person { IsEnabled = true, SegmentType = new SegmentType { IsFlight = true } }, "fareClass", "Fare class is required.");
        v.ValidateAsSuccess(new Person { IsEnabled = false, SegmentType = new SegmentType { IsFlight = true } });
        v.ValidateAsSuccess(new Person { IsEnabled = true, SegmentType = new SegmentType { IsFlight = false } });
    }

    [Test]
    public void DependsOn_WhenDependencyIsMissing_SkipsPredicateAndPropertyRules()
    {
        var predicateInvoked = false;
        var v = Validator.Create<Person>()
            .HasProperty(p => p.FareClass, c => c.Mandatory().DependsOn(p => p.SegmentType, s =>
            {
                predicateInvoked = true;
                return s!.IsFlight;
            }));

        v.ValidateAsSuccess(new Person());
        predicateInvoked.Should().BeFalse();
    }

    [Test]
    public void DependsOn_WhenDependencyHasValidationError_SkipsPredicateAndPropertyRules()
    {
        var predicateInvoked = false;
        var v = Validator.Create<Person>()
            .HasProperty(p => p.SegmentType, c => c.Error("Segment type is invalid.").WhenValue(value => value != null))
            .HasProperty(p => p.FareClass, c => c.Mandatory().DependsOn(p => p.SegmentType, s =>
            {
                predicateInvoked = true;
                return s!.IsFlight;
            }));

        v.ValidateAsError(new Person { SegmentType = new SegmentType { IsFlight = true } }, "segmentType", "Segment type is invalid.");
        predicateInvoked.Should().BeFalse();
    }

    [Test]
    public void DependsOn_WhenPredicateIsNull_Throws()
    {
        var v = Validator.Create<Person>();
        var ex = Assert.Throws<ArgumentNullException>(() => v.HasProperty(p => p.FareClass,
            c => c.Mandatory().DependsOn(p => p.SegmentType, (Func<SegmentType?, bool>)null!)));

        ex!.ParamName.Should().Be("andAlso");
    }

    private class Person
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FareClass { get; set; }
        public SegmentType? SegmentType { get; set; }
        public bool IsEnabled { get; set; }
    }

    private class SegmentType
    {
        public bool IsFlight { get; set; }
    }
}
