namespace CoreEx.Cosmos.Test.Unit;

/// <summary>
/// A simple single-partition test model (partition key equals <see cref="Id"/>) used for basic CRUD/concurrency/not-found tests.
/// </summary>
public class TestItem : CosmosDbModelBase, IEntityKey
{
    public string Name { get; set; } = string.Empty;

    public CompositeKey EntityKey => CompositeKey.Create(Id);
}

/// <summary>
/// A test model implementing <see cref="ILogicallyDeleted"/> used for logical-delete tests.
/// </summary>
public class SoftDeleteItem : CosmosDbModelBase, IEntityKey, ILogicallyDeleted
{
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public CompositeKey EntityKey => CompositeKey.Create(Id);
}

/// <summary>
/// A test model implementing <see cref="ITenantId"/> used for tenant-isolation tests.
/// </summary>
public class TenantItem : CosmosDbModelBase, IEntityKey, ITenantId
{
    public string Name { get; set; } = string.Empty;

    public string? TenantId { get; set; }

    public CompositeKey EntityKey => CompositeKey.Create(Id);
}

/// <summary>
/// A test model implementing <see cref="ITypeDiscriminator"/> ("animal") used, alongside <see cref="PlantItem"/>, for multi-type container tests.
/// </summary>
/// <remarks>Decorated with an explicit <see cref="Schemas.SchemaAttribute"/> so <c>Model.PrepareCreate</c> stamps a specific, readable <see cref="TypeDiscriminator"/> value - without it, <c>Model.PrepareTypeDiscriminator</c>
/// would still stamp a value (falling back to the type name itself, per <see cref="IReadOnlyTypeDiscriminator.TypeDiscriminator"/>'s doc remarks), just the less descriptive <see cref="AnimalItem"/> default.</remarks>
[Schemas.Schema(Name = nameof(AnimalItem))]
public class AnimalItem : CosmosDbModelBase, IEntityKey, ITypeDiscriminator
{
    public string Name { get; set; } = string.Empty;

    public string? TypeDiscriminator { get; set; }

    public CompositeKey EntityKey => CompositeKey.Create(Id);
}

/// <summary>
/// A test model implementing <see cref="ITypeDiscriminator"/> ("plant") used, alongside <see cref="AnimalItem"/>, for multi-type container tests.
/// </summary>
[Schemas.Schema(Name = nameof(PlantItem))]
public class PlantItem : CosmosDbModelBase, IEntityKey, ITypeDiscriminator
{
    public string Name { get; set; } = string.Empty;

    public string? TypeDiscriminator { get; set; }

    public CompositeKey EntityKey => CompositeKey.Create(Id);
}

/// <summary>
/// A test model implementing both <see cref="ITypeDiscriminator"/> and <see cref="ITenantId"/> - used, alongside <see cref="TenantPlantItem"/>, to verify that per-item tenant filtering (see
/// <see cref="CosmosDbContainer{TModel}.CheckModel"/>) is still applied when a model is read via a multi-set query.
/// </summary>
[Schemas.Schema(Name = nameof(TenantAnimalItem))]
public class TenantAnimalItem : CosmosDbModelBase, IEntityKey, ITypeDiscriminator, ITenantId
{
    public string Name { get; set; } = string.Empty;

    public string? TypeDiscriminator { get; set; }

    public string? TenantId { get; set; }

    public CompositeKey EntityKey => CompositeKey.Create(Id);
}

/// <summary>
/// A test model implementing both <see cref="ITypeDiscriminator"/> and <see cref="ITenantId"/> - used, alongside <see cref="TenantAnimalItem"/>, to verify that per-item tenant filtering (see
/// <see cref="CosmosDbContainer{TModel}.CheckModel"/>) is still applied when a model is read via a multi-set query.
/// </summary>
[Schemas.Schema(Name = nameof(TenantPlantItem))]
public class TenantPlantItem : CosmosDbModelBase, IEntityKey, ITypeDiscriminator, ITenantId
{
    public string Name { get; set; } = string.Empty;

