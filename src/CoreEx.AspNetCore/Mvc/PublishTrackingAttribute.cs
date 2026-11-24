namespace CoreEx.AspNetCore.Mvc;

/// <summary>
/// Indicates that the action/operation publishes a tracked operation.
/// </summary>
/// <remarks>Enables runtime tracking for a <see cref="CoreEx.AspNetCore.Abstractions.WebApi{TResult}.PublishAsync(Microsoft.AspNetCore.Http.HttpRequest, CoreEx.AspNetCore.Publishing.WebApiPublishOptionsBase, System.Threading.CancellationToken)"/> call on this endpoint and describes the <c>202 Accepted</c> <see cref="CoreEx.AspNetCore.Operations.OperationStatus"/> response, polling headers and long-running-operation metadata. Per-call <see cref="CoreEx.AspNetCore.Publishing.WebApiPublishOperationOptions"/> only tunes tracking; it does not enable tracking on routed endpoints.</remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class PublishTrackingAttribute : Attribute { }
