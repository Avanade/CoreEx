﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.RefData;
using CoreEx.Results;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace CoreEx
{
    /// <summary>
    /// Represents a thread-bound (request) execution context using <see cref="AsyncLocal{ExecutionContext}"/>.
    /// </summary>
    /// <remarks>Used to house/pass context parameters and capabilities that are outside of the general operation arguments. This class should be extended by consumers where additional properties are required.
    /// <para>The <see cref="ExecutionContext"/> implements <see cref="IDisposable"/>; however, from a standard implementation perspective there are no unmanaged resources leveraged. The <see cref="Dispose()"/> will result in a <see cref="Reset"/>.</para></remarks>
    public class ExecutionContext : ITenantId, IDisposable
    {
        private static readonly AsyncLocal<ExecutionContext?> _asyncLocal = new();

        private DateTime? _timestamp;
        private Lazy<MessageItemCollection> _messages = new(true);
        private Lazy<ConcurrentDictionary<string, object?>> _properties = new(true);
        private IReferenceDataContext? _referenceDataContext;
        private HashSet<string>? _roles;
        private HashSet<string>? _permissions;
        private bool _disposed;
        private readonly object _lock = new();

        /// <summary>
        /// Gets or sets the function to create a default <see cref="ExecutionContext"/> instance.
        /// </summary>
        public static Func<ExecutionContext>? Create { get; set; } = () => new ExecutionContext();

        /// <summary>
        /// Indicates whether the <see cref="ExecutionContext"/> <see cref="Current"/> has a value.
        /// </summary>
        public static bool HasCurrent => _asyncLocal.Value != null;

        /// <summary>
        /// Gets the current <see cref="ExecutionContext"/> for the executing thread graph (see <see cref="AsyncLocal{T}"/>).
        /// </summary>
        /// <remarks>Where not previously set (see <see cref="SetCurrent(ExecutionContext?)"/> then the <see cref="Create"/> will be invoked as a backup to create an instance.</remarks>
        public static ExecutionContext Current => _asyncLocal.Value ??= Create?.Invoke() ?? 
            throw new InvalidOperationException("There is currently no ExecutionContext.Current instance; this must be set (SetCurrent) prior to access. Use ExecutionContext.HasCurrent to verify value and avoid this exception if appropriate.");

        /// <summary>
        /// Resets (clears) the <see cref="Current"/> <see cref="ExecutionContext"/>.
        /// </summary>
        public static void Reset() => _asyncLocal.Value = null;

        /// <summary>
        /// Sets the <see cref="Current"/> instance (only allowed where <see cref="HasCurrent"/> is <c>false</c>).
        /// </summary>
        /// <param name="executionContext">The <see cref="ExecutionContext"/> instance.</param>
        public static void SetCurrent(ExecutionContext? executionContext)
        {
            if (HasCurrent)
                throw new InvalidOperationException("The SetCurrent method can only be used where there is no Current instance.");

            _asyncLocal.Value = executionContext.ThrowIfNull(nameof(executionContext));
        }

        /// <summary>
        /// Gets the service of <see cref="Type"/> <typeparamref name="T"/> from the <see cref="Current"/> <see cref="ServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">The service <see cref="Type"/>.</typeparam>
        /// <returns>The corresponding instance.</returns>
        public static T? GetService<T>()
        {
            if (HasCurrent && Current.ServiceProvider != null)
                return Current.ServiceProvider.GetService<T>();

            return default;
        }

        /// <summary>
        /// Gets the service of <see cref="Type"/> <typeparamref name="T"/> from the <see cref="Current"/> <see cref="ServiceProvider"/> and will throw an <see cref="InvalidOperationException"/> where not found.
        /// </summary>
        /// <typeparam name="T">The service <see cref="Type"/>.</typeparam>
        /// <returns>The corresponding instance.</returns>
        public static T GetRequiredService<T>() where T : notnull
        {
            if (HasCurrent && Current.ServiceProvider != null)
                return Current.ServiceProvider.GetRequiredService<T>();

            throw new InvalidOperationException($"Attempted to get service '{typeof(T).FullName}' but there is either no ExecutionContext.Current or the ExecutionContext.ServiceProvider has not been configured.");
        }

        /// <summary>
        /// Gets the service of <see cref="Type"/> <paramref name="type"/> from the <see cref="Current"/> <see cref="ServiceProvider"/>.
        /// </summary>
        /// <param name="type">The service <see cref="Type"/>.</param>
        /// <returns>The corresponding instance.</returns>
        public static object? GetService(Type type)
        {
            type.ThrowIfNull(nameof(type));
            if (HasCurrent && Current.ServiceProvider != null)
                return Current.ServiceProvider.GetService(type);

            return null;
        }

        /// <summary>
        /// Gets the service of <see cref="Type"/> <paramref name="type"/> from the <see cref="Current"/> <see cref="ServiceProvider"/> and will throw an <see cref="InvalidOperationException"/> where not found.
        /// </summary>
        /// <param name="type">The service <see cref="Type"/>.</param>
        /// <returns>The corresponding instance.</returns>
        public static object GetRequiredService(Type type)
        {
            type.ThrowIfNull(nameof(type));
            if (HasCurrent && Current.ServiceProvider != null)
                return Current.ServiceProvider.GetRequiredService(type);

            throw new InvalidOperationException($"Attempted to get service '{type.FullName}' but there is either no ExecutionContext.Current or the ExecutionContext.ServiceProvider has not been configured.");
        }

        /// <summary>
        /// Gets the <see cref="ISystemTime"/> instance from the <see cref="ServiceProvider"/>; where not found the <see cref="CoreEx.SystemTime.Default"/> will be used.
        /// </summary>
        public static ISystemTime SystemTime => GetService<ISystemTime>() ?? CoreEx.SystemTime.Default;

        /// <summary>
        /// Gets the username from the <see cref="Environment"/> settings.
        /// </summary>
        /// <returns>The fully qualified username.</returns>
        public static string EnvironmentUserName => Environment.UserDomainName == null ? Environment.UserName : Environment.UserDomainName + "\\" + Environment.UserName;

        /// <summary>
        /// Gets the <see cref="ServiceProvider"/>.
        /// </summary>
        /// <remarks>This is automatically set via the <see cref="IServiceCollectionExtensions.AddExecutionContext(IServiceCollection, Func{IServiceProvider, ExecutionContext}?)"/>.</remarks>
        public IServiceProvider? ServiceProvider { get; set; }

        /// <summary>
        /// Gets or sets the correlation identifier.
        /// </summary>
        /// <remarks>Defaults to <see cref="Guid.NewGuid"/>.</remarks>
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString().ToLowerInvariant();

        /// <summary>
        /// Gets or sets the <see cref="OperationType"/>.
        /// </summary>
        public OperationType OperationType { get; set; }

        /// <summary>
        /// Indicates whether text serialization is enabled; see <see cref="HttpConsts.IncludeTextQueryStringName"/>.
        /// </summary>
        public bool IsTextSerializationEnabled { get; set; }

        /// <summary>
        /// Gets or sets the corresponding user name.
        /// </summary>
        public string UserName { get; set; } = EnvironmentUserName;

        /// <summary>
        /// Gets or sets the corresponding user identifier.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp for the <see cref="ExecutionContext"/> lifetime; i.e (to enable consistent execution-related timestamping).
        /// </summary>
        /// <remarks>Defaults the value from <see cref="ISystemTime"/>, where this has not been registered it will default to <see cref="DateTime.UtcNow"/>. The value will also be passed through <see cref="Cleaner.Clean(DateTime)"/>.</remarks>
        public DateTime Timestamp { get => _timestamp ??= SystemTime.UtcNow; set => _timestamp = Cleaner.Clean(value); }

        /// <summary>
        /// Gets the <see cref="MessageItemCollection"/> to be passed back to the originating consumer.
        /// </summary>
        public MessageItemCollection Messages { get => _messages.Value; }

        /// <summary>
        /// Gets the properties <see cref="ConcurrentDictionary{TKey, TValue}"/> for passing/storing additional data.
        /// </summary>
        public ConcurrentDictionary<string, object?> Properties { get => _properties.Value; }

        /// <summary>
        /// Gets the <see cref="IReferenceDataContext"/>.
        /// </summary>
        /// <remarks>Where not configured will instantiate a <see cref="ReferenceDataContext"/>.</remarks>
        public IReferenceDataContext ReferenceDataContext => _referenceDataContext ??= (GetService<IReferenceDataContext>() ?? new ReferenceDataContext());

        /// <summary>
        /// Creates a copy of the <see cref="ExecutionContext"/> using the <see cref="Create"/> function to instantiate before copying all underlying properties.
        /// </summary>
        /// <returns>The new <see cref="ExecutionContext"/> instance.</returns>
        /// <remarks><i>Note:</i> the <see cref="Messages"/>, <see cref="Properties"/> and <see cref="GetRoles">Roles</see> share same instance, i.e. are not copied.</remarks>
        public virtual ExecutionContext CreateCopy()
        {
            var ec = Create == null ? throw new InvalidOperationException($"The {nameof(Create)} function must not be null to create a copy.") : Create();
            ec._timestamp = _timestamp;
            ec._messages = _messages;
            ec._properties = _properties;
            ec._referenceDataContext = _referenceDataContext;
            ec._roles = _roles;
            ec.ServiceProvider = ServiceProvider;
            ec.CorrelationId = CorrelationId;
            ec.OperationType = OperationType;
            ec.IsTextSerializationEnabled = IsTextSerializationEnabled;
            ec.UserName = UserName;
            ec.UserId = UserId;
            ec.TenantId = TenantId;
            return ec;
        }

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_lock)
                {
                    if (!_disposed)
                    {
                        _disposed = true;
                        Dispose(true);
                    }
                }
            }

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="ExecutionContext"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing) => Reset();

        #region Security

        /// <summary>
        /// Gets the list of roles for the <see cref="UserName"/> (as previously <see cref="SetRoles(IEnumerable{string})">set</see>).
        /// </summary>
        public IEnumerable<string> GetRoles() => _roles == null ? Array.Empty<string>() : _roles;

        /// <summary>
        /// Sets (replaces) the roles the current user is in (the roles should be unique).
        /// </summary>
        /// <param name="roles">The <see cref="IEnumerable{String}"/> of roles the user is in.</param>
        public virtual void SetRoles(IEnumerable<string> roles) => _roles = new HashSet<string>(roles);

        /// <summary>
        /// Gets the list of permissions for the <see cref="UserName"/> (as previously <see cref="SetPermissions(IEnumerable{string})">set</see>).
        /// </summary>
        public IEnumerable<string> GetPermissions() => _permissions == null ? Array.Empty<string>() : _permissions;

        /// <summary>
        /// Sets (replaces) the permissions the current user is in (the roles should be unique).
        /// </summary>
        /// <param name="roles">The <see cref="IEnumerable{String}"/> of roles the user is in.</param>
        public virtual void SetPermissions(IEnumerable<string> roles) => _permissions = new HashSet<string>(roles);

        /// <summary>
        /// Checks whether the user has the required <paramref name="permission"/> (see <see cref="SetPermissions"/> and <see cref="GetPermissions"/>).
        /// </summary>
        /// <param name="permission">The permission to validate.</param>
        /// <returns>The corresponding <see cref="Result"/>.</returns>
        public virtual Result UserIsAuthorized(string permission)
        {
            permission.ThrowIfNullOrEmpty(nameof(permission));
            return _permissions is not null && _permissions.Contains(permission) ? Result.Success : Result.AuthorizationError();
        }

        /// <summary>
        /// Checks whether the user has the required permission (as a combination of an <paramref name="entity"/> and <paramref name="action"/>).
        /// </summary>
        /// <param name="entity">The entity name.</param>
        /// <param name="action">The action name.</param>
        /// <returns>The corresponding <see cref="Result"/>.</returns>
        /// <remarks>This default implementation formats as <c>{entity}.{action}</c> and invokes <see cref="UserIsAuthorized(string)"/>.
        /// <para>An example is <c>Customer</c> and <c>Create</c> formatted as <c>Customer.Create</c>.</para></remarks>
        public virtual Result UserIsAuthorized(string entity, string action)
        {
            entity.ThrowIfNullOrEmpty(nameof(entity));
            action.ThrowIfNullOrEmpty(nameof(action));
            return UserIsAuthorized($"{entity}.{action}");
        }

        /// <summary>
        /// Determines whether the user is in the specified role (see <see cref="SetRoles"/> and <see cref="GetRoles"/>).
        /// </summary>
        /// <param name="role">The role name.</param>
        /// <returns>The corresponding <see cref="Result"/>.</returns>
        public virtual Result UserIsInRole(string role)
        {
            role.ThrowIfNullOrEmpty(nameof(role));
            return _roles is not null && _roles.Contains(role) ? Result.Success : Result.AuthorizationError();
        }

        #endregion
    }
}