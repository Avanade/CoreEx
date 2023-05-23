// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Results
{
    /// <summary>
    /// Enables the <see cref="ToResult"/> to convert into a corresponding <see cref="Result{T}"/>.
    /// </summary>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public interface ITypedToResult
    {
        /// <summary>
        /// Converts into a corresponding <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="System.Type"/>.</typeparam>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        Result<T> ToResult<T>();
    }
}