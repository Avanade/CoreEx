// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Results;
using CoreEx.Results.Abstractions;
using System;
using System.Threading.Tasks;

namespace CoreEx.Invokers
{
    /// <summary>
    /// Provides a <typeparamref name="TInvoker"/> <see cref="WithWrapper"/> implementation.
    /// </summary>
    /// <typeparam name="TInvoker">The <see cref="InvokerBase"/> <see cref="Type"/>.</typeparam>
    public class WithInvokerWrapper<TInvoker> : WithWrapper<(object?, InvokerArgs?)> where TInvoker : InvokerBase, new()
    {
        /// <summary>
        /// Gets or sets the default <see cref="WithTryCatchWrapper"/> instance.
        /// </summary>
        public static WithInvokerWrapper<TInvoker> Default { get; set; } = new WithInvokerWrapper<TInvoker>();

        private static TInvoker? _default;

        /// <inheritdoc/>
        protected override IResult Execute(IResult result, Func<IResult> func, (object?, InvokerArgs?) args)
        {
            if (result.IsFailure)
                return result;

            var invoker = ExecutionContext.GetService<TInvoker>() ?? (_default ??= new TInvoker());
            return invoker.Invoke(args.Item1!, func, args.Item2);
        }

        /// <inheritdoc/>
        protected override Task<IResult> ExecuteAsync(IResult result, Func<Task<IResult>> func, (object?, InvokerArgs?) args = default)
        {
            if (result.IsFailure)
                return Task.FromResult(result);

            var invoker = ExecutionContext.GetService<TInvoker>() ?? (_default ??= new TInvoker());
            return invoker.InvokeAsync(args.Item1!, _ => func(), args.Item2);
        }
    }
}