namespace CoreEx.Cosmos.Extended;

/// <summary>
/// Provides <see cref="ICosmosDb"/> multi-set query extension methods; enabling multiple, type-discriminator-keyed sets of documents to be read from the same container/partition in a single round-trip.
/// </summary>
/// <remarks>See the remarks on <see cref="SelectMultiSetWithResultAsync(ICosmosDb, string, MultiSetOptions, CancellationToken)"/> (or its <see cref="SelectMultiSetAsync(ICosmosDb, string, MultiSetOptions, CancellationToken)"/>
/// exception-based counterpart) for the full mechanism.</remarks>
public static class CosmosDbMultiSetExtensions
{
    /// <summary>
    /// Executes a multi-set query with the specified <paramref name="options"/> against the specified <paramref name="containerId"/>.
    /// </summary>
    /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
    /// <param name="containerId">The <see cref="Container"/> identifier.</param>
    /// <param name="options">The <see cref="MultiSetOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>See <see cref="SelectMultiSetWithResultAsync(ICosmosDb, string, MultiSetOptions, CancellationToken)"/> for the <see cref="Result"/> (Railway-Oriented Programming) counterpart of this method -
    /// it shares the exact same mechanics and throws the exact same exceptions for the exact same conditions (see its remarks for the full breakdown of what does, and does not, become a
    /// <see cref="Result.IsFailure"/> there); this method simply calls it and then <see cref="Result.ThrowOnError"/>s.</remarks>
    public static async Task SelectMultiSetAsync(this ICosmosDb cosmosDb, string containerId, MultiSetOptions options, CancellationToken cancellationToken = default)
        => (await SelectMultiSetWithResultAsync(cosmosDb, containerId, options, cancellationToken).ConfigureAwait(false)).ThrowOnError();

