namespace CoreEx.Invokers;

/// <summary>
/// Provides a generic invoker.
/// </summary>
[InvokerName("CoreEx.Invokers.Invoker")]
public sealed class Invoker : InvokerBase
{
    /// <summary>
    /// Gets the default <see cref="Invoker"/> for general-purpose use.
    /// </summary>
    /// <remarks>Note that both <see cref="IsLoggingDisabled"/> and <see cref="IsTracingDisabled"/> are <see langword="true"/>; i.e. they are disabled.</remarks>
    public static Invoker Default { get; } = new Invoker();

    /// <summary>
    /// Executes an async <paramref name="func"/> synchronously.
    /// </summary>
    /// <param name="func">The async function.</param>
    /// <remarks>The general guidance is to avoid sync over async as this may result in deadlock, so please consider all options before using. There are many <see href="https://stackoverflow.com/questions/5095183/how-would-i-run-an-async-taskt-method-synchronously">articles</see>
    /// written discussing this subject; however, if sync over async is needed this method provides a consistent approach to perform.</remarks>
    public static void RunSync(Func<Task> func)
    {
        var task = func();
        if (!task.IsCompleted)
            task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes an async <paramref name="func"/> synchronously with a result.
    /// </summary>
    /// <typeparam name="T">The result <see cref="Type"/>.</typeparam>
    /// <param name="func">The async function.</param>
    /// <returns>The resulting value.</returns>
    /// <remarks>The general guidance is to avoid sync over async as this may result in deadlock, so please consider all options before using. There are many <see href="https://stackoverflow.com/questions/5095183/how-would-i-run-an-async-taskt-method-synchronously">articles</see>
    /// written discussing this subject; however, if sync over async is needed this method provides a consistent approach to perform.</remarks>
    public static T RunSync<T>(Func<Task<T>> func)
    {
        var task = func();
        if (task.IsCompleted)
            return task.Result;

        return task.GetAwaiter().GetResult();
    }

    /// <inheritdoc/>>
    public override bool IsLoggingDisabled => true;

    /// <inheritdoc/>>
    public override bool IsTracingDisabled => true;
}