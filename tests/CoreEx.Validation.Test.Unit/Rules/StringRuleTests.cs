using System.Text.RegularExpressions;

namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class StringRuleTests
{
    [Test]
    public void String_MaximumLength()
    {
        ((string?)null).Validator(c => c.String(3)).ValidateAsSuccess();
        "".Validator(c => c.String(3)).ValidateAsSuccess();
        "abc".Validator(c => c.String(3)).ValidateAsSuccess();

        "abcd".Validator(c => c.String(3)).ValidateAsError(" must not exceed 3 character(s) in length.");
    }

    [Test]
    public void String_MinimumLength()
    {
        ((string?)null).Validator(c => c.String(1, null)).ValidateAsSuccess();
        "abc".Validator(c => c.String(2, null)).ValidateAsSuccess();
        "ab".Validator(c => c.String(2, null)).ValidateAsSuccess();

        "".Validator(c => c.String(1, null)).ValidateAsError(" must be at least 1 character(s) in length.");
        "a".Validator(c => c.String(2, null)).ValidateAsError(" must be at least 2 character(s) in length.");
    }

    [Test]
    public void String_MinimumMaximumLength()
    {
        ((string?)null).Validator(c => c.String(1, 3)).ValidateAsSuccess();
        "a".Validator(c => c.String(1, 3)).ValidateAsSuccess();
        "ab".Validator(c => c.String(1, 3)).ValidateAsSuccess();
        "abc".Validator(c => c.String(1, 3)).ValidateAsSuccess();

        "".Validator(c => c.String(1, 3)).ValidateAsError(" must be at least 1 character(s) in length.");
        "abcd".Validator(c => c.String(1, 3)).ValidateAsError(" must not exceed 3 character(s) in length.");
    }

    [Test]
    public void String_ExactLength()
    {
        ((string?)null).Validator(c => c.String(3, 3)).ValidateAsSuccess();
        "abc".Validator(c => c.String(3, 3)).ValidateAsSuccess();

        "".Validator(c => c.String(3, 3)).ValidateAsError("must be exactly 3 character(s) in length.");
        "ab".Validator(c => c.String(3, 3)).ValidateAsError("must be exactly 3 character(s) in length.");
        "abcd".Validator(c => c.String(3, 3)).ValidateAsError("must be exactly 3 character(s) in length.");
    }

    [Test]
    public void String_Regex()
    {
        var regex = new Regex(@"^\w+$");

        ((string?)null).Validator(c => c.String(regex)).ValidateAsSuccess();
        "abc123".Validator(c => c.String(regex)).ValidateAsSuccess();

        "".Validator(c => c.String(regex)).ValidateAsError(" is invalid.");
        "abc 123".Validator(c => c.String(regex)).ValidateAsError(" is invalid.");
        "abc 123".Validator(c => c.String(regex).WithMessage("No matchy matchy!")).ValidateAsError("No matchy matchy!");
    }

    [Test]
    public void MaximumLength()
    {
        ((string?)null).Validator(c => c.MaximumLength(3)).ValidateAsSuccess();
        "".Validator(c => c.MaximumLength(3)).ValidateAsSuccess();
        "abc".Validator(c => c.MaximumLength(3)).ValidateAsSuccess();

        "abcd".Validator(c => c.MaximumLength(3)).ValidateAsError(" must not exceed 3 character(s) in length.");
    }

    [Test]
    public void MinimumLength()
    {
        ((string?)null).Validator(c => c.MinimumLength(1)).ValidateAsSuccess();
        "abc".Validator(c => c.MinimumLength(2)).ValidateAsSuccess();
        "ab".Validator(c => c.MinimumLength(2)).ValidateAsSuccess();

        "".Validator(c => c.MinimumLength(1)).ValidateAsError(" must be at least 1 character(s) in length.");
        "a".Validator(c => c.MinimumLength(2)).ValidateAsError(" must be at least 2 character(s) in length.");
    }

    [Test]
    public void Length()
    {
        ((string?)null).Validator(c => c.Length(3)).ValidateAsSuccess();
        "abc".Validator(c => c.Length(3)).ValidateAsSuccess();

        "".Validator(c => c.Length(3)).ValidateAsError("must be exactly 3 character(s) in length.");
        "ab".Validator(c => c.Length(3)).ValidateAsError("must be exactly 3 character(s) in length.");
        "abcd".Validator(c => c.Length(3)).ValidateAsError("must be exactly 3 character(s) in length.");
    }

    [Test]
    public void Matches()
    {
        var regex = new Regex(@"^\w+$");

        ((string?)null).Validator(c => c.Matches(regex)).ValidateAsSuccess();
        "abc123".Validator(c => c.Matches(regex)).ValidateAsSuccess();

        "".Validator(c => c.Matches(regex)).ValidateAsError(" is invalid.");
        "abc 123".Validator(c => c.Matches(regex)).ValidateAsError(" is invalid.");
        "abc 123".Validator(c => c.Matches(regex).WithMessage("No matchy matchy!")).ValidateAsError("No matchy matchy!");
    }
}