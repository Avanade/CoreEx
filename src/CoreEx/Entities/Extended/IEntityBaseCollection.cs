// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Represents an <see cref="EntityBase"/> collection and therefore certain capabilities can be assumed.
    /// </summary>
    public interface IEntityBaseCollection : ICloneable, ICleanUp, IInitial { }
}