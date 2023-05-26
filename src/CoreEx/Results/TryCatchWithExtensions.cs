// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using CoreEx.Results.Abstractions;
using System;
using System.Diagnostics;

namespace CoreEx.Results
{
    /// <summary>
    /// Provides the <see cref="TryCatchWith{T}"/> extension methods.
    /// </summary>
    [DebuggerStepThrough]
    public static class TryCatchWithExtensions
    {
        /// <summary>
        /// Initiates a try-catch-with operation (see <see cref="TryCatchWith{T}"/>) where <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="IResult"/> <see cref="Type"/>.</typeparam>
        /// <param name="defaultMessage">The default message function to use where the <see cref="Exception"/> has been caught.</param>
        /// <param name="result">The originating <see cref="IResult"/>.</param>
        /// <returns>The <see cref="TryCatchWith{T}"/>.</returns>
        public static TryCatchWith<T> Try<T>(this T result, Func<T, LText?>? defaultMessage = null) where T : IResult => new(result, defaultMessage);

        /// <summary>
        /// Initiates a try-catch-with operation (see <see cref="TryCatchWith{T}"/>) regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="IResult"/> <see cref="Type"/>.</typeparam>
        /// <param name="defaultMessage">The default message to use where the <see cref="Exception"/> has been caught.</param>
        /// <param name="result">The originating <see cref="IResult"/>.</param>
        /// <returns>The <see cref="TryCatchWith{T}"/>.</returns>
        public static TryCatchWith<T> TryAny<T>(this T result, Func<T, LText?>? defaultMessage = null) where T : IResult => new(result, defaultMessage, alsoExecuteOnFailure: true);
    }
}