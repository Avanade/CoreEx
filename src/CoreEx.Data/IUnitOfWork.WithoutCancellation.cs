namespace CoreEx.Data;

public partial interface IUnitOfWork
{
    /// <summary>
    /// Executes either a new or <i>flows</i> an existing transaction managing its lifetime and underlying <paramref name="work"/> execution.
    /// </summary>
    /// <param name="work">The work to be executed within the transaction.</param>
    public Task ExecuteAsync(Func<Task> work) => TransactionAsync(async _ => await work().ConfigureAwait(false), default);

    /// <summary>
    /// Executes either a new or <i>flows</i> an existing transaction managing its lifetime and underlying <paramref name="work"/> execution that returns a value.
    /// </summary>
    /// <typeparam name="T">The resulting value <see cref="Type"/>.</typeparam>
    /// <param name="work">The work to be executed within the transaction.</param>
    /// <returns>The resulting value.</returns>
    public Task<T> ExecuteAsync<T>(Func<Task<T>> work) => TransactionAsync(async _ => await work().ConfigureAwait(false), default);

    /// <summary>
    /// Executes either a new or <i>flows</i> an existing transaction managing its lifetime and underlying <paramref name="work"/> execution.
    /// </summary>
    /// <param name="args">The <see cref="IDataArgs"/>.</param>
    /// <param name="work">The work to be executed within the transaction.</param>
    public Task ExecuteAsync(IDataArgs args, Func<Task> work) => TransactionAsync(args, async _ => await work().ConfigureAwait(false), default);

    /// <summary>
    /// Executes either a new or <i>flows</i> an existing transaction managing its lifetime and underlying <paramref name="work"/> execution that returns a value.
    /// </summary>
    /// <typeparam name="T">The resulting value <see cref="Type"/>.</typeparam>
    /// <param name="args">The <see cref="IDataArgs"/>.</param>
    /// <param name="work">The work to be executed within the transaction.</param>
    /// <returns>The resulting value.</returns>
    public Task<T> ExecuteAsync<T>(IDataArgs args, Func<Task<T>> work) => TransactionAsync(args, async _ => await work().ConfigureAwait(false), default);
}