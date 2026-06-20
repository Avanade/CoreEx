namespace CoreEx.Validation.Abstractions;

/// <summary>
/// Enables validation context for an entity.
/// </summary>
public interface IValidationContext : IValidationResult
{
    /// <summary>
    /// Gets the entity <see cref="Type"/>.
    /// </summary>
    Type EntityType { get; }

    /// <summary>
    /// Gets the entity prefix used for fully qualified <i>entity.property</i> naming (<see langword="null"/> represents the root).
    /// </summary>
    string? FullyQualifiedEntityName { get; }

    /// <summary>
    /// Gets the entity prefix used for fully qualified JSON <i>entity.property</i> naming (<see langword="null"/> represents the root).
    /// </summary>
    string? FullyQualifiedJsonEntityName { get; }

    /// <summary>
    /// Indicates whether JSON names were used for the <see cref="MessageItem.Property"/>.
    /// </summary>
    /// <remarks>See <see cref="ValidationArgs.DefaultUseJsonNames"/> and <see cref="ValidationArgs.UseJsonNames"/>.</remarks>
    bool UseJsonNames { get; }

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> used for JSON property naming.
    /// </summary>
    JsonSerializerOptions? JsonSerializerOptions { get; }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> to use when resolving services.
    /// </summary>
    /// <remarks>The <see cref="ExecutionContext.ServiceProvider"/> will be used as the default where not specified.</remarks>
    IServiceProvider? ServiceProvider { get; }

    /// <summary>
    /// Gets the additional parameters (see <see cref="ValidationArgs.Parameters"/>).
    /// </summary>
    IDictionary<string, object?> Parameters { get; }

    /// <summary>
    /// Determines whether the specified fully qualified property has an error.
    /// </summary>
    /// <param name="fullyQualifiedPropertyName">The fully qualified property name.</param>
    /// <returns><see langword="true"/> where an error exists for the specified property; otherwise, <see langword="false"/>.</returns>
    bool HasError(string fullyQualifiedPropertyName);
}