    /// <summary>
    /// Executes a multi-set query with the specified <paramref name="options"/> against the specified <paramref name="containerId"/>, with a <see cref="Result"/> (Railway-Oriented Programming).
    /// </summary>
    /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
    /// <param name="containerId">The <see cref="Container"/> identifier.</param>
    /// <param name="options">The <see cref="MultiSetOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>Each <see cref="MultiSetOptions.MultiSetArgs"/> must resolve to a unique <see cref="IMultiSetArgs.ResolveTypeDiscriminator(ICosmosDb, string)"/> value.
    /// <para>Unlike the relational <c>CoreEx.Database.Extended</c> equivalent - where result sets are positional/ordered within a single multi-statement query - Cosmos DB has no equivalent construct; a single
    /// query instead returns a mixed stream of documents from the same container/partition, each demuxed to its corresponding <see cref="IMultiSetArgs"/> via a server-side <c>WHERE ... IN (...)</c> filter on
    /// the type-discriminator property (see <see cref="CosmosDbModelOptions{TModel}.WithTypeDiscriminator(string?)"/>), then further checked per-item (tenant/logical-delete/additive-filter - see
    /// <c>CosmosDbContainer{TModel}.CheckModel</c>) before being handed to the corresponding <see cref="IMultiSetArgs"/>.</para>
    /// <para>The type-discriminator's underlying JSON property name is resolved once per call from the ambient <see cref="System.Text.Json.JsonSerializerOptions.PropertyNamingPolicy"/> configured via
    /// <see cref="CosmosClientOptions.UseSystemTextJsonSerializerWithOptions"/> (e.g. <c>camelCase</c>) - the same naming policy that already governs how <see cref="IReadOnlyTypeDiscriminator.TypeDiscriminator"/>
    /// (and every other model property) is serialized to/from Cosmos DB. This requires the underlying <see cref="CosmosClient"/> to be configured with a <see cref="System.Text.Json"/>-based serializer (the
    /// de facto requirement for this package - see <see cref="CosmosDbModelBase"/>'s reliance on <see cref="System.Text.Json.Serialization.JsonPropertyNameAttribute"/>); an explicit per-model override of the
    /// type-discriminator's JSON property name (e.g. via <see cref="System.Text.Json.Serialization.JsonPropertyNameAttribute"/>) is <b>not</b> supported - all <c>TModel</c>s within a single multi-set call must
    /// rely on the one ambient naming policy.</para>
    /// <para>The number of <see cref="IMultiSetArgs"/> specified has no relationship to the number of documents returned (unlike the relational equivalent's positional result sets) - each is matched
    /// independently by its own <see cref="IMultiSetArgs.ResolveTypeDiscriminator(ICosmosDb, string)"/> value, and <see cref="IMultiSetArgsCore.MinimumRows"/>/<see cref="IMultiSetArgsCore.MaximumRows"/> are enforced, and
    /// <see cref="IMultiSetArgsCore.InvokeResult"/> is invoked, in the order the <see cref="IMultiSetArgs"/> were supplied - honoring <see cref="IMultiSetArgsCore.StopOnNull"/> to short-circuit
    /// subsequent invocations, exactly as the relational equivalent does for its (positionally) subsequent result sets.</para>
    /// <para>Any co-located <see cref="CosmosDbOutboxEvent"/> documents (see <see cref="CosmosDbModelOptions{TModel}.ApplyFilters"/>'s equivalent automatic exclusion for the LINQ query path) are
    /// always excluded server-side via the same reserved <see cref="CosmosDbOutboxEvent.OutboxKeyPrefix"/> <c>id</c>-prefix check - no opt-in required. Similarly, where an individual <see cref="IMultiSetArgs"/>'s
    /// model has <see cref="CosmosDbModelOptions{TModel}.WithTenantFilter"/> and/or <see cref="CosmosDbModelOptions{TModel}.WithLogicalDeleteFilter"/> configured, an equivalent - but defensively
    /// <c>IS_DEFINED</c>-guarded - predicate is added for that model's subset of the query as a server-side (RU/bandwidth) optimization; see <see cref="IMultiSetArgs.BuildFilterClause"/> for the exact
    /// predicate shape and why it never excludes a document purely for predating the property. This is additive to, not a replacement for, the always-applied per-item <c>CheckModel</c> check - a model with
    /// neither configured still relies solely on that per-item check, exactly as before. A model with a <see cref="CosmosDbModelOptions{TModel}.WithFilter"/> registered <i>without</i> a <c>nonQueryResult</c>
    /// (a query-only filter - see <see cref="CosmosDbModelOptions{TModel}.HasQueryOnlyFilters"/>) is not supported here at all: unlike the tenant/logical-delete filters, an arbitrary filter predicate cannot
    /// be safely translated into this query's raw SQL text, so <see cref="IMultiSetArgs.BuildFilterClause"/> throws <see cref="NotSupportedException"/> for it rather than risk silently returning documents
    /// an equivalent <see cref="CosmosDbQuery{TModel}"/> would have excluded.</para>
    /// <para><see cref="CosmosDbArgs.QueryRequestOptions"/> (see <see cref="MultiSetOptions.Args"/>), where supplied, takes precedence over one freshly constructed from <see cref="MultiSetOptions.PartitionKey"/>;
    /// its own <see cref="QueryRequestOptions.PartitionKey"/>, if already set, must either agree with a non-<see langword="null"/> <see cref="MultiSetOptions.PartitionKey"/> or the latter must be omitted
    /// (<see langword="null"/>) - a genuine mismatch between the two throws <see cref="ArgumentException"/> (an argument/guard-clause violation, not a <see cref="Result.IsFailure"/>) rather than silently
    /// preferring one. The caller's <see cref="CosmosDbArgs"/> is never mutated (it is expected to be immutable/shareable - see <see cref="CosmosDbArgs"/>'s own remarks); a partition key is layered in via a
    /// shallow clone where required.</para>
    /// <para>Only a genuine domain/business-level outcome results in <see cref="Result.IsFailure"/> here - specifically, a <see cref="CosmosException"/> translated by <see cref="ICosmosDb.HandleCosmosException(CosmosException)"/>
    /// into an <c>IExtendedException</c> (e.g. a downstream <see cref="Microsoft.Azure.Cosmos.CosmosException"/> mapped the same way every other <c>CosmosDbContainer{TModel}.WithResultAsync</c> operation maps it), or an
    /// <see cref="IMultiSetArgs.AddItem(ICosmosDb, string, CosmosDbArgs, object)"/> failure surfaced from a per-item <c>CheckModel</c> check - i.e. a <see cref="CosmosDbModelOptions{TModel}.WithFilter"/>'s own
    /// <c>nonQueryResult</c> callback returning a genuine <see cref="Result.IsFailure"/> (e.g. an authorization denial), exactly as it would for a single-item <c>GetAsync</c>/<c>GetWithResultAsync</c>.</para>
    /// <para>Everything else - an empty/duplicate-discriminator <see cref="MultiSetOptions.MultiSetArgs"/>, a <see cref="CosmosDbArgs.QueryRequestOptions"/>/<see cref="MultiSetOptions.PartitionKey"/> mismatch, a
    /// <see cref="CosmosClient"/> not configured with <see cref="CosmosClientOptions.UseSystemTextJsonSerializerWithOptions"/>, a malformed/undeserializable Cosmos DB response, or
    /// <see cref="IMultiSetArgsCore.MinimumRows"/>/<see cref="IMultiSetArgsCore.MaximumRows"/> being violated - is an <see cref="ArgumentException"/>/<see cref="InvalidOperationException"/>/<see cref="NotSupportedException"/>
    /// guard-clause/invariant violation, not a business error, and is always thrown directly instead - even from this <see cref="Result"/>-returning method - exactly as it is from the exception-based
    /// <see cref="SelectMultiSetAsync(ICosmosDb, string, MultiSetOptions, CancellationToken)"/> counterpart (this mirrors how, e.g., <c>CosmosDbContainer{TModel}.DeleteWithResultAsync</c> still throws
    /// <see cref="InvalidOperationException"/> directly for its own ambiguous-logical-delete-configuration guard clause). <see cref="Result"/> is reserved here for genuinely expected/business-level
    /// outcomes, not as a blanket substitute for exceptions.</para></remarks>
    public static Task<Result> SelectMultiSetWithResultAsync(this ICosmosDb cosmosDb, string containerId, MultiSetOptions options, CancellationToken cancellationToken = default)
        => SelectMultiSetInternalAsync(cosmosDb.ThrowIfNull(), containerId.ThrowIfNullOrEmpty(), options.ThrowIfNull(), cancellationToken);

