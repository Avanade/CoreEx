// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides a comparision validation against one or more values.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public class CompareValuesRule<TEntity, TProperty> : ValueRuleBase<TEntity, TProperty> where TEntity : class
    {
        private readonly IEnumerable<TProperty>? _compareToValues;
        private readonly Func<TEntity, CancellationToken, Task<IEnumerable<TProperty>>>? _compareToValuesFunctionAsync;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompareValuesRule{TEntity, TProperty}"/> class.
        /// </summary>
        private CompareValuesRule() => ValidateWhenDefault = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompareValuesRule{TEntity, TProperty}"/> class specifying the compare to values (as an <see cref="CompareOperator.Equal"/>).
        /// </summary>
        /// <param name="compareToValues">The compare to values.</param>
        public CompareValuesRule(IEnumerable<TProperty> compareToValues) : this() 
            => _compareToValues = compareToValues.ThrowIfNull(nameof(compareToValues));

        /// <summary>
        /// Initializes a new instance of the <see cref="CompareValuesRule{TEntity, TProperty}"/> class specifying the compare to values async function (as an <see cref="CompareOperator.Equal"/>).
        /// </summary>
        /// <param name="compareToValuesFunctionAsync">The compare to values function.</param>
        public CompareValuesRule(Func<TEntity, CancellationToken, Task<IEnumerable<TProperty>>> compareToValuesFunctionAsync) : this()
            =>  _compareToValuesFunctionAsync = compareToValuesFunctionAsync.ThrowIfNull(nameof(compareToValuesFunctionAsync));

        /// <summary>
        /// Gets or sets the <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        public IEqualityComparer<TProperty?> EqualityComparer { get; set; } = EqualityComparer<TProperty?>.Default;

        /// <summary>
        /// Indicates whether to override the underlying property value with the corresponding <see cref="Enum"/> name.
        /// </summary>
        public bool OverrideValue { get; set; }

        /// <inheritdoc/>
        protected override async Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
        {
            context.ThrowIfNull(nameof(context));

            // Perform the comparison.
            var values = _compareToValues != null ? _compareToValues! : await _compareToValuesFunctionAsync!(context.Parent.Value!, cancellationToken).ConfigureAwait(false);
            if (!values.Where(x => EqualityComparer.Equals(x, context.Value)).Any())
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.InvalidFormat);

            // Override where selected.
            if (OverrideValue)
                context.OverrideValue(values.Where(x => EqualityComparer.Equals(x, context.Value)).First());
        }
    }
}