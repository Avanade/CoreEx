// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Enables value validation.
    /// </summary>
    public interface IValidator
    {
        /// <summary>
        /// Gets the <see cref="Type"/> for the value that is being validated.
        /// </summary>
        Type ValueType { get; }

        /// <summary>
        /// Validate the <paramref name="value"/> asynchronously.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IValidationResult"/>.</returns>
        Task<IValidationResult> ValidateAsync(object value, CancellationToken cancellationToken = default);
    }
}