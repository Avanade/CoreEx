using CoreEx.Entities;
using CoreEx.Entities.Extended;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEx.Test.Unit.Entities;

[TestFixture]
public class IdentifierGeneratorTests
{
    private class GuidEntity : IIdentifier<Guid>
    {
        public Guid Id { get; set; }
    }

    private class StringEntity : IIdentifier<string>
    {
        public string Id { get; set; } = null!;
    }

    private class IntEntity : IIdentifier<int>
    {
        public int Id { get; set; }
    }

    private class PlainEntity { }

    [TearDown]
    public void TearDown() => ExecutionContext.Reset();

    [Test]
    public void GenerateGuid_ReturnsNonEmptyGuid()
        => new IdentifierGenerator().GenerateGuid().Should().NotBe(Guid.Empty);

    [Test]
    public void GenerateGuid_ReturnsDistinctValues()
    {
        var gen = new IdentifierGenerator();
        gen.GenerateGuid().Should().NotBe(gen.GenerateGuid());
    }

    [Test]
    public async Task GenerateIdentifierAsync_String_ReturnsGuidFormattedString()
    {
        var id = await new IdentifierGenerator().GenerateIdentifierAsync<string>();
        Guid.TryParse(id, out _).Should().BeTrue();
    }

    [Test]
    public async Task GenerateIdentifierAsync_Guid_ReturnsNonEmptyGuid()
    {
        var id = await new IdentifierGenerator().GenerateIdentifierAsync<Guid>();
        id.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void GenerateIdentifierAsync_UnsupportedType_ThrowsNotSupportedException()
    {
        Action act = () => new IdentifierGenerator().GenerateIdentifierAsync<int>();
        act.Should().Throw<NotSupportedException>().WithMessage("*Int32*");
    }

    [Test]
    public async Task GenerateIdentifierAsync_WithFor_DelegatesToGenerateIdentifierAsync()
    {
        var id = await new IdentifierGenerator().GenerateIdentifierAsync<string, StringEntity>();
        Guid.TryParse(id, out _).Should().BeTrue();
    }

    [Test]
    public async Task AssignIdentifierAsync_StringEntity_NullId_AssignsGeneratedId()
    {
        var entity = new StringEntity { Id = null! };
        await new IdentifierGenerator().AssignIdentifierAsync(entity);

        entity.Id.Should().NotBeNullOrEmpty();
        Guid.TryParse(entity.Id, out _).Should().BeTrue();
    }

    [Test]
    public async Task AssignIdentifierAsync_StringEntity_ExistingId_DoesNotOverwrite()
    {
        var entity = new StringEntity { Id = "existing-id" };
        await new IdentifierGenerator().AssignIdentifierAsync(entity);

        entity.Id.Should().Be("existing-id");
    }

    [Test]
    public async Task AssignIdentifierAsync_GuidEntity_EmptyId_AssignsGeneratedId()
    {
        var entity = new GuidEntity { Id = Guid.Empty };
        await new IdentifierGenerator().AssignIdentifierAsync(entity);

        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Test]
    public async Task AssignIdentifierAsync_GuidEntity_ExistingId_DoesNotOverwrite()
    {
        var existing = Guid.NewGuid();
        var entity = new GuidEntity { Id = existing };
        await new IdentifierGenerator().AssignIdentifierAsync(entity);

        entity.Id.Should().Be(existing);
    }

    [Test]
    public async Task AssignIdentifierAsync_UnsupportedIdType_ThrowsNotSupportedException()
    {
        var entity = new IntEntity { Id = 0 };
        Func<Task> act = () => new IdentifierGenerator().AssignIdentifierAsync(entity);

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Int32*");
    }

    [Test]
    public async Task AssignIdentifierAsync_NotAnIdentifier_NoOp()
    {
        var entity = new PlainEntity();
        Func<Task> act = () => new IdentifierGenerator().AssignIdentifierAsync(entity);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public void Current_NoExecutionContextService_ReturnsDefaultInstance()
    {
        ExecutionContext.Reset();
        IdentifierGenerator.Current.Should().NotBeNull();
    }

    [Test]
    public void Current_WithRegisteredService_ReturnsRegisteredInstance()
    {
        var custom = new IdentifierGenerator();
        var sc = new ServiceCollection();
        sc.AddSingleton<IIdentifierGenerator>(custom);
        using var sp = sc.BuildServiceProvider();

        ExecutionContext.SetCurrent(new ExecutionContext { ServiceProvider = sp });

        IdentifierGenerator.Current.Should().BeSameAs(custom);
    }
}
