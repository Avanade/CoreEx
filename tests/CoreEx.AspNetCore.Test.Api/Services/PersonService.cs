using CoreEx.AspNetCore.Test.Api.Entities;
using CoreEx.Data;
using CoreEx.Data.Querying;
using CoreEx.DependencyInjection;
using CoreEx.Validation;
using System.Collections.Concurrent;

namespace CoreEx.AspNetCore.Test.Api.Services;

[ScopedService<PersonService>]
public class PersonService
{
    internal static QueryArgsConfig QueryConfig { get; } = QueryArgsConfig.Create()
        .WithFilter(f => f
            .AddField<string>("FirstName", c => c.AsLowerCase().WithOperators(QueryFilterOperator.AllStringOperators))
            .AddField<string>("LastName", c => c.AsLowerCase().WithOperators(QueryFilterOperator.AllStringOperators))
            .AddField<DateOnly>("Birthday")
            .AddReferenceDataField<Gender>("Gender", "GenderSid"))
        .WithOrderBy(o => o
            .AddField("LastName", c => c.WithDefault())
            .AddField("FirstName", c => c.WithDefault()));

    private static ConcurrentDictionary<string, Person> _people = Reset();

    public static ConcurrentDictionary<string, Person> Reset() => _people = new()
    {
        ["1"] = new Person
        {
            Id = "1",
            FirstName = "John",
            LastName = "Doe",
            Birthday = new DateOnly(1980, 1, 1),
            GenderSid = "M",
            ETag = Runtime.NewId()
        },
        ["2"] = new Person
        {
            Id = "2",
            FirstName = "Jane",
            LastName = "Doe",
            Birthday = new DateOnly(1985, 2, 2),
            GenderSid = "F",
            ETag = Runtime.NewId()
        },
        ["3"] = new Person
        {
            Id = "3",
            FirstName = "Alice",
            LastName = "Smith",
            Birthday = new DateOnly(1990, 3, 3),
            GenderSid = "F",
            ETag = Runtime.NewId()
        },
        ["4"] = new Person
        {
            Id = "4",
            FirstName = "Bob",
            LastName = "Smith",
            Birthday = new DateOnly(1995, 4, 4),
            GenderSid = "M",
            ETag = Runtime.NewId()
        }
    };

    public Task<Person?> GetAsync(string id) => Task.FromResult(_people.TryGetValue(id.Required(), out var person) ? person : null);

    public Task<Person> CreateAsync(Person person)
    {
        person.ETag = Guid.NewGuid().ToString();
        if (!_people.TryAdd(person.Id!, person))
            throw new ConflictException();

        return Task.FromResult(person);
    }

    public Task<Person> UpdateAsync(Person person)
    {
        person.ThrowIfNull();
        person.Id.ThrowIfNull();

        if (!_people.TryGetValue(person.Id!, out var existing))
            throw new NotFoundException();

        if (existing.ETag != person.ETag)
            throw new ConcurrencyException();

        person.ETag = Guid.NewGuid().ToString();
        _people[person.Id!] = person;
        return Task.FromResult(person);
    }

    public Task DeleteAsync(string id)
    {
        _people.TryRemove(id.Required(), out _);
        return Task.CompletedTask;
    }

    public Task<ItemsResult<Person>> GetByQueryAsync(QueryArgs query, PagingArgs paging)
    {
        var ir = new ItemsResult<Person>(paging)
        {
            Items = [.. _people.Values.AsQueryable().Where(QueryConfig, query).WithPaging(paging).OrderBy(QueryConfig, query)]
        };

        ir.WithTotalCount(() => _people.Count);
        return Task.FromResult(ir);
    }
}