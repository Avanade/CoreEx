// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Azure.Cosmos;
using System;

namespace CoreEx.Cosmos.Model
{
    /// <summary>
    /// Enables the model-only <see cref="Container"/>.
    /// </summary>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    public interface ICosmosDbModelContainer<TModel> : ICosmosDbContainerCore where TModel : class, new() { }
}