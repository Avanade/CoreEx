// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.AspNetCore.WebApis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides <see cref="ReferenceDataOrchestrator"/> extensions.
    /// </summary>
    public static class ReferenceDataOrchestratorExtensions
    {
        /// <summary>
        /// Gets the reference data items for the specified names and related codes (see <see cref="IReferenceData.Code"/>) from the <paramref name="requestOptions"/>.
        /// </summary>
        /// <param name="orchestrator">The <see cref="ReferenceDataOrchestrator"/>.</param>
        /// <param name="requestOptions">The <see cref="WebApiRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="ReferenceDataMultiDictionary"/>.</returns>
        /// <remarks>The reference data names and codes are specified as part of the query string. Either '<c>?names=RefA,RefB,RefX</c>' or <c>?RefA,RefB=CodeA,CodeB,RefX=CodeX</c> or any combination thereof.</remarks>
        public static Task<ReferenceDataMultiDictionary> GetNamedAsync(this ReferenceDataOrchestrator orchestrator, WebApiRequestOptions requestOptions, CancellationToken cancellationToken = default)
        {
            var dict = new Dictionary<string, List<string>>();

            foreach (var q in requestOptions.Request.Query.Where(x => !string.IsNullOrEmpty(x.Key)))
            {
                if (string.Compare(q.Key, "names", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    foreach (var v in SplitStringValues(q.Value.Where(x => !string.IsNullOrEmpty(x)).Distinct()!))
                    {
                        dict.TryAdd(v, []);
                    }
                }
                else
                {
                    if (dict.TryGetValue(q.Key, out var codes))
                    {
                        foreach (var code in SplitStringValues(q.Value.Distinct()!))
                        {
                            if (!codes.Contains(code))
                                codes.Add(code);
                        }
                    }
                    else
                        dict.Add(q.Key, new List<string>(SplitStringValues(q.Value.Distinct()!)));
                }
            }

            return orchestrator.GetNamedAsync(dict.ToList(), requestOptions.IncludeInactive, cancellationToken);
        }

        /// <summary>
        /// Perform a further split of the string values.
        /// </summary>
        private static IEnumerable<string> SplitStringValues(IEnumerable<string> values)
        {
            var list = new List<string>();
            foreach (var value in values)
            {
                list.AddRange(value.Split(',', StringSplitOptions.RemoveEmptyEntries));
            }

            return list;
        }
    }
}