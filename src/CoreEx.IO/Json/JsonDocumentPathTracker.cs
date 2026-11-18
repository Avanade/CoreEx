namespace CoreEx.IO.Json;

/// <summary>Tracks decoded object-member paths and checks duplicate names at every nesting level.</summary>
internal sealed class JsonDocumentPathTracker
{
    private readonly List<Frame> _frames = [];

    /// <summary>Advances the path state for one token.</summary>
    /// <param name="token">The parsed token.</param>
    /// <returns>The logical position and duplicate-member result.</returns>
    public JsonDocumentPosition Advance(JsonDocumentToken token)
    {
        if (token.Type == JsonTokenType.PropertyName)
        {
            if (_frames.Count == 0 || _frames[^1].Type != JsonTokenType.StartObject)
                throw new InvalidDataException("The JSON reader emitted a property outside an object.");

            var frame = _frames[^1];
            var name = token.Name.ThrowIfNull();
            var duplicate = !frame.Names!.Add(name);
            frame.PendingName = token.Name;
            return new(duplicate ? JsonPointer.Append(frame.Path, name) : frame.Path, frame.ObjectPath, frame.Type, frame.Path, _frames.Count, duplicate, false);
        }

        if (token.Type is JsonTokenType.EndObject or JsonTokenType.EndArray)
        {
            if (_frames.Count == 0)
                throw new InvalidDataException("The JSON reader emitted an unmatched closing token.");

            var frame = _frames[^1];
            _frames.RemoveAt(_frames.Count - 1);
            return new(frame.Path, frame.ObjectPath, _frames.Count == 0 ? null : _frames[^1].Type,
                _frames.Count == 0 ? null : _frames[^1].Path, _frames.Count, false, false);
        }

        var parent = _frames.Count == 0 ? null : _frames[^1];
        string path;
        bool objectPath;
        if (parent is null)
        {
            path = string.Empty;
            objectPath = true;
        }
        else if (parent.Type == JsonTokenType.StartObject)
        {
            path = JsonPointer.Append(parent.Path, parent.PendingName.ThrowIfNull());
            parent.PendingName = null;
            objectPath = parent.ObjectPath;
        }
        else
        {
            path = JsonPointer.Append(parent.Path, parent.NextIndex.ToString(CultureInfo.InvariantCulture));
            parent.NextIndex = checked(parent.NextIndex + 1);
            objectPath = false;
        }

        var position = new JsonDocumentPosition(path, objectPath, parent?.Type, parent?.Path, _frames.Count, false, true);
        if (token.Type is JsonTokenType.StartObject or JsonTokenType.StartArray)
            _frames.Add(new(token.Type, path, objectPath));

        return position;
    }

    /// <summary>Retains the nesting and member state for one JSON object or array.</summary>
    private sealed class Frame(JsonTokenType type, string path, bool objectPath)
    {
        /// <summary>Gets the container kind.</summary>
        public JsonTokenType Type { get; } = type;

        /// <summary>Gets the source pointer of the container.</summary>
        public string Path { get; } = path;

        /// <summary>Gets whether every selector segment to this value traversed an object member.</summary>
        public bool ObjectPath { get; } = objectPath;

        /// <summary>Gets the decoded names already encountered in this object.</summary>
        public HashSet<string>? Names { get; } = type == JsonTokenType.StartObject ? new(StringComparer.Ordinal) : null;

        /// <summary>Gets or sets the next object member name.</summary>
        public string? PendingName { get; set; }

        /// <summary>Gets or sets the next array index.</summary>
        public long NextIndex { get; set; }
    }
}
