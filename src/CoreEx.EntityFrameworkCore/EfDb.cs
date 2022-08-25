// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Database;
using CoreEx.Entities;
using CoreEx.Mapping;
using Microsoft.EntityFrameworkCore;

namespace CoreEx.EntityFrameworkCore
{
    /// <summary>
    /// Provides the extended <b>Entity Framework</b> <see cref="IEfDbContext"/> functionality.
    /// </summary>
    /// <typeparam name="TDbContext">The <see cref="DbContext"/> <see cref="Type"/>.</typeparam>
    public class EfDb<TDbContext> : IEfDb where TDbContext : DbContext, IEfDbContext
    {
        private readonly TDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="EfDb{TDbContext}"/> class.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/>.</param>
        /// <param name="mapper">The <see cref="IMapper"/>.</param>
        /// <param name="invoker">Enables the <see cref="Invoker"/> to be overridden; defaults to <see cref="EfDbInvoker"/>.</param>
        public EfDb(TDbContext dbContext, IMapper mapper, EfDbInvoker? invoker = null)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Invoker = invoker ?? new EfDbInvoker();
        }

        /// <inheritdoc/>
        public DbContext DbContext => _dbContext;

        /// <inheritdoc/>
        public EfDbInvoker Invoker { get; }

        /// <inheritdoc/>
        public IDatabase Database => _dbContext.BaseDatabase;

        /// <inheritdoc/>
        public IMapper Mapper { get; }

        /// <inheritdoc/>
        public EfDbArgs DbArgs { get; set; } = new EfDbArgs();

        /// <inheritdoc/>
        public EfDbQuery<T, TModel> Query<T, TModel>(EfDbArgs args, Func<IQueryable<TModel>, EfDbArgs, IQueryable<TModel>>? query = null) where T : class, new() where TModel : class, new() => new(this, args, query);

        /// <inheritdoc/>
        public Task<T?> GetAsync<T, TModel>(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken = default) where T : class, new() where TModel : class, new()
            => Invoker.InvokeAsync(this, key, (key, ct) => FindAsync<T, TModel>(key.Args.ToArray(), ct), cancellationToken);

        /// <inheritdoc/>
        public async Task<T> CreateAsync<T, TModel>(EfDbArgs args, T value, CancellationToken cancellationToken = default) where T : class, new() where TModel : class, new()
        {
            CheckSaveArgs(args);
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            ChangeLog.PrepareCreated(value);
            Cleaner.ResetTenantId(value);

            return await Invoker.InvokeAsync(this, args, value, async (args, value, ct) =>
            {
                TModel model = Mapper.Map<T, TModel>(value, Mapping.OperationTypes.Create) ?? throw new InvalidOperationException("Mapping to the EF model must not result in a null value.");
                Cleaner.ResetTenantId(model);

                DbContext.Add(model);

                if (args.SaveChanges)
                    await DbContext.SaveChangesAsync(true, ct).ConfigureAwait(false);

                return args.Refresh ? Mapper.Map<TModel, T>(model, Mapping.OperationTypes.Get)! : value;
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<T> UpdateAsync<T, TModel>(EfDbArgs args, T value, CancellationToken cancellationToken = default) where T : class, new() where TModel : class, new()
        {
            CheckSaveArgs(args);
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            ChangeLog.PrepareUpdated(value);
            Cleaner.ResetTenantId(value);

            return await Invoker.InvokeAsync(this, args, value, async (args, value, ct) =>
            {
                // Check (find) if the entity exists.
                var model = await DbContext.FindAsync<TModel>(GetEfKeys(value), ct).ConfigureAwait(false);
                if (model == null)
                    throw new NotFoundException();

                // Remove the entity from the tracker before we attempt to update; otherwise, will use existing rowversion and concurrency will not work as expected.
                DbContext.Remove(model);
                DbContext.ChangeTracker.AcceptAllChanges();

                Mapper.Map(value, model, Mapping.OperationTypes.Update);

                DbContext.Update(model);

                if (args.SaveChanges)
                    await DbContext.SaveChangesAsync(true, ct).ConfigureAwait(false);

                return args.Refresh ? Mapper.Map<TModel, T>(model, Mapping.OperationTypes.Get)! : value;
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync<T, TModel>(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken = default) where T : class where TModel : class, new()
        {
            CheckSaveArgs(args);

            await Invoker.InvokeAsync(this, args, key, async (args, key, ct) =>
            {
                // A pre-read is required to get the row version for concurrency.
                var model = await DbContext.FindAsync<TModel>(key.Args.ToArray(), ct).ConfigureAwait(false);
                if (model == null)
                    throw new NotFoundException();

                // Delete; either logically or physically.
                if (model is ILogicallyDeleted emld)
                {
                    emld.IsDeleted = true;
                    DbContext.Update(model);
                }
                else
                    DbContext.Remove(model);

                if (args.SaveChanges)
                    await DbContext.SaveChangesAsync(true, ct).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs the EF select single (find).
        /// </summary>
        private async Task<T?> FindAsync<T, TModel>(object?[] keys, CancellationToken cancellationToken) where T : class, new() where TModel : class, new()
        {
            var model = await DbContext.FindAsync<TModel>(keys, cancellationToken).ConfigureAwait(false);
            if (model == default)
                return default!;

            return Mapper.Map<T>(model) ?? throw new InvalidOperationException("Mapping from the EF model must not result in a null value.");
        }

        /// <summary>
        /// Gets the <b>Entity Framework</b> keys from the value.
        /// </summary>
        /// <param name="value">The entity value.</param>
        /// <returns>The key values.</returns>
        /// <remarks>The <paramref name="value"/> must implement either <see cref="IIdentifier"/> or <see cref="IPrimaryKey"/>.</remarks>
        public static object?[] GetEfKeys(object value) => value switch
        {
            IIdentifier ii => new object?[] { ii.Id! },
            IPrimaryKey pk => pk.PrimaryKey.Args.ToArray(),
            _ => throw new NotSupportedException($"Value Type must implement either {nameof(IIdentifier)} or {nameof(IPrimaryKey)}."),
        };

        /// <summary>
        /// Check the consistency of the save arguments.
        /// </summary>
        private static void CheckSaveArgs(EfDbArgs args)
        {
            if (args.Refresh && !args.SaveChanges)
                throw new ArgumentException($"The {nameof(EfDbArgs.Refresh)} property cannot be set to true without the {nameof(EfDbArgs.SaveChanges)} also being set to true (given the save will occur after this method call).", nameof(args));
        }

        /// <inheritdoc/>
        public void WithWildcard(string? with, Action<string> action)
        {
            if (with != null)
            {
                with = Database.Wildcard.Replace(with);
                if (with != null)
                    action?.Invoke(with);
            }
        }

        /// <inheritdoc/>
        public void With<T>(T with, Action action)
        {
            if (Comparer<T>.Default.Compare(with, default!) != 0 && Comparer<T>.Default.Compare(with, default!) != 0)
            {
                if (with is not string && with is System.Collections.IEnumerable ie && !ie.GetEnumerator().MoveNext())
                    return;

                action?.Invoke();
            }
        }
    }
}