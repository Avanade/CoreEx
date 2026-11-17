#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class UnitTestExExtensions
{
    /// <summary>
    /// Resets the Azure Service Bus queues and topics/subscriptions to an initial state by deleting and recreating them.
    /// </summary>
    /// <param name="tester">The <see cref="TesterBase"/>.</param>
    /// <param name="queues">The queues to reset.</param>
    /// <param name="topicsAndSubscriptions">The topics and their subscriptions to reset.</param>
    /// <param name="connectionName">The named connection string/configuration section as used by <c>AddAzureServiceBusClient</c>/<c>AddKeyedAzureServiceBusClient</c> to configure the underlying <see cref="Asb.ServiceBusClient"/>; defaults to <c>"ServiceBus"</c>.</param>
    /// <param name="adminPort">The port to use for the administration client.</param>
    /// <remarks>The <see cref="Asb.ServiceBusClient"/> registered by Aspire does not retain the connection string it was created from, so this reads it directly from configuration instead - first
    /// the standard Aspire-orchestrated <c>ConnectionStrings:{connectionName}</c> key, then falling back to the <c>Aspire:Azure:Messaging:ServiceBus:ConnectionString</c> section used when the
    /// connection string is configured directly (e.g. via <c>appsettings.Development.json</c>).</remarks>
    public static async Task ResetAzureServiceBusAsync(this TesterBase tester, CreateQueueOptions[]? queues = null, (CreateTopicOptions Topic, CreateSubscriptionOptions[] Subscriptions)[]? topicsAndSubscriptions = null, string connectionName = "ServiceBus", int adminPort = 5300)
    {
        var config = tester.ThrowIfNull().Configuration;
        var cs = config.GetConnectionString(connectionName.ThrowIfNullOrEmpty())
            ?? config[$"Aspire:Azure:Messaging:ServiceBus:ConnectionString"]
            ?? throw new InvalidOperationException($"The '{connectionName}' Azure Service Bus connection string was not found in configuration.");

        await ResetAzureServiceBusAsync(CreateAzureServiceBusAdminConnectionString(cs, adminPort), queues, topicsAndSubscriptions).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all messages for the Azure Service Bus queue or topic subscription completing each resulting in all messages also being cleared.
    /// </summary>
    /// <param name="tester">The <see cref="TesterBase"/>.</param>
    /// <param name="sbo">The <see cref="ServiceBusReceiverOptions"/>.</param>
    /// <returns>A list of <see cref="Asb.ServiceBusReceivedMessage"/> that were cleared.</returns>
    public static async Task<List<Asb.ServiceBusReceivedMessage>> GetAndClearAzureServiceBusAsync(this TesterBase tester, ServiceBusReceiverOptions sbo)
    {
        var sbc = tester.ThrowIfNull().Services.GetRequiredService<Asb.ServiceBusClient>();
        var qtn = CoreEx.Abstractions.Internal.GetValueFromConfigurationWhereApplicable(sbo.QueueOrTopicName, tester.Configuration);
        var list = new List<Asb.ServiceBusReceivedMessage>();

        await using var receiver = sbo.IsSubscription
            ? sbc.CreateReceiver(qtn, CoreEx.Abstractions.Internal.GetValueFromConfigurationWhereApplicable(sbo.SubscriptionName!, tester.Configuration))
            : sbc.CreateReceiver(qtn);

        while (true)
        {
            // A generous maxWaitTime (rather than a near-zero poll) avoids a real broker/emulator's receive-visibility latency
            // being mistaken for "queue is empty" - ReceiveMessagesAsync still returns as soon as messages are available, so
            // this only adds latency on the final, genuinely-empty call that ends the loop.
            var messages = await receiver.ReceiveMessagesAsync(maxMessages: 50, maxWaitTime: TimeSpan.FromSeconds(1));
            if (messages.Count == 0)
                break;

            foreach (var m in messages)
                await receiver.CompleteMessageAsync(m);

            list.AddRange(messages);
        }

        return list;
    }

    /// <summary>
    /// Gets all messages for the Azure Service Bus queue or topic subscription completing each resulting in all messages also being cleared.
    /// </summary>
    /// <param name="tester">The <see cref="TesterBase"/>.</param>
    /// <param name="sbo">The <see cref="ServiceBusSessionReceiverOptions"/>.</param>
    /// <returns>A list of <see cref="Asb.ServiceBusReceivedMessage"/> that were cleared.</returns>
    /// <remarks>This method is used for session-enabled queues or topic subscriptions.</remarks>
    public static async Task<List<Asb.ServiceBusReceivedMessage>> GetAndClearAzureServiceBusAsync(this TesterBase tester, ServiceBusSessionReceiverOptions sbo)
    {
        var sbc = tester.ThrowIfNull().Services.GetRequiredService<Asb.ServiceBusClient>();
        var qtn = CoreEx.Abstractions.Internal.GetValueFromConfigurationWhereApplicable(sbo.QueueOrTopicName, tester.Configuration);
        var list = new List<Asb.ServiceBusReceivedMessage>();

        while (true)
        {
            Asb.ServiceBusSessionReceiver? session;

            try
            {
                session = sbo.IsSubscription
                    ? await sbc.AcceptNextSessionAsync(qtn, CoreEx.Abstractions.Internal.GetValueFromConfigurationWhereApplicable(sbo.SubscriptionName!, tester.Configuration))
                    : await sbc.AcceptNextSessionAsync(qtn);
            }
            catch (Asb.ServiceBusException ex)
            {
                if (ex.Reason == Asb.ServiceBusFailureReason.ServiceTimeout || (ex.InnerException is System.Net.Sockets.SocketException innerEx && innerEx.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut))
                    break; // No more sessions available

                throw;
            }

            if (session is null)
                break;

            await using (session)
            {
                while (true)
                {
                    // See the queue/topic overload above for why this uses a generous maxWaitTime rather than a near-zero poll.
                    var messages = await session.ReceiveMessagesAsync(maxMessages: 50, maxWaitTime: TimeSpan.FromSeconds(1));
                    if (messages.Count == 0)
                        break;

                    foreach (var msg in messages)
                        await session.CompleteMessageAsync(msg);

                    list.AddRange(messages);
                }
            }
        }

        return list;
    }

    /// <summary>
    /// Replaces the registered <see cref="IEventPublisher"/> with a decorator (<see cref="EventPublisherDecorator"/>) that also captures the published events for expectation assertions.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="serviceKey">The service key for the previously registered <see cref="IEventPublisher"/>.</param>
    /// <param name="bypassPassThrough">Indicates whether to bypass the pass-through to the original event publisher.</param>
    /// <returns>The <see cref="IServiceCollection"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This is a convenience method that defaults the <paramref name="serviceKey"/> to <see cref="ServiceBusPublisher.DefaultServiceKey"/> where invoking the underlying <see cref="UseExpectedEventPublisher(IServiceCollection, string, bool)"/>.
    /// <para>The <paramref name="bypassPassThrough"/> when set to <see langword="true"/> will bypass the pass-through to the original event publisher and leverage the <see cref="NoOpEventPublisher"/> instead.</para></remarks>
    public static IServiceCollection UseExpectedAzureServiceBusPublisher(this IServiceCollection services, string serviceKey = ServiceBusPublisher.DefaultServiceKey, bool bypassPassThrough = false)
        => UseExpectedEventPublisher(services, serviceKey, bypassPassThrough);

    /// <summary>
    /// Replaces the registered <see cref="IEventPublisher"/> with a decorator (<see cref="EventPublisherDecorator"/>) that also captures the published events for expectation assertions; whilst also adding post-run expectations for the captured events.
    /// </summary>
    /// <typeparam name="TEntryPoint">The API startup <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="AspNetCore.ApiTester{TEntryPoint}"/>.</param>
    /// <param name="serviceKey">The service key for the previously registered <see cref="IEventPublisher"/>.</param>
    /// <param name="bypassPassThrough">Indicates whether to bypass the pass-through to the original event publisher.</param>
    /// <param name="expectNoEvents">Indicates whether to expect no events to be published.</param>
    /// <returns>The <see cref="AspNetCore.ApiTester{TEntryPoint}"/> instance to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="expectNoEvents"/> parameter is only actioned when no explicit event expectations are defined for the underlying test; acts as a catch all.</remarks>
    public static AspNetCore.ApiTester<TEntryPoint> UseExpectedAzureServiceBusPublisher<TEntryPoint>(this AspNetCore.ApiTester<TEntryPoint> tester, string serviceKey = ServiceBusPublisher.DefaultServiceKey, bool bypassPassThrough = false, bool expectNoEvents = true) where TEntryPoint : class
        => tester.ConfigureServices(services => services.UseExpectedAzureServiceBusPublisher(serviceKey, bypassPassThrough))
                 .AddEventExpectationsPostRun(serviceKey, expectNoEvents);

    /// <summary>
    /// Converts a <see cref="CloudEvent"/> to a <see cref="Asb.ServiceBusReceivedMessage"/>.
    /// </summary>
    /// <param name="cloudEvent">The <see cref="CloudEvent"/>.</param>
    /// <param name="contentMode">The <see cref="ContentMode"/> to use; defaults to <see cref="ContentMode.Structured"/>.</param>
    /// <param name="includeAttributes">Indicates whether to include all <see cref="CloudEvent.GetPopulatedAttributes"/> as <see cref="Asb.ServiceBusMessage.ApplicationProperties"/>; defaults to <see langword="true"/>.</param>
    /// <returns>The <see cref="Asb.ServiceBusReceivedMessage"/>.</returns>
    /// <remarks>Uses the standard publishing conversion, including native payload and subject policies when requested by the internal envelope. This does not run publisher callbacks or destination-specific session mapping.</remarks>
    public static Asb.ServiceBusReceivedMessage ToServiceBusReceivedMessage(this CloudEvent cloudEvent, ContentMode contentMode = ContentMode.Structured, bool includeAttributes = true)
        => cloudEvent.ToServiceBusMessage(contentMode, includeAttributes).ToServiceBusReceivedMessage();

    /// <summary>
    /// Converts a <see cref="Asb.ServiceBusMessage"/> to a <see cref="Asb.ServiceBusReceivedMessage"/>.
    /// </summary>
    /// <param name="message">The <see cref="Asb.ServiceBusMessage"/>.</param>
    /// <returns>The <see cref="Asb.ServiceBusReceivedMessage"/>.</returns>
    public static Asb.ServiceBusReceivedMessage ToServiceBusReceivedMessage(this Asb.ServiceBusMessage message)
    {
        // Copy application properties
        var props = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in message.ApplicationProperties)
            props[kvp.Key] = kvp.Value;

        // Create a ServiceBusReceivedMessage using the ServiceBusModelFactory with the same properties as the original message.
        return Asb.ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: message.Body,
            messageId: message.MessageId,
            partitionKey: message.PartitionKey,
            sessionId: message.SessionId,
            replyToSessionId: message.ReplyToSessionId,
            timeToLive: message.TimeToLive,
            correlationId: message.CorrelationId,
            subject: message.Subject,
            to: message.To,
            contentType: message.ContentType,
            replyTo: message.ReplyTo,
            scheduledEnqueueTime: message.ScheduledEnqueueTime,
            properties: props,
            deliveryCount: 1,
            sequenceNumber: DateTimeOffset.UtcNow.Ticks,
            enqueuedTime: DateTimeOffset.UtcNow
        );
    }

    #region Aspire

    /// <summary>
    /// Resets the Azure Service Bus queues and topics/subscriptions to an initial state by deleting and recreating them.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication"/>.</param>
    /// <param name="aspireResourceName">The name of the Aspire resource to retrieve the connection string for.</param>
    /// <param name="queues">The queues to reset.</param>
    /// <param name="topicsAndSubscriptions">The topics and their subscriptions to reset.</param>
    /// <param name="adminPort">The port to use for the administration client.</param>
    public static async Task ResetAzureServiceBusAsync(this DistributedApplication app, string aspireResourceName, CreateQueueOptions[]? queues = null, (CreateTopicOptions Topic, CreateSubscriptionOptions[] Subscriptions)[]? topicsAndSubscriptions = null, int adminPort = 5300)
    {
        var cs = (await AspireTesterBase.GetConnectionStringAsync(app.ThrowIfNull(), aspireResourceName.ThrowIfNullOrEmpty()).ConfigureAwait(false)) ?? throw new InvalidOperationException($"The '{aspireResourceName}' connection string not found.");
        await ResetAzureServiceBusAsync(CreateAzureServiceBusAdminConnectionString(cs, adminPort), queues, topicsAndSubscriptions).ConfigureAwait(false);
    }

    #endregion

    #region Admin

    /// <summary>
    /// Creates an Azure Service Bus connection string for the administration client by replacing the port in the Endpoint with the specified <paramref name="adminPort"/>.
    /// </summary>
    /// <param name="connectionString">The original Azure Service Bus connection string.</param>
    /// <param name="adminPort">The port to use for the administration client.</param>
    /// <returns>The modified Azure Service Bus connection string for the administration client.</returns>
    public static string CreateAzureServiceBusAdminConnectionString(string connectionString, int adminPort = 5300)
    {
        var parts = connectionString.ThrowIfNullOrEmpty().Split(';', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            var eq = parts[i].IndexOf('=');
            if (eq < 0 || !parts[i][..eq].Equals("Endpoint", StringComparison.OrdinalIgnoreCase))
                continue;
            var uri = new Uri(parts[i][(eq + 1)..], UriKind.Absolute);
            parts[i] = $"Endpoint={new UriBuilder(uri) { Port = adminPort }.Uri}";
        }

        return string.Join(";", parts) + ";";
    }

    /// <summary>
    /// Resets the Azure Service Bus queues and topics/subscriptions to an initial state by deleting and recreating them.
    /// </summary>
    /// <param name="adminConnectionString">The Azure Service Bus administration connection string.</param>
    /// <param name="queues">The queues to reset.</param>
    /// <param name="topicsAndSubscriptions">The topics and their subscriptions to reset.</param>
    public static Task ResetAzureServiceBusAsync(string adminConnectionString, CreateQueueOptions[]? queues = null, (CreateTopicOptions Topic, CreateSubscriptionOptions[] Subscriptions)[]? topicsAndSubscriptions = null)
        => ResetAzureServiceBusAsync(adminConnectionString, queues, topicsAndSubscriptions, CancellationToken.None);

    /// <summary>
    /// Resets local Azure Service Bus emulator entities after validating the namespace-level loopback connection and waiting for management readiness.
    /// </summary>
    /// <param name="connectionString">The Azure Service Bus emulator connection string.</param>
    /// <param name="queues">The queues to reset.</param>
    /// <param name="topicsAndSubscriptions">The topics and their subscriptions to reset.</param>
    /// <param name="adminPort">The emulator management port.</param>
    /// <param name="readinessTimeout">The maximum time to wait for emulator management readiness; defaults to one minute.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>This is destructive: the specified entities are deleted and recreated, discarding their messages. The caller must ensure the emulator is isolated and no listeners, producers, consumers or competing setup runs are active. This helper does not provide an NUnit/environment-variable opt-in; callers must gate manual setup before calling it.</remarks>
    public static async Task ResetLocalAzureServiceBusAsync(string connectionString, CreateQueueOptions[]? queues = null,
        (CreateTopicOptions Topic, CreateSubscriptionOptions[] Subscriptions)[]? topicsAndSubscriptions = null, int adminPort = 5300, TimeSpan? readinessTimeout = null, CancellationToken cancellationToken = default)
    {
        var endpoint = ValidateLocalAzureServiceBusConnectionString(connectionString);
        if (adminPort is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(adminPort), "The management port must be between 1 and 65535.");

        var timeout = readinessTimeout ?? TimeSpan.FromMinutes(1);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(readinessTimeout), "The readiness timeout must be greater than zero.");

        var health = new UriBuilder(endpoint) { Scheme = Uri.UriSchemeHttp, Port = adminPort, Path = "/health", Query = "", Fragment = "" }.Uri;
        await WaitForLocalAzureServiceBusEmulatorAsync(health, timeout, cancellationToken).ConfigureAwait(false);

        var adminConnectionString = CreateAzureServiceBusAdminConnectionString(connectionString, adminPort);
        await ResetAzureServiceBusAsync(adminConnectionString, queues, topicsAndSubscriptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resets local Azure Service Bus emulator entities using the named connection string from configuration.
    /// </summary>
    /// <param name="configuration">The configuration containing the connection string.</param>
    /// <param name="queues">The queues to reset.</param>
    /// <param name="topicsAndSubscriptions">The topics and their subscriptions to reset.</param>
    /// <param name="connectionName">The connection-string name; defaults to <c>"ServiceBus"</c>.</param>
    /// <param name="adminPort">The emulator management port.</param>
    /// <param name="readinessTimeout">The maximum time to wait for emulator management readiness; defaults to one minute.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A task that represents the asynchronous reset operation.</returns>
    /// <remarks>This operation is destructive. The string overload documents its safety requirements.</remarks>
    public static Task ResetLocalAzureServiceBusAsync(IConfiguration configuration, CreateQueueOptions[]? queues = null,
        (CreateTopicOptions Topic, CreateSubscriptionOptions[] Subscriptions)[]? topicsAndSubscriptions = null, string connectionName = "ServiceBus", int adminPort = 5300, TimeSpan? readinessTimeout = null, CancellationToken cancellationToken = default)
    {
        var name = connectionName.ThrowIfNullOrEmpty();
        var connectionString = configuration.ThrowIfNull().GetConnectionString(name) ?? throw new InvalidOperationException($"The '{name}' Azure Service Bus connection string was not found in configuration.");
        return ResetLocalAzureServiceBusAsync(connectionString, queues, topicsAndSubscriptions, adminPort, readinessTimeout, cancellationToken);
    }

    /// <summary>
    /// Validates a local Azure Service Bus emulator connection string and returns its namespace endpoint.
    /// </summary>
    private static Uri ValidateLocalAzureServiceBusConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("A local Azure Service Bus emulator connection string is required.");

        var options = new DbConnectionStringBuilder();
        try
        {
            options.ConnectionString = connectionString;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            throw new InvalidOperationException("The Azure Service Bus connection string is malformed.");
        }

        string? GetValue(string key) => options.TryGetValue(key, out var value) ? value?.ToString() : null;

        if (!Uri.TryCreate(GetValue("Endpoint"), UriKind.Absolute, out var endpoint)
            || !string.Equals(endpoint.Scheme, "sb", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(endpoint.UserInfo)
            || endpoint.AbsolutePath != "/"
            || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment))
            throw new InvalidOperationException("The Azure Service Bus connection must use a namespace-level sb:// endpoint without user info, path, query or fragment.");

        if (!bool.TryParse(GetValue("UseDevelopmentEmulator"), out var useDevelopmentEmulator) || !useDevelopmentEmulator)
            throw new InvalidOperationException("Local Service Bus setup requires UseDevelopmentEmulator=true.");

        var isLoopback = string.Equals(endpoint.DnsSafeHost, "localhost", StringComparison.OrdinalIgnoreCase) || IPAddress.TryParse(endpoint.DnsSafeHost, out var address) && IPAddress.IsLoopback(address);
        if (!isLoopback)
            throw new InvalidOperationException("Local Service Bus setup requires a localhost or loopback IP endpoint.");

        if (!string.IsNullOrEmpty(GetValue("EntityPath"))
            || string.IsNullOrWhiteSpace(GetValue("SharedAccessKeyName"))
            || string.IsNullOrWhiteSpace(GetValue("SharedAccessKey")))
            throw new InvalidOperationException("Local Service Bus setup requires a namespace-level shared access key connection without EntityPath.");

        return endpoint;
    }

    /// <summary>
    /// Waits until the local Azure Service Bus emulator reports healthy.
    /// </summary>
    private static async Task WaitForLocalAzureServiceBusEmulatorAsync(Uri health, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        using var handler = new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false };
        using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        Exception? lastFailure = null;

        try
        {
            while (true)
            {
                try
                {
                    using var response = await client.GetAsync(health, timeoutSource.Token).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                        return;

                    lastFailure = new HttpRequestException($"Service Bus emulator management health returned HTTP {(int)response.StatusCode}.");
                }
                catch (HttpRequestException ex)
                {
                    lastFailure = ex;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), timeoutSource.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
        {
            throw new TimeoutException($"Service Bus emulator management did not become ready at '{health}' within {timeout}. Start the emulator and check its logs.", lastFailure ?? ex);
        }
    }

    /// <summary>
    /// Resets the Azure Service Bus queues and topics/subscriptions to an initial state by deleting and recreating them.
    /// </summary>
    /// <param name="adminConnectionString">The Azure Service Bus administration connection string.</param>
    /// <param name="queues">The queues to reset.</param>
    /// <param name="topicsAndSubscriptions">The topics and their subscriptions to reset.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A task that represents the asynchronous reset operation.</returns>
    public static async Task ResetAzureServiceBusAsync(string adminConnectionString, CreateQueueOptions[]? queues, (CreateTopicOptions Topic, CreateSubscriptionOptions[] Subscriptions)[]? topicsAndSubscriptions, CancellationToken cancellationToken)
    {
        var admin = new ServiceBusAdministrationClient(adminConnectionString);

        // Recreate each queue fresh - deleting first (if present) so the outcome is identical regardless of prior state.
        foreach (var queue in queues ?? [])
        {
            if (await admin.QueueExistsAsync(queue.Name, cancellationToken).ConfigureAwait(false))
                await admin.DeleteQueueAsync(queue.Name, cancellationToken).ConfigureAwait(false);

            await admin.CreateQueueAsync(queue, cancellationToken).ConfigureAwait(false);
        }

        // Recreate each topic (and its subscriptions) fresh - deleting the topic first (if present), which also removes any stale subscriptions.
        foreach (var (topic, subscriptions) in topicsAndSubscriptions ?? [])
        {
            if (await admin.TopicExistsAsync(topic.Name, cancellationToken).ConfigureAwait(false))
                await admin.DeleteTopicAsync(topic.Name, cancellationToken).ConfigureAwait(false);

            await admin.CreateTopicAsync(topic, cancellationToken).ConfigureAwait(false);

            foreach (var subscription in subscriptions)
            {
                if (subscription.TopicName != topic.Name)
                    throw new InvalidOperationException($"The subscription '{subscription.SubscriptionName}' TopicName '{subscription.TopicName}' does not match the corresponding topic '{topic.Name}'.");

                await admin.CreateSubscriptionAsync(subscription, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    #endregion
}
