namespace Contoso.Products.Infrastructure.Repositories;

/// <summary>Combines bounded lexical and semantic candidate lists using Reciprocal Rank Fusion.</summary>
internal static class ProductSearchFusion
{
    /// <summary>The maximum number of candidates retained from each retrieval branch.</summary>
    internal const int CandidateLimit = 100;

    /// <summary>The fixed RRF smoothing constant.</summary>
    internal const int RrfK = 60;

    /// <summary>The maximum size of the deduplicated candidate union.</summary>
    internal const int MaximumCandidateCount = CandidateLimit * 2;

    /// <summary>Materializes the first bounded candidates from an already filtered and deterministically ranked query.</summary>
    /// <param name="orderedCandidates">The branch query after its eligibility and structured filters.</param>
    /// <param name="candidateLimit">The branch candidate limit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The bounded candidate list.</returns>
    internal static Task<List<T>> TakeCandidatesAsync<T>(IQueryable<T> orderedCandidates, int candidateLimit, CancellationToken cancellationToken)
    {
        if (candidateLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(candidateLimit), "The candidate limit must be greater than zero.");

        return orderedCandidates.Take(candidateLimit).ToListAsync(cancellationToken);
    }

    /// <summary>Fuses ordered branch candidates, prioritizing an eligible exact SKU before the combined score.</summary>
    /// <param name="lexical">Lexical candidates ordered by exact-SKU status, rank and product ID.</param>
    /// <param name="semantic">Semantic product IDs ordered by distance and product ID.</param>
    /// <returns>The deduplicated candidates in deterministic fused order.</returns>
    internal static IReadOnlyList<FusedCandidate> Fuse(IReadOnlyList<LexicalCandidate> lexical, IReadOnlyList<string> semantic)
    {
        var candidates = new Dictionary<string, CandidateRanks>(StringComparer.Ordinal);
        for (var i = 0; i < lexical.Count; i++)
        {
            var item = lexical[i];
            if (!candidates.TryGetValue(item.Id, out var ranks))
                candidates.Add(item.Id, ranks = new CandidateRanks(item.Id));

            if (ranks.LexicalRank is null)
            {
                ranks.LexicalRank = i + 1;
                ranks.ExactSku = item.ExactSku;
            }
        }

        for (var i = 0; i < semantic.Count; i++)
        {
            var id = semantic[i];
            if (!candidates.TryGetValue(id, out var ranks))
                candidates.Add(id, ranks = new CandidateRanks(id));

            ranks.SemanticRank ??= i + 1;
        }

        return [.. candidates.Values
            .Select(ranks => new FusedCandidate(
                ranks.Id,
                ranks.LexicalRank,
                ranks.SemanticRank,
                ranks.ExactSku,
                (ranks.LexicalRank is { } lexicalRank ? 1d / (RrfK + lexicalRank) : 0d) +
                (ranks.SemanticRank is { } semanticRank ? 1d / (RrfK + semanticRank) : 0d)))
            .OrderByDescending(candidate => candidate.ExactSku)
            .ThenByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.LexicalRank ?? int.MaxValue)
            .ThenBy(candidate => candidate.SemanticRank ?? int.MaxValue)
            .ThenBy(candidate => candidate.Id, StringComparer.Ordinal)];
    }

    /// <summary>Represents a lexical candidate and whether it is an exact canonical SKU match.</summary>
    /// <param name="Id">The product identifier.</param>
    /// <param name="ExactSku">Whether the complete query matches the product SKU.</param>
    internal sealed record LexicalCandidate(string Id, bool ExactSku);

    /// <summary>Represents a fused candidate and its component rank positions.</summary>
    /// <param name="Id">The product identifier.</param>
    /// <param name="LexicalRank">The one-based lexical rank, when present.</param>
    /// <param name="SemanticRank">The one-based semantic rank, when present.</param>
    /// <param name="ExactSku">Whether the candidate is an eligible exact canonical SKU match.</param>
    /// <param name="Score">The sum of reciprocal rank contributions.</param>
    internal sealed record FusedCandidate(string Id, int? LexicalRank, int? SemanticRank, bool ExactSku, double Score);

    /// <summary>Collects the independently ranked positions for one product.</summary>
    private sealed class CandidateRanks(string id)
    {
        /// <summary>The product identifier.</summary>
        public string Id { get; } = id;

        /// <summary>The lexical rank, if the product appears in that branch.</summary>
        public int? LexicalRank { get; set; }

        /// <summary>The semantic rank, if the product appears in that branch.</summary>
        public int? SemanticRank { get; set; }

        /// <summary>Whether the lexical branch recognized an exact canonical SKU.</summary>
        public bool ExactSku { get; set; }
    }
}
