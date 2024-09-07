// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Provides the <see cref="QueryFilterToken"/> kind.
    /// </summary>
    [Flags]
    public enum QueryFilterTokenKind
    {
        /// <summary>
        /// An unspecified/undetermined token.
        /// </summary>
        Unspecified = 1,

        /// <summary>
        /// The field token.
        /// </summary>
        Field = 2,

        /// <summary>
        /// The equal operator token.
        /// </summary>
        Equal = 4,

        /// <summary>
        /// The not equal operator token.
        /// </summary>
        NotEqual = 8,

        /// <summary>
        /// The less than operator token.
        /// </summary>
        LessThan = 16,

        /// <summary>
        /// The less than or equal operator token.
        /// </summary>
        LessThanOrEqual = 32,

        /// <summary>
        /// The greater than or equal operator token.
        /// </summary>
        GreaterThanOrEqual = 64,

        /// <summary>
        /// The greater than operator token.
        /// </summary>
        GreaterThan = 128,

        /// <summary>
        /// The value token.
        /// </summary>
        Value = 256,

        /// <summary>
        /// The string literal token.
        /// </summary>
        Literal = 512,

        /// <summary>
        /// The <see langword="true"/> token.
        /// </summary>
        True = 1024,

        /// <summary>
        /// The <see langword="false"/> token.
        /// </summary>
        False = 2048,

        /// <summary>
        /// The <see langword="null"/> token.
        /// </summary>
        Null = 4096,

        /// <summary>
        /// The logical AND operator token.
        /// </summary>
        And = 8192,

        /// <summary>
        /// The logical OR operator token.
        /// </summary>
        Or = 16384,

        /// <summary>
        /// The open parenthesis token.
        /// </summary>
        OpenParenthesis = 32768,

        /// <summary>
        /// The close parenthesis token.
        /// </summary>
        CloseParenthesis = 65536,

        /// <summary>
        /// The comma token.
        /// </summary>
        Comma = 131072,

        /// <summary>
        /// The starts with token.
        /// </summary>
        StartsWith = 262144,

        /// <summary>
        /// The contains token.
        /// </summary>
        Contains = 524288,

        /// <summary>
        /// The ends with token.
        /// </summary>
        EndsWith = 1048576,

        /// <summary>
        /// The logical IN operator token.
        /// </summary>
        In = 2097152,

        /// <summary>
        /// The logical NOT operator token.
        /// </summary>
        Not = 4194304,

        /// <summary>
        /// An expression operator token.
        /// </summary>
        Operator = Equal | NotEqual | GreaterThan | GreaterThanOrEqual | LessThan | LessThanOrEqual | In,

        /// <summary>
        /// An expression equality operator token.
        /// </summary>
        EqualityOperator = Equal | NotEqual | In,

        /// <summary>
        /// An expression constant token.
        /// </summary>
        Constant = Value | Literal | True | False | Null,

        /// <summary>
        /// A logical operator token.
        /// </summary>
        Logical = And | Or,

        /// <summary>
        /// A general syntax token.
        /// </summary>
        Syntax = OpenParenthesis | CloseParenthesis | Comma,

        /// <summary>
        /// A string oriented function-based operator.
        /// </summary>
        StringFunction = StartsWith | EndsWith | Contains,

        /// <summary>
        /// All string oriented operators.
        /// </summary>
        AllStringOperators = Operator | StringFunction
    }
}