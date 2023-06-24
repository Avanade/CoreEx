// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Stj = System.Text.Json;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Provides a substitution and camel case naming policy.
    /// </summary>
    /// <remarks>Converts the name by checking <see cref="CoreEx.Json.JsonSerializer.NameSubstitutions"/>, then uses <see cref="Stj.JsonNamingPolicy.CamelCase"/>.</remarks>
    public class SubstituteNamingPolicy : Stj.JsonNamingPolicy
    {
        /// <summary>
        /// Gets the <see cref="SubstituteNamingPolicy"/> instance.
        /// </summary>
        public static SubstituteNamingPolicy Substitute { get; } = new SubstituteNamingPolicy();

        /// <inheritdoc/>
        public override string ConvertName(string name) => CoreEx.Json.JsonSerializer.NameSubstitutions.TryGetValue(name, out var jsonName) ? jsonName : CamelCase.ConvertName(name);
    }
}