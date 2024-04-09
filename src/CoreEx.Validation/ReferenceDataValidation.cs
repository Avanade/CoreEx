// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;

namespace CoreEx.Validation
{
    /// <summary>
    /// Represents the standard <see cref="IReferenceData"/> validation configuration settings.
    /// </summary>
    public static class ReferenceDataValidation
    {
        /// <summary>
        /// Gets or sets the maximum length for the <see cref="IReferenceData.Code"/>.
        /// </summary>
        public static int MaxCodeLength { get; set; } = 30;

        /// <summary>
        /// Gets or sets the maximum length for the <see cref="IReferenceData.Text"/>.
        /// </summary>
        public static int MaxTextLength { get; set; } = 256;

        /// <summary>
        /// Gets or sets the maximum length for the <see cref="IReferenceData.Description"/>.
        /// </summary>
        public static int MaxDescriptionLength { get; set; } = 1000;

        /// <summary>
        /// Indicates whether the <see cref="IReferenceData.Description"/> is supported.
        /// </summary>
        public static bool SupportsDescription { get; set; } = false;
    }
}