    public string? TypeDiscriminator { get; set; }

    public string? TenantId { get; set; }

    public CompositeKey EntityKey => CompositeKey.Create(Id);
}

/// <summary>
/// A test model implementing both <see cref="ITypeDiscriminator"/> and <see cref="ILogicallyDeleted"/> - used to verify the query-level, <c>IS_DEFINED</c>-guarded logical-delete SQL optimization
/// (see <see cref="CosmosDbModelOptions{TModel}.WithLogicalDeleteFilter"/>) applied by a multi-set query.
/// </summary>
[Schemas.Schema(Name = nameof(SoftDeleteAnimalItem))]
public class SoftDeleteAnimalItem : CosmosDbModelBase, IEntityKey, ITypeDiscriminator, ILogicallyDeleted
{
    public string Name { get; set; } = string.Empty;

    public string? TypeDiscriminator { get; set; }

    public bool IsDeleted { get; set; }

    public CompositeKey EntityKey => CompositeKey.Create(Id);
}

/// <summary>
/// A test model implementing the standard interfaces directly (not via <see cref="CosmosDbModelBase"/>) and deliberately omitting <see cref="ITimeToLive"/>, used to test the
/// <see cref="CosmosDbModelOptions{TModel}.WithTimeToLive"/> guard.
/// </summary>
public class NoTimeToLiveItem : IEntityKey, IIdentifier<string>, IETag, IPartitionKey
{
    public string Id { get; set; } = string.Empty;

    public string? ETag { get; set; }

    public string? PartitionKey { get; set; }

    public CompositeKey EntityKey => CompositeKey.Create(Id);
}

/// <summary>
/// A test model implementing neither <see cref="IPartitionKey"/> nor <see cref="IReadOnlyPartitionKey"/> at all, used to test <see cref="CosmosDbModelOptions{TModel}"/>'s "no configuration, no model
/// support" fallback to <see cref="Microsoft.Azure.Cosmos.PartitionKey.None"/> - the simplest possible container shape.
/// </summary>
public class NoPartitionKeyItem : IEntityKey, IIdentifier<string>, IETag
{
    public string Id { get; set; } = string.Empty;

    public string? ETag { get; set; }

    public string Name { get; set; } = string.Empty;

    public CompositeKey EntityKey => CompositeKey.Create(Id);
}

/// <summary>
/// A domain "contract" value used to exercise <see cref="CosmosDbMappedContainer{TValue, TModel, TBiDirectionMapper}"/> (mapped to/from <see cref="TestItem"/>), and <see cref="IUnitOfWork.SynchronizeETag{T}(CompositeKey, T)"/>
/// (a distinct object instance/type from the <see cref="TestItem"/> model a <see cref="CosmosDbUnitOfWork"/> actually mutates - the scenario that mechanism exists for).
/// </summary>
public class TestValue : IETag
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? ETag { get; set; }
}

/// <summary>
/// A hand-written <see cref="IBiDirectionMapper{TSource, TDestination}"/> between <see cref="TestValue"/> and <see cref="TestItem"/>.
/// </summary>
public class TestValueMapper : IBiDirectionMapper<TestValue, TestItem>
{
    public IMapper<TestValue, TestItem> To { get; } = new ToMapper();

    public IMapper<TestItem, TestValue> From { get; } = new FromMapper();

    private sealed class ToMapper : IMapper<TestValue, TestItem>
    {
        public TestItem? Map(TestValue? source) => source is null
            ? null
            : new TestItem { Id = source.Id ?? string.Empty, PartitionKey = source.Id, Name = source.Name ?? string.Empty, ETag = source.ETag };
    }

    private sealed class FromMapper : IMapper<TestItem, TestValue>
    {
        public TestValue? Map(TestItem? source) => source is null
            ? null
            : new TestValue { Id = source.Id, Name = source.Name, ETag = source.ETag };
    }
}
