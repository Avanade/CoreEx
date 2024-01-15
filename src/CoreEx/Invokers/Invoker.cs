// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Results;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Invokers
{
    /// <summary>
    /// Provides invoking capabilities including <see cref="RunSync(Func{Task})"/> and <see cref="RunSync{T}(Func{Task{T}})"/> to execute an async <see cref="Task"/> synchronously.
    /// </summary>
    public static class Invoker
    {
        private static readonly TaskFactory _taskFactory = new(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Returns the <paramref name="task"/> where not <c>null</c>; otherwise, a <see cref="Task.CompletedTask"/>.
        /// </summary>
        /// <param name="task">The <see cref="Task"/>.</param>
        /// <returns>The <paramref name="task"/> where not <c>null</c>; otherwise, a <see cref="Task.CompletedTask"/>.</returns>
        public static Task InvokeAsync(Task? task) => task ?? Task.CompletedTask;

        /// <summary>
        /// Returns the <paramref name="task"/> where not <c>null</c>; otherwise, a <see cref="Task.FromResult{TResult}(TResult)"/> with a default value.
        /// </summary>
        /// <param name="task">The <see cref="Task"/>.</param>
        /// <returns>The <paramref name="task"/> where not <c>null</c>; otherwise, a <see cref="Task.FromResult{TResult}(TResult)"/> with a default value.</returns>
        public static Task<T> InvokeAsync<T>(Task<T>? task) => task ?? Task.FromResult<T>(default!);

        /// <summary>
        /// Executes an async <see cref="Task"/> sychronously.
        /// </summary>
        /// <param name="task">The async <see cref="Task"/>.</param>
        /// <remarks>The general guidance is to avoid sync over async as this may result in deadlock, so please consider all options before using. There are many <see href="https://stackoverflow.com/questions/5095183/how-would-i-run-an-async-taskt-method-synchronously">articles</see>
        /// written discussing this subject; however, if sync over async is needed this method provides a consistent approach to perform. This implementation has been inspired by <see href="https://www.ryadel.com/en/asyncutil-c-helper-class-async-method-sync-result-wait/"/>.</remarks>
        public static void RunSync(Func<Task> task) => _taskFactory.StartNew(task.ThrowIfNull(nameof(task))).Unwrap().GetAwaiter().GetResult();

        /// <summary>
        /// Executes an async <see cref="Task"/> sychronously with a result.
        /// </summary>
        /// <typeparam name="T">The result <see cref="Type"/>.</typeparam>
        /// <param name="task">The async <see cref="Task"/>.</param>
        /// <returns>The resulting value.</returns>
        /// <remarks>The general guidance is to avoid sync over async as this may result in deadlock, so please consider all options before using. There are many <see href="https://stackoverflow.com/questions/5095183/how-would-i-run-an-async-taskt-method-synchronously">articles</see>
        /// written discussing this subject; however, if sync over async is needed this method provides a consistent approach to perform. This implementation has been inspired by <see href="https://www.ryadel.com/en/asyncutil-c-helper-class-async-method-sync-result-wait/"/>.</remarks>
        public static T RunSync<T>(Func<Task<T>> task) => _taskFactory.StartNew(task.ThrowIfNull(nameof(task))).Unwrap().GetAwaiter().GetResult();
    }
}