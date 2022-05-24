// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides the standard text format strings.
    /// </summary>
    /// <remarks>For the format defaults within, the '<c>{0}</c>' and '<c>{1}</c>' placeholders represent a property's friendly text and value itself. Any placeholders '<c>{2}</c>', or above, are specific to the underlying valitator.</remarks>
    public static class ValidatorStrings
    {
        /// <summary>
        /// Gets the format string for the compare equal error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} must be between {2} and {3}.</i></remarks>
        public static LText BetweenInclusiveFormat { get; } = new("CoreEx.Validation.BetweenInclusiveFormat");

        /// <summary>
        /// Gets the format string for the compare equal error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} must be between {2} and {3} (exclusive).</i></remarks>
        public static LText BetweenExclusiveFormat { get; } = new("CoreEx.Validation.BetweenExclusiveFormat");

        /// <summary>
        /// Gets the format string for the compare equal error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} must be equal to {2}.</i></remarks>
        public static LText CompareEqualFormat { get; } = new("CoreEx.Validation.CompareEqualFormat");

        /// <summary>
        /// Gets the format string for the compare not equal error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} must not be equal to {2}.</i></remarks>
        public static LText CompareNotEqualFormat { get; } = new("CoreEx.Validation.CompareNotEqualFormat");

        /// <summary>
        /// Gets the format string for the compare less than error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} must be less than {2}.</i></remarks>
        public static LText CompareLessThanFormat { get; } = new("CoreEx.Validation.CompareLessThanFormat");

        /// <summary>
        /// Gets the format string for the compare less than or equal error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} must be less than or equal to {2}.</i></remarks>
        public static LText CompareLessThanEqualFormat { get; } = new("CoreEx.Validation.CompareLessThanEqualFormat");

        /// <summary>
        /// Gets the format string for the compare greater than error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} must be greater than {2}.</i></remarks>
        public static LText CompareGreaterThanFormat { get; } = new("CoreEx.Validation.CompareGreaterThanFormat");

        /// <summary>
        /// Gets the format string for the compare greater than or equal error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} must be greater than or equal to {2}.</i></remarks>
        public static LText CompareGreaterThanEqualFormat { get; } = new("CoreEx.Validation.CompareGreaterThanEqualFormat");

        /// <summary>
        /// Gets the format string for the Maximum digits error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} must not exceed {2} digits in total.</i></remarks>
        public static LText MaxDigitsFormat { get; } = new("CoreEx.Validation.MaxDigitsFormat");

        /// <summary>
        /// Gets the format string for the Decimal places error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} exceeds the maximum specified number of decimal places ({2}).</i></remarks>
        public static LText DecimalPlacesFormat { get; } = new("CoreEx.Validation.DecimalPlacesFormat");

        /// <summary>
        /// Gets the format string for the duplicate error message.
        /// </summary>
        /// <remarks>Defaults to: <i>xxx</i></remarks>
        public static LText DuplicateFormat { get; } = new("CoreEx.Validation.DuplicateFormat");

        /// <summary>
        /// Gets the format string for a duplicate value error message; includes ability to specify values.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} contains duplicates; {2} value '{3}' specified more than once.</i></remarks>
        public static LText DuplicateValueFormat { get; } = new("CoreEx.Validation.DuplicateValueFormat");

        /// <summary>
        /// Gets the format string for a duplicate value error message; no values specified.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} contains duplicates; {2} value specified more than once.</i></remarks>
        public static LText DuplicateValue2Format { get; } = new("CoreEx.Validation.DuplicateValue2Format");

        /// <summary>
        /// Gets the format string for the minimum count error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} must have at least {2} item(s).</i></remarks>
        public static LText MinCountFormat { get; } = new("CoreEx.Validation.MinCountFormat");

        /// <summary>
        /// Gets the format string for the maximum count error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} must not exceed {2} item(s).</i></remarks>
        public static LText MaxCountFormat { get; } = new("CoreEx.Validation.MaxCountFormat");

        /// <summary>
        /// Gets the format string for the exists error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} is not found; a valid value is required.</i></remarks>
        public static LText ExistsFormat { get; } = new("CoreEx.Validation.ExistsFormat");

        /// <summary>
        /// Gets the format string for the immutable error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} is not allowed to change; please reset value.</i></remarks>
        public static LText ImmutableFormat { get; } = new("CoreEx.Validation.ImmutableFormat");

        /// <summary>
        /// Gets the format string for the Mandatory error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} is required.</i></remarks>
        public static LText MandatoryFormat { get; } = new("CoreEx.Validation.MandatoryFormat");

        /// <summary>
        /// Gets the format string for the must error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} is invalid.</i></remarks>
        public static LText MustFormat { get; } = new("CoreEx.Validation.MustFormat");

        /// <summary>
        /// Gets the format string for the allow negatives error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} must not be negative.</i></remarks>
        public static LText AllowNegativesFormat { get; } = new("CoreEx.Validation.AllowNegativesFormat");

        /// <summary>
        /// Gets the format string for the invalid error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} is invalid.</i></remarks>
        public static LText InvalidFormat { get; } = new("CoreEx.Validation.InvalidFormat");

        /// <summary>
        /// Gets the format string for the invalid items error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} contains one or more invalid items.</i></remarks>
        public static LText InvalidItemsFormat { get; } = new("CoreEx.Validation.InvalidItemsFormat");

        /// <summary>
        /// Gets the format string for the minimum length error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} must be at least {2} characters in length.</i></remarks>
        public static LText MinLengthFormat { get; } = new("CoreEx.Validation.MinLengthFormat");

        /// <summary>
        /// Gets the format string for the maximum length error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} must not exceed {2} characters in length.</i></remarks>
        public static LText MaxLengthFormat { get; } = new("CoreEx.Validation.MaxLengthFormat");

        /// <summary>
        /// Gets the format string for the regex error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} is invalid.</i></remarks>
        public static LText RegexFormat { get; } = new("CoreEx.Validation.RegexFormat");

        /// <summary>
        /// Gets the format string for the wildcard error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} contains invalid or non-supported wildcard selection.</i></remarks>
        public static LText WildcardFormat { get; } = new("CoreEx.Validation.WildcardFormat");

        /// <summary>
        /// Gets the format string for the collection null item error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} contains one or more items that are not specified.</i></remarks>
        public static LText CollectionNullItemFormat { get; } = new("CoreEx.Validation.CollectionNullItemFormat");

        /// <summary>
        /// Gets the format string for the dictionary null key error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} contains one or more keys that are not specified.</i></remarks>
        public static LText DictionaryNullKeyFormat { get; } = new("CoreEx.Validation.DictionaryNullKeyFormat");

        /// <summary>
        /// Gets the format string for the dictionary null value error message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} contains one or more values that are not specified.</i></remarks>
        public static LText DictionaryNullValueFormat { get; } = new("CoreEx.Validation.DictionaryNullValueFormat");

        /// <summary>
        /// Gets the format string for the invalid email message.
        /// </summary>
        /// <remarks>Defaults to: <i>{0} is an invalid e-mail address.</i></remarks>
        public static LText EmailFormat { get; } = new("CoreEx.Validation.EmailFormat");

        /// <summary>
        /// Gets the string for the <see cref="Entities.IPrimaryKey.PrimaryKey"/> literal.
        /// </summary>
        /// <remarks>Defaults to: <i>Primary Key</i></remarks>
        public static LText PrimaryKey { get; } = new("CoreEx.Validation.PrimaryKey");
    }
}