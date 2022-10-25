// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;

namespace CoreEx.RefData.Models
{
    /// <summary>
    /// Represents a <see cref="Name">named</see> <see cref="IReferenceDataCollection"/>.
    /// </summary>
    public class ReferenceDataMultiItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataMultiItem"/> class for a <paramref name="name"/> and it corresponding <paramref name="items"/>.
        /// </summary>
        /// <param name="name">The <see cref="Name"/>.</param>
        /// <param name="items">The <see cref="IReferenceData"/> items.</param>
        public ReferenceDataMultiItem(string name, IEnumerable<ReferenceDataBase> items)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Items = items ?? throw new ArgumentNullException(nameof(items));
        }

        /// <summary>
        /// Gets or sets the reference data name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the reference data collection.
        /// </summary>
        public IEnumerable<ReferenceDataBase> Items { get; set; }
    }
}