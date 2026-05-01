namespace Contoso.Orders.Contracts;

[ReferenceData]
public partial class OrderStatus : ReferenceData<OrderStatus> { }

public class OrderStatusCollection() : ReferenceDataCollection<OrderStatus>(ReferenceDataSortOrder.Code) { }