// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.Entities;
using System.Linq;
using UnitTestEx.Assertors;

[assembly: UnitTestEx.Abstractions.OneOffTestSetUp(typeof(UnitTestEx.Abstractions.CoreExOneOffTestSetUp))]

namespace UnitTestEx.Abstractions
{
    /// <summary>
    /// Provides the one-off test set-up for the <see cref="CoreEx"/>-related testing.
    /// </summary>
    /// <remarks>Adds the <see cref="CoreExExtension"/> to support the runtime extension inclusion. Also, changes the <see cref="TestSetUp.Default"/> <see cref="TestSetUp.JsonSerializer"/> to the <see cref="CoreEx.Text.Json.JsonSerializer"/>.</remarks>
    public class CoreExOneOffTestSetUp : OneOffTestSetUpBase
    {
        private static bool _loaded;

        /// <inheritdoc/>
        public override void SetUp()
        {
            if (_loaded)
                return;

            _loaded = true;
            TestSetUp.Extensions.Add(new CoreExExtension());
            TestSetUp.Default.JsonSerializer = new CoreEx.Text.Json.JsonSerializer().ToUnitTestEx();

            // Extend the AssertErrors functionality to support ValidationException.
            AssertorBase.AddAssertErrorsExtension((assertor, errors) =>
            {
                if (assertor.Exception is ValidationException vex)
                {
                    var actual = vex.Messages?.Where(x => x.Type == MessageType.Error).Select(x => new ApiError(x.Property, x.Text ?? string.Empty)).ToArray() ?? [];
                    if (!Assertor.TryAreErrorsMatched(errors, actual, out var errorMessage))
                        assertor.Owner.Implementor.AssertFail(errorMessage);

                    return true;
                }

                return false;
            });
        }
    }
}