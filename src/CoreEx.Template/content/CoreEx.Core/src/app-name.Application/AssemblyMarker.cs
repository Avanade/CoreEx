namespace app-name.Application;

/// <summary>Stable assembly marker for this project.</summary>
/// <remarks>Resolves this project's <see cref="System.Reflection.Assembly"/> for dynamic <c>[ScopedService]</c> registration via <c>AddDynamicServicesUsing(typeof(AssemblyMarker).Assembly)</c> in the host. A neutral marker is used deliberately — anchoring on a real type (e.g. a service) would read as if the registration were scoped to that one type and would break if it were renamed/removed. Do not remove and do not replace with a domain type.</remarks>
public abstract class AssemblyMarker;
