namespace CoreEx.Events;

/// <summary>
/// Provides the formatting (<see cref="Format"/>) and parsing (<see cref="Parse"/>) of an <see cref="EventData"/>, and its conversion to (<see cref="ConvertToCloudEvent"/>) and from (<see cref="ConvertFromCloudEvent"/>) a <see cref="CloudEvent"/>.
/// </summary>
/// <remarks>The <see cref="IEventFormatter"/> methods are <i>virtual</i> to allow this class to be easily extended; this is the <i>intended</i> behavior.</remarks>
public class EventFormatter : IEventFormatter
{
    private const string _defaultSegment = "?";
    private bool _initializeNameArray = true;
    private string[] _nameArray = [];
    private readonly BaggagePropagator _propagator = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventFormatter"/> class.
    /// </summary>
    /// <param name="hostSettings">The optional <see cref="Hosting.IHostSettings"/> used to default the <see cref="TitlePrefix"/> (<see cref="IHostSettings.SolutionName"/>), <see cref="DomainName"/> and <see cref="SourceBaseUri"/>.</param>
    public EventFormatter(IHostSettings? hostSettings = null)
    {
        HostSettings = hostSettings ?? ExecutionContext.GetService<IHostSettings>();
        TitlePrefix = HostSettings?.SolutionName;
        SourceBaseUri = HostSettings?.Source;
        DomainName = HostSettings?.DomainName;
    }

    /// <summary>
    /// Gets the <see cref="HostSettings"/>.
    /// </summary>
    /// <remarks>Enables the <see cref="TitlePrefix"/> (<see cref="IHostSettings.SolutionName"/>) and <see cref="DomainName"/> (<see cref="IHostSettings.DomainName"/>).</remarks>
    public IHostSettings? HostSettings { get; }

    /// <summary>
    /// Gets or sets the <see cref="CloudEvent"/> extension attribute name for the <see cref="EventData.TraceParent"/>.
    /// </summary>
    public string TraceParentAttributeName { get; set => field = SetAndInitializeNameArray(value); } = "traceparent";

    /// <summary>
    /// Gets or sets the <see cref="CloudEvent"/> extension attribute name for the <see cref="EventData.TraceState"/>.
    /// </summary>
    public string TraceStateAttributeName { get; set => field = SetAndInitializeNameArray(value); } = "tracestate";

    /// <summary>
    /// Gets or sets the <see cref="CloudEvent"/> extension attribute name for the <see cref="EventData.TraceBaggage"/>.
    /// </summary>
    public string TraceBaggageAttributeName { get; set => field = SetAndInitializeNameArray(value); } = "baggage";

    /// <summary>
    /// Gets or sets the <see cref="CloudEvent"/> extension attribute name for the <see cref="EventData.TenantId"/>.
    /// </summary>
    public string TenantIdAttributeName { get; set => field = SetAndInitializeNameArray(value); } = "tenantid";

    /// <summary>
    /// Gets or sets the <see cref="CloudEvent"/> extension attribute name for the <see cref="EventData.DataSchemaVersion"/>.
    /// </summary>
    public string DataSchemaVersionAttributeName { get; set => field = SetAndInitializeNameArray(value); } = "dataschemaversion";

    /// <summary>
    /// Gets or sets the <see cref="CloudEvent"/> extension attribute name for the <see cref="EventData.UserType"/>.
    /// </summary>
    public string AuthTypeAttributeName { get; set => field = SetAndInitializeNameArray(value); } = "authtype";

    /// <summary>
    /// Gets or sets the <see cref="CloudEvent"/> extension attribute name for the <see cref="EventData.UserId"/>.
    /// </summary>
    public string AuthIdAttributeName { get; set => field = SetAndInitializeNameArray(value); } = "authid";

    /// <summary>
    /// Gets or sets the <see cref="CloudEvent"/> extension attribute name for the <see cref="EventData.ReplyTo"/>.
    /// </summary>
    public string ReplyToAttributeName { get; set => field = SetAndInitializeNameArray(value); } = "replyto";

    /// <summary>
    /// Gets or sets the casing to apply to the <see cref="EventData.Title"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="StringCase.Lower"/>.</remarks>
    public StringCase TitleCase { get; set; } = StringCase.Lower;

    /// <summary>
    /// Gets or sets the casing to apply to the <see cref="EventData.Source"/>.
    /// </summary>
    public StringCase SourceCase { get; set; } = StringCase.Lower;

