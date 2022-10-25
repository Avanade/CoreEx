// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Enables multiple validations to be performed (<see cref="ValidateAsync(CancellationToken)"/>) resulting in a single consolidated <see cref="MultiValidatorResult"/>.
    /// </summary>
    public class MultiValidator
    {
        /// <summary>
        /// Creates a new <see cref="MultiValidator"/> instance.
        /// </summary>
        /// <returns>The <see cref="MultiValidator"/> instance.</returns>
        public static MultiValidator Create() => new();

        /// <summary>
        /// Gets the list of validators.
        /// </summary>
        public List<Func<CancellationToken, Task<IValidationResult>>> Validators { get; } = new();

        /// <summary>
        /// Adds an <see cref="IValidator{T}"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="validator">The <see cref="IValidator{T}"/>.</param>
        /// <param name="value">The entity value.</param>
        /// <returns>The (this) <see cref="MultiValidator"/>.</returns>
        public MultiValidator Add<TEntity, TValidator>(TValidator validator, TEntity value) where TEntity : class where TValidator : IValidator<TEntity>
        {
            Validators.Add(async ct => await validator.ValidateAsync(value, ct).ConfigureAwait(false));
            return this;
        }

        /// <summary>
        /// Adds (chains) a child <see cref="MultiValidator"/>.
        /// </summary>
        /// <param name="validator">The child <see cref="MultiValidator"/>.</param>
        /// <returns>The (this) <see cref="MultiValidator"/>.</returns>
        public MultiValidator Add(MultiValidator validator)
        {
            Validators.Add(async ct => await validator.ValidateAsync(ct).ConfigureAwait(false));
            return this;
        }

        /// <summary>
        /// Runs the validations.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="MultiValidatorResult"/>.</returns>
        public async Task<MultiValidatorResult> ValidateAsync(CancellationToken cancellationToken = default)
        {
            var res = new MultiValidatorResult();

            foreach (var v in Validators)
            {
                var r = await v(cancellationToken).ConfigureAwait(false);
                if (r != null && r.Messages != null && r.Messages.Count > 0)
                    res.Messages.AddRange(r.Messages);
            }

            return res;
        }

        /// <summary>
        /// Runs the validations.
        /// </summary>
        /// <param name="throwOnError">Indicates whether to automatically throw a <see cref="ValidationException"/> where <see cref="IValidationResult.HasErrors"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="MultiValidatorResult"/>.</returns>
        public async Task<MultiValidatorResult> ValidateAsync(bool throwOnError, CancellationToken cancellationToken = default)
        {
            var mvr = await ValidateAsync(cancellationToken).ConfigureAwait(false);
            return throwOnError ? mvr.ThrowOnError() : mvr;
        }

        /// <summary>
        /// Defines an <paramref name="action"/> to enable additional validations to be added (see <see cref="Add"/>).
        /// </summary>
        /// <param name="action">The custom action.</param>
        /// <returns>The (this) <see cref="MultiValidator"/>.</returns>
        public MultiValidator Additional(Action<MultiValidator> action)
        {
            action?.Invoke(this);
            return this;
        }
    }
}