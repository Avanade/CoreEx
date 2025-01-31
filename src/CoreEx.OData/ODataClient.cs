// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.OData.Mapping;
using CoreEx.Results;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Soc = Simple.OData.Client;

namespace CoreEx.OData
{
    /// <summary>
    /// Provides the <b>OData</b> client functionality.
    /// </summary>
    /// <param name="client">The <see cref="Soc.ODataClient"/>.</param>
    /// <param name="mapper">The <see cref="IMapper"/>; defaults to <see cref="Mapper.Empty"/>.</param>
    /// <param name="invoker">Enables the <see cref="Invoker"/> to be overridden; defaults to <see cref="ODataInvoker"/>.</param>
    /// <remarks>Where <paramref name="mapper"/> is not specified then <see cref="Mapper.Empty"/> will be used. Note that an <see cref="IMapper"/> will be required where performing any operations outside of any <see cref="ODataItemCollection{T}"/> (being untyped).</remarks>
    public class ODataClient(Soc.ODataClient client, IMapper? mapper = null, ODataInvoker? invoker = null) : IOData
    {
        /// <inheritdoc/>
        public Soc.ODataClient Client { get; } = client.ThrowIfNull(nameof(client));

        /// <inheritdoc/>
        public ODataInvoker Invoker { get; } = invoker ?? new ODataInvoker();

        /// <inheritdoc/>
        public IMapper Mapper { get; } = mapper ?? CoreEx.Mapping.Mapper.Empty;

        /// <inheritdoc/>
        public ODataArgs Args { get; set; } = new ODataArgs();

        /// <inheritdoc/>
        public ODataQuery<T, TModel> Query<T, TModel>(ODataArgs queryArgs, string? collectionName, Func<Soc.IBoundClient<TModel>, Soc.IBoundClient<TModel>>? query = null) where T : class, IEntityKey, new() where TModel : class, new()
            => new(this, queryArgs, collectionName, query);

