namespace CoreEx.EntityFrameworkCore;

/// <summary>
/// Provides the extended <see href="https://learn.microsoft.com/en-us/ef/core/">Entity Framework Core</see> arguments.
/// </summary>
public record class EfDbArgs : DatabaseArgsBase
{
    /// <summary>
    /// Indicates whether the query results will be tracked or not (see <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}(IQueryable{TEntity})"/>.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>; in that there will be <i>no</i> tracking.</remarks>
    public bool QueryTracking { get; init; } = false;

    /// <summary>
    /// Indicates whether the <see cref="EfDbModel{TModel}.GetAsync(CompositeKey, CancellationToken)"/> performs a <see cref="ChangeTracker.Clear"/> so that the model is not tracked.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>; in that there <i>will</i> be tracking.
    /// <para>The <see cref="EfDbModel{TModel}.GetAsync(CompositeKey, CancellationToken)"/> implementation performs a <see cref="Microsoft.EntityFrameworkCore.DbContext.FindAsync{TEntity}(object?[], CancellationToken)"/>
    /// internally which automatically attaches and tracks.</para>
    /// <para><b>Warning:</b> <see cref="ChangeTracker.Clear"/> detaches <i>every</i> entity currently tracked by the underlying <see cref="DbContext"/> — not just the model just retrieved. Because the <see cref="DbContext"/> is typically
    /// scoped and may be shared across multiple repository calls within the same unit of work, enabling this on one model's <see cref="EfDbArgs"/> will also silently detach unrelated entities tracked elsewhere in that same scope
    /// (e.g. pending unsaved changes from another repository call in the same <c>SaveChanges</c> batch). Only enable this where the <see cref="DbContext"/> is known not to be shared for the duration of the operation.</para></remarks>
    public bool ClearChangeTrackerAfterGet { get; init; } = false;

    /// <summary>
    /// Indicates that the underlying <see cref="DbContext"/> <see cref="DbContext.SaveChanges()"/> is to be performed automatically once the mutation operation is complete.
    /// </summary>
    /// <remarks>Defaults to <see langword="true"/>.
    /// <para>This is generally required for a mutation where the changes need to be realized (persisted); i.e. to get the likes of the generated row-version, etc.</para></remarks>
    public bool SaveChanges { get; set; } = true;

    /// <summary>
    /// Indicates whether to bypass all configured filters (where allowed).
    /// </summary>
    /// <remarks>This is an advanced feature that should only be used where specifically desired, and/or applying the filtering manually, to avoid unintended side-effects.</remarks>
    public bool BypassFilters { get; init; } = false;

    /// <summary>
    /// Checks the <see cref="DatabaseArgsBase.Refresh"/> and <see cref="SaveChanges"/> combination to ensure that they are valid for the operation.
    /// </summary>
    internal void CheckRefreshAndSaveChangesCombination()
    {
        if (Refresh && !SaveChanges)
            throw new InvalidOperationException($"The {nameof(Refresh)} property cannot be set to true without the {nameof(SaveChanges)} also being set to true (as the refresh is predicated on the save occurring).");
    }
}