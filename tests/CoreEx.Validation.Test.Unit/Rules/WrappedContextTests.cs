namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class WrappedContextTests
{
    [Test]
    public void Unwrap_ErrorMessageUsesInnerValueAndOuterLabel()
    {
        var validator = Validator.Create<Request>()
            .HasProperty(x => x.Text, rule => rule.WithText("Additional notes").Unwrap().String(3).WithMessage("{0}: {1} is too long."));

        validator.ValidateAsError(new Request { Text = "long" }, "text", "Additional notes: 'long' is too long.");
    }

    [Test]
    public void Unwrap_OverrideWritesBackAndRefreshesTheChain()
    {
        var validator = Validator.Create<Request>()
            .HasProperty(x => x.Text, rule => rule.Unwrap().String(3)
                .CompareValues(["ABC"], StringComparer.OrdinalIgnoreCase, overrideValueWhereMatched: true)
                .CompareValues(["ABC"]));
        var request = new Request { Text = "abc" };

        validator.ValidateAsSuccess(new Request());
        validator.ValidateAsSuccess(request);
        request.Text.IsSpecified.Should().BeTrue();
        request.Text.Value.Should().Be("ABC");
    }

    [Test]
    public void Unwrap_ReadOnlyPropertyCannotBeOverridden()
    {
        var validator = Validator.Create<Request>()
            .HasProperty(x => x.ReadOnlyText, rule => rule.Unwrap().CompareValues(["ABC"], StringComparer.OrdinalIgnoreCase, overrideValueWhereMatched: true));

        Assert.ThrowsAsync<InvalidOperationException>(async () => await validator.ValidateAsync(new Request()))
            .Message.Should().Be("The property 'ReadOnlyText' is read-only and cannot be overridden.");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Unwrap_EntityUsesInnerValueAndNestedErrorPath(bool configure)
    {
        var childValidator = Validator.Create<Child>().HasProperty(x => x.Name, rule => rule.Mandatory());
        var validator = Validator.Create<Request>();
        if (configure)
            validator.HasProperty(x => x.Child, rule => rule.Unwrap().Entity(with => with.WithValidator(childValidator)));
        else
            validator.HasProperty(x => x.Child, rule => rule.Unwrap().Entity(childValidator));

        validator.ValidateAsSuccess(new Request());
        validator.ValidateAsSuccess(new Request { Child = CoreEx.Entities.Optional<Child?>.Specified(null) });
        validator.ValidateAsSuccess(new Request { Child = new Child { Name = "abc" } });
        validator.ValidateAsError(new Request { Child = new Child() }, "child.name", "Name is required.");
    }

    [Test]
    public void Unwrap_CollectionAndDictionaryRetainNestedErrorPaths()
    {
        var childValidator = Validator.Create<Child>().HasProperty(x => x.Name, rule => rule.Mandatory());
        var collectionValidator = Validator.Create<Request>()
            .HasProperty(x => x.Children, rule => rule.Unwrap().Collection(with => with.WithItemValidator(childValidator)));
        var dictionaryValidator = Validator.Create<Request>()
            .HasProperty(x => x.ChildrenByName, rule => rule.Unwrap().Dictionary(with => with.WithValueValidator(childValidator)));

        collectionValidator.ValidateAsSuccess(new Request());
        collectionValidator.ValidateAsSuccess(new Request { Children = new List<Child> { new() { Name = "abc" } } });
        collectionValidator.ValidateAsError(new Request { Children = new List<Child> { new() } }, "children[0].name", "Name is required.");
        dictionaryValidator.ValidateAsSuccess(new Request());
        dictionaryValidator.ValidateAsSuccess(new Request { ChildrenByName = new Dictionary<string, Child> { ["Home"] = new() { Name = "abc" } } });
        dictionaryValidator.ValidateAsError(new Request { ChildrenByName = new Dictionary<string, Child> { ["Home"] = new() } }, "childrenByName.Home.name", "Name is required.");
    }

    [Test]
    public void Unwrap_NullableStringNormalizationPreservesSpecifiedNull()
    {
        var validator = Validator.Create<Request>()
            .HasProperty(x => x.NullableText, rule => rule.Unwrap()
                .CompareValues(["ABC"], StringComparer.OrdinalIgnoreCase, overrideValueWhereMatched: true).CompareValues(["ABC"]));
        var request = new Request { NullableText = "abc" };
        var nullRequest = new Request { NullableText = CoreEx.Entities.Optional<string?>.Specified(null) };

        validator.ValidateAsSuccess(request);
        request.NullableText.Value.Should().Be("ABC");
        validator.ValidateAsSuccess(nullRequest);
        nullRequest.NullableText.IsSpecified.Should().BeTrue();
        nullRequest.NullableText.Value.Should().BeNull();
    }

    [TestCase("CompareValues", "ZZZ")]
    [TestCase("CompareValuesDelegate", "ZZZ")]
    [TestCase("Between", "ZZZ")]
    [TestCase("BetweenDelegate", "ZZZ")]
    [TestCase("InclusiveBetween", "ZZZ")]
    [TestCase("InclusiveBetweenDelegate", "ZZZ")]
    [TestCase("ExclusiveBetween", "ZZZ")]
    [TestCase("ExclusiveBetweenDelegate", "ZZZ")]
    [TestCase("Compare", "ZZZ")]
    [TestCase("CompareDelegate", "ZZZ")]
    [TestCase("Equal", "ZZZ")]
    [TestCase("EqualDelegate", "ZZZ")]
    [TestCase("NotEqual", "ZZZ")]
    [TestCase("NotEqualDelegate", "ZZZ")]
    [TestCase("LessThan", "ZZZ")]
    [TestCase("LessThanDelegate", "ZZZ")]
    [TestCase("LessThanOrEqualTo", "ZZZ")]
    [TestCase("LessThanOrEqualToDelegate", "ZZZ")]
    [TestCase("GreaterThan", "000")]
    [TestCase("GreaterThanDelegate", "000")]
    [TestCase("GreaterThanOrEqualTo", "000")]
    [TestCase("GreaterThanOrEqualToDelegate", "000")]
    [TestCase("CompareProperty", "ZZZ")]
    public async Task Unwrap_ComparisonsAcceptNullableStringsDirectly(string comparison, string invalid)
    {
        var validator = Validator.Create<Request>().HasProperty(x => x.NullableText, rule => ConfigureComparison(rule.Unwrap(), comparison));

        validator.ValidateAsSuccess(new Request());
        validator.ValidateAsSuccess(new Request { NullableText = CoreEx.Entities.Optional<string?>.Specified(null) });
        validator.ValidateAsSuccess(new Request { NullableText = "ABC", Code = "ABC" });
        var result = await validator.ValidateAsync(new Request { NullableText = invalid, Code = "ABC" });
        result.HasErrors.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Property.Should().Be("nullableText");
    }

    private static CoreEx.Validation.Rules.IPropertyRule<Request, string> ConfigureComparison(CoreEx.Validation.Rules.IPropertyRule<Request, string?> rule, string comparison) => comparison switch
    {
        "CompareValues" => rule.CompareValues(["ABC"]),
        "CompareValuesDelegate" => rule.CompareValues(_ => ["ABC"]),
        "Between" => rule.Between("AAA", "CCC"),
        "BetweenDelegate" => rule.Between(_ => "AAA", _ => "CCC"),
        "InclusiveBetween" => rule.InclusiveBetween("AAA", "CCC"),
        "InclusiveBetweenDelegate" => rule.InclusiveBetween(_ => "AAA", _ => "CCC"),
        "ExclusiveBetween" => rule.ExclusiveBetween("AAA", "CCC"),
        "ExclusiveBetweenDelegate" => rule.ExclusiveBetween(_ => "AAA", _ => "CCC"),
        "Compare" => rule.Compare(CompareOperator.Equal, "ABC"),
        "CompareDelegate" => rule.Compare(CompareOperator.Equal, _ => "ABC"),
        "Equal" => rule.Equal("ABC"),
        "EqualDelegate" => rule.Equal(_ => "ABC"),
        "NotEqual" => rule.NotEqual("ZZZ"),
        "NotEqualDelegate" => rule.NotEqual(_ => "ZZZ"),
        "LessThan" => rule.LessThan("CCC"),
        "LessThanDelegate" => rule.LessThan(_ => "CCC"),
        "LessThanOrEqualTo" => rule.LessThanOrEqualTo("CCC"),
        "LessThanOrEqualToDelegate" => rule.LessThanOrEqualTo(_ => "CCC"),
        "GreaterThan" => rule.GreaterThan("AAA"),
        "GreaterThanDelegate" => rule.GreaterThan(_ => "AAA"),
        "GreaterThanOrEqualTo" => rule.GreaterThanOrEqualTo("AAA"),
        "GreaterThanOrEqualToDelegate" => rule.GreaterThanOrEqualTo(_ => "AAA"),
        "CompareProperty" => rule.CompareProperty(CompareOperator.Equal, x => x.Code),
        _ => throw new ArgumentOutOfRangeException(nameof(comparison))
    };

    [Test]
    public void Common_PreservesOuterLabelAndValueFormatting()
    {
        var common = Validator.CreateCommon<decimal>(rule => rule.GreaterThan(10).WithMessage("{0}: {1} is too small."));
        var validator = Validator.Create<Request>()
            .HasProperty(x => x.Amount, rule => rule.WithText("Order amount").WithFormat("F2", System.Globalization.CultureInfo.InvariantCulture, null).Common(common));

        validator.ValidateAsError(new Request { Amount = 2m }, "amount", "Order amount: 2.00 is too small.");
    }

    [Test]
    public void Unwrap_CommonPreservesOuterLabel()
    {
        var common = Validator.CreateCommon<string>(rule => rule.String(3));
        var validator = Validator.Create<Request>()
            .HasProperty(x => x.Text, rule => rule.WithText("Additional notes").Unwrap().Common(common));

        validator.ValidateAsError(new Request { Text = "long" }, "text", "Additional notes must not exceed 3 character(s) in length.");
    }

    [Test]
    public void Common_OverrideUpdatesOrdinaryAndWrappedProperties()
    {
        var common = Validator.CreateCommon<string>(rule => rule.CompareValues(["ABC"], StringComparer.OrdinalIgnoreCase, overrideValueWhereMatched: true).CompareValues(["ABC"]));
        var validator = Validator.Create<Request>()
            .HasProperty(x => x.Code, rule => rule.Common(common).CompareValues(["ABC"]))
            .HasProperty(x => x.Text, rule => rule.Unwrap().Common(common).CompareValues(["ABC"]));
        var request = new Request { Code = "abc", Text = "abc" };

        validator.ValidateAsSuccess(request);
        request.Code.Should().Be("ABC");
        request.Text.Value.Should().Be("ABC");
    }

    [Test]
    public void Common_OverrideRespectsReadOnlyProperties()
    {
        var common = Validator.CreateCommon<string>(rule => rule.CompareValues(["ABC"], StringComparer.OrdinalIgnoreCase, overrideValueWhereMatched: true));
        var validator = Validator.Create<Request>()
            .HasProperty(x => x.ReadOnlyText, rule => rule.Unwrap().Common(common));

        Assert.ThrowsAsync<InvalidOperationException>(async () => await validator.ValidateAsync(new Request()))
            .Message.Should().Be("The property 'ReadOnlyText' is read-only and cannot be overridden.");
    }

    [Test]
    public void Common_NullableNumberDoesNotBecomeZero()
    {
        var common = Validator.CreateCommon<int>(rule => rule.Between(1, 3).Equal(2));
        var ordinary = Validator.Create<Request>().HasProperty(x => x.NullableCount, rule => rule.Common(common));
        var wrapped = Validator.Create<Request>().HasProperty(x => x.WrappedNullableCount, rule => rule.Unwrap().Common(common));

        ordinary.ValidateAsSuccess(new Request());
        ordinary.ValidateAsSuccess(new Request { NullableCount = 2 });
        ordinary.ValidateAsError(new Request { NullableCount = 0 }, "nullableCount", "Nullable count must be between '1' and '3'.");
        wrapped.ValidateAsSuccess(new Request { WrappedNullableCount = CoreEx.Entities.Optional<int?>.Specified(null) });
        wrapped.ValidateAsSuccess(new Request { WrappedNullableCount = 2 });
        wrapped.ValidateAsError(new Request { WrappedNullableCount = 0 }, "wrappedNullableCount", "Wrapped nullable count must be between '1' and '3'.");
    }

    [Test]
    public void Common_NotNullDistinguishesNullFromZero()
    {
        var common = Validator.CreateCommon<int>(rule => rule.Between(0, 3).NotNull().WithMessage("{0}: {1} must be supplied."));
        var ordinary = Validator.Create<Request>().HasProperty(x => x.NullableCount, rule => rule.Common(common));
        var wrapped = Validator.Create<Request>().HasProperty(x => x.WrappedNullableCount, rule => rule.Unwrap().Common(common));

        ordinary.ValidateAsError(new Request(), "nullableCount", "Nullable count: (null) must be supplied.");
        ordinary.ValidateAsSuccess(new Request { NullableCount = 0 });
        wrapped.ValidateAsSuccess(new Request());
        wrapped.ValidateAsError(new Request { WrappedNullableCount = CoreEx.Entities.Optional<int?>.Specified(null) }, "wrappedNullableCount", "Wrapped nullable count: (null) must be supplied.");
        wrapped.ValidateAsSuccess(new Request { WrappedNullableCount = 0 });
    }

    [TestCase(false, null)]
    [TestCase(false, 0)]
    [TestCase(false, 2)]
    [TestCase(true, null)]
    [TestCase(true, 0)]
    [TestCase(true, 2)]
    public void Common_TypedMetadataReadsDoNotAllocateAndPreserveNullState(bool wrapped, int? value)
    {
        var invoked = false;
        var inner = Validator.CreateCommon<int>(rule => rule.Between(0, 3))
            .AdditionalAsync((context, _) =>
            {
                invoked = true;
                var metadata = ((CoreEx.Validation.Abstractions.IPropertyContext)context).Metadata;
                metadata.Type.Should().Be(typeof(int));
                context.IsValueNull.Should().Be(!value.HasValue);
                context.Value.Should().Be(value.GetValueOrDefault());

                var entity = context.Entity;
                var total = 0;
                for (var i = 0; i < 1000; i++)
                    total += metadata.GetValue<int>(entity);

                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 10000; i++)
                    total += metadata.GetValue<int>(entity);

                var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                allocated.Should().Be(0, "typed metadata reads must not box each integer value");
                total.Should().Be(11000 * value.GetValueOrDefault());
                return Task.CompletedTask;
            });
        var common = Validator.CreateCommon<int>(rule => rule.Common(inner).Between(0, 3));
        var validator = Validator.Create<Request>();
        if (wrapped)
            validator.HasProperty(x => x.WrappedNullableCount, rule => rule.Unwrap().Common(common));
        else
            validator.HasProperty(x => x.NullableCount, rule => rule.Common(common));

        validator.ValidateAsSuccess(new Request { NullableCount = value, WrappedNullableCount = CoreEx.Entities.Optional<int?>.Specified(value) });
        invoked.Should().BeTrue();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NumericOverride_RefreshesOrdinaryAndWrappedNullableValues(bool useCommon)
    {
        var common = Validator.CreateCommon<int>(rule => rule.CompareValues([1], new LastDigitComparer(), overrideValueWhereMatched: true).Equal(1));
        var validator = Validator.Create<Request>();
        if (useCommon)
        {
            validator.HasProperty(x => x.Count, rule => rule.Common(common).Equal(1))
                .HasProperty(x => x.NullableCount, rule => rule.Common(common).Equal(1))
                .HasProperty(x => x.WrappedCount, rule => rule.Unwrap().Common(common).Equal(1))
                .HasProperty(x => x.WrappedNullableCount, rule => rule.Unwrap().Common(common).Equal(1));
        }
        else
        {
            validator.HasProperty(x => x.Count, rule => rule.CompareValues([1], new LastDigitComparer(), overrideValueWhereMatched: true).Equal(1))
                .HasProperty(x => x.NullableCount, rule => rule.CompareValues([1], new LastDigitComparer(), overrideValueWhereMatched: true).Equal(1))
                .HasProperty(x => x.WrappedCount, rule => rule.Unwrap().CompareValues([1], new LastDigitComparer(), overrideValueWhereMatched: true).Equal(1))
                .HasProperty(x => x.WrappedNullableCount, rule => rule.Unwrap().CompareValues([1], new LastDigitComparer(), overrideValueWhereMatched: true).Equal(1));
        }

        var request = new Request { Count = 11, NullableCount = 11, WrappedCount = 11, WrappedNullableCount = 11 };
        validator.ValidateAsSuccess(request);
        request.Count.Should().Be(1);
        request.NullableCount.Should().Be(1);
        request.WrappedCount.Value.Should().Be(1);
        request.WrappedNullableCount.Value.Should().Be(1);
    }

    [Test]
    public void Common_SecondaryValidationObservesNormalizedValue()
    {
        var common = Validator.CreateCommon<string>(rule => rule.CompareValues(["ABC"], StringComparer.OrdinalIgnoreCase, overrideValueWhereMatched: true))
            .AdditionalAsync((context, _) =>
            {
                context.Value.Should().Be("ABC");
                context.Override("DEF");
                return Task.CompletedTask;
            })
            .AdditionalAsync((context, _) =>
            {
                context.Value.Should().Be("DEF");
                return Task.CompletedTask;
            });

        var validator = Validator.Create<Request>()
            .HasProperty(x => x.Text, rule => rule.Unwrap().Common(common).CompareValues(["DEF"]));
        var request = new Request { Text = "abc" };

        validator.ValidateAsSuccess(request);
        request.Text.Value.Should().Be("DEF");
    }

    private sealed class LastDigitComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) => x % 10 == y % 10;
        public int GetHashCode(int obj) => obj % 10;
    }

    public class Request
    {
        public string? Code { get; set; }
        public int Count { get; set; }
        public int? NullableCount { get; set; }
        public CoreEx.Entities.Optional<int> WrappedCount { get; set; }
        public CoreEx.Entities.Optional<int?> WrappedNullableCount { get; set; }
        public CoreEx.Entities.Optional<string> Text { get; set; }
        public CoreEx.Entities.Optional<string?> NullableText { get; set; }
        public CoreEx.Entities.Optional<string> ReadOnlyText => "abc";
        public CoreEx.Entities.Optional<Child?> Child { get; set; }
        public CoreEx.Entities.Optional<List<Child>> Children { get; set; }
        public CoreEx.Entities.Optional<Dictionary<string, Child>> ChildrenByName { get; set; }
        public decimal Amount { get; set; }
    }

    public class Child
    {
        public string? Name { get; set; }
    }
}
