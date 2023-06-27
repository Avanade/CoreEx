// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CoreEx.Abstractions
{
    /// <summary>
    /// Provides utility functionality for embedded resources. 
    /// </summary>
    public static class Resource
    {
        /// <summary>
        /// Gets the named embedded resource <see cref="Stream"/> from the specified <paramref name="assembly"/>.
        /// </summary>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name).</param>
        /// <param name="assembly">The <see cref="Assembly"/> that contains the embedded resource; defaults to <see cref="Assembly.GetCallingAssembly"/>.</param>
        /// <returns>The <see cref="Stream"/>; otherwise, an <see cref="ArgumentException"/> will be thrown.</returns>
        public static Stream GetStream(string resourceName, Assembly? assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            var coll = assembly.GetManifestResourceNames().Where(x => x.EndsWith(resourceName, StringComparison.InvariantCultureIgnoreCase));
            return coll.Count() switch
            {
                0 => throw new ArgumentException($"No embedded resource ending with '{resourceName}' was found in {assembly.FullName}.", nameof(resourceName)),
                1 => assembly.GetManifestResourceStream(coll.First())!,
                _ => GetPrefixedStream(resourceName, assembly)
            };
        }

        /// <summary>
        /// Multiple resources found so try prefixed stream as a fallback before failing.
        /// </summary>
        private static Stream GetPrefixedStream(string resourceName, Assembly assembly)
        {
            if (resourceName.Length == 0 || resourceName[0] == '.')
                throw new ArgumentException($"More than one embedded resource ending with '{resourceName}' was found in {assembly.FullName}.", nameof(resourceName));

            return GetStream("." + resourceName, assembly);
        }

        /// <summary>
        /// Gets the named embedded resource <see cref="Stream"/> from the <see name="Assembly"/> inferred from the <typeparamref name="TResource"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TResource">The <see cref="Type"/> to infer the <see cref="Assembly"/> to find manifest resources (see <see cref="Assembly.GetManifestResourceStream(string)"/>).</typeparam>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name).</param>
        /// <returns>The <see cref="Stream"/>; otherwise, an <see cref="ArgumentException"/> will be thrown.</returns>
        public static Stream GetStream<TResource>(string resourceName) => GetStream(resourceName, typeof(TResource).Assembly);

        /// <summary>
        /// Gets the named embedded resource <see cref="StreamReader"/> from the specified <paramref name="assembly"/>.
        /// </summary>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name).</param>
        /// <param name="assembly">The <see cref="Assembly"/> that contains the embedded resource; defaults to <see cref="Assembly.GetCallingAssembly"/>.</param>
        /// <returns>The <see cref="StreamReader"/>; otherwise, an <see cref="ArgumentException"/> will be thrown.</returns>
        public static StreamReader GetStreamReader(string resourceName, Assembly? assembly = null) => new(GetStream(resourceName, assembly ?? Assembly.GetCallingAssembly()));

        /// <summary>
        /// Gets the named embedded resource <see cref="StreamReader"/> from the <see name="Assembly"/> inferred from the <typeparamref name="TResource"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TResource">The <see cref="Type"/> to infer the <see cref="Assembly"/> to find manifest resources (see <see cref="Assembly.GetManifestResourceStream(string)"/>).</typeparam>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name).</param>
        /// <returns>The <see cref="StreamReader"/>; otherwise, an <see cref="ArgumentException"/> will be thrown.</returns>
        public static StreamReader GetStreamReader<TResource>(string resourceName) => GetStreamReader(resourceName, typeof(TResource).Assembly);
    }
}