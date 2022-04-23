// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Represents the core <b>Entity</b> capabilities including <see cref="INotifyPropertyChanged"/> <see cref="IChangeTracking"/> support.
    /// </summary>
    /// <remarks>The <see cref="EntityCore"/> is not thread-safe; it does however, place a lock around all <b>set</b> operations to minimise concurrency challenges.</remarks>
    [System.Diagnostics.DebuggerStepThrough]
    public abstract class EntityCore : INotifyPropertyChanged, IChangeTracking, IReadOnly
    {
        /// <summary>
        /// Gets the value is immutable message.
        /// </summary>
        public const string ValueIsImmutableMessage = "Value is immutable; cannot be changed once already set to a value.";

        /// <summary>
        /// Gets the entity is read only message.
        /// </summary>
        public const string EntityIsReadOnlyMessage = "Entity is read only; property cannot be changed.";

        private readonly object _lock = new();
        private Dictionary<string, PropertyChangedEventHandler>? _propertyEventHandlers;

        /// <summary>
        /// Indicates whether the <see cref="INotifyPropertyChanged.PropertyChanged"/> event is raised when a property is set with a value that is the same as the existing; 
        /// unless overridden (see <see cref="NotifyChangesWhenSameValue"/>) for a specific instance. Defaults to <c>false</c> indicating to <b>not</b> notify changes for same.
        /// </summary>
        public static bool ShouldNotifyChangesWhenSameValue { get; set; } = false;

        /// <summary>
        /// Indicates whether the <see cref="INotifyPropertyChanged.PropertyChanged"/> event is raised when a property is set with a value that is the same as the existing overriding
        /// the <see cref="ShouldNotifyChangesWhenSameValue"/> for the specific instance. A value of <c>null</c> indicates to use the <see cref="ShouldNotifyChangesWhenSameValue"/> setting.
        /// </summary>
        public bool? NotifyChangesWhenSameValue { get; set; } = null;

        /// <summary>
        /// Occurs before a property value is about to change.
        /// </summary>
        public event BeforePropertyChangedEventHandler? BeforePropertyChanged;

        /// <summary>
        /// Raises the <see cref="BeforePropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="newValue">The new value.</param>
        /// <returns><c>true</c> indicates that the property change is to be cancelled; otherwise, <c>false</c>.</returns>
        protected virtual bool OnBeforePropertyChanged(string propertyName, object? newValue)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            if (BeforePropertyChanged != null)
            {
                var e = new BeforePropertyChangedEventArgs(propertyName, newValue);
                BeforePropertyChanged(this, e);
                if (e.Cancel)
                    return true;
            }

            return false;
        }

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
        public void RaisePropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? throw new ArgumentNullException(nameof(propertyName))));

        /// <summary>
        /// Gets a property value (automatically instantiating new where current value is null).
        /// </summary>
        /// <typeparam name="T">The property <see cref="Type"/>.</typeparam>
        /// <param name="propertyValue">The property value to get.</param>
        static protected T GetAutoValue<T>(ref T propertyValue) where T : class, new()
        {
            if (propertyValue == null)
                propertyValue = new T();

            return propertyValue;
        }

        /// <summary>
        /// Sets a property value and raises the <see cref="PropertyChanged"/> event where applicable.
        /// </summary>
        /// <param name="propertyValue">The property value to set.</param>
        /// <param name="setValue">The value to set.</param>
        /// <param name="propertyName">The name of the primary property that changed.</param>
        /// <returns><c>true</c> indicates that the property value changed; otherwise, <c>false</c>.</returns>
        protected bool SetValue<T>(ref T propertyValue, T setValue, [CallerMemberName] string? propertyName = null)
            => SetValue(ref propertyValue, setValue, immutable: false, @default: default!, propertyName: propertyName);

        /// <summary>
        /// Sets a property value and raises the <see cref="PropertyChanged"/> event where applicable.
        /// </summary>
        /// <param name="propertyValue">The property value to set.</param>
        /// <param name="setValue">The value to set.</param>
        /// <param name="default">The default value to perform immutable check against.</param>
        /// <param name="immutable">Indicates whether the value is immutable; can not be changed once set.</param>
        /// <param name="propertyName">The name of the primary property that changed.</param>
        /// <returns><c>true</c> indicates that the property value changed; otherwise, <c>false</c>.</returns>
        protected bool SetValue<T>(ref T propertyValue, T setValue, bool immutable, T @default = default!, [CallerMemberName] string? propertyName = null)
            => SetValue(ref propertyValue, setValue, immutable: immutable, @default: @default, bubblePropertyChanged: true, propertyName: propertyName);

        /// <summary>
        /// Sets a property value and raises the <see cref="PropertyChanged"/> event where applicable.
        /// </summary>
        /// <typeparam name="T">The property <see cref="Type"/>.</typeparam>
        /// <param name="propertyValue">The property value to set.</param>
        /// <param name="setValue">The value to set.</param>
        /// <param name="immutable">Indicates whether the value is immutable; can not be changed once set.</param>
        /// <param name="default">The default value to perform immutable check against.</param>
        /// <param name="bubblePropertyChanged">Indicates whether the value should bubble up property changes versus only recording within the sub-entity itself.</param>
        /// <param name="beforeChange">Function to invoke before changing the value; a result of <c>true</c> indicates that the property change is to be cancelled; otherwise, <c>false</c>.</param>
        /// <param name="propertyName">The name of the primary property that changed.</param>
        /// <param name="secondaryPropertyNames">The names of the secondary properties that need to be advised of the change.</param>
        /// <returns><c>true</c> indicates that the property value changed; otherwise, <c>false</c>.</returns>
        private bool SetValue<T>(ref T propertyValue, T setValue, bool immutable = false, T @default = default!, bool bubblePropertyChanged = true, Func<T, bool>? beforeChange = null, [CallerMemberName] string? propertyName = null, params string[] secondaryPropertyNames)
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

                if (!isChanged && !RaisePropertyChangedWhenSame)
                    return false;

                // Test is read only.
                if (IsReadOnly)
                    return !isChanged ? false : throw new InvalidOperationException(EntityIsReadOnlyMessage);

                // Test immutability.
                if (immutable && isChanged && Comparer<T>.Default.Compare(propertyValue, @default) != 0)
                    throw new InvalidOperationException(ValueIsImmutableMessage);

                // Handle on before property changed.
                if (beforeChange != null)
                {
                    if (beforeChange.Invoke(val))
                        return false;
                }

                if (OnBeforePropertyChanged(propertyName, val))
                    return false;

                // Determine bubbling and unwire old value.
                INotifyPropertyChanged? npc;
                if (bubblePropertyChanged && propertyValue != null)
                {
                    npc = propertyValue as INotifyPropertyChanged;
                    if (npc != null)
                        npc.PropertyChanged -= GetValue_PropertyChanged(propertyName);
                }

                // Update the property and trigger the property changed.
                propertyValue = val;
                TriggerPropertyChanged(propertyName, secondaryPropertyNames);

                // Determine bubbling and wire up new value.
                if (bubblePropertyChanged && val != null)
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
        private PropertyChangedEventHandler GetValue_PropertyChanged(string propertyName, params string[] secondaryPropertyNames)
        {
            if (_propertyEventHandlers == null)
                _propertyEventHandlers = new Dictionary<string, PropertyChangedEventHandler>();

            if (!_propertyEventHandlers.ContainsKey(propertyName))
                _propertyEventHandlers.Add(propertyName, (sender, e) => TriggerPropertyChanged(propertyName, secondaryPropertyNames));

            return _propertyEventHandlers[propertyName];
        }

        /// <summary>
        /// Sets a <see cref="string"/> property value and raises the <see cref="PropertyChanged"/> event where applicable.
        /// </summary>
        /// <param name="propertyValue">The property value to set.</param>
        /// <param name="setValue">The value to set.</param>
        /// <param name="propertyName">The name of the primary property that changed.</param>
        /// <returns><c>true</c> indicates that the property value changed; otherwise, <c>false</c>.</returns>
        protected bool SetValue(ref string? propertyValue, string? setValue, [CallerMemberName] string? propertyName = null)
            => SetValue(ref propertyValue, setValue, StringTrim.UseDefault, propertyName: propertyName);

        /// <summary>
        /// Sets a <see cref="string"/> property value and raises the <see cref="PropertyChanged"/> event where applicable.
        /// </summary>
        /// <param name="propertyValue">The property value to set.</param>
        /// <param name="setValue">The value to set.</param>
        /// <param name="trim">The <see cref="StringTrim"/>.</param>
        /// <param name="transform">The <see cref="StringTransform"/> (defaults to <see cref="StringTransform.UseDefault"/>).</param>
        /// <param name="immutable">Indicates whether the value is immutable; can not be changed once set.</param>
        /// <param name="beforeChange">Function to invoke before changing the value; a result of <c>true</c> indicates that the property change is to be cancelled; otherwise, <c>false</c>.</param>
        /// <param name="propertyName">The name of the primary property that changed.</param>
        /// <param name="secondaryPropertyNames">The names of the secondary properties that need to be advised of the change.</param>
        /// <returns><c>true</c> indicates that the property value changed; otherwise, <c>false</c>.</returns>
        protected bool SetValue(ref string? propertyValue, string? setValue, StringTrim trim, StringTransform transform = StringTransform.UseDefault, bool immutable = false, Func<string?, bool>? beforeChange = null, [CallerMemberName] string? propertyName = null, params string[] secondaryPropertyNames)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            lock (_lock)
            {
                string? val = Cleaner.Clean(setValue, trim, transform);
                var isChanged = val != propertyValue;
                if (!RaisePropertyChangedWhenSame && !isChanged)
                    return false;

                if (IsReadOnly && isChanged)
                    throw new InvalidOperationException(EntityIsReadOnlyMessage);

                if (immutable && isChanged && propertyValue != null)
                    throw new InvalidOperationException(ValueIsImmutableMessage);

                if (beforeChange != null)
                {
                    if (beforeChange.Invoke(setValue))
                        return false;
                }

                if (OnBeforePropertyChanged(propertyName, setValue))
                    return false;

                propertyValue = val!;
                TriggerPropertyChanged(propertyName, secondaryPropertyNames);

                return true;
            }
        }

        /// <summary>
        /// Sets a <see cref="DateTime"/> property value and raises the <see cref="PropertyChanged"/> event where applicable.
        /// </summary>
        /// <param name="propertyValue">The property value to set.</param>
        /// <param name="setValue">The value to set.</param>
        /// <param name="propertyName">The name of the primary property that changed.</param>
        /// <returns><c>true</c> indicates that the property value changed; otherwise, <c>false</c>.</returns>
        protected bool SetValue(ref DateTime propertyValue, DateTime setValue, [CallerMemberName] string? propertyName = null)
            => SetValue(ref propertyValue, setValue, DateTimeTransform.UseDefault, propertyName: propertyName);

        /// <summary>
        /// Sets a <see cref="DateTime"/> property value and raises the <see cref="PropertyChanged"/> event where applicable.
        /// </summary>
        /// <param name="propertyValue">The property value to set.</param>
        /// <param name="setValue">The value to set.</param>
        /// <param name="immutable">Indicates whether the value is immutable; can not be changed once set.</param>
        /// <param name="transform">The <see cref="DateTimeTransform"/> to be applied (defaults to <see cref="DateTimeTransform.UseDefault"/>).</param>
        /// <param name="beforeChange">Function to invoke before changing the value; a result of <c>true</c> indicates that the property change is to be cancelled; otherwise, <c>false</c>.</param>
        /// <param name="propertyName">The name of the primary property that changed.</param>
        /// <param name="secondaryPropertyNames">The names of the secondary properties that need to be advised of the change.</param>
        /// <returns><c>true</c> indicates that the property value changed; otherwise, <c>false</c>.</returns>
        protected bool SetValue(ref DateTime propertyValue, DateTime setValue, DateTimeTransform transform, bool immutable = false, Func<DateTime, bool>? beforeChange = null, [CallerMemberName] string? propertyName = null, params string[] secondaryPropertyNames)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            lock (_lock)
            {
                DateTime val = Cleaner.Clean(setValue, transform);
                var isChanged = val != propertyValue;
                if (!RaisePropertyChangedWhenSame && !isChanged)
                    return false;

                if (IsReadOnly && isChanged)
                    throw new InvalidOperationException(EntityIsReadOnlyMessage);

                if (immutable && isChanged && propertyValue != DateTime.MinValue)
                    throw new InvalidOperationException(ValueIsImmutableMessage);

                if (beforeChange != null)
                {
                    if (beforeChange.Invoke(setValue))
                        return false;
                }

                if (OnBeforePropertyChanged(propertyName, setValue))
                    return false;

                propertyValue = val;
                TriggerPropertyChanged(propertyName, secondaryPropertyNames);
                return true;
            }
        }

        /// <summary>
        /// Sets a <see cref="DateTime"/> property value and raises the <see cref="PropertyChanged"/> event where applicable.
        /// </summary>
        /// <param name="propertyValue">The property value to set.</param>
        /// <param name="setValue">The value to set.</param>
        /// <param name="propertyName">The name of the primary property that changed.</param>
        /// <returns><c>true</c> indicates that the property value changed; otherwise, <c>false</c>.</returns>
        protected bool SetValue(ref DateTime? propertyValue, DateTime? setValue, [CallerMemberName] string? propertyName = null)
            => SetValue(ref propertyValue, setValue, DateTimeTransform.UseDefault, propertyName: propertyName);

        /// <summary>
        /// Sets a <see cref="Nullable{DateTime}"/> property value and raises the <see cref="PropertyChanged"/> event where applicable.
        /// </summary>
        /// <param name="propertyValue">The property value to set.</param>
        /// <param name="setValue">The value to set.</param>
        /// <param name="immutable">Indicates whether the value is immutable; can not be changed once set.</param>
        /// <param name="transform">The <see cref="DateTimeTransform"/> to be applied.</param>
        /// <param name="beforeChange">Function to invoke before changing the value; a result of <c>true</c> indicates that the property change is to be cancelled; otherwise, <c>false</c>.</param>
        /// <param name="propertyName">The name of the primary property that changed.</param>
        /// <param name="secondaryPropertyNames">The names of the secondary properties that need to be advised of the change.</param>
        /// <returns><c>true</c> indicates that the property value changed; otherwise, <c>false</c>.</returns>
        protected bool SetValue(ref DateTime? propertyValue, DateTime? setValue, DateTimeTransform transform, bool immutable = false, Func<DateTime?, bool>? beforeChange = null, [CallerMemberName] string? propertyName = null, params string[] secondaryPropertyNames)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            lock (_lock)
            {
                DateTime? val = Cleaner.Clean(setValue, transform);
                var isChanged = val != propertyValue;
                if (!RaisePropertyChangedWhenSame && !isChanged)
                    return false;

                if (IsReadOnly && isChanged)
                    throw new InvalidOperationException(EntityIsReadOnlyMessage);

                if (immutable && isChanged && propertyValue != null)
                    throw new InvalidOperationException(ValueIsImmutableMessage);

                if (beforeChange != null)
                {
                    if (beforeChange.Invoke(setValue))
                        return false;
                }

                if (OnBeforePropertyChanged(propertyName, setValue))
                    return false;

                propertyValue = val;
                TriggerPropertyChanged(propertyName, secondaryPropertyNames);
                return true;
            }
        }

        /// <summary>
        /// Indicates whether to raise the property changed event when same value by reviewing the current settings for <see cref="NotifyChangesWhenSameValue"/>
        /// and <see cref="ShouldNotifyChangesWhenSameValue"/>.
        /// </summary>
        protected bool RaisePropertyChangedWhenSame => NotifyChangesWhenSameValue ?? ShouldNotifyChangesWhenSameValue;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <remarks>This will trigger the <see cref="OnApplyAction(EntityAction)"/> with <see cref="EntityAction.AcceptChanges"/>.</remarks>
        public void AcceptChanges()
        {
            OnApplyAction(EntityAction.AcceptChanges);
            IsChanged = false;
        }

        /// <inheritdoc/>
        public bool IsChanged { get; private set; }

        /// <inheritdoc/>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <remarks>This will trigger the <see cref="OnApplyAction(EntityAction)"/> with <see cref="EntityAction.MakeReadOnly"/>.</remarks>
        public void MakeReadOnly()
        {
            OnApplyAction(EntityAction.MakeReadOnly);
            IsChanged = false;
            IsReadOnly = true;
        }

        /// <summary>
        /// Apply the specified <paramref name="action"/>.
        /// </summary>
        /// <param name="action">The <see cref="EntityAction"/> to perform.</param>
        protected virtual void OnApplyAction(EntityAction action) { }

        /// <summary>
        /// Applies the specified <paramref name="action"/> on the <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to perform the <paramref name="action"/> on.</param>
        /// <param name="action">The <see cref="EntityAction"/> to perform.</param>
        /// <returns>The value.</returns>
        protected static T ApplyAction<T>(T value, EntityAction action)
        {
            switch (action)
            {
                case EntityAction.CleanUp:
                    return Cleaner.Clean(value);

                case EntityAction.AcceptChanges:
                    if (value is EntityCore ac)
                        ac.AcceptChanges();

                    break;

                case EntityAction.MakeReadOnly:
                    if (value is EntityCore mro)
                        mro.MakeReadOnly();

                    break;
            }

            return value;
        }
    }
}