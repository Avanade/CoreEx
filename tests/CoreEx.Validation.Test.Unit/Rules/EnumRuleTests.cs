namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class EnumRuleTests
{
    [Test]
    public void Enum()
    {
        DayOfWeek.Monday.Validator(c => c.Enum()).ValidateAsSuccess();
        ((DayOfWeek)100).Validator(c => c.Enum()).ValidateAsError("is invalid.");
        ((DayOfWeek)(-1)).Validator(c => c.Enum()).ValidateAsError("is invalid.");

        DayOfWeek.Monday.Validator(c => c.Enum((List<DayOfWeek>)null!)).ValidateAsSuccess();
        DayOfWeek.Monday.Validator(c => c.Enum(DayOfWeek.Monday, DayOfWeek.Tuesday)).ValidateAsSuccess();
        DayOfWeek.Tuesday.Validator(c => c.Enum(new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday })).ValidateAsSuccess();
        DayOfWeek.Wednesday.Validator(c => c.Enum(DayOfWeek.Monday, DayOfWeek.Tuesday)).ValidateAsError("is invalid.");
        DayOfWeek.Wednesday.Validator(c => c.Enum(new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday })).ValidateAsError("is invalid.");

        ((DayOfWeek?)DayOfWeek.Monday).Validator(c => c.Enum()).ValidateAsSuccess();
        ((DayOfWeek?)DayOfWeek.Monday).Validator(c => c.Enum(DayOfWeek.Monday, DayOfWeek.Tuesday)).ValidateAsSuccess();
        ((DayOfWeek?)DayOfWeek.Wednesday).Validator(c => c.Enum(DayOfWeek.Monday, DayOfWeek.Tuesday)).ValidateAsError("is invalid.");

        DayOfWeek? dow = null;
        dow.Validator(c => c.Enum()).ValidateAsSuccess();

        dow = DayOfWeek.Monday;
        dow.Validator(c => c.Enum()).ValidateAsSuccess();
        dow.Validator(c => c.Enum(new List<DayOfWeek> { DayOfWeek.Wednesday, DayOfWeek.Thursday })).ValidateAsError("is invalid.");
    }

    [Test]
    public void Enum_IEnumerable_Allowed_OnlyEnumeratedOnce()
    {
        // Regression: the IEnumerable<T> overload used to re-materialize (re-enumerate/re-allocate) the allowed
        // sequence on every validation call instead of once at configuration time.
        var enumerationCount = 0;

        IEnumerable<DayOfWeek> Allowed()
        {
            enumerationCount++;
            yield return DayOfWeek.Monday;
            yield return DayOfWeek.Tuesday;
        }

        var v = DayOfWeek.Monday.Validator(c => c.Enum(Allowed()));
        v.ValidateAsSuccess();
        v.ValidateAsSuccess();

        enumerationCount.Should().Be(1);
    }

    [Test]
    public void EnumString()
    {
        "Monday".Validator(c => c.Enum(e => e.With<DayOfWeek>())).ValidateAsSuccess();
        "monday".Validator(c => c.Enum(e => e.With<DayOfWeek>())).ValidateAsError("is invalid.");
        "monday".Validator(c => c.Enum(e => e.With<DayOfWeek>().IgnoreCase())).ValidateAsSuccess();
        "monday".Validator(c => c.Enum(e => e.With<DayOfWeek>().IgnoreCase())).ValidateAsSuccess();

        ((string?)null).Validator(c => c.Enum(e => e.With<DayOfWeek>())).ValidateAsSuccess();
        ((string?)"monday").Validator(c => c.Enum(e => e.With(DayOfWeek.Monday, DayOfWeek.Tuesday).IgnoreCase())).ValidateAsSuccess();
        "friday".Validator(c => c.Enum(e => e.With(DayOfWeek.Monday, DayOfWeek.Tuesday).IgnoreCase())).ValidateAsError("is invalid.");
    }

    public class AppointmentValidator : Validator<Appointment>
    {
        public AppointmentValidator()
        {
            Property(p => p.DayOfWeek).Enum();
            //Property(p => p.AlternateDay).Enum();
        }
    }

    public class Appointment
    {
        public DayOfWeek DayOfWeek { get; set; }

        public DayOfWeek? AlternateDay { get; set; }
    }
}