    /// <summary>
    /// Executes a multi-set query with the specified <paramref name="options"/> (internal).
    /// </summary>
    private static async Task<Result> SelectMultiSetInternalAsync(ICosmosDb cosmosDb, string containerId, MultiSetOptions options, CancellationToken cancellationToken)
    {
        var multiSetList = options.MultiSetArgs?.ToList();
        if (multiSetList is null || multiSetList.Count == 0)
            throw new ArgumentException($"At least one {nameof(IMultiSetArgs)} must be supplied.", $"{nameof(options)}.{nameof(MultiSetOptions.MultiSetArgs)}");

        var byDiscriminator = new Dictionary<string, IMultiSetArgs>();
        var discriminators = new Dictionary<IMultiSetArgs, string>();
        foreach (var msa in multiSetList)
        {
            var discriminator = msa.ResolveTypeDiscriminator(cosmosDb, containerId);
            discriminators[msa] = discriminator;
            if (!byDiscriminator.TryAdd(discriminator, msa))
                throw new ArgumentException($"Multiple {nameof(IMultiSetArgs)} resolve to the same {nameof(IMultiSetArgs.ResolveTypeDiscriminator)} '{discriminator}'; each must be unique.", $"{nameof(options)}.{nameof(MultiSetOptions.MultiSetArgs)}");
        }

        var args = options.Args ?? cosmosDb.DbArgs;

        var jsonSerializerOptions = cosmosDb.Client.ClientOptions.UseSystemTextJsonSerializerWithOptions
            ?? throw new NotSupportedException($"{nameof(SelectMultiSetAsync)} requires the underlying {nameof(CosmosClient)} to be configured with {nameof(CosmosClientOptions.UseSystemTextJsonSerializerWithOptions)}.");

        var discriminatorProperty = jsonSerializerOptions.PropertyNamingPolicy?.ConvertName(nameof(IReadOnlyTypeDiscriminator.TypeDiscriminator)) ?? nameof(IReadOnlyTypeDiscriminator.TypeDiscriminator);
        var partitionKeyValue = options.PartitionKey is null ? PartitionKey.None : new PartitionKey(options.PartitionKey);
        var requestOptions = ResolveQueryRequestOptions(args.QueryRequestOptions, options.PartitionKey, partitionKeyValue);

        return await cosmosDb.Invoker.InvokeAsync(cosmosDb, args, async (_, args, cancellationToken) =>
        {
            // Build a per-discriminator predicate honoring any configured tenant/logical-delete query filter (see IMultiSetArgs.BuildFilterClause) - a model with neither configured falls back to a
            // plain discriminator-equality predicate, so the overall query text is unchanged from before this optimization existed where no IMultiSetArgs configures either filter.
            var extraParameters = new Dictionary<string, object?>();
            var typeClauses = new string[multiSetList.Count];
            var anyModelFilters = false;

            for (var i = 0; i < multiSetList.Count; i++)
            {
                var filterClause = multiSetList[i].BuildFilterClause(cosmosDb, containerId, jsonSerializerOptions, $"@f{i}", extraParameters);
                typeClauses[i] = filterClause is null ? $"c[\"{discriminatorProperty}\"] = @p{i}" : $"(c[\"{discriminatorProperty}\"] = @p{i} AND {filterClause})";
                anyModelFilters |= filterClause is not null;
            }

            var discriminatorPredicate = anyModelFilters
                ? string.Join(" OR ", typeClauses)
                : $"c[\"{discriminatorProperty}\"] IN ({string.Join(", ", Enumerable.Range(0, multiSetList.Count).Select(i => $"@p{i}"))})";

            // Always exclude any co-located outbox event documents (see CosmosDbModelOptions<TModel>.ApplyFilters's equivalent automatic exclusion for the LINQ query path) - no opt-in required.
            var query = new QueryDefinition($"SELECT * FROM c WHERE NOT STARTSWITH(c.id, @outboxKeyPrefix) AND ({discriminatorPredicate})")
                .WithParameter("@outboxKeyPrefix", CosmosDbOutboxEvent.OutboxKeyPrefix);

            for (var i = 0; i < multiSetList.Count; i++)
                query = query.WithParameter($"@p{i}", discriminators[multiSetList[i]]);

            foreach (var (parameterName, value) in extraParameters)
                query = query.WithParameter(parameterName, value);

            var counts = new Dictionary<IMultiSetArgs, int>();
            using var iterator = cosmosDb.GetContainer(containerId).GetItemQueryStreamIterator(query, requestOptions: requestOptions);

            while (iterator.HasMoreResults)
            {
                using var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                using var doc = await JsonDocument.ParseAsync(response.Content, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (!doc.RootElement.TryGetProperty("Documents", out var documents) || documents.ValueKind != JsonValueKind.Array)
                    throw new InvalidOperationException($"{nameof(SelectMultiSetAsync)} response JSON 'Documents' property either not found in result or is not an array.");

                foreach (var item in documents.EnumerateArray())
                {
                    if (!item.TryGetProperty(discriminatorProperty, out var discriminatorElement) || discriminatorElement.ValueKind != JsonValueKind.String)
                        continue; // Not a discriminated document (e.g. another document shape sharing the container) - ignore.

                    var discriminator = discriminatorElement.GetString();
                    if (discriminator is null || !byDiscriminator.TryGetValue(discriminator, out var msa))
                        continue; // Not one of the requested types - ignore.

                    var model = item.Deserialize(msa.ModelType, jsonSerializerOptions)
                        ?? throw new InvalidOperationException($"{nameof(SelectMultiSetAsync)} failed to deserialize a document with {nameof(IMultiSetArgs.ResolveTypeDiscriminator)} '{discriminator}' into '{msa.ModelType.Name}'.");

                    var addResult = msa.AddItem(cosmosDb, containerId, args, model);
                    if (addResult.IsFailure)
                        return (Result)addResult;

                    if (!addResult.Value)
                        continue; // Silently excluded by the per-item check (e.g. wrong tenant, logically deleted) - must not count towards MinimumRows/MaximumRows/StopOnNull.

                    var count = counts[msa] = counts.GetValueOrDefault(msa) + 1;
                    if (msa.MaximumRows.HasValue && count > msa.MaximumRows.Value)
                        throw new InvalidOperationException($"{nameof(SelectMultiSetAsync)} ({nameof(IMultiSetArgs.ResolveTypeDiscriminator)} '{discriminator}') has returned more items ({count}) than expected ({msa.MaximumRows.Value}).");
                }
            }

            // Validate minimum rows and invoke results, in the order the multi-set args were supplied - honoring StopOnNull to short-circuit subsequent invocations.
            foreach (var msa in multiSetList)
            {
                var count = counts.GetValueOrDefault(msa);
                if (count < msa.MinimumRows)
                    throw new InvalidOperationException($"{nameof(SelectMultiSetAsync)} ({nameof(IMultiSetArgs.ResolveTypeDiscriminator)} '{discriminators[msa]}') has returned less items ({count}) than expected ({msa.MinimumRows}).");

                if (count == 0 && msa.StopOnNull)
                    return Result.Success;

                msa.InvokeResult();
            }

            return Result.Success;
        }, cancellationToken, nameof(SelectMultiSetAsync)).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves the <see cref="QueryRequestOptions"/> to use, honoring a caller-supplied <see cref="CosmosDbArgs.QueryRequestOptions"/> (see this type's remarks).
    /// </summary>
    private static QueryRequestOptions ResolveQueryRequestOptions(QueryRequestOptions? queryRequestOptions, string? partitionKey, PartitionKey partitionKeyValue)
    {
        if (queryRequestOptions is null)
            return new QueryRequestOptions { PartitionKey = partitionKeyValue };

        if (queryRequestOptions.PartitionKey is PartitionKey existing)
        {
            if (partitionKey is not null && !existing.Equals(partitionKeyValue))
                throw new ArgumentException(
                    $"The partition key '{partitionKey}' does not match the partition key already configured on {nameof(CosmosDbArgs)}.{nameof(CosmosDbArgs.QueryRequestOptions)}.{nameof(Microsoft.Azure.Cosmos.QueryRequestOptions.PartitionKey)}.",
                    nameof(partitionKey));

            return queryRequestOptions; // Already set and consistent (or no explicit partitionKey override supplied) - the caller's QueryRequestOptions takes precedence, used as-is.
        }

        // The caller's QueryRequestOptions did not itself specify a partition key - clone (never mutate the caller's own, potentially shared/cached, CosmosDbArgs) and layer in the resolved value.
        return CloneWithPartitionKey(queryRequestOptions, partitionKeyValue);
    }

    /// <summary>
    /// Shallow-clones a <see cref="QueryRequestOptions"/>, applying the resolved <paramref name="partitionKey"/>.
    /// </summary>
    /// <remarks>The Cosmos DB SDK's <see cref="QueryRequestOptions"/> has no public copy constructor/<c>Clone()</c> method, so each property is copied explicitly; <see cref="CosmosDbArgs"/> is expected to be
    /// immutable/shareable (see its own remarks), so the caller's instance is never mutated in place.</remarks>
    private static QueryRequestOptions CloneWithPartitionKey(QueryRequestOptions source, PartitionKey partitionKey) => new()
    {
        ResponseContinuationTokenLimitInKb = source.ResponseContinuationTokenLimitInKb,
        EnableScanInQuery = source.EnableScanInQuery,
        EnableLowPrecisionOrderBy = source.EnableLowPrecisionOrderBy,
        EnableOptimisticDirectExecution = source.EnableOptimisticDirectExecution,
        MaxBufferedItemCount = source.MaxBufferedItemCount,
        MaxItemCount = source.MaxItemCount,
        MaxConcurrency = source.MaxConcurrency,
        PartitionKey = partitionKey,
        PopulateIndexMetrics = source.PopulateIndexMetrics,
        PopulateQueryAdvice = source.PopulateQueryAdvice,
        ConsistencyLevel = source.ConsistencyLevel,
        SessionToken = source.SessionToken,
        DedicatedGatewayRequestOptions = source.DedicatedGatewayRequestOptions,
        QueryTextMode = source.QueryTextMode,
        FullTextScoreScope = source.FullTextScoreScope,
        IfMatchEtag = source.IfMatchEtag,
        IfNoneMatchEtag = source.IfNoneMatchEtag,
        Properties = source.Properties,
        AddRequestHeaders = source.AddRequestHeaders,
        PriorityLevel = source.PriorityLevel,
        CosmosThresholdOptions = source.CosmosThresholdOptions,
        ExcludeRegions = source.ExcludeRegions,
        AvailabilityStrategy = source.AvailabilityStrategy
    };
}
