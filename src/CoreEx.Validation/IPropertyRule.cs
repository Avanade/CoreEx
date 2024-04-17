// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;

namespace CoreEx.Validation
{
    /// <summary>
    /// Enables a validation rule for an entity property. 
    /// </summary>
    public interface IPropertyRule
    {
        /// <summary>
        /// Gets the property name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the JSON property name.
        /// </summary>
        public string JsonName { get; }

        /// <summary>
        /// Gets or sets the friendly text name used in validation messages.
        /// </summary>
        public LText Text { get; set; }

        /// <summary>
        /// Gets or sets the error message format text (overrides the default) used for all validation errors.
        /// </summary>
        public LText? ErrorText { get; set; }
    }
}