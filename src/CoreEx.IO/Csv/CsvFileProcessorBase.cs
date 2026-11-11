namespace CoreEx.IO.Csv;

/// <summary>Provides CSV-specific decoding and parser ownership for flat, routed, and hierarchical processors.</summary>
/// <typeparam name="TUnit">The flat or unordered routed record, or projected root group.</typeparam>
public abstract class CsvFileProcessorBase<TUnit> : FileProcessor<TUnit> where TUnit : class
{
    private readonly CsvFileOptions _options;

    /// <summary>Initializes CSV syntax, common processing, and optional scoped business handlers.</summary>
    /// <param name="options">CSV syntax and explicit decoding.</param>
    /// <param name="validator">A pure unit validator; dependency-backed validation belongs in the scoped handler.</param>
    /// <param name="serviceProvider">The provider from which unit scopes are created.</param>
    /// <param name="handlerFactory">Resolves the unit handler after trusted context configuration.</param>
    private protected CsvFileProcessorBase(CsvFileOptions? options, IValidator<TUnit>? validator, IServiceProvider? serviceProvider, Func<IServiceProvider, FileUnitHandler<TUnit>>? handlerFactory)
        : base(validator, serviceProvider, handlerFactory)
    {
        options ??= new CsvFileOptions();
        if (new[] { options.Delimiter, options.Quote }.Concat(options.Comment.HasValue ? [options.Comment.Value] : Array.Empty<char>()).Any(c => c is '\r' or '\n' or '\0' || char.IsSurrogate(c))
            || options.Delimiter == options.Quote || options.Comment == options.Delimiter || options.Comment == options.Quote)
            throw new ArgumentException("CSV syntax characters must be distinct non-surrogate characters other than newline or null.", nameof(options));

        _options = options with { Encoding = (Encoding)options.Encoding.ThrowIfNull().Clone() };
        _options.Encoding.DecoderFallback = DecoderFallback.ExceptionFallback;
    }

    /// <summary>Owns one asynchronous input and parser, exposing only the current buffered record.</summary>
    /// <param name="openReadAsync">Opens a fresh source stream for the pass.</param>
    /// <param name="state">The shared progress and diagnostic state.</param>
    /// <param name="execute">Whether this is the execution replay pass.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The parser's records and any terminal encoding diagnostic.</returns>
    private protected async IAsyncEnumerable<CsvParsedRecord> ReadRecordsAsync(Func<CancellationToken, Task<Stream>> openReadAsync, ProcessingState state, bool execute,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var input = await openReadAsync(cancellationToken).ConfigureAwait(false);
        if (input is null)
            throw new InvalidOperationException("The stream factory returned null.");

        await using var ownedInput = input;
        if (!input.CanRead)
            throw new InvalidOperationException("The stream factory must return a readable stream at its beginning.");

        using var countingInput = new CsvReadStream(input, count =>
        {
            if (!execute)
            {
                state.Bytes = checked(state.Bytes + count);
                state.Update();
            }
        });

        using var text = new StreamReader(countingInput, _options.Encoding, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        FileDiagnostic? syntaxError = null;

        await using var parser = CsvReader.Create(text, new CsvReaderOptions
        {
            HasHeader = false,
            Separator = _options.Delimiter,
            Quote = _options.Quote,
            CommentPrefix = _options.Comment,
            TrackLineNumbers = true,
            DetectSeparatorHint = false,
            IncludeValuesInExceptions = false,
            MalformedData = CsvMalformedDataHandling.Recover,
            OnMalformedRecord = error => syntaxError = new(FileDiagnosticCategory.Parsing, "csv.record.invalid", "CSV syntax could not be parsed.", location: new(error.LineNumber))
        }, leaveOpen: true);

        while (true)
        {
            var read = false;
            FileDiagnostic? encodingDiagnostic = null;
            try
            {
                read = await parser.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DecoderFallbackException)
            {
                encodingDiagnostic = new(FileDiagnosticCategory.Parsing, "csv.encoding.invalid", "Input bytes are invalid for the configured encoding.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (encodingDiagnostic is not null)
            {
                yield return new(parser, encodingDiagnostic, true);
                yield break;
            }

            if (!read)
                yield break;

            yield return new(parser, syntaxError);
            syntaxError = null;
        }
    }
}
