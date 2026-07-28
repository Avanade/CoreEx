using CoreEx.Data.GraphQL.Internal;
using CoreEx.Data.GraphQL.Test.Unit.Model;
using CoreEx.Json;

namespace CoreEx.Data.GraphQL.Test.Unit.Internal;

[TestFixture]
public class GraphQLTypeShapeTests
{
    private sealed class DateShapeModel
    {
        public DateOnly EffectiveDate { get; set; }

        public TimeOnly StartTime { get; set; }
    }

    [Test]
    public void GetFieldMap_DateOnlyAndTimeOnlyProperties_ClassifiedAsScalarNotComplex()
    {
        var map = GraphQLTypeShape.GetFieldMap(typeof(DateShapeModel), JsonDefaults.SerializerOptions);

        map["effectiveDate"].IsComplex.Should().BeFalse();
        map["startTime"].IsComplex.Should().BeFalse();
    }

    [Test]
    public void GetFieldMap_NestedBeyondMaxDepth_StopsRecursingWithEmptyFieldMap()
    {
        // Depth0 (the GetFieldMap root) is depth 0; each ".next.Children.Value" hop increments depth by one, matching BuildFieldMap's own recursion exactly.
        var map = GraphQLTypeShape.GetFieldMap(typeof(Depth0), JsonDefaults.SerializerOptions);

        for (var depth = 0; depth < GraphQLTypeShape.MaxDepth - 1; depth++)
        {
            map["next"].IsComplex.Should().BeTrue();
            map["next"].Children.Should().NotBeNull();
            map = map["next"].Children!.Value; // Advances to the field map for Depth{depth+1}.
        }

        // 'map' is now Depth7's field map (depth 7 < MaxDepth of 8): its own 'next' field should still be populated.
        map["next"].IsComplex.Should().BeTrue();

        // Depth8's field map is built at depth 8, which is no longer < MaxDepth - so it must be empty, even though Depth8 itself declares a further 'next' (Depth9) property.
        var depth8Map = map["next"].Children!.Value;
        depth8Map.Should().BeEmpty();
    }
}
