namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class ChainRuleTests
{
    [Test]
    public void Chain_PreservesNextRuleTypeForNullableAndNonNullableInputs()
    {
        var validator = Validator.Create<Request>()
            .HasProperty(x => x.Text, rule =>
            {
                CoreEx.Validation.Rules.IPropertyRule<Request, string> next = ValidationExtensions.Chain(rule, new CoreEx.Validation.Rules.StringRule<Request>(maxLength: _ => 3));
                next.Between("aaa", "zzz");
            })
            .HasProperty(x => x.NullableText, rule =>
            {
                CoreEx.Validation.Rules.IPropertyRule<Request, string> next = ValidationExtensions.Chain(rule, new CoreEx.Validation.Rules.StringRule<Request>(maxLength: _ => 3));
                next.Between("aaa", "zzz");
            })
            .HasProperty(x => x.Number, rule =>
            {
                CoreEx.Validation.Rules.IPropertyRule<Request, int> next = ValidationExtensions.Chain(rule, new CoreEx.Validation.Rules.MandatoryRule<Request, int>(_ => false, _ => false));
                next.Between(0, 3);
            })
            .HasProperty(x => x.NullableNumber, rule =>
            {
                CoreEx.Validation.Rules.IPropertyRule<Request, int> next = ValidationExtensions.Chain(rule, new CoreEx.Validation.Rules.MandatoryRule<Request, int>(_ => false, _ => false));
                next.Between(0, 3);
            });

        validator.ValidateAsSuccess(new Request());
        validator.ValidateAsSuccess(new Request { NullableText = "abc", NullableNumber = 0 });
        validator.ValidateAsError(new Request { NullableText = "long" }, "nullableText", "Nullable text must not exceed 3 character(s) in length.");
        validator.ValidateAsError(new Request { NullableNumber = 4 }, "nullableNumber", "Nullable number must be between '0' and '3'.");
    }

    public class Request
    {
        public string Text { get; set; } = "abc";
        public string? NullableText { get; set; }
        public int Number { get; set; }
        public int? NullableNumber { get; set; } = 2;
    }
}
