// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Newtonsoft.Json.Serialization;

namespace CoreEx.Newtonsoft.Json
{
    /// <summary>
    /// Provides a substitution and camel case naming strategy.
    /// </summary>
    /// <remarks>Converts the name by checking <see cref="CoreEx.Json.JsonSerializer.NameSubstitutions"/>, then uses <see cref="CamelCaseNamingStrategy"/>.</remarks>
    public class SubstituteNamingStrategy : CamelCaseNamingStrategy
    {
        /// <summary>
        /// Gets the <see cref="SubstituteNamingStrategy"/> instance.
        /// </summary>
        public static SubstituteNamingStrategy Substitute { get; } = new SubstituteNamingStrategy();

        /// <summary>
        /// Initializes a new instance of the <see cref="SubstituteNamingStrategy"/> class.
        /// </summary>
        public SubstituteNamingStrategy()
        {
            ProcessDictionaryKeys = true;
            OverrideSpecifiedNames = false;
        }

        /// <inheritdoc/>
        public override string GetPropertyName(string name, bool hasSpecifiedName) 
            => (!hasSpecifiedName && CoreEx.Json.JsonSerializer.NameSubstitutions.TryGetValue(name, out var jsonName)) ? jsonName : base.GetPropertyName(name, hasSpecifiedName);

        /// <inheritdoc/>
        public override string GetDictionaryKey(string key) => CoreEx.Json.JsonSerializer.NameSubstitutions.TryGetValue(key, out var jsonName) ? jsonName : base.GetDictionaryKey(key);
    }
}