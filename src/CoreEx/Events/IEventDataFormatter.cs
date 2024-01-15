// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Events
{
    /// <summary>
    /// Enables additional formatting of an <see cref="EventData"/> by the <see cref="EventDataFormatter"/>.
    /// </summary>
    /// <remarks>Invoked by the <see cref="EventDataFormatter.Format(EventData)"/> where formatting an <see cref="EventData.Value"/> and the corresponding value implements.</remarks>
    public interface IEventDataFormatter
    {
        /// <summary>
        /// Format the <see cref="EventData"/>.
        /// </summary>
        /// <param name="eventData">The <see cref="EventData"/> being formatted.</param>
        void Format(EventData eventData);
    }
}