// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using AutoMapper;
using AutoMapper.Configuration;
using System;
using System.Linq;
using System.Linq.Expressions;

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
            if (mce == null)
                throw new ArgumentNullException(nameof(mce));

            mce.PreCondition((ResolutionContext rc) => !rc.Options.Items.TryGetValue(OperationTypesName, out var ot) || operationTypes.HasFlag((OperationTypes)ot));
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
            if (pce == null)
                throw new ArgumentNullException(nameof(pce));

            pce.Condition(cp => !cp.Context.Options.Items.TryGetValue(OperationTypesName, out var ot) || operationTypes.HasFlag((OperationTypes)ot));
            return pce;
        }

        /// <summary>
        /// Flattens the complex typed source member into the same named destination members.
        /// </summary>
        /// <typeparam name="TDestination">The destination entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TSource">The source entity <see cref="Type"/>.</typeparam>
        /// <param name="me">The <see cref="IMappingExpression{TSource, TDestination}"/>.</param>
        /// <param name="memberExpressions">One or more members to flatten.</param>
        /// <remarks>Uses the <see cref="IMappingExpression{TSource, TDestination}"/> <see cref="IProjectionExpression{TSource, TDestination, TMappingExpression}.IncludeMembers(Expression{Func{TSource, object}}[])"/>.</remarks>
        public static IMappingExpression<TDestination, TSource> Flatten<TDestination, TSource>(this IMappingExpression<TDestination, TSource> me, params Expression<Func<TDestination, object>>[] memberExpressions)
        {
            if (me == null)
                throw new ArgumentNullException(nameof(me));

            me.IncludeMembers(memberExpressions);
            return me;
        }

        /// <summary>
        /// Flattens the complex typed source member from the same named destination members.
        /// </summary>
        /// <typeparam name="TDestination">The destination entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TSource">The source entity <see cref="Type"/>.</typeparam>
        /// <param name="me">The <see cref="IMappingExpression{TSource, TDestination}"/>.</param>
        /// <param name="memberExpressions">One or more members to flatten.</param>
        /// <remarks>Executes similar to: <c>d2s.ForMember(expression, o => o.MapFrom(d => d))</c></remarks>
        public static IMappingExpression<TDestination, TSource> Unflatten<TDestination, TSource>(this IMappingExpression<TDestination, TSource> me, params Expression<Func<TSource, object>>[] memberExpressions)
        {
            if (me == null)
                throw new ArgumentNullException(nameof(me));

            if (memberExpressions != null)
                memberExpressions.ForEach(exp => me.ForMember(exp, o => o.MapFrom(d => d)));

            return me;
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
            => (mapper ?? throw new ArgumentNullException(nameof(mapper))).Map<TDestination>(source, o => o.Items.Add(OperationTypesName, operationType));

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
            => (mapper ?? throw new ArgumentNullException(nameof(mapper))).Map<TSource, TDestination>(source, o => o.Items.Add(OperationTypesName, operationType));

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
            => (mapper ?? throw new ArgumentNullException(nameof(mapper))).Map(source, destination, o => o.Items.Add(OperationTypesName, operationType));
    }
}