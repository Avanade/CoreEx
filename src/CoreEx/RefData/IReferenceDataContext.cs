// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.RefData
{
    /// <summary>
    /// Enables the contextual validation <see cref="Date"/> for a <see cref="IReferenceData.StartDate"/> and <see cref="IReferenceData.EndDate"/> <see cref="IReferenceData.IsValid"/> verification.
    /// </summary>
    /// <remarks>This allows for the validatity dates to be adjusted, per requrest or on-demand, to validate or invalidate the reference date where a <see cref="IReferenceData.StartDate"/> and/or
    /// <see cref="IReferenceData.EndDate"/> have been configured. As such, this should be a scoped service from an ASP.NET Core dependency injection (DI) perspective. 
    /// <para>The <see cref="Date"/> is a master setting for all <see cref="IReferenceData"/> <see cref="Type">types</see>. An individual
    /// <see cref="Type"/> can be overridden where required, and all dates can be <see cref="Reset"/>.</para></remarks>
    public interface IReferenceDataContext
    {
        /// <summary>
        /// Gets or sets the <see cref="IReferenceData"/> <see cref="IReferenceData.StartDate"/> and <see cref="IReferenceData.EndDate"/> contextual validation date.
        /// </summary>
        DateTime? Date { get; set; }

        /// <summary>
        /// Gets or sets a contextual validation date for a specific <see cref="IReferenceData"/> <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <returns>The contextual validation date where found.</returns>
        DateTime? this[Type type] { get; set; }

        /// <summary>
        /// Resets all dates.
        /// </summary>
        void Reset();
    }
}