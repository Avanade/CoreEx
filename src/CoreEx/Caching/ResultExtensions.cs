// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;
using CoreEx.Results;
using System;
using System.Threading.Tasks;

namespace CoreEx.Caching
{
    /// <summary>
    /// Provides <see cref="Result"/>-based <see cref="IRequestCache"/> extension methods.
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// Gets the cached value associated with the specified <see cref="Type"/> and <see cref="CompositeKey"/> where it exists; otherwise, adds and returns the value created by the <paramref name="addFactory"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to get or add.</param>
        /// <param name="addFactory">The factory function to create the <see cref="Result{T}"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding <paramref name="result"/> has <see cref="IResult.IsSuccess"/>.</remarks>
        public static Task<Result<T>> CacheGetOrAddAsync<T>(this Result result, IRequestCache cache, object? key, Func<Task<Result<T>>> addFactory)
            => CacheGetOrAddAsync(result, cache, new CompositeKey(key), addFactory);

        /// <summary>
        /// Gets the cached value associated with the specified <see cref="Type"/> and <see cref="CompositeKey"/> where it exists; otherwise, adds and returns the value created by the <paramref name="addFactory"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to get or add.</param>
        /// <param name="addFactory">The factory function to create the <see cref="Result{T}"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding <paramref name="result"/> has <see cref="IResult.IsSuccess"/>.</remarks>
        public static Task<Result<T>> CacheGetOrAddAsync<T>(this Result result, IRequestCache cache, CompositeKey key, Func<Task<Result<T>>> addFactory)
            => Task.FromResult(result).CacheGetOrAddAsync(cache, key, addFactory);

        /// <summary>
        /// Gets the cached value associated with the specified <see cref="Type"/> and <see cref="CompositeKey"/> where it exists; otherwise, adds and returns the value created by the <paramref name="addFactory"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to get or add.</param>
        /// <param name="addFactory">The factory function to create the <see cref="Result{T}"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding <paramref name="result"/> has <see cref="IResult.IsSuccess"/>.</remarks>
        public static Task<Result<T>> CacheGetOrAddAsync<T>(this Task<Result> result, IRequestCache cache, object? key, Func<Task<Result<T>>> addFactory)
            => CacheGetOrAddAsync(result, cache, new CompositeKey(key), addFactory);

        /// <summary>
        /// Gets the cached value associated with the specified <see cref="Type"/> and <see cref="CompositeKey"/> where it exists; otherwise, adds and returns the value created by the <paramref name="addFactory"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to get or add.</param>
        /// <param name="addFactory">The factory function to create the <see cref="Result{T}"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding <paramref name="result"/> has <see cref="IResult.IsSuccess"/>.</remarks>
        public static async Task<Result<T>> CacheGetOrAddAsync<T>(this Task<Result> result, IRequestCache cache, CompositeKey key, Func<Task<Result<T>>> addFactory)
        {
            cache.ThrowIfNull(nameof(cache));
            addFactory.ThrowIfNull(nameof(addFactory));

            var r = await result.ConfigureAwait(false);
            return await r.ThenAsAsync(async () =>
            {
                if (cache.TryGetValue<T>(key, out var val))
                    return Result.Ok(val!);

                var ar = await addFactory().ConfigureAwait(false);
                return ar.Then(v =>
                {
                    cache.SetValue(key, v);
                    return Result.Ok(v);
                });
            });
        }

        /// <summary>
        /// Sets (caches) the <see cref="Result{T}.Value"/> into the supplied <paramref name="cache"/> (using the underlying <see cref="IUniqueKey"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/> which must be an <see cref="IUniqueKey"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding <paramref name="result"/> has <see cref="IResult.IsSuccess"/>.</remarks>
        public static Result<T> CacheSet<T>(this Result<T> result, IRequestCache cache) where T : IUniqueKey
        {
            cache.ThrowIfNull(nameof(cache));
            return result.Then(r => { cache.SetValue(r); });
        }

        /// <summary>
        /// Sets (caches) the <see cref="Result{T}.Value"/> into the supplied <paramref name="cache"/> using the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to set.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding <paramref name="result"/> has <see cref="IResult.IsSuccess"/>.</remarks>
        public static Result<T> CacheSet<T>(this Result<T> result, IRequestCache cache, CompositeKey key)
        {
            cache.ThrowIfNull(nameof(cache));
            return result.Then(r => { cache.SetValue(key, r); });
        }

