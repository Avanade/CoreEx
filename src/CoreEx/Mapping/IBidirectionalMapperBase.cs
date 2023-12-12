// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Mapping
{
    /// <summary>
    /// Provides a bidirectional mapper that encapsulates two <see cref="IMapperBase"/>'s; one for each direction.
    /// </summary>
    public interface IBidirectionalMapperBase
    {
        /// <summary>
        /// Gets the <i>from</i> to <i>to</i> <see cref="IMapperBase"/>.
        /// </summary>
        IMapperBase MapperFromTo { get; }

        /// <summary>
        /// Gets the <i>to</i> to <i>from</i> <see cref="IMapperBase"/>.
        /// </summary>
        IMapperBase MapperToFrom { get; }
    }
}