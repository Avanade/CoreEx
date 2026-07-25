using CoreEx.Data.GraphQL.Internal;
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
}
