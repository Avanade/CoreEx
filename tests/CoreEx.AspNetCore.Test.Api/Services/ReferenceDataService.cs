using CoreEx.AspNetCore.Test.Api.Entities;
using CoreEx.DependencyInjection;
using CoreEx.RefData;
using CoreEx.RefData.Abstractions;
using CoreEx.Results;

namespace CoreEx.AspNetCore.Test.Api.Services;

[ScopedService<ReferenceDataService>]
public class ReferenceDataService : IReferenceDataProvider
{
    public IEnumerable<(Type, Type)> Types =>
    [
        (typeof(Gender), typeof(GenderCollection))
    ];

    public Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default) => type switch
    {
        _ when type == typeof(Gender) => Task.FromResult<IReferenceDataCollection>(new GenderCollection { { new Gender { Id = "F", Code = "F", Text = "Female" } }, { new Gender { Id = "M", Code = "M", Text = "Male" } }, { new Gender { Id = "X", Code = "X", Text = "Xxx", IsInactive = true } } }),
        _ => throw new InvalidOperationException($"Type {type.FullName} is not a known {nameof(IReferenceData)}.")
    };
}