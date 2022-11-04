// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Wraps a <see cref="ReferenceDataIdConverter{TRef, TId}"/> to enable for <b>AutoMapper</b>.
    /// </summary>
    public class AutoMapperReferenceDataIdConverter<TRef, TId> : AutoMapperConverterWrapper<TRef?, TId, ReferenceDataIdConverter<TRef, TId>, AutoMapperReferenceDataIdConverter<TRef, TId>> where TRef : IReferenceData, new() { }
}