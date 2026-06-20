namespace CoreEx.EntityFrameworkCore;

public partial class EfDbModel<TModel>
{
    /// <summary>
    /// Gets the <see cref="IQueryable{T}"/> for the model.
    /// </summary>
    /// <param name="args">The optional <see cref="EfDbArgs"/>.</param>
    /// <returns>The <see cref="IQueryable{T}"/> for the model.</returns>
    /// <remarks>This automatically applies any applicable <see cref="Options"/> filters; see <see cref="EfDbModelOptions{TModel}.ApplyFilters"/>.</remarks>
    public IQueryable<TModel> Query(EfDbArgs? args = null)
    {
        args ??= Args;
        return Options.ApplyFilters(args, args.QueryTracking ? EfDb.DbContext.Set<TModel>() : EfDb.DbContext.Set<TModel>().AsNoTracking());
    }

    /// <summary>
    /// Gets the <see cref="IQueryable{T}"/> for the model with tracking explicitly enabled (i.e. <see cref="EfDbArgs.QueryTracking"/> is <see langword="true"/>).
    /// </summary>
    /// <param name="args">The optional <see cref="EfDbArgs"/>.</param>
    /// <returns>The <see cref="IQueryable{T}"/> for the model with tracking enabled.</returns>
    /// <remarks>This automatically applies any applicable <see cref="Options"/> filters; see <see cref="EfDbModelOptions{TModel}.ApplyFilters"/>. The <paramref name="args"/> <see cref="EfDbArgs.QueryTracking"/>
    /// is set (overridden) to <see langword="true"/>.</remarks>
    public IQueryable<TModel> QueryTracked(EfDbArgs? args = null) => Query((args ?? Args) with { QueryTracking = true });
}