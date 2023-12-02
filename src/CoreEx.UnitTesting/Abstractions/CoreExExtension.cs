// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.AspNetCore.WebApis;
using CoreEx.Entities;
using CoreEx.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnitTestEx.Assertors;
using UnitTestEx.Expectations;

namespace UnitTestEx.Abstractions
{
    /// <summary>
    /// Provides the <see cref="CoreEx"/> extension capabilities.
    /// </summary>
    public class CoreExExtension : TesterExtensionsConfig
    {
        /// <inheritdoc/>
        public override void OnUseSetUp(TesterBase owner)
        {
            // Where the TestSetUp has been configured to use ExpectedEvents then automatically use it.
            if (owner.SetUp.IsExpectedEventsEnabled())
                owner.UseExpectedEvents();

            // Override username from ExecutionContext.
            if (ExecutionContext.HasCurrent)
                owner.SetUp.DefaultUserName = ExecutionContext.Current.UserName;
        }

        /// <inheritdoc/>
        /// <remarks>This is a duplication of the underlying <see cref="HttpResult.CreateAsync{T}(HttpResponseMessage, CoreEx.Json.IJsonSerializer?, System.Threading.CancellationToken)"/> logic.</remarks>
        public override void UpdateValueFromHttpResponseMessage<TValue>(TesterBase owner, HttpResponseMessage response, ref TValue? value) where TValue : default
        {
            // This is a duplication of the HttpResult logic.
            if (value != null && value is IETag etag && etag.ETag == null && response.Headers.ETag != null)
                etag.ETag = response.Headers.ETag.Tag;

            // Where the value is an ICollectionResult then update the Paging property from the corresponding response headers.
            if (value is ICollectionResult cr && cr != null && cr.Paging is null)
            {
                if (response.TryGetPagingResult(out var paging))
                    cr.Paging = paging;
            }
        }

        /// <inheritdoc/>
        public override void UpdateValueFromActionResult<TValue>(TesterBase owner, IActionResult actionResult, ref TValue? value) where TValue : default
        {
            if (actionResult is ValueContentResult vcr)
            {
                if (value != null && value is IETag etag && etag.ETag == null && vcr.ETag != null)
                    etag.ETag = vcr.ETag;

                if (value is ICollectionResult cr && cr != null && cr.Paging is null)
                    cr.Paging = vcr.PagingResult;
            }
        }

        /// <inheritdoc/>
        public override Task ExpectationAssertAsync<TTester>(ExpectationsBase<TTester> expectation, AssertArgs args)
        {
            // Where an ErrorExpectations and ValidationException then assert/match the errors/messages.
            if (expectation is ErrorExpectations<TTester> ee)
            {
                if (!ee.ErrorsMatched && ee.Errors.Count > 0 && args.Exception is not null && args.Exception is ValidationException vex)
                {
                    var actual = vex.Messages?.Where(x => x.Type == MessageType.Error).Select(x => new ApiError(x.Property, x.Text ?? string.Empty)).ToArray() ?? [];
                    if (!Assertor.TryAreErrorsMatched(ee.Errors, actual, out var errorMessage))
                        args.Tester.Implementor.AssertFail(errorMessage);

                    ee.ErrorsMatched = true;
                }
            }

            return Task.CompletedTask;
        }
    }
}