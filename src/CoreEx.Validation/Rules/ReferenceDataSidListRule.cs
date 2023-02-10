// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides validation for a <see cref="IReferenceDataCodeList"/> including <see cref="MinCount"/>, <see cref="MaxCount"/>, per item <see cref="IReferenceData.IsValid"/>, and whether to <see cref="AllowDuplicates"/>.
    /// </summary>
    public class ReferenceDataSidListRule<TEntity, TProperty> : ValueRuleBase<TEntity, TProperty?> where TEntity : class where TProperty : IReferenceDataCodeList
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataSidListRule{TEntity, TProperty}"/> class.
        /// </summary>
        public ReferenceDataSidListRule() => ValidateWhenDefault = false;

        /// <summary>
        /// Gets or sets the minimum count;
        /// </summary>
        public int MinCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum count.
        /// </summary>
        public int? MaxCount { get; set; }

        /// <summary>
        /// Indicates whether duplicate values are allowed.
        /// </summary>
        public bool AllowDuplicates { get; set; } = false;

        /// <inheritdoc/>
        protected override Task ValidateAsync(PropertyContext<TEntity, TProperty?> context, CancellationToken cancellationToken = default)
        {
            if (context.Value!.HasInvalidItems)
            {
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.InvalidItemsFormat);
                return Task.CompletedTask;
            }

            // Check Min and Max counts.
            if (context.Value.Count < MinCount)
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.MinCountFormat, MinCount);
            else if (MaxCount.HasValue && context.Value.Count > MaxCount.Value)
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.MaxCountFormat, MaxCount);

            // Check duplicates.
            if (!AllowDuplicates)
            {
                var dict = new HashSet<string?>();
                foreach (var item in context.Value.ToRefDataList().Where(x => x.IsValid))
                {
                    if (dict.TryGetValue(item.Code, out _))
                    {
                        context.CreateErrorMessage(ErrorText ?? ValidatorStrings.DuplicateValueFormat, "Code", item.ToString());
                        return Task.CompletedTask;
                    }

                    dict.Add(item.Code);
                }
            }

            return Task.CompletedTask;
        }
    }
}