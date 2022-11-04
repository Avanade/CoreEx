// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Provides constants for the extended entities.
    /// </summary>
    public static class EntityConsts
    {
        /// <summary>
        /// Gets or sets the value is immutable message.
        /// </summary>
        public static string ValueIsImmutableMessage { get; set; } = "Value is immutable; cannot be changed once already set to a value.";

        /// <summary>
        /// Gets or sets the entity is read only message.
        /// </summary>
        public static string EntityIsReadOnlyMessage { get; set; } = "Entity is read only; property cannot be changed.";
    }
}