// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.Results.Abstractions
{
    /// <summary>
    /// Provides a '<c>try/catch</c>' <see cref="WithWrapper"/> implementation.
    /// </summary>
    public class WithTryCatchWrapper : WithWrapper
    {
        /// <summary>
        /// Gets or sets the default <see cref="WithTryCatchWrapper"/> instance.
        /// </summary>
        public static WithTryCatchWrapper Default { get; set; } = new WithTryCatchWrapper();

        /// <inheritdoc/>
        protected override IResult Execute(IResult result, Func<IResult> func)
        {
            if (result.IsFailure)
                return result;

            try
            {
                return func();
            }
            catch (Exception ex)
            {
                return result.ToFailure(ex);
            }
        }

        /// <inheritdoc/>
        protected override async Task<IResult> ExecuteAsync(IResult result, Func<Task<IResult>> func)
        {
            if (result.IsFailure)
                return result;

            try
            {
                return await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return result.ToFailure(ex);
            }
        }
    }
}