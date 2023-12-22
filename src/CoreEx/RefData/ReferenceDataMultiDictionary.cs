// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;

namespace CoreEx.RefData
{
    /// <summary>
    /// Represents a dictionary where the key is the <see cref="IReferenceData"/> <see cref="Type"/> and the value is the corresponding <see cref="IReferenceData"/> items.
    /// </summary>
    /// <remarks>This is generally intended for the <see cref="ReferenceDataOrchestrator"/>.</remarks>
    public class ReferenceDataMultiDictionary : Dictionary<string, IEnumerable<IReferenceData>> 
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataMultiDictionary"/> class.
        /// </summary>
        public ReferenceDataMultiDictionary() : base(StringComparer.OrdinalIgnoreCase) { }
    }
}