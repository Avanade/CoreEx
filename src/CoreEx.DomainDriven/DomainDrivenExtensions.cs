namespace CoreEx.DomainDriven;

/// <summary>
/// Provides standard extension methods for domain-driven design (DDD) related functionality.
/// </summary>
public static class DomainDrivenExtensions
{
    extension(PersistenceState persistenceState)
    {
        /// <summary>
        /// Indicates whether the <paramref name="persistenceState"/> is <see cref="PersistenceState.New"/>.
        /// </summary>
        public bool IsNew => persistenceState == PersistenceState.New;

        /// <summary>
        /// Indicates whether the <paramref name="persistenceState"/> is <see cref="PersistenceState.NotModified"/>.
        /// </summary>
        public bool IsNotModified => persistenceState == PersistenceState.NotModified;

        /// <summary>
        /// Indicates whether the <paramref name="persistenceState"/> is <see cref="PersistenceState.Modified"/>.
        /// </summary>
        public bool IsModified => persistenceState == PersistenceState.Modified;

        /// <summary>
        /// Indicates whether the <paramref name="persistenceState"/> is <see cref="PersistenceState.Removed"/>.
        /// </summary>
        public bool IsRemoved => persistenceState == PersistenceState.Removed;

        /// <summary>
        /// Indicates whether the <paramref name="persistenceState"/> is <i>not</i> <see cref="PersistenceState.Removed"/>.
        /// </summary>
        public bool IsNotRemoved => persistenceState != PersistenceState.Removed;

        /// <summary>
        /// Indicates whether the <paramref name="persistenceState"/> is <see cref="PersistenceState.New"/> or <see cref="PersistenceState.Modified"/>.
        /// </summary>
        public bool IsNewOrModified => persistenceState == PersistenceState.New || persistenceState == PersistenceState.Modified;
    }
}