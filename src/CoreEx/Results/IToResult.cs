// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Results
{
    /// <summary>
    /// Enables the <see cref="ToResult"/> to convert into a corresponding <see cref="Result"/>.
    /// </summary>
    public interface IToResult
    {
        /// <summary>
        /// Converts into a corresponding <see cref="Result"/>.
        /// </summary>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        Result ToResult();
    }
}
