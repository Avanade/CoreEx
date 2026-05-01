using CoreEx.Data;
using CoreEx.Entities;
using CoreEx.Mapping;

namespace CoreEx.Test.Unit.Mapping;

public class MapTests
{
    private class Source : IReadOnlyIdentifier<int>, IReadOnlyETag, IReadOnlyTenantId, IReadOnlyLogicallyDeleted, IReadOnlyTypeDiscriminator, IReadOnlyPartitionKey, IReadOnlyChangeLogEx
    {
        public int Id { get; init; }
        public string? ETag { get; init; }
        public string? TenantId { get; init; }
        public bool IsDeleted { get; init; }
        public string? TypeDiscriminator { get; init; }
        public string? PartitionKey { get; init; }
        public string? CreatedBy { get; init; }
        public DateTimeOffset? CreatedOn { get; init; }
        public string? UpdatedBy { get; init; }
        public DateTimeOffset? UpdatedOn { get; init; }
    }

    private class Source2 : IReadOnlyIdentifier<int>, IReadOnlyETag, IReadOnlyTenantId, IReadOnlyLogicallyDeleted, IReadOnlyTypeDiscriminator, IReadOnlyPartitionKey, IReadOnlyChangeLog
    {
        public int Id { get; init; }
        public string? ETag { get; init; }
        public string? TenantId { get; init; }
        public bool IsDeleted { get; init; }
        public string? TypeDiscriminator { get; init; }
        public string? PartitionKey { get; init; }
        public ChangeLog? ChangeLog { get; init; }
    }

    private class Destination : IIdentifier<int>, IETag, ITenantId, ILogicallyDeleted, ITypeDiscriminator, IPartitionKey, IChangeLogEx
    {
        public int Id { get; set; }
        public string? ETag { get; set; }
        public string? TenantId { get; set; }
        public bool IsDeleted { get; set; }
        public string? TypeDiscriminator { get; set; }
        public string? PartitionKey { get; set; }
        public string? CreatedBy { get; set; }
        public DateTimeOffset? CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedOn { get; set; }
    }

    private class Destination2 : IIdentifier<int>, IETag, ITenantId, ILogicallyDeleted, ITypeDiscriminator, IPartitionKey, IChangeLog
    {
        public int Id { get; set; }
        public Type IdType => typeof(int);
        public string? ETag { get; set; }
        public string? TenantId { get; set; }
        public bool IsDeleted { get; set; }
        public string? TypeDiscriminator { get; set; }
        public string? PartitionKey { get; set; }
        public ChangeLog? ChangeLog { get; set; }
    }

    [Test]
    public void Standard_MapsAllStandardProperties()
    {
        var src = new Source
        {
            Id = 123,
            ETag = "etag",
            TenantId = "tenant",
            IsDeleted = true,
            TypeDiscriminator = "type",
            PartitionKey = "pk",
            CreatedBy = "cb",
            CreatedOn = DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedBy = "ub",
            UpdatedOn = DateTimeOffset.UtcNow
        };

        var dest = new Destination();
        Mapper.MapStandardInto(src, dest);

        dest.Id.Should().Be(123);
        dest.ETag.Should().Be("etag");
        dest.TenantId.Should().Be("tenant");
        dest.IsDeleted.Should().BeTrue();
        dest.TypeDiscriminator.Should().Be("type");
        dest.PartitionKey.Should().Be("pk");
        dest.CreatedBy.Should().Be("cb");
        dest.CreatedOn.Should().Be(src.CreatedOn);
        dest.UpdatedBy.Should().Be("ub");
        dest.UpdatedOn.Should().Be(src.UpdatedOn);
    }

    [Test]
    public void Standard_MapsAllStandardProperties2()
    {
        var src = new Source
        {
            Id = 123,
            ETag = "etag",
            TenantId = "tenant",
            IsDeleted = true,
            TypeDiscriminator = "type",
            PartitionKey = "pk",
            CreatedBy = "cb",
            CreatedOn = DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedBy = "ub",
            UpdatedOn = DateTimeOffset.UtcNow
        };

        var dest = new Destination2();
        Mapper.MapStandardInto(src, dest);

        dest.Id.Should().Be(123);
        dest.ETag.Should().Be("etag");
        dest.TenantId.Should().Be("tenant");
        dest.IsDeleted.Should().BeTrue();
        dest.TypeDiscriminator.Should().Be("type");
        dest.PartitionKey.Should().Be("pk");
        dest.ChangeLog.Should().NotBeNull();
        dest.ChangeLog.CreatedBy.Should().Be("cb");
        dest.ChangeLog.CreatedOn.Should().Be(src.CreatedOn);
        dest.ChangeLog.UpdatedBy.Should().Be("ub");
        dest.ChangeLog.UpdatedOn.Should().Be(src.UpdatedOn);
    }
}