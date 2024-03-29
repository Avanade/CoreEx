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
        /// Gets or sets the format string for the compare equal error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} must be between {2} and {3}</c>'.</remarks>
        public static LText BetweenInclusiveFormat { get; set; } = new("CoreEx.Validation.BetweenInclusiveFormat", "{0} must be between {2} and {3}.");

        /// <summary>
        /// Gets or sets the format string for the compare equal error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} must be between {2} and {3} (exclusive)</c>'.</remarks>
        public static LText BetweenExclusiveFormat { get; set; } = new("CoreEx.Validation.BetweenExclusiveFormat", "{0} must be between {2} and {3} (exclusive).");

        /// <summary>
        /// Gets or sets the format string for the compare equal error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} must be equal to {2}</c>'.</remarks>
        public static LText CompareEqualFormat { get; set; } = new("CoreEx.Validation.CompareEqualFormat", "{0} must be equal to {2}.");

        /// <summary>
        /// Gets or sets the format string for the compare not equal error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} must not be equal to {2}</c>'.</remarks>
        public static LText CompareNotEqualFormat { get; set; } = new("CoreEx.Validation.CompareNotEqualFormat", "{0} must not be equal to {2}.");

        /// <summary>
        /// Gets or sets the format string for the compare less than error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} must be less than {2}</c>'.</remarks>
        public static LText CompareLessThanFormat { get; set; } = new("CoreEx.Validation.CompareLessThanFormat", "{0} must be less than {2}.");

        /// <summary>
        /// Gets or sets the format string for the compare less than or equal error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} must be less than or equal to {2}</c>'.</remarks>
        public static LText CompareLessThanEqualFormat { get; set; } = new("CoreEx.Validation.CompareLessThanEqualFormat", "{0} must be less than or equal to {2}.");

        /// <summary>
        /// Gets or sets the format string for the compare greater than error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} must be greater than {2}</c>'.</remarks>
        public static LText CompareGreaterThanFormat { get; set; } = new("CoreEx.Validation.CompareGreaterThanFormat", "{0} must be greater than {2}.");

        /// <summary>
        /// Gets or sets the format string for the compare greater than or equal error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} must be greater than or equal to {2}</c>'.</remarks>
        public static LText CompareGreaterThanEqualFormat { get; set; } = new("CoreEx.Validation.CompareGreaterThanEqualFormat", "{0} must be greater than or equal to {2}.");

        /// <summary>
        /// Gets or sets the format string for the Maximum digits error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} must not exceed {2} digits in total</c>'.</remarks>
        public static LText MaxDigitsFormat { get; set; } = new("CoreEx.Validation.MaxDigitsFormat", "{0} must not exceed {2} digits in total.");

        /// <summary>
        /// Gets or sets the format string for the Decimal places error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} exceeds the maximum specified number of decimal places ({2})</c>'.</remarks>
        public static LText DecimalPlacesFormat { get; set; } = new("CoreEx.Validation.DecimalPlacesFormat", "{0} exceeds the maximum specified number of decimal places ({2}).");

        /// <summary>
        /// Gets or sets the format string for the duplicate error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} already exists and would result in a duplicate.</c>'</remarks>
        public static LText DuplicateFormat { get; set; } = new("CoreEx.Validation.DuplicateFormat", "{0} already exists and would result in a duplicate.");

        /// <summary>
        /// Gets or sets the format string for a duplicate value error message; includes ability to specify values.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} contains duplicates; {2} value '{3}' specified more than once</c>'.</remarks>
        public static LText DuplicateValueFormat { get; set; } = new("CoreEx.Validation.DuplicateValueFormat", "{0} contains duplicates; {2} value '{3}' specified more than once.");

        /// <summary>
        /// Gets or sets the format string for a duplicate value error message; no values specified.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} contains duplicates; {2} value specified more than once</c>'.</remarks>
        public static LText DuplicateValue2Format { get; set; } = new("CoreEx.Validation.DuplicateValue2Format", "{0} contains duplicates; {2} value specified more than once.");

        /// <summary>
        /// Gets or sets the format string for the minimum count error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} must have at least {2} item(s)</c>'.</remarks>
        public static LText MinCountFormat { get; set; } = new("CoreEx.Validation.MinCountFormat", "{0} must have at least {2} item(s).");

        /// <summary>
        /// Gets or sets the format string for the maximum count error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} must not exceed {2} item(s)</c>'.</remarks>
        public static LText MaxCountFormat { get; set; } = new("CoreEx.Validation.MaxCountFormat", "{0} must not exceed {2} item(s).");

        /// <summary>
        /// Gets or sets the format string for the exists error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} is not found; a valid value is required</c>'.</remarks>
        public static LText ExistsFormat { get; set; } = new("CoreEx.Validation.ExistsFormat", "{0} is not found; a valid value is required.");

        /// <summary>
        /// Gets or sets the format string for the immutable error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} is not allowed to change; please reset value</c>'.</remarks>
        public static LText ImmutableFormat { get; set; } = new("CoreEx.Validation.ImmutableFormat", "{0} is not allowed to change; please reset value.");

        /// <summary>
        /// Gets the format string for the Mandatory error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} is required</c>'. This references <see cref="Validation.MandatoryFormat"/>.</remarks>
        public static LText MandatoryFormat => Validation.MandatoryFormat;

        /// <summary>
        /// Gets or sets the format string for the must error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} is invalid</c>'.</remarks>
        public static LText MustFormat { get; set; } = new("CoreEx.Validation.MustFormat", "{0} is invalid.");

        /// <summary>
        /// Gets or sets the format string for the allow negatives error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} must not be negative</c>'.</remarks>
        public static LText AllowNegativesFormat { get; set; } = new("CoreEx.Validation.AllowNegativesFormat", "{0} must not be negative.");

        /// <summary>
        /// Gets or sets the format string for the invalid error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} is invalid</c>'.</remarks>
        public static LText InvalidFormat { get; set; } = new("CoreEx.Validation.InvalidFormat", "{0} is invalid.");

        /// <summary>
        /// Gets or sets the format string for the invalid items error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} contains one or more invalid items</c>'.</remarks>
        public static LText InvalidItemsFormat { get; set; } = new("CoreEx.Validation.InvalidItemsFormat", "{0} contains one or more invalid items.");

        /// <summary>
        /// Gets or sets the format string for the minimum length error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} must be at least {2} characters in length</c>'.</remarks>
        public static LText MinLengthFormat { get; set; } = new("CoreEx.Validation.MinLengthFormat", "{0} must be at least {2} characters in length.");

        /// <summary>
        /// Gets or sets the format string for the maximum length error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} must not exceed {2} characters in length</c>'.</remarks>
        public static LText MaxLengthFormat { get; set; } = new("CoreEx.Validation.MaxLengthFormat", "{0} must not exceed {2} characters in length.");

        /// <summary>
        /// Gets or sets the format string for the exact length error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} must be exactly {2} characters in length</c>'.</remarks>
        public static LText ExactLengthFormat { get; set; } = new("CoreEx.Validation.ExactLengthFormat", "{0} must be exactly {2} characters in length.");

        /// <summary>
        /// Gets or sets the format string for the regex error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} is invalid</c>'.</remarks>
        public static LText RegexFormat { get; set; } = new("CoreEx.Validation.RegexFormat", "{0} is invalid.");

        /// <summary>
        /// Gets or sets the format string for the wildcard error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} contains invalid or non-supported wildcard selection</c>'.</remarks>
        public static LText WildcardFormat { get; set; } = new("CoreEx.Validation.WildcardFormat", "{0} contains invalid or non-supported wildcard selection.");

        /// <summary>
        /// Gets or sets the format string for the collection null item error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} contains one or more items that are not specified</c>'.</remarks>
        public static LText CollectionNullItemFormat { get; set; } = new("CoreEx.Validation.CollectionNullItemFormat", "{0} contains one or more items that are not specified.");

        /// <summary>
        /// Gets or sets the format string for the dictionary null key error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} contains one or more keys that are not specified</c>'.</remarks>
        public static LText DictionaryNullKeyFormat { get; set; } = new("CoreEx.Validation.DictionaryNullKeyFormat", "{0} contains one or more keys that are not specified.");

        /// <summary>
        /// Gets or sets the format string for the dictionary null value error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} contains one or more values that are not specified</c>'.</remarks>
        public static LText DictionaryNullValueFormat { get; set; } = new("CoreEx.Validation.DictionaryNullValueFormat", "{0} contains one or more values that are not specified.");

        /// <summary>
        /// Gets or sets the format string for the invalid email message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} is an invalid e-mail address</c>'.</remarks>
        public static LText EmailFormat { get; set; } = new("CoreEx.Validation.EmailFormat", "{0} is an invalid e-mail address.");

        /// <summary>
        /// Gets or sets the format string for when no (none) value is to be specified.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} must not be specified.</c>'.</remarks>
        public static LText NoneFormat { get; set; } = new("CoreEx.Validation.NoneFormat", "{0} must not be specified.");

        /// <summary>
        /// Gets or sets the string for the <see cref="Entities.IPrimaryKey.PrimaryKey"/> literal.
        /// </summary>
        /// <remarks>Defaults to: '<c>Primary Key</c>'</remarks>
        public static LText PrimaryKey { get; set; } = new("CoreEx.Validation.PrimaryKey", "Primary Key");

        /// <summary>
        /// Gets or sets the string for the <see cref="Entities.IIdentifier.Id"/> literal.
        /// </summary>
        /// <remarks>Defaults to: '<c>Identifier</c>'</remarks>
        public static LText Identifier { get; set; } = new("CoreEx.Validation.Identifier", "Identifier");
    }
}