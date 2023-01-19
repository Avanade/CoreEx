// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Database.Extended
{
    /// <summary>
    /// Provides the extended <see cref="IDatabase"/> arguments.
    /// </summary>
    public struct DatabaseArgs
    {
        private readonly IDatabaseMapper? _mapper = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseArgs"/> struct.
        /// </summary>
        public DatabaseArgs() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseArgs"/> struct.
        /// </summary>
        /// <param name="template">The template <see cref="DatabaseArgs"/> to copy from.</param>
        /// <param name="mapper">The <see cref="IDatabaseMapper"/>.</param>
        public DatabaseArgs(DatabaseArgs template, IDatabaseMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Refresh = template.Refresh;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseArgs"/> struct.
        /// </summary>
        /// <param name="mapper">The <see cref="IDatabaseMapper"/>.</param>
        public DatabaseArgs(IDatabaseMapper mapper) => _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        /// <summary>
        /// Gets the <see cref="IDatabaseMapper"/>.
        /// </summary>
        public IDatabaseMapper Mapper => _mapper ?? throw new InvalidOperationException("Mapper must have been specified for it to be referenced.");

        /// <summary>
        /// Indicates whether the <see cref="Mapper"/> has been specified.
        /// </summary>
        public bool HasMapper => _mapper != null;

        /// <summary>
        /// Indicates whether the data should be refreshed (reselected where applicable) after a <b>save</b> operation (defaults to <c>true</c>).
        /// </summary>
        public bool Refresh { get; set; } = true;
    }
}