// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using AutoMapper;
using AutoMapper.Configuration;
using System;

namespace CoreEx.Mapping
{
    /// <summary>
    /// Adds additional extension methods for <c>AutoMapper</c>.
    /// </summary>
    public static class AutoMapperExtensions
    {
        /// <summary>
        /// Gets the <see cref="CoreEx.Mapping.OperationTypes"/> name used for indexing <see cref="IMappingOperationOptions.Items"/>.
        /// </summary>
        public const string OperationTypesName = nameof(OperationTypes);

        /// <summary>
        /// Conditionally map this member with the specified the <see cref="CoreEx.Mapping.OperationTypes"/> against the <see cref="IMappingOperationOptions.Items"/> <see cref="OperationTypesName"/> value, evaluated before accessing the source value.
        /// </summary>
        /// <typeparam name="TSource">The source entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TDestination">The destination entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TSourceMember">The source entity member <see cref="Type"/>.</typeparam>
        /// <param name="mce">The <see cref="IMemberConfigurationExpression{TSource, TDestination, TMember}"/>.</param>
        /// <param name="operationTypes">The <see cref="CoreEx.Mapping.OperationTypes"/>.</param>
        /// <remarks>Uses the <see cref="IMemberConfigurationExpression{TSource, TDestination, TMember}.PreCondition(Func{ResolutionContext, bool})"/>.</remarks>
        public static IMemberConfigurationExpression<TSource, TDestination, TSourceMember> OperationTypes<TSource, TDestination, TSourceMember>(this IMemberConfigurationExpression<TSource, TDestination, TSourceMember> mce, OperationTypes operationTypes)
        {
            mce.ThrowIfNull(nameof(mce)).PreCondition((ResolutionContext rc) => !rc.Items.TryGetValue(OperationTypesName, out var ot) || operationTypes.HasFlag((OperationTypes)ot));
            return mce;
        }

        /// <summary>
        /// Conditionally map this path with the specified the <see cref="CoreEx.Mapping.OperationTypes"/> against the <see cref="IMappingOperationOptions.Items"/> <see cref="OperationTypesName"/> value, evaluated before accessing the source value.
        /// </summary>
        /// <typeparam name="TSource">The source entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TDestination">The destination entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TMember">The member <see cref="Type"/>.</typeparam>
        /// <param name="pce">The <see cref="IPathConfigurationExpression{TSource, TDestination, TMember}"/>.</param>
        /// <param name="operationTypes">The <see cref="CoreEx.Mapping.OperationTypes"/>.</param>
        /// <remarks>Uses the <see cref="IPathConfigurationExpression{TSource, TDestination, TMember}.Condition(Func{ConditionParameters{TSource, TDestination, TMember}, bool})"/>.</remarks>
        public static IPathConfigurationExpression<TSource, TDestination, TMember> OperationTypes<TSource, TDestination, TMember>(this IPathConfigurationExpression<TSource, TDestination, TMember> pce, OperationTypes operationTypes)
        {
            pce.ThrowIfNull(nameof(pce)).Condition(cp => !cp.Context.Items.TryGetValue(OperationTypesName, out var ot) || operationTypes.HasFlag((OperationTypes)ot));
            return pce;
        }

        /// <summary>
        /// Maps the <paramref name="source"/> (inferring <see cref="Type"/>) value to a new <typeparamref name="TDestination"/> value.
        /// </summary>
        /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="AutoMapper.IMapper"/>.</param>
        /// <param name="source">The source value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="CoreEx.Mapping.OperationTypes"/> value being performed.</param>
        /// <returns>The destination value.</returns>
        public static TDestination Map<TDestination>(this AutoMapper.IMapper mapper, object source, OperationTypes operationType)
            => mapper.ThrowIfNull(nameof(mapper)).Map<TDestination>(source, o => o.Items.Add(OperationTypesName, operationType));

        /// <summary>
        /// Maps the <paramref name="source"/> value to a new <typeparamref name="TDestination"/> value.
        /// </summary>
        /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
        /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="AutoMapper.IMapper"/>.</param>
        /// <param name="source">The source value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="CoreEx.Mapping.OperationTypes"/> value being performed.</param>
        /// <returns>The destination value.</returns>
        public static TDestination Map<TSource, TDestination>(this AutoMapper.IMapper mapper, TSource source, OperationTypes operationType)
            => mapper.ThrowIfNull(nameof(mapper)).Map<TSource, TDestination>(source, o => o.Items.Add(OperationTypesName, operationType));

        /// <summary>
        /// Maps the <paramref name="source"/> value into the existing <paramref name="destination"/> value.
        /// </summary>
        /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
        /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="AutoMapper.IMapper"/>.</param>
        /// <param name="source">The source value.</param>
        /// <param name="destination">The destination value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="CoreEx.Mapping.OperationTypes"/> value being performed.</param>
        /// <returns>The <paramref name="destination"/> value.</returns>
        public static TDestination Map<TSource, TDestination>(this AutoMapper.IMapper mapper, TSource source, TDestination destination, OperationTypes operationType)
            => mapper.ThrowIfNull(nameof(mapper)).Map(source, destination, o => o.Items.Add(OperationTypesName, operationType));
    }
}