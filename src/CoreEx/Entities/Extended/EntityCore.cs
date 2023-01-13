// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Represents the core <b>Entity</b> capabilities including <see cref="INotifyPropertyChanged"/> <see cref="IChangeTracking"/> support.
    /// </summary>
    /// <remarks>The <see cref="EntityCore"/> class is not thread-safe; it does however, place a lock around all <b>set</b> operations to minimise concurrency challenges.</remarks>
    [System.Diagnostics.DebuggerStepThrough]
    public abstract class EntityCore : INotifyPropertyChanged, IChangeTracking, IReadOnly
    {
        private readonly object _lock = new();
        private Dictionary<string, PropertyChangedEventHandler>? _propertyEventHandlers;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Trigger the property(s) changed.
        /// </summary>
        private void TriggerPropertyChanged(string propertyName, params string[] propertyNames)
        {
            IsChanged = true;

            OnPropertyChanged(propertyName);

            foreach (string name in propertyNames)
            {
                if (!string.IsNullOrEmpty(name))
                    OnPropertyChanged(name);
            }
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event (typically overridden with additional logic).
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        protected virtual void OnPropertyChanged(string propertyName) => RaisePropertyChanged(propertyName);

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event only (<see cref="OnPropertyChanged"/>).
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        protected void RaisePropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? throw new ArgumentNullException(nameof(propertyName))));

        /// <summary>
        /// Gets a property value (automatically instantiating new where current value is null).
        /// </summary>
        /// <typeparam name="T">The property <see cref="Type"/>.</typeparam>
        /// <param name="propertyValue">The property value to get.</param>
        static protected T GetAutoValue<T>(ref T propertyValue) where T : class, new() => propertyValue ??= new T();

        /// <summary>
        /// Sets a property value and raises the <see cref="PropertyChanged"/> event where applicable.
        /// </summary>
        /// <param name="propertyValue">The property value to set.</param>
        /// <param name="setValue">The value to set.</param>
        /// <param name="default">The default value to perform immutable check against.</param>
        /// <param name="immutable">Indicates whether the value is immutable; can not be changed once set.</param>
        /// <param name="propertyName">The name of the primary property that changed.</param>
        /// <returns><c>true</c> indicates that the property value changed; otherwise, <c>false</c>.</returns>
        protected bool SetValue<T>(ref T propertyValue, T setValue, bool immutable = false, T @default = default!, [CallerMemberName] string? propertyName = null)
        { 
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            lock (_lock)
            {
                // Check and see if the value has changed or not; exit if being set to same value.
                var isChanged = true;
                T val = Cleaner.Clean(setValue, false);
                if (ReferenceEquals(propertyValue, val))
                    isChanged = false;
                else if (propertyValue is IComparable<T>)
                {
                    if (Comparer<T>.Default.Compare(val, propertyValue) == 0)
                        isChanged = false;
                }

                if (!isChanged)
                    return false;

                // Test is read only.
                if (IsReadOnly)
                    throw new InvalidOperationException(EntityConsts.EntityIsReadOnlyMessage);

                // Test immutability.
                if (immutable && Comparer<T>.Default.Compare(propertyValue, @default) != 0)
                    throw new InvalidOperationException(EntityConsts.ValueIsImmutableMessage);

                // Unwire old value.
                INotifyPropertyChanged? npc;
                if (propertyValue != null)
                {
                    npc = propertyValue as INotifyPropertyChanged;
                    if (npc != null)
                        npc.PropertyChanged -= GetValue_PropertyChanged(propertyName);
                }

                // Update the property and trigger the property changed.
                propertyValue = val;
                TriggerPropertyChanged(propertyName);

                // Wire up new value.
                if (val != null)
                {
                    npc = val as INotifyPropertyChanged;
                    if (npc != null)
                        npc.PropertyChanged += GetValue_PropertyChanged(propertyName);
                }

                return true;
            }
        }

        /// <summary>
        /// Gets the <see cref="PropertyChangedEventHandler"/> for the named property.
        /// </summary>
        private PropertyChangedEventHandler GetValue_PropertyChanged(string propertyName)
        {
            _propertyEventHandlers ??= new Dictionary<string, PropertyChangedEventHandler>();

            if (!_propertyEventHandlers.ContainsKey(propertyName))
                _propertyEventHandlers.Add(propertyName, (sender, e) => TriggerPropertyChanged(propertyName));

            return _propertyEventHandlers[propertyName];
        }

        /// <summary>
        /// Sets a <see cref="string"/> property value and raises the <see cref="PropertyChanged"/> event where applicable.
        /// </summary>
        /// <param name="propertyValue">The property value to set.</param>
        /// <param name="setValue">The value to set.</param>
        /// <param name="trim">The <see cref="StringTrim"/>.</param>
        /// <param name="transform">The <see cref="StringTransform"/> (defaults to <see cref="StringTransform.UseDefault"/>).</param>
        /// <param name="casing">The <see cref="StringCase"/> (defaults to <see cref="StringCase.UseDefault"/>).</param>
        /// <param name="immutable">Indicates whether the value is immutable; can not be changed once set.</param>
        /// <param name="propertyName">The name of the primary property that changed.</param>
        /// <returns><c>true</c> indicates that the property value changed; otherwise, <c>false</c>.</returns>
        protected bool SetValue(ref string? propertyValue, string? setValue, StringTrim trim, StringTransform transform = StringTransform.UseDefault, StringCase casing = StringCase.UseDefault, bool immutable = false, [CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            lock (_lock)
            {
                string? val = Cleaner.Clean(setValue, trim, transform, casing);
                if (val == propertyValue)
                    return false;

                if (IsReadOnly)
                    throw new InvalidOperationException(EntityConsts.EntityIsReadOnlyMessage);

                if (immutable && propertyValue != null)
                    throw new InvalidOperationException(EntityConsts.ValueIsImmutableMessage);

                propertyValue = val!;
                TriggerPropertyChanged(propertyName);

                return true;
            }
        }

        /// <summary>
        /// Sets a <see cref="string"/> property value and raises the <see cref="PropertyChanged"/> event where applicable.
        /// </summary>
        /// <param name="propertyValue">The property value to set.</param>
        /// <param name="setValue">The value to set.</param>
        /// <param name="trim">The <see cref="StringTrim"/>.</param>
        /// <param name="transform">The <see cref="StringTransform"/> (defaults to <see cref="StringTransform.UseDefault"/>).</param>
        /// <param name="casing">The <see cref="StringCase"/> (defaults to <see cref="StringCase.UseDefault"/>).</param>
        /// <param name="immutable">Indicates whether the value is immutable; can not be changed once set.</param>
        /// <param name="propertyName">The name of the primary property that changed.</param>
        /// <returns><c>true</c> indicates that the property value changed; otherwise, <c>false</c>.</returns>
        protected bool SetValue(ref string? propertyValue, string? setValue, StringTransform transform, StringTrim trim = StringTrim.UseDefault, StringCase casing = StringCase.UseDefault, bool immutable = false, [CallerMemberName] string? propertyName = null)
            => SetValue(ref propertyValue, setValue, trim, transform, casing, immutable, propertyName);

        /// <summary>
        /// Sets a <see cref="string"/> property value and raises the <see cref="PropertyChanged"/> event where applicable.
        /// </summary>
        /// <param name="propertyValue">The property value to set.</param>
        /// <param name="setValue">The value to set.</param>
        /// <param name="trim">The <see cref="StringTrim"/>.</param>
        /// <param name="transform">The <see cref="StringTransform"/> (defaults to <see cref="StringTransform.UseDefault"/>).</param>
        /// <param name="casing">The <see cref="StringCase"/> (defaults to <see cref="StringCase.UseDefault"/>).</param>
        /// <param name="immutable">Indicates whether the value is immutable; can not be changed once set.</param>
        /// <param name="propertyName">The name of the primary property that changed.</param>
        /// <returns><c>true</c> indicates that the property value changed; otherwise, <c>false</c>.</returns>
        protected bool SetValue(ref string? propertyValue, string? setValue, StringCase casing, StringTransform transform = StringTransform.UseDefault, StringTrim trim = StringTrim.UseDefault, bool immutable = false, [CallerMemberName] string? propertyName = null)
            => SetValue(ref propertyValue, setValue, trim, transform, casing, immutable, propertyName);

        /// <summary>
        /// Sets a <see cref="DateTime"/> property value and raises the <see cref="PropertyChanged"/> event where applicable.
        /// </summary>
        /// <param name="propertyValue">The property value to set.</param>
        /// <param name="setValue">The value to set.</param>
        /// <param name="transform">The <see cref="DateTimeTransform"/> to be applied (defaults to <see cref="DateTimeTransform.UseDefault"/>).</param>
        /// <param name="immutable">Indicates whether the value is immutable; can not be changed once set.</param>
        /// <param name="propertyName">The name of the primary property that changed.</param>
        /// <returns><c>true</c> indicates that the property value changed; otherwise, <c>false</c>.</returns>
        protected bool SetValue(ref DateTime propertyValue, DateTime setValue, DateTimeTransform transform = DateTimeTransform.UseDefault, bool immutable = false, [CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            lock (_lock)
            {
                DateTime val = Cleaner.Clean(setValue, transform);
                if (val == propertyValue)
                    return false;

                if (IsReadOnly)
                    throw new InvalidOperationException(EntityConsts.EntityIsReadOnlyMessage);

                if (immutable && propertyValue != DateTime.MinValue)
                    throw new InvalidOperationException(EntityConsts.ValueIsImmutableMessage);

                propertyValue = val;
                TriggerPropertyChanged(propertyName);
                return true;
            }
        }

        /// <summary>
        /// Sets a <see cref="Nullable{DateTime}"/> property value and raises the <see cref="PropertyChanged"/> event where applicable.
        /// </summary>
        /// <param name="propertyValue">The property value to set.</param>
        /// <param name="setValue">The value to set.</param>
        /// <param name="immutable">Indicates whether the value is immutable; can not be changed once set.</param>
        /// <param name="transform">The <see cref="DateTimeTransform"/> to be applied.</param>
        /// <param name="propertyName">The name of the primary property that changed.</param>
        /// <returns><c>true</c> indicates that the property value changed; otherwise, <c>false</c>.</returns>
        protected bool SetValue(ref DateTime? propertyValue, DateTime? setValue, DateTimeTransform transform, bool immutable = false, [CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            lock (_lock)
            {
                DateTime? val = Cleaner.Clean(setValue, transform);
                if (val == propertyValue)
                    return false;

                if (IsReadOnly)
                    throw new InvalidOperationException(EntityConsts.EntityIsReadOnlyMessage);

                if (immutable && propertyValue != null)
                    throw new InvalidOperationException(EntityConsts.ValueIsImmutableMessage);

                propertyValue = val;
                TriggerPropertyChanged(propertyName);
                return true;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <remarks>This will trigger the <see cref="OnAcceptChanges"/> to perform the operation for all properties.</remarks>
        public void AcceptChanges()
        {
            lock (_lock)
            {
                OnAcceptChanges();
                IsChanged = false;
            }
        }

        /// <summary>
        /// Applies the <see cref="AcceptChanges"/> to all the underlying properties.
        /// </summary>
        protected virtual void OnAcceptChanges() { }

        /// <inheritdoc/>
        [JsonIgnore]
        public bool IsChanged { get; private set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <remarks>This will trigger the <see cref="OnMakeReadOnly"/> to perform the operation for all properties.</remarks>
        public void MakeReadOnly()
        {
            lock (_lock)
            {
                OnMakeReadOnly();
                IsChanged = false;
                IsReadOnly = true;
            }
        }

        /// <summary>
        /// Applies the <see cref="MakeReadOnly"/> to all the underlying properties.
        /// </summary>
        protected virtual void OnMakeReadOnly() { }
    }
}