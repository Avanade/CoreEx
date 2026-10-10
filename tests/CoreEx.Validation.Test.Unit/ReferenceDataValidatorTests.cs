using CoreEx.Entities;
using CoreEx.Entities.Abstractions;
using CoreEx.RefData.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace CoreEx.Validation.Test.Unit;

public class ReferenceDataValidatorTests
{
    [Test]
    public void Validate_Success()
    {
        var rd = new TestRefData { Code = "ABC", Text = "Some text" };
        var v = new ReferenceDataValidator<TestRefData>();

        v.ValidateAsSuccess(rd);
    }

    [Test]
    public void Validate_Code_Mandatory()
    {
        var rd = new TestRefData { Code = null, Text = "Some text" };
        var v = new ReferenceDataValidator<TestRefData>();

        v.ValidateAsError(rd, "code", "Code is required.");
    }

    [Test]
    public void Validate_Code_MaximumLength()
    {
        var rd = new TestRefData { Code = new string('x', ReferenceDataValidator<TestRefData>.CodeMaximumLength + 1), Text = "Some text" };
        var v = new ReferenceDataValidator<TestRefData>();

        v.ValidateAsError(rd, "code", "Code must not exceed");
    }

    [Test]
    public void Validate_Text_Mandatory()
    {
        var rd = new TestRefData { Code = "ABC", Text = null };
        var v = new ReferenceDataValidator<TestRefData>();

        v.ValidateAsError(rd, "text", "Text is required.");
    }

    [Test]
    public void Validate_Text_MaximumLength()
    {
        var rd = new TestRefData { Code = "ABC", Text = new string('x', ReferenceDataValidator<TestRefData>.TextMaximumLength + 1) };
        var v = new ReferenceDataValidator<TestRefData>();

        v.ValidateAsError(rd, "text", "Text must not exceed");
    }

    [Test]
    public void Validate_Description_MaximumLength()
    {
        var rd = new TestRefData { Code = "ABC", Text = "Some text", Description = new string('x', ReferenceDataValidator<TestRefData>.DescriptionMaximumLength + 1) };
        var v = new ReferenceDataValidator<TestRefData>();

        v.ValidateAsError(rd, "description", "Description must not exceed");
    }

    [Test]
    public void Validate_EndsOn_MustBeGreaterThanOrEqualToStartsOn()
    {
        var rd = new TestRefData { Code = "ABC", Text = "Some text", StartsOn = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero), EndsOn = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var v = new ReferenceDataValidator<TestRefData>();

        v.ValidateAsError(rd, "endsOn", "Ends on must be greater than or equal to Starts on.");
    }

    [Test]
    public void Validate_EndsOn_WithStartsOn_Success()
    {
        var rd = new TestRefData { Code = "ABC", Text = "Some text", StartsOn = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), EndsOn = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var v = new ReferenceDataValidator<TestRefData>();

        v.ValidateAsSuccess(rd);
    }

    private sealed class TestRefData : IReferenceData
    {
        object? IReferenceData.Id { get; init; }

        object? IIdentifierCore.Id => ((IReferenceData)this).Id;

        public string? Code { get; init; }

        public string? Text { get; init; }

        public string? Description { get; init; }

        public int SortOrder { get; init; }

        public bool IsInactive { get; init; }

        public bool IsActive => !IsInactive;

        public DateTimeOffset? StartsOn { get; init; }

        public DateTimeOffset? EndsOn { get; init; }

        public bool IsValid => true;

        public bool HasMappings => false;

        public IReadOnlyDictionary<string, object?>? Mappings => null;

        public string? ETag => null;

        public Type IdType => typeof(object);

        public bool IsIdReadOnly => true;

        public CompositeKey EntityKey => CompositeKey.Create(Code);

        public void SetIdentifier(object? id) => throw new NotSupportedException();

        public void SetInvalid() => throw new NotSupportedException();

        public string? GetText() => Text;

        public string? GetDescription() => Description;

        public void SetMapping<T>(string name, T? value) where T : IComparable<T?>, IEquatable<T?> => throw new NotSupportedException();

        public bool TryGetMapping<T>(string name, [NotNullWhen(true)] out T? value) where T : IComparable<T?>, IEquatable<T?>
        {
            value = default;
            return false;
        }
    }
}
