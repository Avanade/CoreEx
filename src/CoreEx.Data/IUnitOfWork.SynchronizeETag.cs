namespace CoreEx.Data;

public partial interface IUnitOfWork
{
    /// <summary>
    /// Synchronizes the <paramref name="value"/>'s <see cref="IETag.ETag"/> with the true, underlying-store-persisted value, deriving the correlation key from <paramref name="value"/>'s own <see cref="IEntityKey.EntityKey"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value (implementing both <see cref="IEntityKey"/> and <see cref="IETag"/>) whose <see cref="IETag.ETag"/> is to be synchronized.</param>
    /// <remarks>See <see cref="SynchronizeETag{T}(CompositeKey, T)"/> for the full semantics. This convenience overload is only usable where <typeparamref name="T"/> already implements <see cref="IEntityKey"/>;
    /// where the value being synchronized is a mapped <i>contract</i> that does not (the common case for a published event's payload), use the primary <see cref="SynchronizeETag{T}(CompositeKey, T)"/> overload
    /// and supply the correlation key explicitly.</remarks>
    public void SynchronizeETag<T>(T value) where T : IEntityKey, IETag => SynchronizeETag(value.EntityKey, value);
}
