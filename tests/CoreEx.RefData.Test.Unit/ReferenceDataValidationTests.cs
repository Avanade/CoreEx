using CoreEx.Entities;
using CoreEx.Validation;

namespace CoreEx.RefData.Test.Unit;

[TestFixture]
public partial class ReferenceDataOrchestratorTests
{
    [Test]
    public async Task Validation()
    {
        var vr = await ((DummyRefData?)null).Validator(c => c.IsValid()).ValidateAsync();
        vr.HasErrors.Should().BeFalse();

        vr = await ((DummyRefData)"A").Validator(c => c.IsValid()).ValidateAsync();
        vr.HasErrors.Should().BeFalse();

        vr = await ((DummyRefData)"Z").Validator(c => c.IsValid()).ValidateAsync();
        vr.HasErrors.Should().BeTrue();
        vr.Messages.Should().ContainSingle().Which.Text.ToString().Should().EndWith("is invalid.");

        vr = await ((DummyRefData)"D").Validator(c => c.IsValid()).ValidateAsync();
        vr.HasErrors.Should().BeTrue();
        vr.Messages.Should().ContainSingle().Which.Text.ToString().Should().EndWith("is invalid.");

        vr = await ((DummyRefData)"D").Validator(c => c.IsValid(allowInactive: true)).ValidateAsync();
        vr.HasErrors.Should().BeFalse();
    }

    [Test]
    public async Task Validation_Code()
    {
        var vr = await ((string?)null).Validator(c => c.ReferenceData(r => r.With<DummyRefData>())).ValidateAsync();
        vr.HasErrors.Should().BeFalse();

        vr = await "A".Validator(c => c.ReferenceData(r => r.With<DummyRefData>())).ValidateAsync();
        vr.HasErrors.Should().BeFalse();

        vr = await "Z".Validator(c => c.ReferenceData(r => r.With<DummyRefData>())).ValidateAsync();
        vr.HasErrors.Should().BeTrue();
        vr.Messages.Should().ContainSingle().Which.Text.ToString().Should().EndWith("is invalid.");

        vr = await "D".Validator(c => c.ReferenceData(r => r.With<DummyRefData>())).ValidateAsync();
        vr.HasErrors.Should().BeTrue();
        vr.Messages.Should().ContainSingle().Which.Text.ToString().Should().EndWith("is invalid.");

        vr = await "D".Validator(c => c.ReferenceData(r => r.With<DummyRefData>().AllowInactive())).ValidateAsync();
        vr.HasErrors.Should().BeFalse();
    }

    [Test]
    public async Task Validation_Override()
    {
        var e = new Entity { Code = "a" };

        var ev = Validator.Create<Entity>().HasProperty(p => p.Code, p => p.ReferenceData(r => r.With<DummyRefData>()));
        var vr = await ev.ValidateAsync(e);
        vr.HasErrors.Should().BeFalse();
        e.Code.Should().Be("a");

        ev = Validator.Create<Entity>().HasProperty(p => p.Code, p => p.ReferenceData(r => r.With<DummyRefData>().Override()));
        vr = await ev.ValidateAsync(e);
        vr.HasErrors.Should().BeFalse();
        e.Code.Should().Be("A");
    }

    [Test]
    public async Task Validation_CodeCollection()
    {
        var e = new Entity { DummySids = ["A", "B", "C"] };

        var ev = Validator.Create<Entity>().HasProperty(p => p.Dummies, p => p.AreValid());
        var vr = await ev.ValidateAsync(e);
        vr.HasErrors.Should().BeFalse();

        e.DummySids.Add("D");
        vr = await ev.ValidateAsync(e);
        vr.HasErrors.Should().BeTrue();
        vr.Messages.Should().ContainSingle().Which.Text.ToString().Should().EndWith("contains one or more invalid items.");

        ev = Validator.Create<Entity>().HasProperty(p => p.Dummies, p => p.AreValid(allowInactive: true));
        vr = await ev.ValidateAsync(e);
        vr.HasErrors.Should().BeFalse();

        e.DummySids.Add("Z");
        vr = await ev.ValidateAsync(e);
        vr.HasErrors.Should().BeTrue();
        vr.Messages.Should().ContainSingle().Which.Text.ToString().Should().EndWith("contains one or more invalid items.");
    }

    [Contract]
    internal partial class Entity
    {
        public string? Code { get; set; }

        [ReferenceDataCodeCollection<DummyRefData>]
        public partial List<string?>? DummySids { get; set; }
    }
}