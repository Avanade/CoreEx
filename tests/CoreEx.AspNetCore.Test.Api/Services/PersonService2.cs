using CoreEx.AspNetCore.Test.Api.Entities;
using CoreEx.Data;
using CoreEx.Data.Querying;
using CoreEx.DependencyInjection;
using CoreEx.Results;
using CoreEx.Validation;
using System.Collections.Concurrent;

namespace CoreEx.AspNetCore.Test.Api.Services;

[ScopedService<PersonService2>]
public class PersonService2
{
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

    public Task<Result<Person?>> GetAsync(string id) => Task.FromResult(Result.Ok(_people.TryGetValue(id, out var person) ? person : null));

    public Task<Result<Person>> CreateAsync(Person person)
    {
        person.ETag = Guid.NewGuid().ToString();
        if (!_people.TryAdd(person.Id!, person))
            return Task.FromResult(Result<Person>.ConflictError());

        return Task.FromResult(Result.Ok(person));
    }

    public Task<Result<Person>> UpdateAsync(Person person)
    {
        person.ThrowIfNull();
        person.Id.ThrowIfNull();

        if (!_people.TryGetValue(person.Id!, out var existing))
            return Task.FromResult(Result<Person>.NotFoundError());

        if (existing.ETag != person.ETag)
            return Task.FromResult(Result<Person>.ConcurrencyError());

        person.ETag = Guid.NewGuid().ToString();
        _people[person.Id!] = person;
        return Task.FromResult(Result.Ok(person));
    }

    public Task<Result> DeleteAsync(string id)
    {
        _people.TryRemove(id.Required(), out _);
        return Result.SuccessTask;
    }

    public Task<Result<ItemsResult<Person>>> GetByQueryAsync(QueryArgs query, PagingArgs paging)
    {
        var ir = new ItemsResult<Person>(paging)
        {
            Items = [.. _people.Values.AsQueryable().Where(PersonService.QueryConfig, query).WithPaging(paging).OrderBy(PersonService.QueryConfig, query)]
        };

        ir.WithTotalCount(() => _people.Count);
        return Result.Go(ir).AsTask();
    }
}