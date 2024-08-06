// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Database
{
    /// <summary>
    /// Provides the base <b>Database</b> multi-set arguments when expecting a collection of items/records.
    /// </summary>
    public abstract class MultiSetCollArgs : IMultiSetArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiSetCollArgs"/> class.
        /// </summary>
        /// <param name="minRows">The minimum number of rows allowed.</param>
        /// <param name="maxRows">The maximum number of rows allowed.</param>
        /// <param name="stopOnNull">Indicates whether to stop further query result set processing where the current set has resulted in a null (i.e. no records).</param>
        public MultiSetCollArgs(int minRows = 0, int? maxRows = null, bool stopOnNull = false)
        {
            if (maxRows.HasValue && minRows <= maxRows.Value)
                throw new ArgumentException("Max Rows is less than Min Rows.", nameof(maxRows));

            MinRows = minRows;
            MaxRows = maxRows;
            StopOnNull = stopOnNull;
        }

        /// <summary>
        /// Gets the minimum number of rows allowed.
        /// </summary>
        public int MinRows { get; }

        /// <summary>
        /// Gets the maximum number of rows allowed.
        /// </summary>
        public int? MaxRows { get; }

        /// <summary>
        /// Indicates whether to stop further query result set processing where the current set has resulted in a null (i.e. no records).
        /// </summary>
        public bool StopOnNull { get; set; }

        /// <summary>
        /// The <see cref="DatabaseRecord"/> method invoked for each record for its respective dataset.
        /// </summary>
        /// <param name="dr">The <see cref="DatabaseRecord"/>.</param>
        public abstract void DatasetRecord(DatabaseRecord dr);

        /// <summary>
        /// Invokes the corresponding result function.
        /// </summary>
        public virtual void InvokeResult() { }
    }
}