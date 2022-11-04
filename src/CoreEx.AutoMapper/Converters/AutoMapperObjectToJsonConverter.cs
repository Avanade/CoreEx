// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Wraps a <see cref="ObjectToJsonConverter{T}"/> to enable for <b>AutoMapper</b>.
    /// </summary>
    public class AutoMapperObjectToJsonConverter<T> : AutoMapperConverterWrapper<T?, string?, ObjectToJsonConverter<T>, AutoMapperObjectToJsonConverter<T>> { }
}