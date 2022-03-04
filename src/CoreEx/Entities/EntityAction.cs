// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the action to perform on an <see cref="EntityCore"/> (and <see cref="EntityBase{TSelf}"/>) via the <see cref="EntityCore.ApplyAction"/>
    /// </summary>
    public enum EntityAction
    {
        /// <summary>
        /// Perform a <see cref="EntityCore.AcceptChanges"/>.
        /// </summary>
        AcceptChanges,

        /// <summary>
        /// Perform a <see cref="EntityBase{TSelf}.CleanUp"/>.
        /// </summary>
        CleanUp,

        /// <summary>
        /// Perform a <see cref="EntityCore.MakeReadOnly"/>.
        /// </summary>
        MakeReadOnly
    }
}