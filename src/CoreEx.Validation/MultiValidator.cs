// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Enables multiple validations to be performed (<see cref="RunAsync"/>) resulting in a single result.
    /// </summary>
    public class MultiValidator
    {
        private readonly List<Func<CancellationToken, Task<MessageItemCollection?>>> _validators = new();

        /// <summary>
        /// Creates a new <see cref="MultiValidator"/> instance.
        /// </summary>
        /// <returns>The <see cref="MultiValidator"/> instance.</returns>
        public static MultiValidator Create() => new();

        /// <summary>
        /// Adds a <see cref="ValueValidator{T}"/>. 
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="validator">The <see cref="ValueValidator{T}"/>. </param>
        /// <returns>The (this) <see cref="MultiValidator"/>.</returns>
        public MultiValidator Add<T>(ValueValidator<T> validator)
        {
            _validators.Add(async ct => (await validator.RunAsync(false, ct).ConfigureAwait(false)).Messages);
            return this;
        }

        /// <summary>
        /// Adds a <see cref="ValidationValue{T}"/> <see cref="PropertyRuleBase{TEntity, TProperty}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="validator">The property rule validator.</param>
        /// <returns>The (this) <see cref="MultiValidator"/>.</returns>
        public MultiValidator Add<T>(PropertyRuleBase<ValidationValue<T>, T> validator)
        {
            _validators.Add(async ct => (await validator.RunAsync(false, ct).ConfigureAwait(false)).Messages);
            return this;
        }

        /// <summary>
        /// Adds an entity <see cref="ValidatorBase{TEntity}"/> with specified <see cref="ValidationArgs"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="validator">The <see cref="ValidatorBase{TEntity}"/>.</param>
        /// <param name="value">The entity value.</param>
        /// <param name="args">An optional <see cref="ValidationArgs"/>.</param>
        /// <returns>The (this) <see cref="MultiValidator"/>.</returns>
        public MultiValidator Add<TEntity, TValidator>(ValidatorBase<TEntity> validator, TEntity value, ValidationArgs args) where TEntity : class where TValidator : IValidatorEx<TEntity>
        {
            _validators.Add(async ct => (await validator.ValidateAsync(value, args, ct).ConfigureAwait(false)).Messages);
            return this;
        }

        /// <summary>
        /// Adds an entity <see cref="ValidatorBase{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="validator">The <see cref="ValidatorBase{TEntity}"/>.</param>
        /// <param name="value">The entity value.</param>
        /// <returns>The (this) <see cref="MultiValidator"/>.</returns>
        public MultiValidator Add<TEntity, TValidator>(ValidatorBase<TEntity> validator, TEntity value) where TEntity : class where TValidator : ValidatorBase<TEntity>
        {
            _validators.Add(async ct => (await validator.ValidateAsync(value, null, ct).ConfigureAwait(false)).Messages);
            return this;
        }

        /// <summary>
        /// Adds (chains) a child <see cref="MultiValidator"/>.
        /// </summary>
        /// <param name="validator">The child <see cref="MultiValidator"/>.</param>
        /// <returns>The (this) <see cref="MultiValidator"/>.</returns>
        public MultiValidator Add(MultiValidator validator)
        {
            _validators.Add(async ct => (await validator.RunAsync(false, ct).ConfigureAwait(false)).Messages);
            return this;
        }

        /// <summary>
        /// Runs the validations.
        /// </summary>
        /// <param name="throwOnError">Indicates to throw a <see cref="ValidationException"/> where an error was found.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="MultiValidatorResult"/>.</returns>
        public async Task<MultiValidatorResult> RunAsync(bool throwOnError = false, CancellationToken cancellationToken = default)
        {
            var res = new MultiValidatorResult();

            foreach (var v in _validators)
            {
                var msgs = await v(cancellationToken).ConfigureAwait(false);
                if (msgs != null && msgs.Count > 0)
                    res.Messages.AddRange(msgs);
            }

            if (throwOnError)
                res.ThrowOnError();

            return res;
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