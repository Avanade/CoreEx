// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Wraps a <see cref="ReferenceDataCodeConverter{TRef}"/> to enable for <b>AutoMapper</b>.
    /// </summary>
    public class AutoMapperReferenceDataCodeConverter<TRef> : AutoMapperConverterWrapper<TRef?, string?, ReferenceDataCodeConverter<TRef>, AutoMapperReferenceDataCodeConverter<TRef>> where TRef : IReferenceData, new() { }
}