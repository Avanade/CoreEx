// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Wraps a <see cref="TypeToStringConverter{T}"/> to enable for <b>AutoMapper</b>.
    /// </summary>
    public class AutoMapperTypeToStringConverter<T> : AutoMapperConverterWrapper<T, string?, TypeToStringConverter<T>, AutoMapperTypeToStringConverter<T>> { }
}