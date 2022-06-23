// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;

namespace CoreEx.Database
{
    /// <summary>
    /// Provides the <b>Database</b> multi-set arguments when expecting a collection of items/records.
    /// </summary>
    /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    public class MultiSetCollArgs<TColl, TItem> : MultiSetCollArgs, IMultiSetArgs<TItem>
        where TItem : class, new()
        where TColl : class, ICollection<TItem>, new()
    {
        private TColl? _coll;
        private readonly Action<TColl> _result;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiSetCollArgs{TColl, TItem}"/> class.
        /// </summary>
        /// <param name="mapper">The <see cref="IDatabaseMapper{TItem}"/> for the <see cref="DatabaseRecord"/>.</param>
        /// <param name="result">The action that will be invoked with the result of the set.</param>
        /// <param name="minRows">The minimum number of rows allowed.</param>
        /// <param name="maxRows">The maximum number of rows allowed.</param>
        /// <param name="stopOnNull">Indicates whether to stop further query result set processing where the current set has resulted in a null (i.e. no records).</param>
        public MultiSetCollArgs(IDatabaseMapper<TItem> mapper, Action<TColl> result, int minRows = 0, int? maxRows = null, bool stopOnNull = false) : base(minRows, maxRows, stopOnNull)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _result = result ?? throw new ArgumentNullException(nameof(result));
        }

        /// <summary>
        /// Gets the <see cref="IDatabaseMapper{TItem}"/> for the <see cref="DatabaseRecord"/>.
        /// </summary>
        public IDatabaseMapper<TItem> Mapper { get; private set; }

        /// <summary>
        /// The <see cref="DatabaseRecord"/> method invoked for each record for its respective dataset.
        /// </summary>
        /// <param name="dr">The <see cref="DatabaseRecord"/>.</param>
        public override void DatasetRecord(DatabaseRecord dr)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));

            if (_coll == null)
                _coll = new TColl();

            var item = Mapper.MapFromDb(dr);
            if (item != null)
                _coll.Add(item);
        }

        /// <summary>
        /// Invokes the corresponding result function.
        /// </summary>
        public override void InvokeResult()
        {
            if (_coll != null)
                _result(_coll);
        }
    }
}