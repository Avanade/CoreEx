// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping;
using System;
using System.Net.Http;

namespace CoreEx.Http.Extended
{
    /// <summary>
    /// Provides <see cref="IMapper"/> support for a typed <see cref="HttpClient"/>.
    /// </summary>
    public interface ITypedMappedHttpClient
    {
        /// <summary>
        /// Gets the <see cref="IMapper"/>.
        /// </summary>
        IMapper Mapper { get; }

        /// <summary>
        /// Maps the <typeparamref name="TResponseHttp"/> value to the <typeparamref name="TResponse"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponseHttp">The response HTTP <see cref="Type"/>.</typeparam>
        /// <param name="httpResult">The <see cref="HttpResult{T}"/>.</param>
        /// <returns>The mapped <see cref="HttpResult{T}"/>.</returns>
        public HttpResult<TResponse> MapResponse<TResponse, TResponseHttp>(HttpResult<TResponseHttp> httpResult) => new(httpResult.Response, httpResult.Content, httpResult.IsSuccess && httpResult.Value is not null ? Mapper.Map<TResponse>(httpResult.Value, OperationTypes.Get) : default!);

        /// <summary>
        /// Maps the <typeparamref name="TRequest"/> <paramref name="value"/> to the <typeparamref name="TRequestHttp"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <typeparam name="TRequestHttp">The request HTTP <see cref="Type"/>.</typeparam>
        /// <param name="value">The request value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The mapped <typeparamref name="TRequestHttp"/> value.</returns>
        public TRequestHttp MapRequest<TRequest, TRequestHttp>(TRequest value, OperationTypes operationType) => value is null ? default! : Mapper.Map<TRequestHttp>(value, operationType);
    }
}