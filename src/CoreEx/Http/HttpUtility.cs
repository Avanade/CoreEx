// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Http
{
    /// <summary>
    /// Provides internal utility functions.
    /// </summary>
    internal static class HttpUtility
    {
        /// <summary>
        /// Parses the value as a <see cref="long"/>.
        /// </summary>
        public static long? ParseLongValue(string? value)
        {
            if (value == null)
                return null;

            if (!long.TryParse(value, out long val))
                return null;

            return val;
        }

        /// <summary>
        /// Parses the value as a <see cref="bool"/>.
        /// </summary>
        public static bool ParseBoolValue(string? value)
        {
            if (value == null)
                return false;

            if (!bool.TryParse(value, out bool val))
                return false;

            return val;
        }
    }
}