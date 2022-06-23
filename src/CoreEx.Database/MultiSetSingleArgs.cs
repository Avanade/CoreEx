// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Database
{
    /// <summary>
    /// Provides the base <b>Database</b> multi-set arguments when expecting a single item/record only.
    /// </summary>
    public abstract class MultiSetSingleArgs : IMultiSetArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiSetSingleArgs"/> class.
        /// </summary>
        /// <param name="isMandatory">Indicates whether the value is mandatory; defaults to <c>true</c>.</param>
        /// <param name="stopOnNull">Indicates whether to stop further query result set processing where the current set has resulted in a null (i.e. no records).</param>
        protected MultiSetSingleArgs(bool isMandatory = true, bool stopOnNull = false)
        {
            IsMandatory = isMandatory;
            StopOnNull = stopOnNull;
        }

        /// <summary>
        /// Indicates whether the value is mandatory; i.e. a corresponding record must be read.
        /// </summary>
        public bool IsMandatory { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of rows allowed.
        /// </summary>
        public int MinRows => IsMandatory ? 1 : 0;

        /// <summary>
        /// Gets or sets the maximum number of rows allowed.
        /// </summary>
        public int? MaxRows => 1;

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