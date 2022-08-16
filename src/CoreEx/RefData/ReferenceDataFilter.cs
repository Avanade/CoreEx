// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Entities.Extended;
using System.Collections.Generic;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides the <see cref="IReferenceData"/> filter properties specifically for HTTP Agent usage; see <see cref="ReferenceDataOrchestrator.GetWithFilterAsync(string, IEnumerable{string}?, string?, bool, System.Threading.CancellationToken)"/>
    /// </summary>
    public class ReferenceDataFilter : EntityBase
    {
        private IEnumerable<string>? _codes;
        private string? _text;

        /// <summary>
        /// Gets or sets the list of codes.
        /// </summary>
        public IEnumerable<string>? Codes { get => _codes; set => SetValue(ref _codes, value); }

        /// <summary>
        /// Gets or sets the text (including wildcards).
        /// </summary>
        public string? Text { get => _text; set => SetValue(ref _text, value, StringTrim.Both ); }

        /// <inheritdoc/>
        protected override IEnumerable<IPropertyValue> GetPropertyValues()
        {
            yield return CreateProperty(Codes, v => Codes = v);
            yield return CreateProperty(Text, v => Text = v);
        }
    }
}