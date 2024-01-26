// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Entities;
using System.Collections.Generic;

namespace CoreEx.Validation
{
    /// <summary>
    /// Represents the optional extended arguments for an entity validation.
    /// </summary>
    public class ValidationArgs
    {
        private static bool? _defaultUseJsonNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationArgs"/> class.
        /// </summary>
        public ValidationArgs() { }

        /// <summary>
        /// Indicates whether to use the JSON name for the <see cref="MessageItem"/> <see cref="MessageItem.Property"/>; by default (<c>false</c>) uses the .NET name.
        /// </summary>
        /// <remarks>Will attempt to use <see cref="SettingsBase.ValidationUseJsonNames"/> as a default where possible.</remarks>
        public static bool DefaultUseJsonNames 
        { 
            get => _defaultUseJsonNames ?? ExecutionContext.GetService<SettingsBase>()?.ValidationUseJsonNames ?? false;
            set => _defaultUseJsonNames = value;
        } 

        /// <summary>
        /// Gets or sets the optional name of a selected (specific) property to validate for the entity (<c>null</c> indicates to validate all).
        /// </summary>
        /// <remarks>Nested or fully quailified entity names are not supported for this type of validation; only a property of the primary entity can be selected.</remarks>
        public string? SelectedPropertyName { get; set; }

        /// <summary>
        /// Gets or sets the entity prefix used for fully qualified <i>entity.property</i> naming (<c>null</c> represents the root).
        /// </summary>
        public string? FullyQualifiedEntityName { get; set; }

        /// <summary>
        /// Gets or sets the entity prefix used for fully qualified <i>entity.property</i> naming (<c>null</c> represents the root).
        /// </summary>
        public string? FullyQualifiedJsonEntityName { get; set; }

        /// <summary>
        /// Indicates (overrides <see cref="DefaultUseJsonNames"/>) whether to use the JSON name for the <see cref="MessageItem"/> <see cref="MessageItem.Property"/>;
        /// defaults to <c>null</c> (uses the <see cref="DefaultUseJsonNames"/> value).
        /// </summary>
        public bool? UseJsonNames { get; set; }

        /// <summary>
        /// Gets <see cref="UseJsonNames"/> selection.
        /// </summary>
        internal bool UseJsonNamesSelection => UseJsonNames ?? DefaultUseJsonNames;

        /// <summary>
        /// Indicates that a shallow validation is required; i.e. will only validate the top level properties.
        /// </summary>
        /// <remarks>The default deep validation will not only validate the top level properties, but also those children down the object graph;
        /// i.e. sub-objects and collections.</remarks>
        public bool ShallowValidation { get; set; }

        /// <summary>
        /// Gets the configuration parameters.
        /// </summary>
        /// <remarks>Configuration parameters provide a means to pass values down through the validation stack. The consuming developer must instantiate the property on first use.</remarks>
        public IDictionary<string, object?>? Config { get; set; }
    }
}