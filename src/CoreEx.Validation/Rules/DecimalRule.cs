﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Represents a numeric rule that validates the maximum <see cref="DecimalPlaces"/> (fractional-part length aka scale) and <see cref="MaxDigits"/> (being the sum of the integer-part and fractional-part lengths aka precision).
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <remarks>Internally converts to the property value to a <see cref="decimal"/>. Floating-point types (<see cref="float"/> and <see cref="double"/>) are generally not supported
    /// as precision might be lost during conversion. For more information on integer- and fractional-part see <see href="https://en.wikipedia.org/wiki/Fractional_part"/>.</remarks>
    public class DecimalRule<TEntity, TProperty> : NumericRule<TEntity, TProperty> where TEntity : class
    {
        private int? _maxDigits;
        private int? _decimalPlaces;

        /// <summary>
        /// Initializes a new instance of the <see cref="DecimalRule{TEntity, TProperty}"/> class.
        /// </summary>
        public DecimalRule() => ValidateWhenDefault = false;

        /// <summary>
        /// Gets or sets the maximum digits being the sum of the integer-part and fractional-part (<see cref="DecimalPlaces"/>) lengths; also known as precision.
        /// </summary>
        /// <remarks>For example, to validate a number with the pattern '999.99', then <see cref="MaxDigits"/> would be 5 and <see cref="DecimalPlaces"/> would be 2. Minimum specified value is 1.</remarks>
        public int? MaxDigits
        {
            get { return _maxDigits; }

            set
            {
                if (value.HasValue && value.Value < 1)
                    throw new ArgumentException("Minimum value (where specified) for MaxDigits is 1.");

                _maxDigits = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum supported number of decimal places (fractional-part length); also known as scale.
        /// </summary>
        /// <remarks>Minimum specified value is 0.</remarks>
        public int? DecimalPlaces
        {
            get { return _decimalPlaces; }

            set
            {
                if (value.HasValue && value.Value < 0)
                    throw new ArgumentException("Minimum value (where specified) for DecimalPlaces is 0.");

                _decimalPlaces = value;
            }
        }

        /// <inheritdoc/>
        protected override Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
        {
            // Convert numeric to a decimal value.
            decimal value = Convert.ToDecimal(context.Value, System.Globalization.CultureInfo.CurrentCulture);

            // Check if negative.
            if (!AllowNegatives && value < 0)
            {
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.AllowNegativesFormat);
                return Task.CompletedTask;
            }

            int il = MaxDigits.HasValue ? DecimalRuleHelper.CalcIntegerPartLength(value) : 0;
            int dp = MaxDigits.HasValue || DecimalPlaces.HasValue ? DecimalRuleHelper.CalcFractionalPartLength(value) : 0;

            // Check max digits.
            if (MaxDigits.HasValue && !DecimalRuleHelper.CheckMaxDigits(MaxDigits.Value, DecimalPlaces, il, dp))
            {
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.MaxDigitsFormat, MaxDigits);
                return Task.CompletedTask;
            }

            // Check decimal places.
            if (DecimalPlaces.HasValue && !DecimalRuleHelper.CheckDecimalPlaces(DecimalPlaces.Value, dp))
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.DecimalPlacesFormat, DecimalPlaces);

            return Task.CompletedTask;
        }
    }
}