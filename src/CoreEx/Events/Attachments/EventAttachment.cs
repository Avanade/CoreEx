// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Events.Attachments
{
    /// <summary>
    /// Represents the attachment reference metadata.
    /// </summary>
    public class EventAttachment
    {
        /// <summary>
        /// Gets or sets the optional attachment content type.
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EventData.Value"/> attachment reference (i.e. file location).
        /// </summary>
        public string? Attachment { get; set; }

        /// <summary>
        /// Indicates whether the <see cref="Attachment"/> is considered empty (not specified).
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(Attachment) && string.IsNullOrEmpty(ContentType);
    }
}