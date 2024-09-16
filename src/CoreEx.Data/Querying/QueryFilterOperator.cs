using System;
using CoreEx.Data.Querying.Expressions;

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Enables the <see cref="IQueryFilterFieldConfig.Operators"/>.
    /// </summary>
    /// <remarks>Values are the same as the <see cref="QueryFilterTokenKind"/> to simplify usage.</remarks>
    [Flags]
    public enum QueryFilterOperator
    {
        /// <summary>
        /// The equal operator.
        /// </summary>
        Equal = 4,

        /// <summary>
        /// The not equal operator.
        /// </summary>
        NotEqual = 8,

        /// <summary>
        /// The less than operator.
        /// </summary>
        LessThan = 16,

        /// <summary>
        /// The less than or equal operator.
        /// </summary>
        LessThanOrEqual = 32,

        /// <summary>
        /// The greater than or equal operator.
        /// </summary>
        GreaterThanOrEqual = 64,

        /// <summary>
        /// The greater than operator.
        /// </summary>
        GreaterThan = 128,

        /// <summary>
        /// The logical IN operator.
        /// </summary>
        In = 256,

        /// <summary>
        /// The starts with function.
        /// </summary>
        StartsWith = 524288,

        /// <summary>
        /// The contains function.
        /// </summary>
        Contains = 1048576,

        /// <summary>
        /// The ends with function.
        /// </summary>
        EndsWith = 2097152,

        /// <summary>
        /// The <i>equality</i> operators.
        /// </summary>
        EqualityOperators = Equal | NotEqual | In,

        /// <summary>
        /// The <i>comparison</i> operators.
        /// </summary>
        ComparisonOperators = EqualityOperators | GreaterThan | GreaterThanOrEqual | LessThan | LessThanOrEqual,

        /// <summary>
        /// The string oriented function-based operators.
        /// </summary>
        StringFunctions = StartsWith | EndsWith | Contains,

        /// <summary>
        /// All string oriented operators and functions.
        /// </summary>
        AllStringOperators = ComparisonOperators | StringFunctions
    }
}