    /// <summary>
    /// Indicates whether to set the <see cref="EventData.PartitionKey"/> where <see langword="null"/> to the <see cref="EventData.Key"/>.
    /// </summary>
    /// <remarks>Defaults to <see langword="true"/>.</remarks>
    public bool SetPartitionKeyToKey { get; set; } = true;

    /// <summary>
    /// Indicates whether to throw an <see cref="InvalidOperationException"/> during <see cref="Format(EventData)"/> when the <see cref="EventData.PartitionKey"/> is <see langword="null"/>.
    /// </summary>
    public bool PartitionKeyIsRequired { get; set; } = true;

    /// <summary>
    /// Gets or sets the <see cref="EventData.Title"/> prefix used by the default formatting.
    /// </summary>
    /// <remarks>This should use the '<c>.</c>' to separate segments.
    /// <para>This defaults to the <see cref="IHostSettings.SolutionName"/>.</para></remarks>
    public string? TitlePrefix { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="EventData.Source"/> base <see cref="Uri"/> used by the default formatting.
    /// </summary>
    public Uri? SourceBaseUri { get; set; }

    /// <summary>
    /// Gets or sets the default Domain (DDD) name to be used where not specified on the <see cref="EventData"/> (see <see cref="EventData.DomainName"/>).
    /// </summary>
    /// <remarks>This defaults to the <see cref="IHostSettings.DomainName"/>.</remarks>
    public string? DomainName { get; set; }

    /// <inheritdoc/>
    /// <remarks>The <see cref="EventData.Title"/> is formatted by default using the following convention:
    /// <code>[{EventFormatter.TitlePrefix}.]{EventData.DomainName}.{EventData.Entity}.{EventData.Action}[.v{EventData.DataSchemaVersion.Major}]</code></remarks>
    public virtual EventData Format(EventData @event)
    {
        string[] tpa = TitlePrefix is null ? [] : [TitlePrefix];

        if (@event.Title is null)
        {
            @event.Title = Cleaner.Clean(string.Join('.', [.. tpa, @event.DomainName ?? DomainName ?? _defaultSegment, @event.Entity ?? _defaultSegment, @event.Action ?? _defaultSegment]), casing: TitleCase);
            if (@event.DataSchemaVersion is not null)
                @event.Title += $".v{@event.DataSchemaVersion.Major}";
        }

        if (@event.Source is null)
        {
            if (@event.TenantId is null)
            {
                if (SourceBaseUri is not null)
                    @event.Source = new Uri(SourceBaseUri.OriginalString);
            }
            else if (Uri.TryCreate(SourceBaseUri, Cleaner.Clean(@event.TenantId, casing: SourceCase), out var uri))
                @event.Source = uri;

            if (@event.Source is null)
            {
                if (SourceBaseUri is not null)
                    throw new InvalidOperationException($"The {nameof(EventData)}.{nameof(EventData.Source)} URI could not be successfully created using the {nameof(SourceBaseUri)} and {nameof(EventData)}.{nameof(EventData.TenantId)}");
                else
                    @event.Source = SourceBaseUri;
            }
        }

        if (SetPartitionKeyToKey)
            @event.PartitionKey ??= @event.Key;

        if (@event.PartitionKey is null && PartitionKeyIsRequired)
            throw new InvalidOperationException($"A {nameof(EventData)}.{nameof(EventData.PartitionKey)} PartitionKey is required.");

        return @event;
    }

    /// <inheritdoc/>
    /// <remarks>Parses with the following convention:
    /// <code>[{EventFormatter.TitlePrefix}.]{EventData.DomainName}.{EventData.Entity}.{EventData.Action}[.v{EventData.DataSchemaVersion.Major}]</code>
    /// <para>Updates the corresponding <see cref="EventData"/> properties where not currently set; however, where there is a mismatch with the convention during parsing no update will occur.</para></remarks>
    public virtual EventData Parse(EventData @event)
    {
        var title = @event.Title?.Trim();
        if (string.IsNullOrEmpty(title))
            return @event;

        if (TitlePrefix is not null)
        {
            if (title.Length <= TitlePrefix.Length + 1)
                return @event;

            if (title.StartsWith(TitlePrefix, StringComparison.OrdinalIgnoreCase))
                title = title[TitlePrefix.Length..].TrimStart('.');
            else
                return @event;
        }

        var segments = title.Split('.');
        if (segments.Length < 3)
            return @event;

        // At this point we believe we have at least 3 segments: DomainName, Entity, Action - which is enough to carry on.
        @event.DomainName = segments[0];
        @event.Entity = segments[1];
        @event.Action = segments[2];

        // The fourth segment is optional and represents the DataSchemaVersion.
        if (segments.Length >= 4)
        {
            if (segments[3].Length > 1 && segments[3].StartsWith("v", StringComparison.OrdinalIgnoreCase) && int.TryParse(segments[3][1..], out var major))
                @event.DataSchemaVersion ??= new Version(major, 0);
        }

        return @event;
    }

    /// <inheritdoc/>
    public virtual CloudEvent ConvertToCloudEvent(EventData @event)
    {
        var ce = new CloudEvent
        {
            Id = @event.ThrowIfNull().Id,
            Type = @event.Title,
            Source = @event.Source,
            Subject = @event.Key,
            Time = @event.Timestamp
        };

        ce.SetExtensionAttribute(TenantIdAttributeName, @event.TenantId);

        var pk = @event.PartitionKey ?? @event.Key;
        if (pk is not null)
            ce.SetPartitionKey(pk);

        ce.SetExtensionAttribute(ReplyToAttributeName, @event.ReplyTo);
        ce.SetExtensionAttribute(AuthTypeAttributeName, ConvertFromAuthenticationType(@event.UserType));
        ce.SetExtensionAttribute(AuthIdAttributeName, @event.UserId);

        AddTracing(ce, @event.TraceParent, @event.TraceState, @event.TraceBaggage);

        foreach (var kvp in @event.Attributes.Where(x => !x.Key.StartsWith('_')).OrderBy(x => x.Key))
        {
            ce.SetExtensionAttribute(kvp.Key, kvp.Value);
        }

        if (@event.Data is not null)
        {
            ce.SetExtensionAttribute(DataSchemaVersionAttributeName, @event.DataSchemaVersion?.ToString());
            ce.DataSchema = @event.DataSchema;
            ce.DataContentType = @event.Data?.MediaType;
            ce.Data = @event.Data;
        }

        return ce;
    }

    /// <inheritdoc/>
    /// <remarks>The <see cref="CloudEvent.Data"/> must be of type <see cref="BinaryData"/> otherwise a <see cref="NotSupportedException"/> will be thrown.</remarks>
    public virtual EventData ConvertFromCloudEvent(CloudEvent cloudEvent)
    {
        var @event = new EventData
        {
            Id = cloudEvent.ThrowIfNull().Id ?? string.Empty,
            Timestamp = cloudEvent.Time ?? DateTimeOffset.MinValue,
            DataSchema = cloudEvent.DataSchema,
            Key = cloudEvent.Subject,
            PartitionKey = cloudEvent.GetPartitionKey()
        };

        if (cloudEvent.Data is not null)
            @event.Data = cloudEvent.Data is BinaryData ed ? ed : throw new NotSupportedException($"The {nameof(CloudEvent)}.{nameof(CloudEvent.Data)} type must be {nameof(BinaryData)}; not '{cloudEvent.Data.GetType().FullName}'.");

        if (cloudEvent.TryGetExtensionAttribute(DataSchemaVersionAttributeName, out string? val))
            @event.DataSchemaVersion = Version.TryParse(val, out var ver) ? ver : null;

        if (cloudEvent.TryGetExtensionAttribute(TenantIdAttributeName, out val))
            @event.TenantId = val;

        if (cloudEvent.TryGetExtensionAttribute(ReplyToAttributeName, out val))
            @event.ReplyTo = val;

        if (cloudEvent.TryGetExtensionAttribute(AuthTypeAttributeName, out val))
            @event.UserType = ConvertToAuthenticationType(val);

        if (cloudEvent.TryGetExtensionAttribute(AuthIdAttributeName, out val))
            @event.UserId = val;

        if (cloudEvent.TryGetExtensionAttribute(TraceParentAttributeName, out val))
            @event.TraceParent = val;

        if (cloudEvent.TryGetExtensionAttribute(TraceStateAttributeName, out val))
            @event.TraceState = val;

        if (cloudEvent.TryGetExtensionAttribute(TraceBaggageAttributeName, out val) && !string.IsNullOrEmpty(val))
        {
            var carrier = new Dictionary<string, string?> { ["baggage"] = val };
            var propagationContext = _propagator.Extract(default, carrier, (msg, key) => msg.TryGetValue(key, out var value) ? [value!] : []);

            if (propagationContext.Baggage.Count > 0)
                @event.TraceBaggage = Baggage.GetBaggage(propagationContext.Baggage).Select(x => new KeyValuePair<string, string?>(x.Key, x.Value));
        }

        if (cloudEvent.Source is not null)
            @event.Source = cloudEvent.Source;

        if (!string.IsNullOrEmpty(cloudEvent.Type))
            @event.Title = cloudEvent.Type;

        InitializeNameArray();
        foreach (var kvp in cloudEvent.GetPopulatedAttributes().Where(x => !_nameArray.Contains(x.Key.Name, StringComparer.OrdinalIgnoreCase)))
        {
            @event.SetAttribute(kvp.Key.Name, kvp.Value);
        }

        return @event;
    }

    /// <summary>
    /// Flags that the initialization of the name array is required.
    /// </summary>
    private string SetAndInitializeNameArray(string value)
    {
        _initializeNameArray = true;
        return value.ThrowIfNullOrEmpty();
    }

    /// <summary>
    /// (Re)initializes the name array used for attribute name checking.
    /// </summary>
    private void InitializeNameArray()
    {
        if (!_initializeNameArray)
            return;

        _initializeNameArray = false;
        _nameArray = [.. CloudEventsSpecVersion.Default.AllAttributes.Select(x => x.Name), Partitioning.PartitionKeyAttribute.Name, TraceParentAttributeName, TraceStateAttributeName, TenantIdAttributeName, DataSchemaVersionAttributeName, AuthTypeAttributeName, AuthIdAttributeName, ReplyToAttributeName];
    }

    /// <inheritdoc/>
    public void AddTracing(CloudEvent @event, string? traceParent = null, string? traceState = null, IEnumerable<KeyValuePair<string, string?>>? traceBaggage = null)
    {
        @event.ThrowIfNull();
        if (@event.TryGetExtensionAttribute(TraceParentAttributeName, out string _))
            return;

        if (string.IsNullOrEmpty(traceParent) && Activity.Current is not null)
        {
            traceParent = Activity.Current.Id;
            traceState = Activity.Current.TraceStateString;
            traceBaggage ??= Activity.Current.Baggage;
        }

        if (string.IsNullOrEmpty(traceParent))  
            return;

        string? formattedBaggage = null;
        if (traceBaggage is not null)
        {
            var baggage = default(Baggage);
            foreach (var item in traceBaggage)
                baggage = baggage.SetBaggage(item.Key, item.Value);

            if (baggage.Count > 0)
            {
                var carrier = new Dictionary<string, string?>();
                var ac = Activity.Current?.Context ?? (ActivityContext.TryParse(traceParent, traceState, out var context) ? context : default);
                if (ac != default)
                {
                    _propagator.Inject(new PropagationContext(Activity.Current?.Context ?? ActivityContext.Parse(traceParent, traceState), baggage), carrier, (msg, key, value) => msg[key] = value);
                    carrier.TryGetValue("baggage", out formattedBaggage);
                }
            }
        }

        @event.SetExtensionAttribute(TraceParentAttributeName, traceParent);
        @event.SetExtensionAttribute(TraceStateAttributeName, traceState);
        @event.SetExtensionAttribute(TraceBaggageAttributeName, formattedBaggage);
    }

    /// <summary>
    /// Converts the <see cref="AuthenticationType"/> to a corresponding CloudEvent 'authtype' attribute value.
    /// </summary>
    /// <param name="type">The <see cref="AuthenticationType"/>.</param>
    /// <returns>The corresponding CloudEvent 'authtype' attribute value.</returns>
    protected string? ConvertFromAuthenticationType(AuthenticationType? type) => type switch
    {
        null => null,
        AuthenticationType.Unknown => "unknown",
        AuthenticationType.Unauthenticated => "unauthenticated",
        AuthenticationType.ApplicationUser => "app_user",
        AuthenticationType.AccountUser => "user",
        AuthenticationType.SystemUser => "system",
        _ => throw new InvalidOperationException($"{nameof(AuthenticationType)} of '{type}' is unable to be converted to a corresponding CloudEvent 'authtype' attribute.")
    };

    /// <summary>
    /// Converts the CloudEvent 'authtype' attribute value to a corresponding <see cref="AuthenticationType"/>.
    /// </summary>
    /// <param name="type">The CloudEvent 'authtype' attribute value.</param>
    /// <returns>The corresponding <see cref="AuthenticationType"/>.</returns>
    protected AuthenticationType? ConvertToAuthenticationType(string? type) => type switch
    {
        null => null,
        "unknown" => AuthenticationType.Unknown,
        "unauthenticated" => AuthenticationType.Unauthenticated,
        "app_user" => AuthenticationType.ApplicationUser,
        "user" => AuthenticationType.AccountUser,
        "system" => AuthenticationType.SystemUser,
        _ => throw new InvalidOperationException($"The CloudEvent 'authtype' attribute value of '{type}' is unable to be converted to a corresponding {nameof(AuthenticationType)}.")
    };
}