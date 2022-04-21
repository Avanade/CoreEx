// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides the extended <b>Reference Data</b> properties with typed <see cref="IIdentifier{T}.Id"/>.
    /// </summary>
    /// <typeparam name="T">The identifier <see cref="Type"/>.</typeparam>
    public interface IReferenceDataExtended<T> : IReferenceData<T>, IReferenceDataExtended { }
}