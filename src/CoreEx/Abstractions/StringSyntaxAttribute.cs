// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

#if !NET7_0_OR_GREATER

using CoreEx;

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Provides a fake version to enable the usage of the <see cref="StringSyntaxAttribute"/> in previous versions of .NET.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class StringSyntaxAttribute(string syntax) : Attribute
    {
        /// <summary>
        /// The syntax identifier for strings containing composite formats.
        /// </summary>
        public const string CompositeFormat = nameof(CompositeFormat);

        /// <summary>
        /// The syntax identifier for strings containing regular expressions.
        /// </summary>
        public const string Regex = nameof(Regex);

        /// <summary>
        /// The syntax identifier for strings containing JavaScript Object Notation (JSON).
        /// </summary>
        public const string Json = nameof(Json);

        /// <summary>
        /// The syntax identifier for strings containing URIs.
        /// </summary>
        public const string Uri = nameof(Uri);

        /// <summary>
        /// Gets the identifier of the syntax used.
        /// </summary>
        public string Syntax { get; } = syntax.ThrowIfNullOrEmpty(nameof(syntax));
    }
}

#endif