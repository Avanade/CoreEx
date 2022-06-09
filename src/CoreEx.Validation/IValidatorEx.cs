// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Extends the <see cref="IValidator"/>.
    /// </summary>
    public interface IValidatorEx : IValidator
    {
        /// <summary>
        /// Validate the entity value with specified <see cref="ValidationArgs"/>.
        /// </summary>
        /// <param name="value">The entity value.</param>
        /// <param name="args">An optional <see cref="ValidationArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="IValidationContext"/>.</returns>
        Task<IValidationContext> ValidateAsync(object value, ValidationArgs? args, CancellationToken cancellationToken);
    }
}