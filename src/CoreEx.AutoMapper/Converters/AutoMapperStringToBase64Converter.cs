// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Wraps a <see cref="StringToBase64Converter"/> to enable for <b>AutoMapper</b>.
    /// </summary>
    public class AutoMapperStringToBase64Converter : AutoMapperConverterWrapper<string?, byte[], StringToBase64Converter, AutoMapperStringToBase64Converter> { }
}