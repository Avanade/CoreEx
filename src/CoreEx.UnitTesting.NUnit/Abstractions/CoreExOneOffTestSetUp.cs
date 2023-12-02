// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Reflection;
using UnitTestEx.Abstractions;

[assembly: OneOffTestSetUp(typeof(UnitTestEx.NUnit.Abstractions.CoreExOneOffTestSetUp))]

namespace UnitTestEx.NUnit.Abstractions
{
    /// <summary>
    /// Provides the one-off test set-up for the <see cref="CoreEx"/>-related testing.
    /// </summary>
    /// <remarks>Adds the <see cref="CoreExExtension"/> to support the runtime extension inclusion. Also, changes the <see cref="TestSetUp.Default"/> <see cref="TestSetUp.JsonSerializer"/> to the <see cref="CoreEx.Text.Json.JsonSerializer"/>.
    /// <para>This inheits the <see cref="UnitTestEx.Abstractions.CoreExOneOffTestSetUp"/> achieving the same functionality, but is delared within this <see cref="Assembly"/> to ensure executed.</para></remarks>
    public class CoreExOneOffTestSetUp : UnitTestEx.Abstractions.CoreExOneOffTestSetUp { }
}