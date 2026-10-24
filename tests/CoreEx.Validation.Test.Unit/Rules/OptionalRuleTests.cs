using CoreEx.Entities;

namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class OptionalRuleTests
{
    [Test]
    public void Unwrap_SkipsUnspecifiedValues_AndValidatesSpecifiedValues()
    {
        var validator = Validator.Create<PatchRequest>()
            .HasProperty(x => x.Notes, rule => rule.Unwrap().String(3));

        validator.ValidateAsSuccess(new PatchRequest());
        validator.ValidateAsSuccess(new PatchRequest { Notes = "abc" });
        validator.ValidateAsError(new PatchRequest { Notes = "long" }, "notes", "Notes must not exceed 3 character(s) in length.");
    }

    [Test]
    public void Unwrap_ValidatesNullableStringsWithoutAnnotationWarnings()
    {
        var validator = Validator.Create<PatchRequest>()
            .HasProperty(x => x.NullableNotes, rule => rule.Unwrap().String(3));

        validator.ValidateAsSuccess(new PatchRequest());
        validator.ValidateAsSuccess(new PatchRequest { NullableNotes = Optional<string?>.Specified(null) });
        validator.ValidateAsError(new PatchRequest { NullableNotes = "long" }, "nullableNotes", "Nullable notes must not exceed 3 character(s) in length.");
    }

    [Test]
    public void Unwrap_StringOverloadsAndSubsequentRules_AcceptNullableStrings()
    {
        var regex = new System.Text.RegularExpressions.Regex("^[a-z]+$", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(1));
        var common = Validator.CreateCommon<string>(rule => rule.String(3));
        var validator = Validator.Create<PatchRequest>()
            .HasProperty(x => x.NullableNotes, rule => rule.Unwrap().Common(common)
                .String(3).String(1, 3).String(_ => 1, _ => 3, _ => regex)
                .String(regex).Matches(regex).Length(3).MinimumLength(1).MaximumLength(3)
                .Between("aaa", "zzz").Common(common));

        validator.ValidateAsSuccess(new PatchRequest());
        validator.ValidateAsSuccess(new PatchRequest { NullableNotes = Optional<string?>.Specified(null) });
        validator.ValidateAsSuccess(new PatchRequest { NullableNotes = "abc" });
        validator.ValidateAsError(new PatchRequest { NullableNotes = "long" }, "nullableNotes", "Nullable notes must not exceed 3 character(s) in length.");
    }

    [Test]
    public void Unwrap_CommonAndBetween_HandleNullableAndNonNullableNumbers()
    {
        var common = Validator.CreateCommon<int>(rule => rule.Between(0, 3));
        var numberValidator = Validator.Create<PatchRequest>()
            .HasProperty(x => x.Count, rule => rule.Unwrap().Common(common).Between(0, 3));
        var nullableValidator = Validator.Create<PatchRequest>()
            .HasProperty(x => x.NullableCount, rule => rule.Unwrap().Common(common).Between(0, 3));

        numberValidator.ValidateAsSuccess(new PatchRequest());
        numberValidator.ValidateAsSuccess(new PatchRequest { Count = 0 });
        numberValidator.ValidateAsSuccess(new PatchRequest { Count = 2 });
        numberValidator.ValidateAsError(new PatchRequest { Count = 4 }, "count", "Count must be between '0' and '3'.");
        nullableValidator.ValidateAsSuccess(new PatchRequest());
        nullableValidator.ValidateAsSuccess(new PatchRequest { NullableCount = Optional<int?>.Specified(null) });
        nullableValidator.ValidateAsSuccess(new PatchRequest { NullableCount = 0 });
        nullableValidator.ValidateAsSuccess(new PatchRequest { NullableCount = 2 });
        nullableValidator.ValidateAsError(new PatchRequest { NullableCount = 4 }, "nullableCount", "Nullable count must be between '0' and '3'.");
    }

    [Test]
    public void Unwrap_NullableNumberRange_PreservesNullAndZero()
    {
        var validator = Validator.Create<PatchRequest>()
            .HasProperty(x => x.NullableCount, rule => rule.Unwrap().Between(0, 3));

        validator.ValidateAsSuccess(new PatchRequest());
        validator.ValidateAsSuccess(new PatchRequest { NullableCount = Optional<int?>.Specified(null) });
        validator.ValidateAsSuccess(new PatchRequest { NullableCount = 0 });
        validator.ValidateAsError(new PatchRequest { NullableCount = 4 }, "nullableCount", "Nullable count must be between '0' and '3'.");
    }

    [Test]
    public void Specified_AcceptsSuppliedDefaultsForAllFourTypes()
    {
        var validator = Validator.Create<PatchRequest>()
            .HasProperty(x => x.Notes, rule => rule.Specified())
            .HasProperty(x => x.NullableNotes, rule => rule.Specified())
            .HasProperty(x => x.Count, rule => rule.Specified())
            .HasProperty(x => x.NullableCount, rule => rule.Specified());

        validator.ValidateAsSuccess(new PatchRequest
        {
            Notes = "",
            NullableNotes = Optional<string?>.Specified(null),
            Count = 0,
            NullableCount = Optional<int?>.Specified(null)
        });
    }

    [Test]
    public void Unwrap_CommonMandatory_PreservesNullableNumericDefaultSemantics()
    {
        var common = Validator.CreateCommon<int>(rule => rule.Mandatory());
        var validator = Validator.Create<PatchRequest>()
            .HasProperty(x => x.NullableCount, rule => rule.Unwrap().Common(common));

        validator.ValidateAsSuccess(new PatchRequest());
        validator.ValidateAsError(new PatchRequest { NullableCount = Optional<int?>.Specified(null) }, "nullableCount", "Nullable count is required.");
        validator.ValidateAsError(new PatchRequest { NullableCount = 0 }, "nullableCount", "Nullable count is required.");
        validator.ValidateAsSuccess(new PatchRequest { NullableCount = 2 });
    }

    [Test]
    public void Unwrap_NullableNumberAfterRange_PreservesDefaultSemantics()
    {
        var validator = Validator.Create<PatchRequest>()
            .HasProperty(x => x.NullableCount, rule => rule.Unwrap().Between(0, 3).Mandatory());
        var notNull = Validator.Create<PatchRequest>()
            .HasProperty(x => x.NullableCount, rule => rule.Unwrap().NotNull());

        validator.ValidateAsError(new PatchRequest { NullableCount = 0 }, "nullableCount", "Nullable count is required.");
        validator.ValidateAsError(new PatchRequest { NullableCount = Optional<int?>.Specified(null) }, "nullableCount", "Nullable count is required.");
        validator.ValidateAsSuccess(new PatchRequest { NullableCount = 2 });
        notNull.ValidateAsSuccess(new PatchRequest { NullableCount = 0 });
        notNull.ValidateAsError(new PatchRequest { NullableCount = Optional<int?>.Specified(null) }, "nullableCount", "Nullable count is required.");
    }

    [Test]
    public void SpecifiedAndUnwrap_PreservePresenceAndMandatorySemanticsForAllFourTypes()
    {
        var text = Validator.Create<PatchRequest>().HasProperty(x => x.Notes, rule => rule.Specified().Unwrap().Mandatory().String(3));
        var nullableText = Validator.Create<PatchRequest>().HasProperty(x => x.NullableNotes, rule => rule.Specified().Unwrap().Mandatory().String(3));
        var number = Validator.Create<PatchRequest>().HasProperty(x => x.Count, rule => rule.Specified().Unwrap().Mandatory());
        var nullableNumber = Validator.Create<PatchRequest>().HasProperty(x => x.NullableCount, rule => rule.Specified().Unwrap().Mandatory());

        text.ValidateAsError(new PatchRequest(), "notes", "Notes is required.");
        nullableText.ValidateAsError(new PatchRequest(), "nullableNotes", "Nullable notes is required.");
        number.ValidateAsError(new PatchRequest(), "count", "Count is required.");
        nullableNumber.ValidateAsError(new PatchRequest(), "nullableCount", "Nullable count is required.");
        text.ValidateAsSuccess(new PatchRequest { Notes = "abc" });
        text.ValidateAsError(new PatchRequest { Notes = "" }, "notes", "Notes is required.");
        nullableText.ValidateAsSuccess(new PatchRequest { NullableNotes = "abc" });
        nullableText.ValidateAsError(new PatchRequest { NullableNotes = Optional<string?>.Specified(null) }, "nullableNotes", "Nullable notes is required.");
        number.ValidateAsError(new PatchRequest { Count = 0 }, "count", "Count is required.");
        number.ValidateAsSuccess(new PatchRequest { Count = 2 });
        nullableNumber.ValidateAsError(new PatchRequest { NullableCount = Optional<int?>.Specified(null) }, "nullableCount", "Nullable count is required.");
        nullableNumber.ValidateAsError(new PatchRequest { NullableCount = 0 }, "nullableCount", "Nullable count is required.");
        nullableNumber.ValidateAsSuccess(new PatchRequest { NullableCount = 2 });
    }

    [Test]
    public void Unwrap_MandatoryValidatesSpecifiedNullAndDefaultValues()
    {
        var textValidator = Validator.Create<PatchRequest>()
            .HasProperty(x => x.NullableNotes, rule => rule.Unwrap().Mandatory());
        var numberValidator = Validator.Create<PatchRequest>()
            .HasProperty(x => x.Count, rule => rule.Unwrap().Mandatory());

        textValidator.ValidateAsSuccess(new PatchRequest());
        textValidator.ValidateAsError(new PatchRequest { NullableNotes = Optional<string?>.Specified(null) }, "nullableNotes", "Nullable notes is required.");
        numberValidator.ValidateAsSuccess(new PatchRequest());
        numberValidator.ValidateAsError(new PatchRequest { Count = 0 }, "count", "Count is required.");
    }

    [Test]
    public void Specified_RequiresPresenceWithoutRejectingSpecifiedNull()
    {
        var validator = Validator.Create<PatchRequest>()
            .HasProperty(x => x.NullableNotes, rule => rule.Specified());

        validator.ValidateAsError(new PatchRequest(), "nullableNotes", "Nullable notes is required.");
        validator.ValidateAsSuccess(new PatchRequest { NullableNotes = Optional<string?>.Specified(null) });
    }

    [Test]
    public void Specified_ChainsInnerValidation()
    {
        var validator = Validator.Create<PatchRequest>()
            .HasProperty(x => x.Notes, rule => rule.Specified().Unwrap().String(3));

        validator.ValidateAsError(new PatchRequest(), "notes", "Notes is required.");
        validator.ValidateAsSuccess(new PatchRequest { Notes = "abc" });
        validator.ValidateAsError(new PatchRequest { Notes = "long" }, "notes", "Notes must not exceed 3 character(s) in length.");
    }

    [Test]
    public void Specified_SupportsCustomMessagesAndConditionalClauses()
    {
        var validator = Validator.Create<PatchRequest>()
            .HasProperty(x => x.Notes, rule => rule.Specified().WithMessage("Supply notes.").WhenEntity(x => x.Count.IsSpecified));

        validator.ValidateAsSuccess(new PatchRequest());
        validator.ValidateAsError(new PatchRequest { Count = 0 }, "notes", "Supply notes.");
        validator.ValidateAsSuccess(new PatchRequest { Count = 0, Notes = "abc" });
    }

    [Test]
    public void Unwrap_PreservesOuterPropertyTextAndPath()
    {
        var validator = Validator.Create<PatchRequest>()
            .HasProperty(x => x.Notes, rule => rule.WithText("Additional notes").Unwrap().String(3));

        validator.ValidateAsError(new PatchRequest { Notes = "long" }, "notes", "Additional notes must not exceed 3 character(s) in length.");
    }

    [Test]
    public void Unwrap_ValidatesOptionalCollections()
    {
        var validator = Validator.Create<PatchRequest>()
            .HasProperty(x => x.Numbers, rule => rule.Unwrap().Collection(maxCount: 2));

        validator.ValidateAsSuccess(new PatchRequest());
        validator.ValidateAsSuccess(new PatchRequest { Numbers = new List<int> { 1, 2 } });
        validator.ValidateAsError(new PatchRequest { Numbers = new List<int> { 1, 2, 3 } }, "numbers", "Numbers must not exceed 2 item(s).");
    }

    public class PatchRequest
    {
        public Optional<string> Notes { get; set; }
        public Optional<string?> NullableNotes { get; set; }
        public Optional<int> Count { get; set; }
        public Optional<int?> NullableCount { get; set; }
        public Optional<List<int>> Numbers { get; set; }
    }
}
