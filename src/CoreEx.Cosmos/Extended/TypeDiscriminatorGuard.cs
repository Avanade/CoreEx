namespace CoreEx.Cosmos.Extended;

/// <summary>
/// Provides the shared static guard used by <see cref="MultiSetSingleArgs{TModel}"/>/<see cref="MultiSetCollArgs{TColl, TModel}"/> to ensure their <c>TModel</c> can participate in a multi-set query.
/// </summary>
internal static class TypeDiscriminatorGuard
{
    /// <summary>
    /// Checks that <typeparamref name="TModel"/> implements <see cref="IReadOnlyTypeDiscriminator"/> (or the mutable <see cref="ITypeDiscriminator"/>); throws where not supported.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <remarks>Mirrors the same check performed by <see cref="CosmosDbModelOptions{TModel}.WithTypeDiscriminator(string?)"/> - a multi-set query is fundamentally a discriminator-keyed demux of a single
    /// container/partition, so a <c>TModel</c> unable to carry a type discriminator can never be used with it. Invoked once (from a static constructor) per closed generic <c>TModel</c>, not per instance.</remarks>
    public static void Check<TModel>() where TModel : class, IEntityKey, new()
    {
        if (!FeatureSupport.Determine<TModel, ITypeDiscriminator, IReadOnlyTypeDiscriminator>().IsSupported)
            throw new NotSupportedException($"'{typeof(TModel).Name}' cannot be used within a multi-set query; the model must implement {nameof(IReadOnlyTypeDiscriminator)} to enable.");
    }
}
