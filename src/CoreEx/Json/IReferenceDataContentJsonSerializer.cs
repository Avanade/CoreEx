// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;

namespace CoreEx.Json
{
    /// <summary>
    /// Provides the JSON Serialize and Deserialize capabilities to allow <see cref="IReferenceData"/> types to serialize contents.
    /// </summary>
    /// <remarks>Generally, <see cref="IReferenceData"/> types will serialize the <see cref="IReferenceData.Code"/> as the value; this allows for full <see cref="IReferenceData"/> contents to be serialized.</remarks>
    public interface IReferenceDataContentJsonSerializer : IJsonSerializer { }
}