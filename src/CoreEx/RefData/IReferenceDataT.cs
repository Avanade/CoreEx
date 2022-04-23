// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides the core <b>Reference Data</b> properties with a typed <see cref="IIdentifier{T}.Id"/>.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
    public interface IReferenceData<TId> : IIdentifier<TId>, IReferenceData { }
}