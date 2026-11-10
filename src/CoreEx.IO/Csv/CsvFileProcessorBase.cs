namespace CoreEx.IO.Csv;

/// <summary>Provides CSV-specific decoding and parser ownership for flat, routed, and hierarchical processors.</summary>
/// <typeparam name="TUnit">The flat or unordered routed record, or projected root group.</typeparam>
public abstract class CsvFileProcessorBase<TUnit> : FileProcessor<TUnit> where TUnit : class
{
    private readonly CsvFileOptions _options;

    /// <summary>Initializes CSV syntax, common processing, and optional scoped business handlers.</summary>
    /// <param name="processingOptions">Format-neutral processing budgets.</param>
    /// <param name="options">CSV syntax, explicit decoding, and hierarchy record limits.</param>
    /// <param name="validator">A pure unit validator; dependency-backed validation belongs in the scoped handler.</param>
    /// <param name="serviceProvider">The provider from which unit scopes are created.</param>
    /// <param name="handlerFactory">Resolves the unit handler after trusted context configuration.</param>
    protected CsvFileProcessorBase(FileProcessingOptions? processingOptions, CsvFileOptions? options, IValidator<TUnit>? validator, IServiceProvider? serviceProvider, Func<IServiceProvider, FileUnitHandler<TUnit>>? handlerFactory)
        : base(processingOptions, validator, serviceProvider, handlerFactory)
    {
        options ??= new CsvFileOptions();
        options.MaxGroupRecords.ThrowIfLessThanOrEqualToZero();
        if (new[] { options.Delimiter, options.Quote }.Concat(options.Comment.HasValue ? [options.Comment.Value] : Array.Empty<char>()).Any(c => c is '\r' or '\n' or '\0' || char.IsSurrogate(c))
            || options.Delimiter == options.Quote || options.Comment == options.Delimiter || options.Comment == options.Quote)
            throw new ArgumentException("CSV syntax characters must be distinct non-surrogate characters other than newline or null.", nameof(options));

        _options = options with { Encoding = (Encoding)options.Encoding.ThrowIfNull().Clone() };
        _options.Encoding.DecoderFallback = DecoderFallback.ExceptionFallback;
    }

    /// <summary>Gets the maximum parser records retained or inspected in one root group.</summary>
    private protected long MaxGroupRecords => _options.MaxGroupRecords;

    /// <summary>Owns one asynchronous input and parser, exposing only the current buffered record.</summary>
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
        }, MaxInputBytes);

        var encodingDiagnostic = await countingInput.InitializeAsync(_options.Encoding, cancellationToken).ConfigureAwait(false);
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
            if (encodingDiagnostic is null && !countingInput.LimitExceeded)
            {
                try
                {
                    read = await parser.ReadAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (DecoderFallbackException)
                {
                    encodingDiagnostic = new(FileDiagnosticCategory.Parsing, "csv.encoding.invalid", "Input bytes are invalid for the configured encoding.");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (countingInput.LimitExceeded || encodingDiagnostic is not null)
            {
                var diagnostic = countingInput.LimitExceeded
                    ? new FileDiagnostic(FileDiagnosticCategory.Limit, "file.limit.input-bytes", "The input exceeds its configured byte limit.")
                    : encodingDiagnostic;

                yield return new(parser, diagnostic, true);
                yield break;
            }

            if (!read)
                yield break;

            yield return new(parser, syntaxError);
            syntaxError = null;
        }
    }
}
