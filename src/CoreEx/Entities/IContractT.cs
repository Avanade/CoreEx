namespace CoreEx.Entities;

/// <summary>
/// Enables the core contract capabilities.
/// </summary>
/// <typeparam name="T">The contract <see cref="Type"/>.</typeparam>
public interface IContract<T> : IContract, IEquatable<T> { }