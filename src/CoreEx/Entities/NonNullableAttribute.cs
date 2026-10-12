namespace CoreEx.Entities;

/// <summary>
/// Specifies that the property is not intended to be nullable, even though it may be nullable in the code.
/// </summary>
/// <remarks>This attribute is typically used to indicate that a property for the likes of OpenAPI is not-nullable. This is intended for a contract (DTO) that is typically used to transfer data between
/// layers or systems, and this attribute helps to signal non-nullability in such scenarios.</remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class NonNullableAttribute : Attribute { }
