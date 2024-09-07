// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Represents a dynamic LINQ statement with optional arguments.
    /// </summary>
    /// <remarks>The LINQ <see cref="Statement"/> may contain placeholders referencing the <see cref="Args"/> by its zero-based index.
    /// <para>Note: no parsing or validation is performed prior to use and as such may result in an internal error.</para>
    /// <para>An example is as follows: 
    /// <code>
    /// new QueryStatement("City == @0", "Brisbane");
    /// </code></para></remarks>
    /// <param name="statement">The dynamic LINQ statement.</param>
    /// <param name="args">The placeholder arguments.</param>
    public class QueryStatement(string statement, params object?[] args)
    {
        /// <summary>
        /// Gets the dynamic LINQ statement.
        /// </summary>
        /// <remarks>The dynamic LINQ statement may contain placeholders referencing the <see cref="Args"/> by its zero-based index.</remarks>
        public string Statement { get; } = statement;

        /// <summary>
        /// Gets the placeholder arguments.
        /// </summary>
        public object?[] Args { get; } = args;
    }
}