using CoreEx.Data.Querying;
using CoreEx.Entities;
using CoreEx.Http;

namespace CoreEx.Data.Test.Unit.Querying;

internal class TestUtility
{
    public static readonly QueryArgsConfig Config = QueryArgsConfig.Create()
        .WithFilter(filter => filter
            .AddField<string>("LastName", c => c.WithOperators(QueryFilterOperator.AllStringOperators).AlsoCheckNotNull())
            .AddField<string>("FirstName", c => c.WithOperators(QueryFilterOperator.AllStringOperators).AsUpperCase())
            .AddField<string>("Code", c => c.WithOperators(QueryFilterOperator.EqualityOperators))
            .AddField<DateTime>("Birthday", "BirthDate")
            .AddField<int>("Age", c => c.WithHelpText("Age is but a number."))
            .AddField<decimal>("Salary")
            .AddField<bool>("IsOld", c => c.AsNullable())
            .AddNullField("Terminated", "TerminatedDate")
            .AddField<MessageType>("MessageType")
            .WithHelpText($"The OData-like filtering is awesome!"))
        .WithOrderBy(order => order
            .AddField("LastName", c => c.WithDefault())
            .AddField("FirstName", c => c.WithDefault())
            .AddField("Birthday", "BirthDate", c => c.WithDirection(QueryOrderByDirection.Descending))
            .WithHelpText($"The OData-like ordering is awesome!"));

    public static void AssertFilterSuccess(string? filter, string? expected, params object[] expectedArgs) => AssertFilterSuccess(Config, filter, expected, expectedArgs);

    public static void AssertFilterSuccess(QueryArgsConfig config, string? filter, string? expected, params object[] expectedArgs)
    {
        var result = config.FilterParser.Parse(filter);
        result.Should().NotBeNull();
        result.HasError.Should().BeFalse();
        result.Error.Should().BeNull();
        result.ToLinqString(out var args).Should().Be(expected);
        args.Should().BeEquivalentTo(expectedArgs);
    }

    public static void AssertFilterError(string? filter, string expected) => AssertFilterError(Config, filter, expected);

    public static void AssertFilterError(QueryArgsConfig config, string? filter, string expected)
    {
        var result = config.FilterParser.Parse(filter);
        result.Should().NotBeNull();
        result.HasError.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Messages.Should().NotBeNull().And.HaveCount(1);
        result.Error.Messages[0].Property.Should().Be(HttpNames.QueryFilterQueryStringName);
        result.Error.Messages[0].Text.ToString().Should().StartWith(expected);
    }

    public static void AssertOrderBySuccess(string? orderBy, string expected)
    {
        var result = Config.OrderByParser.Parse(orderBy);
        result.Should().NotBeNull();
        result.HasError.Should().BeFalse();
        result.Error.Should().BeNull();
        result.ToLinqString().Should().Be(expected);
    }

    public static void AssertOrderByError(string? orderBy, string expected)
    {
        var result = Config.OrderByParser.Parse(orderBy);
        result.Should().NotBeNull();
        result.HasError.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Messages.Should().NotBeNull().And.HaveCount(1);
        result.Error.Messages[0].Property.Should().Be(HttpNames.QueryOrderByQueryStringName);
        result.Error.Messages[0].Text.ToString().Should().StartWith(expected);
    }
}