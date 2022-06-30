// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Database
{
    /// <summary>
    /// Provides the <b>Database</b> multi-set arguments when expecting a single item/record only.
    /// </summary>
    /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
    public class MultiSetSingleArgs<T> : MultiSetSingleArgs, IMultiSetArgs<T>
        where T : class, new()
    {
        private T? _value;
        private readonly Action<T> _result;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiSetSingleArgs{TItem}"/> class.
        /// </summary>
        /// <param name="mapper">The <see cref="IDatabaseMapper{TItem}"/> for the <see cref="DatabaseRecord"/>.</param>
        /// <param name="result">The action that will be invoked with the result of the set.</param>
        /// <param name="isMandatory">Indicates whether the value is mandatory; defaults to <c>true</c>.</param>
        /// <param name="stopOnNull">Indicates whether to stop further query result set processing where the current set has resulted in a null (i.e. no records).</param>
        public MultiSetSingleArgs(IDatabaseMapper<T> mapper, Action<T> result, bool isMandatory = true, bool stopOnNull = false) : base(isMandatory, stopOnNull)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _result = result ?? throw new ArgumentNullException(nameof(result));
        }

        /// <summary>
        /// Gets the <see cref="IDatabaseMapper{T}"/> for the <see cref="DatabaseRecord"/>.
        /// </summary>
        public IDatabaseMapper<T> Mapper { get; private set; }

        /// <summary>
        /// The <see cref="DatabaseRecord"/> method invoked for each record for its respective dataset.
        /// </summary>
        /// <param name="dr">The <see cref="DatabaseRecord"/>.</param>
        public override void DatasetRecord(DatabaseRecord dr) => _value = Mapper.MapFromDb(dr);

        /// <summary>
        /// Invokes the corresponding result function.
        /// </summary>
        public override void InvokeResult()
        {
            if (_value != null)
                _result(_value);
        }
    }
}