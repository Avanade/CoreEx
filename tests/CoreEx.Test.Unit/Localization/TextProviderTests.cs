using System.Globalization;
using CoreEx.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEx.Test.Unit.Localization;

[TestFixture]
public class TextProviderTests
{
    private class TestTextProvider : ITextProvider
    {
        public string? GetText(LText text) => $"[{text.KeyAndOrText}]";
    }

    [TearDown]
    public void TearDown()
    {
        // Reset to default before each test to avoid side effects.
        TextProvider.SetTextProvider(null);
        ExecutionContext.Reset();
    }

    [Test]
    public void SetTextProvider_And_Current()
    {
        var tp = new TestTextProvider();
        TextProvider.SetTextProvider(tp);
        TextProvider.Current.Should().BeSameAs(tp);
    }

    [Test]
    public void Current_FallsBackToNullTextProvider()
    {
        TextProvider.SetTextProvider(null);
        var current = TextProvider.Current;
        current.Should().NotBeNull();
        current.GetType().Name.Should().Be("NullTextProvider");
    }

    [Test]
    public void Current_UsesExecutionContextService()
    {
        var tp = new TestTextProvider();
        var sc = new ServiceCollection();
        sc.AddSingleton<ITextProvider>(tp);
        using var sp = sc.BuildServiceProvider();

        ExecutionContext.Reset();
        ExecutionContext.SetCurrent(new ExecutionContext { ServiceProvider = sp });
        TextProvider.Current.Should().BeSameAs(tp);    
    }

    [Test]
    public void GetUICulture_ReturnsCurrentUICulture()
    {
        var culture = TextProvider.GetUICulture();
        culture.Should().Be(CultureInfo.CurrentUICulture);
    }

    [Test]
    public void Format_NullFormat_ReturnsNull()
    {
        TextProvider.Format(null, [1]).Should().BeNull();
    }

    [Test]
    public void Format_NullArgs_ReturnsFormat()
    {
        TextProvider.Format("abc", null).Should().Be("abc");
    }

    [Test]
    public void Format_EmptyArgs_ReturnsFormat()
    {
        TextProvider.Format("abc", []).Should().Be("abc");
    }

    [Test]
    public void Format_WithArgs_FormatsString()
    {
        var result = TextProvider.Format("x={0},y={1}", [1, "a"]);
        result.Should().Be($"x=1,y=a");
    }
}