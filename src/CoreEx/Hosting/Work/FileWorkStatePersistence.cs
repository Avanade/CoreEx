// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Json;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Hosting.Work
{
    /// <summary>
    /// An <see cref="IWorkStatePersistence"/> that persists the <see cref="WorkState"/> (as JSON) to a file and the related <see cref="BinaryData"/> to a separate file.
    /// </summary>
    public class FileWorkStatePersistence : IWorkStatePersistence
    {
        private static readonly Regex _invalidFileNameChars = new($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]", RegexOptions.Compiled);

        private readonly string _path;
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// Gets the configuration key that defines the directory path for the work persistence files.
        /// </summary>
        public const string ConfigKey = "FileWorkPersistencePath";

        /// <summary>
        /// Initializes a new instance of the <see cref="FileWorkStatePersistence"/> class.
        /// </summary>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="JsonSerializer.Default"/>.</param>
        public FileWorkStatePersistence(SettingsBase settings, IJsonSerializer? jsonSerializer = null)
        {
            _path = settings.ThrowIfNull(nameof(settings)).GetValue<string>(ConfigKey);

            if (string.IsNullOrEmpty(_path))
                throw new ArgumentException($"Configuration setting '{ConfigKey}' either does not exist or has no value.", nameof(settings));

            if (!Directory.Exists(_path))
                throw new ArgumentException($"Configuration setting '{ConfigKey}' path does not exist: {_path}");

            _jsonSerializer = jsonSerializer ?? JsonSerializer.Default;
        }

        /// <summary>
        /// Gets the <see cref="WorkState"/> <see cref="FileInfo"/>.
        /// </summary>
        /// <param name="id">The <see cref="WorkState.Id"/>.</param>
        /// <returns>The <see cref="FileInfo"/>.</returns>
        public FileInfo GetStateFileInfo(string id) => new(Path.Combine(_path, $"{GetSanitizedName(id)}.json"));

        /// <summary>
        /// Gets the <see cref="WorkState"/> data <see cref="FileInfo"/>.
        /// </summary>
        /// <param name="id">The <see cref="WorkState.Id"/>.</param>
        /// <returns>The <see cref="FileInfo"/>.</returns>
        public FileInfo GetDataFileInfo(string id) => new(Path.Combine(_path, $"{GetSanitizedName(id)}.data"));

        /// <summary>
        /// Gets a sanitized file name by replacing any invalid characters with an underscore.
        /// </summary>
        private static string GetSanitizedName(string name) => _invalidFileNameChars.Replace(name, "_");

        /// <inheritdoc/>
        public async Task<WorkState?> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            var fi = GetStateFileInfo(id);
            if (!fi.Exists)
                return default!;

            using var stream = fi.OpenRead();
            var json = await BinaryData.FromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
            return _jsonSerializer.Deserialize<WorkState>(json);
        }

        /// <inheritdoc/>
        public Task CreateAsync(WorkState state, CancellationToken cancellationToken = default) => ReplaceAsync(state, true, cancellationToken);

        /// <inheritdoc/>
        public Task UpdateAsync(WorkState state, CancellationToken cancellationToken = default) => ReplaceAsync(state, false, cancellationToken);

        /// <summary>
        /// Replace the <see cref="WorkState"/> file.
        /// </summary>
        private async Task ReplaceAsync(WorkState state, bool isCreate, CancellationToken cancellationToken = default)
        {
            var fi = GetStateFileInfo(state.Id.ThrowIfNull());
            if (isCreate && fi.Exists)
                throw new ArgumentException("Create can not be performed as the WorkState already exists; the type and identifier combination should be unique.", nameof(state));

            if (fi.Directory is not null && !fi.Directory.Exists)
                fi.Directory.Create();

            using var stream = fi.Open(fi.Exists ? FileMode.Truncate : FileMode.Create);
            var json = _jsonSerializer.SerializeToBinaryData(state);
            await stream.WriteAsync(json, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var fi = GetStateFileInfo(id);
            if (fi.Exists)
                fi.Delete();

            fi = GetDataFileInfo(id);
            if (fi.Exists)
                fi.Delete();

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<BinaryData?> GetDataAsync(string id, CancellationToken cancellationToken)
        {
            var fi = GetDataFileInfo(id);
            if (!fi.Exists)
                return null;

            using var fs = fi.OpenRead();
            return await BinaryData.FromStreamAsync(fs, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task SetDataAsync(string id, BinaryData data, CancellationToken cancellationToken)
        {
            var fi = GetDataFileInfo(id);
            using var fs = fi.Open(fi.Exists ? FileMode.Truncate : FileMode.Create);
            await data.ToStream().CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        }
    }
}