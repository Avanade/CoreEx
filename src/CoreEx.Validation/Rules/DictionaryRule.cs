// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides dictionary validation including <see cref="MinCount"/> and <see cref="MaxCount"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The dictionary property <see cref="Type"/>.</typeparam>
    public class DictionaryRule<TEntity, TProperty> : ValueRuleBase<TEntity, TProperty> where TEntity : class where TProperty : IDictionary?
    {
        private readonly Type _keyType;
        private readonly Type _valueType;
        private IDictionaryRuleItem? _item;

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryRule{TEntity, TProperty}"/> class.
        /// </summary>
        public DictionaryRule()
        {
            var (kt, vt) = TypeReflector.GetDictionaryType(typeof(TProperty));
            _keyType = kt!;
            _valueType = vt!;
        }

        /// <summary>
        /// Indicates whether the underlying dictionary key can be null.
        /// </summary>
        public bool AllowNullKeys { get; set; }

        /// <summary>
        /// Indicates whether the underlying dictionary value can be null.
        /// </summary>
        public bool AllowNullValues { get; set; }

        /// <summary>
        /// Gets or sets the minimum count;
        /// </summary>
        public int MinCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum count.
        /// </summary>
        public int? MaxCount { get; set; }

        /// <summary>
        /// Gets or sets the dictionary item validation configuration.
        /// </summary>
        public IDictionaryRuleItem? Item
        {
            get => _item;

            set
            {
                if (value == null)
                {
                    _item = value;
                    return;
                }

                if (_keyType != value.KeyType)
                    throw new ArgumentException($"A DictionaryRule TProperty KeyType '{_keyType.Name}' must be the same as the Key {value.KeyType.Name}.");

                if (_valueType != value.ValueType)
                    throw new ArgumentException($"A DictionaryRule TProperty ValueType '{_valueType.Name}' must be the same as the Value {value.ValueType.Name}.");

                _item = value;
            }
        }

        /// <summary>
        /// Overrides the <b>Check</b> method and will not validate where performing a shallow validation.
        /// </summary>
        /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
        /// <returns><c>true</c> where validation is to continue; otherwise, <c>false</c> to stop.</returns>
        protected override bool Check(PropertyContext<TEntity, TProperty> context) => !context.ThrowIfNull(nameof(context)).Parent.ShallowValidation && base.Check(context);

        /// <inheritdoc/>
        protected override async Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
        {
            if (context.Value == null)
                return;

            // Iterate through the dictionary validating each of the items.
            var i = 0;
            var hasNullKey = false;
            var hasNullValue = false;
            foreach (var item in context.Value)
            {
                var de = (DictionaryEntry)item;

                // Create the context args.
                var args = context.CreateValidationArgs();
                var indexer = $"[{de.Key}]";
                args.FullyQualifiedEntityName += indexer;
                args.FullyQualifiedJsonEntityName += indexer;
                i++;

                if (!AllowNullKeys && de.Key == null)
                    hasNullKey = true;

                if (!AllowNullValues && de.Value == null)
                    hasNullValue = true;

                // Validate and merge.
                if (de.Key != null && Item?.KeyValidator != null)
                {
                    var r = await Item.KeyValidator.ValidateAsync(de.Key, args, cancellationToken).ConfigureAwait(false);
                    context.MergeResult(r);
                }

                if (de.Value != null && Item?.ValueValidator != null)
                {
                    var r = await Item.ValueValidator.ValidateAsync(de.Value, args, cancellationToken).ConfigureAwait(false);
                    context.MergeResult(r);
                }
            }

            if (hasNullKey)
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.DictionaryNullKeyFormat);

            if (hasNullValue)
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.DictionaryNullValueFormat);

            // Check the length/count.
            if (i < MinCount)
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.MinCountFormat, MinCount);
            else if (MaxCount.HasValue && i > MaxCount.Value)
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.MaxCountFormat, MaxCount);
        }
    }
}