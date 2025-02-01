// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Diagnostics.CodeAnalysis;

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
            QueryNoTracking = template.QueryNoTracking;
            ClearChangeTrackerAfterGet = template.ClearChangeTrackerAfterGet;
            CleanUpResult = template.CleanUpResult;
            FilterByTenantId = template.FilterByTenantId;
            FilterByIsDeleted = template.FilterByIsDeleted;
            GetTenantId = template.GetTenantId;
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
        /// Indicates whether the result should be <see cref="Cleaner.Clean{T}(T)">cleaned up</see>.
        /// </summary>
        public bool CleanUpResult { get; set; } = false;

        /// <summary>
        /// Indicates that when the underlying model implements <see cref="ITenantId"/> it is to be filtered by the corresponding <see cref="GetTenantId"/> value. Defaults to <c>true</c>.
        /// </summary>
        public bool FilterByTenantId { get; set; } = true;

        /// <summary>
        /// Indicates that when the underlying model implements <see cref="ILogicallyDeleted"/> it should filter out any models where the <see cref="ILogicallyDeleted.IsDeleted"/> equals <c>true</c>. Defaults to <c>true</c>.
        /// </summary>
        public bool FilterByIsDeleted { get; set; } = true;

        /// <summary>
        /// Gets or sets the <i>get</i> tenant identifier function; defaults to <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext.TenantId"/>.
        /// </summary>
        public Func<string?> GetTenantId { get; set; } = () => ExecutionContext.HasCurrent ? ExecutionContext.Current.TenantId : null;

        /// <summary>
        /// Checks the <see cref="SaveChanges"/> and <see cref="Refresh"/> properties to ensure that they are valid.
        /// </summary>
        public readonly void CheckSaveArgs()
        {
            if (Refresh && !SaveChanges)
                throw new InvalidOperationException($"The {nameof(Refresh)} property cannot be set to true without the {nameof(SaveChanges)} also being set to true (given the save will occur after this method call).");
        }

        /// <summary>
        /// Determines whether the model is considered valid; i.e. is not <c>null</c>, and where applicable, checks the <see cref="ITenantId.TenantId"/> and <see cref="ILogicallyDeleted.IsDeleted"/> properties.
        /// </summary>
        /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
        /// <param name="model">The model value.</param>
        /// <param name="checkIsDeleted">Indicates whether to perform the <see cref="ILogicallyDeleted"/> check.</param>
        /// <param name="checkTenantId">Indicates whether to perform the <see cref="ITenantId"/> check.</param>
        /// <returns><c>true</c> indicates that the model is valid; otherwise, <c>false</c>.</returns>
        /// <remarks>This is used by the underlying <see cref="EfDb{TDbContext}"/> operations to ensure the model is considered valid or not, and then handled accordingly. The query-based operations leverage the corresponding <see cref="WhereModelValid"/> filter.
        /// <para>This leverages the <see cref="WhereModelValid"/> to perform the check to ensure consistency of implementation.</para></remarks>
        public readonly bool IsModelValid<TModel>([NotNullWhen(true)] TModel? model, bool checkIsDeleted = true, bool checkTenantId = true) where TModel : class
            => model != default && WhereModelValid(new[] { model }.AsQueryable(), checkIsDeleted, checkTenantId).Any();

        /// <summary>
        /// Filters the <paramref name="query"/> to include only valid models; i.e. checks the <see cref="ITenantId.TenantId"/> and <see cref="ILogicallyDeleted.IsDeleted"/> properties.
        /// </summary>
        /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
        /// <param name="query">The current query.</param>
        /// <param name="checkIsDeleted">Indicates whether to perform the <see cref="ILogicallyDeleted"/> check.</param>
        /// <param name="checkTenantId">Indicates whether to perform the <see cref="ITenantId"/> check.</param>
        /// <returns>The updated query.</returns>
        /// <remarks>This is used by the underlying <see cref="EfDbQuery{TModel}"/> and <see cref="EfDbQuery{T, TModel}"/> to apply standardized filtering.</remarks>
        public readonly IQueryable<TModel> WhereModelValid<TModel>(IQueryable<TModel> query, bool checkIsDeleted = true, bool checkTenantId = true) where TModel : class
        {
            query = query.ThrowIfNull(nameof(query));

            if (checkTenantId && FilterByTenantId && typeof(ITenantId).IsAssignableFrom(typeof(TModel)))
            {
                var tenantId = GetTenantId();
                query = query.Where(x => ((ITenantId)x).TenantId == tenantId);
            }

            if (checkIsDeleted && FilterByIsDeleted && typeof(ILogicallyDeleted).IsAssignableFrom(typeof(TModel)))
                query = query.Where(x => ((ILogicallyDeleted)x).IsDeleted == null || ((ILogicallyDeleted)x).IsDeleted == false);

            return query;
        }
    }
}