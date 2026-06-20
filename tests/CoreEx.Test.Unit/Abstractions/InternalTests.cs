using CoreEx.Abstractions;
using Microsoft.Extensions.Configuration;

namespace CoreEx.Test.Unit.Abstractions;

internal class InternalTests
{
    [Test]
    public void TryGetConfigurationValue_Nullable()
    {
        var config = new ConfigurationBuilder().Build();
        var exists = Internal.TryGetConfigurationValue<int?>("MySetting", out var value, config);
        exists.Should().BeFalse();
        value.Should().BeNull();

        config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "MySetting", null} }).Build();
        exists = Internal.TryGetConfigurationValue<int?>("MySetting", out value, config);
        exists.Should().BeFalse();
        value.Should().BeNull();

        config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "MySetting", "0" } }).Build();
        exists = Internal.TryGetConfigurationValue<int?>("MySetting", out value, config);
        exists.Should().BeTrue();
        value.Should().Be(0);
    }

    [Test]
    public void TryGetConfigurationValue_NonNullable()
    {
        var config = new ConfigurationBuilder().Build();
        var exists = Internal.TryGetConfigurationValue<int>("MySetting", out var value, config);
        exists.Should().BeFalse();
        value.Should().Be(0);

        config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "MySetting", null } }).Build();
        exists = Internal.TryGetConfigurationValue<int>("MySetting", out value, config);
        exists.Should().BeFalse();
        value.Should().Be(0);

        config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "MySetting", "0" } }).Build();
        exists = Internal.TryGetConfigurationValue<int>("MySetting", out value, config);
        exists.Should().BeTrue();
        value.Should().Be(0);
    }
}