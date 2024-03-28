// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Localization
{
    /// <summary>
    /// Provides access to the global/static <see cref="Current"/> instance.
    /// </summary>
    public static class TextProvider
    {
        private static ITextProvider? _textProvider;
        private static ITextProvider? _backupTextProvider;

        /// <summary>
        /// Sets the <see cref="Current"/> <see cref="ITextProvider"/> instance explicitly.
        /// </summary>
        /// <param name="textProvider">The concrete <see cref="ITextProvider"/> instance.</param>
        public static void SetTextProvider(ITextProvider? textProvider) => _textProvider = textProvider;

        /// <summary>
        /// Gets the current <see cref="ITextProvider"/> instance using in the following order: <see cref="ExecutionContext.GetService{T}"/>, the explicit <see cref="SetTextProvider(ITextProvider)"/>, otherwise, <see cref="NullTextProvider"/>. 
        /// </summary>
        public static ITextProvider Current
        {
            get
            {
                var tp = ExecutionContext.GetService<ITextProvider>();
                if (tp != null)
                    return tp;

                if (_textProvider != null)
                    return _textProvider;

                return _backupTextProvider ??= new NullTextProvider();
            }
        }
    }
}