namespace CoreEx.Test.Unit.Runtime;

[TestFixture]
public partial class MetadataSourceTests
{
    [Contract]
    private partial class TypeFirstContract
    {
        public string? Name { get; set; }

        [ContractIgnore]
        public string? Excluded { get; set; }
    }

    [Contract]
    private partial class GenericFirstContract
    {
        public string? Name { get; set; }

        [ContractIgnore]
        public string? Excluded { get; set; }
    }

    private class StaticEntity : IRuntimeMetadata
    {
        public StaticEntity() => throw new InvalidOperationException("Metadata must not construct entities.");

        [JsonPropertyName("reflectedName")]
        public string? Name { get; set; }

        public string? Excluded => throw new InvalidOperationException("Metadata must not execute property getters.");

        private static readonly IPropertyRuntimeMetadata[] _properties =
        [
            new PropertyRuntimeMetadata<StaticEntity, string?>(nameof(Name), x => x.Name, text: () => "Custom friendly name", jsonName: "configuredName")
        ];

        static IEnumerable<IPropertyRuntimeMetadata> IRuntimeMetadata.GetStaticPropertyRuntimeMetadata() => _properties;

        public IEnumerable<IPropertyRuntimeMetadata> GetPropertyRuntimeMetadata() => _properties;
    }

    private class Wrapper
    {
        public StaticEntity? Entity { get; set; }
    }

    private class InstanceOnlyEntity : IRuntimeMetadataCore
    {
        public InstanceOnlyEntity() => throw new InvalidOperationException("Metadata must not construct entities.");

        [JsonPropertyName("reflectedName")]
        public string? Name { get; set; }

        public IEnumerable<IPropertyRuntimeMetadata> GetPropertyRuntimeMetadata() => throw new InvalidOperationException("Instance metadata must not be invoked.");
    }

    private class BrokenStaticEntity : IRuntimeMetadata
    {
        public string? Name { get; set; }

        static IEnumerable<IPropertyRuntimeMetadata> IRuntimeMetadata.GetStaticPropertyRuntimeMetadata() => throw new InvalidOperationException("Broken metadata.");

        public IEnumerable<IPropertyRuntimeMetadata> GetPropertyRuntimeMetadata() => throw new NotSupportedException();
    }

    [Test]
    public void GeneratedMetadata_IsConsistentRegardlessOfCacheEntryPoint()
    {
        var typeFirst = RuntimeMetadata.GetCachedProperties(typeof(TypeFirstContract));
        typeFirst.Should().BeSameAs(RuntimeMetadata.GetCachedProperties<TypeFirstContract>());
        typeFirst.Keys.Should().ContainSingle().Which.Should().Be(nameof(TypeFirstContract.Name));

        var genericFirst = RuntimeMetadata.GetCachedProperties<GenericFirstContract>();
        genericFirst.Should().BeSameAs(RuntimeMetadata.GetCachedProperties(typeof(GenericFirstContract)));
        genericFirst.Keys.Should().ContainSingle().Which.Should().Be(nameof(GenericFirstContract.Name));
    }

    [Test]
    public void TypeLookup_PrefersExplicitStaticMetadataAndCachesIt()
    {
        var first = RuntimeMetadata.GetCachedProperties(typeof(StaticEntity));
        var second = RuntimeMetadata.GetCachedProperties<StaticEntity>();

        first.Should().BeSameAs(second);
        first.Keys.Should().ContainSingle().Which.Should().Be(nameof(StaticEntity.Name));
        first[nameof(StaticEntity.Name)].GetJsonName().Should().Be("configuredName");
        first[nameof(StaticEntity.Name)].Text.ToString().Should().Be("Custom friendly name");
        RuntimeMetadata.GetCachedProperties(typeof(StaticEntity), nameof(StaticEntity.Name)).Should().BeEmpty();
        RuntimeMetadata.GetCachedProperties(typeof(StaticEntity)).Should().ContainKey(nameof(StaticEntity.Name));
    }

    [Test]
    public void NestedPath_UsesStaticNamesAndTextWithoutInstances()
    {
        // Null-forgiving is safe because the expression is inspected without executing entity access.
        var metadata = RuntimeMetadata.GetJsonPathAndText<Wrapper>(x => x.Entity!.Name);
        metadata.Path.Should().Be("entity.configuredName");
        metadata.Text.ToString().Should().Be("Custom friendly name");
        // No entity instance is required for inferred messages either.
        var exception = ValidationException.Create<Wrapper>(x => x.Entity!.Name);
        exception.Messages.Should().ContainSingle().Which.Text.ToString().Should().Be("Custom friendly name is invalid.");
    }

    [Test]
    public void ExplicitPropertyLookup_ReflectsMissingPropertiesWithoutChangingStaticCollection()
    {
        var properties = RuntimeMetadata.GetCachedProperties(typeof(StaticEntity));
        var configured = RuntimeMetadata.GetForExpression<StaticEntity, string?>(x => x.Name, null);
        configured.Should().BeSameAs(properties[nameof(StaticEntity.Name)]);
        configured.GetJsonName().Should().Be("configuredName");

        var reflected = RuntimeMetadata.GetForExpression<StaticEntity, string?>(x => x.Excluded, null);
        reflected.Should().BeSameAs(RuntimeMetadata.GetForExpression<StaticEntity, string?>(x => x.Excluded, null));
        reflected.GetJsonName().Should().Be("excluded");
        RuntimeMetadata.GetJsonPath<Wrapper>(x => x.Entity!.Excluded).Should().Be("entity.excluded");
        properties.Keys.Should().ContainSingle().Which.Should().Be(nameof(StaticEntity.Name));
    }

    [Test]
    public void InstanceOnlyMetadata_FallsBackToReflectionWithoutConstruction()
    {
        RuntimeMetadata.GetCachedProperties(typeof(InstanceOnlyEntity))[nameof(InstanceOnlyEntity.Name)].GetJsonName().Should().Be("reflectedName");
        RuntimeMetadata.GetJsonPath<InstanceOnlyEntity>(x => x.Name).Should().Be("reflectedName");
    }

    [Test]
    public void StaticProviderErrors_PropagateWithoutReflectionFallback()
    {
        var action = () => RuntimeMetadata.GetCachedProperties(typeof(BrokenStaticEntity));
        action.Should().Throw<InvalidOperationException>().WithMessage("Broken metadata.");
        var expression = () => RuntimeMetadata.GetJsonPath<BrokenStaticEntity>(x => x.Name);
        expression.Should().Throw<InvalidOperationException>().WithMessage("Broken metadata.");
    }
}
