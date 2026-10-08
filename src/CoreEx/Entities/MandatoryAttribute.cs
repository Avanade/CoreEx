namespace CoreEx.Entities;

/// <summary>
/// Specifies that the property is mandatory and must be provided with a value.
/// </summary>
/// <remarks>This attribute is typically used to indicate that a property is required and cannot be null or empty. This is intended for the likes of OpenAPI, to mark up the property as <i>required</i>.
/// <para>This is similar to the <see cref="RequiredAttribute"/> which also has built-in behaviour within the likes of ASP.NET that errors during model binding that is not desired given usage of validators. This is
/// a side-effect free alternative.</para></remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class MandatoryAttribute : Attribute { }
