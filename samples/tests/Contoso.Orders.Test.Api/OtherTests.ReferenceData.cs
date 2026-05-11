namespace Contoso.Orders.Test.Api;

public partial class OtherTests
{
    [Test]
    public void RefData_OrderStatuses()
    {
        var statuses = Test.Http<OrderStatus[]>()
            .Run(HttpMethod.Get, "/api/refdata/order-statuses")
            .AssertOK()
            .Value;

        statuses.Should().HaveCountGreaterThanOrEqualTo(3);
        statuses.Should().Contain(s => s.Code == "P" && s.Text == "Pending");
        statuses.Should().Contain(s => s.Code == "C" && s.Text == "Confirmed");
        statuses.Should().Contain(s => s.Code == "X" && s.Text == "Cancelled");
    }
}