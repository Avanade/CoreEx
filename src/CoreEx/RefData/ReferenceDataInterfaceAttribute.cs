// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.RefData
{
    /// <summary>
    /// To support the dependency injection capabilities, specifically <see cref="ReferenceDataManagerX.GetByType(Type)"/>, the relationship between a reference data class and the interface that it will be exposed through is required.
    /// This provides this relationship.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class ReferenceDataInterfaceAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataInterfaceAttribute"/> class.
        /// </summary>
        /// <param name="interfaceType">The owning interface <see cref="Type"/>.</param>
        public ReferenceDataInterfaceAttribute(Type interfaceType) => InterfaceType = interfaceType ?? throw new ArgumentNullException(nameof(interfaceType));

        /// <summary>
        /// Gets the owning interface <see cref="Type"/>.
        /// </summary>
        public Type InterfaceType { get; }
    }
}