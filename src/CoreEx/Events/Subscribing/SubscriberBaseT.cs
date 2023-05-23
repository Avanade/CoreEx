// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Validation;
using System;
using System.Threading.Tasks;
using System.Threading;
using CoreEx.Results;

namespace CoreEx.Events.Subscribing
{
    /// <summary>
    /// Represents an <see cref="IEventSubscriber"/> with a <see cref="ValueType"/> of <typeparamref name="TValue"/> (supports <see cref="EventData{T}"/>).
    /// </summary>
    /// <typeparam name="TValue">The <see cref="EventData{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <remarks>This is for use when the <see cref="EventData{T}"/> has to be deserialized.
    /// <para>Additionally, <see cref="ValueIsRequired"/> and <see cref="ValueValidator"/> enable a consistent validation approach prior to the underlying <see cref="ReceiveAsync(EventData{TValue}, CancellationToken)"/> being invoked.</para></remarks>
    public abstract class SubscriberBase<TValue> : SubscriberBase
    {
        /// <inheritdoc/>
        public override Type EventDataType => typeof(EventData<TValue>);

        /// <inheritdoc/>
        public override Type? ValueType => typeof(TValue);

        /// <summary>
        /// Indicates whether the <see cref="EventData{T}.Value"/> is required.
        /// </summary>
        protected bool ValueIsRequired { get; set; } = true;

        /// <summary>
        /// Gets or sets the optional <see cref="IValidator{T}"/> for the value.
        /// </summary>
        protected IValidator<TValue>? ValueValidator { get; set; }

        /// <inheritdoc/>
        /// <remarks>Caution where overridding this method as it contains the underlying functionality to invoke <see cref="ReceiveAsync(EventData{TValue}, CancellationToken)"/> that is the <i>required</i> method to be overridden.</remarks>
        public async override Task<Result> ReceiveAsync(EventData @event, CancellationToken cancellationToken)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            return await Result.Go(@event)
                .When(ed => ValueIsRequired && ed.Value is null, _ => Result<EventData>.ValidationError(EventSubscriberBase.RequiredErrorText))
                .ThenAs(ed => ed is EventData<TValue> edvx ? edvx : new EventData<TValue>(ed).Adjust(e => e.Value = (TValue)ed.Value!))
                .WhenAsync(ed => ValueValidator != null, async ed =>
                {
                    var vr = await ValueValidator!.ValidateAsync(ed.Value, cancellationToken).ConfigureAwait(false);
                    return vr.HasErrors ? Result<EventData<TValue>>.ValidationError(vr.Messages) : Result.Ok(ed);
                })
                .ThenAsAsync(ed => ReceiveAsync(ed, cancellationToken));
        }

        /// <summary>
        /// Receive and process the subscribed typed <paramref name="event"/>.
        /// </summary>
        /// <param name="event">The <see cref="EventData{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where <see cref="ValueIsRequired"/> and/or <see cref="ValueValidator"/> are specified then this method will only be invoked where the aforementioned validation has occured and the underlying <see cref="EventData{T}.Value"/>
        /// is considered valid.</remarks>
        public abstract Task<Result> ReceiveAsync(EventData<TValue> @event, CancellationToken cancellationToken);
    }
}