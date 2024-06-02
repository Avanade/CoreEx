// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CoreEx.EntityFrameworkCore
{
    /// <summary>
    /// Provides the extended <b>Entity Framework</b> arguments.
    /// </summary>
    public struct EfDbArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EfDbArgs"/> struct.
        /// </summary>
        public EfDbArgs() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EfDbArgs"/> struct.
        /// </summary>
        /// <param name="template">The template <see cref="EfDbArgs"/> to copy from.</param>
        public EfDbArgs(EfDbArgs template)
        {
            SaveChanges = template.SaveChanges;
            Refresh = template.Refresh;
        }

        /// <summary>
        /// Indicates that the underlying <see cref="DbContext"/> <see cref="DbContext.SaveChanges()"/> is to be performed automatically. Defaults to <c>true</c>.
        /// </summary>
        public bool SaveChanges { get; set; } = true;

        /// <summary>
        /// Indicates whether the data should be refreshed (reselected where applicable) after a <b>save</b> operation (defaults to <c>true</c>); is dependent on <see cref="SaveChanges"/> being performed.
        /// </summary>
        public bool Refresh { get; set; } = true;

        /// <summary>
        /// Indicates whether the <see cref="EfDbQuery{T, TModel}"/> will <i>not</i> track entities (see <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}(IQueryable{TEntity})"/>. Defaults to <c>true</c> in that the queried entities will not be tracked.
        /// </summary>
        public bool QueryNoTracking { get; set; } = true;

        /// <summary>
        /// Indicates whether the <see cref="IEfDb.GetAsync{T, TModel}(EfDbArgs, Entities.CompositeKey, CancellationToken)"/> performs a <see cref="ChangeTracker.Clear"/> such that the retrieved entity is not tracked. Defaults to <c>false</c>.
        /// </summary>
        /// <remarks>The <see cref="EfDb{TDbContext}.GetAsync{T, TModel}(EfDbArgs, Entities.CompositeKey, CancellationToken)"/> implementation performs a <see cref="Microsoft.EntityFrameworkCore.DbContext.FindAsync{TEntity}(object?[], CancellationToken)"/> 
        /// internally which automatically attaches and tracks the retrieved entity.</remarks>
        public bool ClearChangeTrackerAfterGet { get; set; } = false;

        /// <summary>
        /// Indicates whether the result should be <see cref="Entities.Cleaner.Clean{T}(T)">cleaned up</see>.
        /// </summary>
        public bool CleanUpResult { get; set; } = false;

        /// <summary>
        /// Indicates that when the underlying model implements <see cref="Entities.ITenantId.TenantId"/> it is to be filtered by the corresponding <see cref="GetTenantId"/> value. Defaults to <c>true</c>.
        /// </summary>
        public bool FilterByTenantId { get; set; } = true;

        /// <summary>
        /// Gets or sets the <i>get</i> tenant identifier function; defaults to <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext.TenantId"/>.
        /// </summary>
        public Func<string?> GetTenantId { get; set; } = () => ExecutionContext.HasCurrent ? ExecutionContext.Current.TenantId : null;
    }
}