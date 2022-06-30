// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping
{
    /// <summary>
    /// Represents the mapping <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> operation types (Create, Read, Update and Delete).
    /// </summary>
    [Flags]
    public enum OperationTypes
    {
        /// <summary>
        /// Unspecified operation.
        /// </summary>
        Unspecified = 1,

        /// <summary>
        /// A <b>Get</b> (Read) operation.
        /// </summary>
        Get = 2,

        /// <summary>
        /// A <b>Create</b> operation.
        /// </summary>
        Create = 4,

        /// <summary>
        /// An <b>update</b> operation.
        /// </summary>
        Update = 8,

        /// <summary>
        /// A <b>delete</b> operation.
        /// </summary>
        Delete = 16,

        /// <summary>
        /// Any operation.
        /// </summary>
        Any = Unspecified | Get | Create | Update | Delete,

        /// <summary>
        /// Any operation except <see cref="Get"/>.
        /// </summary>
        AnyExceptGet = Unspecified | Create | Update | Delete,

        /// <summary>
        /// Any operation except <see cref="Create"/>.
        /// </summary>
        AnyExceptCreate = Unspecified | Get | Update | Delete,

        /// <summary>
        /// Any operation except <see cref="Update"/>.
        /// </summary>
        AnyExceptUpdate = Unspecified | Get | Create | Delete
    }
}