        /// <inheritdoc/>
        public async Task<Result<T?>> GetWithResultAsync<T, TModel>(ODataArgs args, string? collectionName, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new() => await Invoker.InvokeAsync(this, async(_, ct) =>
        {
            return (await GetModelAsync<TModel>(args, collectionName, key, ct).ConfigureAwait(false))
                .WhenAs(model => model is null, _ => default!, model => MapToValue<T, TModel>(args, model!));
        }, this, cancellationToken);

        /// <inheritdoc/>
        public async Task<Result<T>> CreateWithResultAsync<T, TModel>(ODataArgs args, string? collectionName, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new() => await Invoker.InvokeAsync(this, async (_, ct) =>
        {
            var model = Mapper.Map<T, TModel>(Cleaner.PrepareCreate(value.ThrowIfNull(nameof(value))), OperationTypes.Create)!;
            var created = await Client.For<TModel>(collectionName).Set(Cleaner.PrepareCreate(model)).InsertEntryAsync(true, ct).ConfigureAwait(false);
            return MapToValue<T, TModel>(args, created);
        }, this, cancellationToken);

        /// <inheritdoc/>
        public async Task<Result<T>> UpdateWithResultAsync<T, TModel>(ODataArgs args, string? collectionName, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new() => await Invoker.InvokeAsync(this, async (_, ct) =>
        {
            Cleaner.PrepareUpdate(value.ThrowIfNull(nameof(value)));
            TModel model;

            if (args.PreReadOnUpdate)
            {
                var get = (await GetModelAsync<TModel>(args, collectionName, value.EntityKey, ct).ConfigureAwait(false))
                    .When(v => v is null, _ => Result.NotFoundError());

                if (get.IsFailure)
                    return get.AsResult();

                model = Mapper.Map(value, get.Value, OperationTypes.Update)!;
            }
            else
                model = Mapper.Map<T, TModel>(value, OperationTypes.Update)!;

            var updated = await Client.For<TModel>(collectionName).Key(value.EntityKey.Args).Set(Cleaner.PrepareUpdate(model)).UpdateEntryAsync(true, ct).ConfigureAwait(false);
            return updated is null ? Result<T>.NotFoundError() : Result<T>.Ok(MapToValue<T, TModel>(args, updated));
        }, this, cancellationToken);

        /// <inheritdoc/>
        public async Task<Result> DeleteWithResultAsync<T, TModel>(ODataArgs args, string? collectionName, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new() => await Invoker.InvokeAsync(this, async (_, ct) =>
        {
            if (args.PreReadOnDelete)
            {
                var get = await Result.GoAsync(GetModelAsync<TModel>(args, collectionName, key, ct))
                    .When(model => model is null, _ => Result.NotFoundError()).ConfigureAwait(false);

                if (get.IsFailure)
                    return get.AsResult();
            }

            await Client.For<TModel>(collectionName).Key(key.Args).DeleteEntryAsync(ct).ConfigureAwait(false);
            return Result.Success;
        }, this, cancellationToken);

        /// <summary>
        /// Gets (reads) the model.
        /// </summary>
        private async Task<Result<TModel?>> GetModelAsync<TModel>(ODataArgs args, string? collectionName, CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            try
            {
                return Result.Go(await Client.For<TModel>(collectionName).Key(key.Args).FindEntryAsync(cancellationToken).ConfigureAwait(false))
                    .WhenAs<TModel, TModel?>(model => model is null || (model is ILogicallyDeleted ld && ld.IsDeleted.HasValue && ld.IsDeleted.Value), _ => args.NullOnNotFound ? default! : Result.NotFoundError(), model => model);
            }
            catch (Soc.WebRequestException odex) when (odex.Code == HttpStatusCode.NotFound && args.NullOnNotFound) { return default!; }
        }

        /// <summary>
        /// Maps from the model to the value.
        /// </summary>
        private T MapToValue<T, TModel>(ODataArgs args, TModel model) where T : class, IEntityKey, new() where TModel : class, new()
        {
            var result = Mapper.Map<T>(model, OperationTypes.Get);
            return (result is not null) ? CleanUpResult(args, result) : throw new InvalidOperationException("Mapping from the OData model must not result in a null value.");
        }

        /// <summary>
        /// Cleans up the result where specified within the args.
        /// </summary>
        internal static T CleanUpResult<T>(ODataArgs args, T value) => args.CleanUpResult ? Cleaner.Clean(value) : value;

        /// <inheritdoc/>
        public Result? HandleODataException(Soc.WebRequestException odex) => OnCosmosException(odex);

        /// <summary>
        /// Provides the <see cref="Soc.WebRequestException"/> handling as a result of <see cref="HandleODataException(Soc.WebRequestException)"/>.
        /// </summary>
        /// <param name="odex">The <see cref="Soc.WebRequestException"/>.</param>
        /// <returns>The <see cref="Result"/> containing the appropriate <see cref="IResult.Error"/>.</returns>
        /// <remarks>Where overridding and the <see cref="Soc.WebRequestException"/> is not specifically handled then invoke the base to ensure any standard handling is executed.</remarks>
        protected virtual Result? OnCosmosException(Soc.WebRequestException odex) => odex.ThrowIfNull(nameof(odex)).Code switch
        {
            HttpStatusCode.NotFound => Result.Fail(new NotFoundException(null, odex)),
            HttpStatusCode.Conflict => Result.Fail(new DuplicateException(null, odex)),
            HttpStatusCode.PreconditionFailed => Result.Fail(new ConcurrencyException(null, odex)),
            _ => Result.Fail(odex)
        };

        /// <inheritdoc/>
        public ODataItemCollection<T> CreateItemCollection<T>(ODataArgs args, string collectionName, IODataMapper<T> mapper) where T : class, new() => new(this, args, collectionName, mapper);
    }
}