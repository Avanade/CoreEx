namespace CoreEx.Database;

public abstract partial class DatabaseCommand
{
    /// <summary>
    /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/>.
    /// </summary>
    /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
    /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
    public Task SelectMultiSetAsync(params IMultiSetArgs[] multiSetArgs) => SelectMultiSetAsync(multiSetArgs, default);

    /// <summary>
    /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/>.
    /// </summary>
    /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
    public Task SelectMultiSetAsync(IEnumerable<IMultiSetArgs> multiSetArgs, CancellationToken cancellationToken = default) => SelectMultiSetInternalAsync(multiSetArgs, nameof(SelectMultiSetAsync), cancellationToken);

    /// <summary>
    /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> (internal).
    /// </summary>
    private async Task SelectMultiSetInternalAsync(IEnumerable<IMultiSetArgs> multiSetArgs, string memberName, CancellationToken cancellationToken = default)
    {
        var multiSetList = multiSetArgs?.ToList();
        if (multiSetList is null || multiSetList.Count == 0)
            throw new ArgumentException($"At least one {nameof(IMultiSetArgs)} must be supplied.", nameof(multiSetArgs));

        await Database.Invoker.InvokeAsync(Database, DbArgs, async (_, _, cancellationToken) =>
        {
            // Create and execute the command. 
            using var cmd = await CreateCommandAsync(cancellationToken).ConfigureAwait(false);
            using var dr = await LogCommand(cmd).ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            // Iterate through the dataset(s).
            var index = 0;
            var records = 0;
            IMultiSetArgs? multiSetArg = null;
            do
            {
                if (index >= multiSetList.Count)
                    throw new InvalidOperationException($"{nameof(SelectMultiSetAsync)} has returned more record sets than expected ({multiSetList.Count}).");

                if (multiSetList[index] is not null)
                {
                    records = 0;
                    multiSetArg = multiSetList[index];
                    while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        records++;
                        if (multiSetArg.MaximumRows.HasValue && records > multiSetArg.MaximumRows.Value)
                            throw new InvalidOperationException($"{nameof(SelectMultiSetAsync)} (msa[{index}]) has returned more records than expected ({multiSetArg.MaximumRows.Value}).");

                        multiSetArg.DatasetRecord(new DatabaseRecord(Database, dr));
                    }

                    if (records < multiSetArg.MinimumRows)
                        throw new InvalidOperationException($"{nameof(SelectMultiSetAsync)} (msa[{index}]) has returned less records ({records}) than expected ({multiSetArg.MinimumRows}).");

                    if (records == 0 && multiSetArg.StopOnNull)
                        return;

                    multiSetArg.InvokeResult();
                }

                index++;
            } while (dr.NextResult());

            if (index < multiSetList.Count && !multiSetList[index].StopOnNull)
                throw new InvalidOperationException($"{nameof(SelectMultiSetAsync)} has returned less ({index}) record sets than expected ({multiSetList.Count}).");
        }, cancellationToken, memberName).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> that supports <paramref name="paging"/>.
    /// </summary>
    /// <param name="paging">The <see cref="PagingArgs"/> or <see cref="PagingResult"/> to add to the <see cref="Parameters"/>.</param>
    /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
    /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
    public Task SelectMultiSetAsync(PagingArgs? paging, params IMultiSetArgs[] multiSetArgs) => SelectMultiSetAsync(paging, multiSetArgs, default);

    /// <summary>
    /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> that supports <paramref name="paging"/>.
    /// </summary>
    /// <param name="paging">The <see cref="PagingArgs"/> or <see cref="PagingResult"/> to add to the <see cref="Parameters"/>.</param>
    /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
    public Task SelectMultiSetAsync(PagingArgs? paging, IEnumerable<IMultiSetArgs> multiSetArgs, CancellationToken cancellationToken = default)
        => SelectMultiSetInternalAsync(paging, multiSetArgs, nameof(SelectMultiSetAsync), cancellationToken);

    /// <summary>
    /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> that supports <paramref name="paging"/> (internal);
    /// </summary>
    private async Task SelectMultiSetInternalAsync(PagingArgs? paging, IEnumerable<IMultiSetArgs> multiSetArgs, string memberName, CancellationToken cancellationToken)
    {
        Parameters.PagingParams(paging);

        var result = await SelectMultiSetWithValueInternalAsync(multiSetArgs, memberName, cancellationToken).ConfigureAwait(false);
        if (paging is PagingResult pr && pr.IsCountRequested && result >= 0)
            pr.WithTotalCount(result);
    }

    /// <summary>
    /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/>; whilst also outputing the resulting <see cref="int"/> <see cref="ParameterDirection.ReturnValue"/>.
    /// </summary>
    /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
    /// <returns>The resultant return value.</returns>
    /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
    public Task<int> SelectMultiSetWithValueAsync(params IMultiSetArgs[] multiSetArgs) => SelectMultiSetWithValueAsync(multiSetArgs, default);

    /// <summary>
    /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/>; whilst also outputing the resulting <see cref="int"/> <see cref="ParameterDirection.ReturnValue"/>.
    /// </summary>
    /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resultant return value.</returns>
    /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
    public Task<int> SelectMultiSetWithValueAsync(IEnumerable<IMultiSetArgs> multiSetArgs, CancellationToken cancellationToken = default)
        => SelectMultiSetWithValueInternalAsync(multiSetArgs, nameof(SelectMultiSetWithValueAsync), cancellationToken);

    /// <summary>
    /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/>; whilst also outputing the resulting <see cref="int"/> <see cref="ParameterDirection.ReturnValue"/> (internal).
    /// </summary>
    private async Task<int> SelectMultiSetWithValueInternalAsync(IEnumerable<IMultiSetArgs> multiSetArgs, string memberName, CancellationToken cancellationToken)
    {
        var rvp = Parameters.AddReturnValueParameter();
        await SelectMultiSetInternalAsync(multiSetArgs, memberName, cancellationToken).ConfigureAwait(false);
        return rvp.Value is null ? -1 : (int)rvp.Value;
    }
}