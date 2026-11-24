namespace CoreEx.AspNetCore.Publishing;

/// <summary>
/// Carries in-process preparation data only; never serialized as a message or HTTP response.
/// </summary>
/// <param name="Message">The initiating message.</param>
/// <param name="Key">The optional submission key.</param>
internal sealed record WebApiPreparedPublish(EventData Message, CompositeKey? Key = null);