        /// <summary>
        /// Sets (caches) the <see cref="Result{T}.Value"/> into the supplied <paramref name="cache"/> (using the underlying <see cref="IUniqueKey"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/> which must be an <see cref="IUniqueKey"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding <paramref name="result"/> has <see cref="IResult.IsSuccess"/>.</remarks>
        public static Task<Result<T>> CacheSet<T>(this Task<Result<T>> result, IRequestCache cache) where T : IUniqueKey
        {
            cache.ThrowIfNull(nameof(cache));
            return result.Then(r => { cache.SetValue(r); });
        }

        /// <summary>
        /// Sets (caches) the <see cref="Result{T}.Value"/> into the supplied <paramref name="cache"/> using the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to set.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding <paramref name="result"/> has <see cref="IResult.IsSuccess"/>.</remarks>
        public static Task<Result<T>> CacheSet<T>(this Task<Result<T>> result, IRequestCache cache, CompositeKey key)
        {
            cache.ThrowIfNull(nameof(cache));
            return result.Then(r => { cache.SetValue(key, r); });
        }

        /// <summary>
        /// Removes the cached value associated with the specified <see cref="Type"/> and <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The cached value <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to remove.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding <paramref name="result"/> has <see cref="IResult.IsSuccess"/>.</remarks>
        public static Result CacheRemove<T>(this Result result, IRequestCache cache, CompositeKey key)
        {
            cache.ThrowIfNull(nameof(cache));
            return result.Then(() => { cache.Remove<T>(key); });
        }

        /// <summary>
        /// Removes the cached value associated with the specified <see cref="Type"/> and <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The cached value <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to remove.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding <paramref name="result"/> has <see cref="IResult.IsSuccess"/>.</remarks>
        public static Result CacheRemove<T>(this Result result, IRequestCache cache, object? key)
        {
            cache.ThrowIfNull(nameof(cache));
            return result.Then(() => { cache.Remove<T>(key); });
        }

        /// <summary>
        /// Removes the cached value associated with the specified <see cref="Type"/> (using the underlying <see cref="IUniqueKey"/>).
        /// </summary>
        /// <typeparam name="T">The cached value <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding <paramref name="result"/> has <see cref="IResult.IsSuccess"/>.</remarks>
        public static Result<T> CacheRemove<T>(this Result<T> result, IRequestCache cache) where T : IUniqueKey
        {
            cache.ThrowIfNull(nameof(cache));
            return result.Then(r => { cache.Remove<T>(r is null ? CompositeKey.Empty : RequestCache.GetKeyFromValue(r)); });
        }

        /// <summary>
        /// Removes the cached value associated with the specified <see cref="Type"/> and <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The cached value <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to remove.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding <paramref name="result"/> has <see cref="IResult.IsSuccess"/>.</remarks>
        public static Task<Result> CacheRemove<T>(this Task<Result> result, IRequestCache cache, CompositeKey key)
        {
            cache.ThrowIfNull(nameof(cache));
            return result.Then(() => { cache.Remove<T>(key); });
        }

        /// <summary>
        /// Removes the cached value associated with the specified <see cref="Type"/> and <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The cached value <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to remove.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding <paramref name="result"/> has <see cref="IResult.IsSuccess"/>.</remarks>
        public static Task<Result> CacheRemove<T>(this Task<Result> result, IRequestCache cache, object? key)
        {
            cache.ThrowIfNull(nameof(cache));
            return result.Then(() => { cache.Remove<T>(key); });
        }

        /// <summary>
        /// Removes the cached value associated with the specified <see cref="Type"/> (using the underlying <see cref="IUniqueKey"/>).
        /// </summary>
        /// <typeparam name="T">The cached value <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding <paramref name="result"/> has <see cref="IResult.IsSuccess"/>.</remarks>
        public static Task<Result<T>> CacheRemove<T>(this Task<Result<T>> result, IRequestCache cache) where T : IUniqueKey
        {
            cache.ThrowIfNull(nameof(cache));
            return result.Then(r => { cache.Remove<T>(r is null ? CompositeKey.Empty : RequestCache.GetKeyFromValue(r)); });
        }
    }
}