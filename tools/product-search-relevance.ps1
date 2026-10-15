[CmdletBinding()]
param(
    [uri] $BaseAddress = 'http://localhost:5207/',
    [uri] $OllamaAddress = 'http://localhost:11434/',
    [string] $DatasetPath = "$PSScriptRoot\product-search-relevance.dataset.json",
    [string] $OutputPath,
    [ValidateSet('semantic', 'fulltext')][string] $Approach = 'semantic',
    [ValidateSet('all', 'tuning', 'held-out')][string] $Split = 'all',
    [string] $BaselinePath
)

$ErrorActionPreference = 'Stop'

function Get-ProductSearchDataset {
    param([Parameter(Mandatory)][string] $Path)
    if (!(Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Relevance dataset not found: $Path" }
    try { return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json }
    catch { throw "Could not parse relevance dataset '$Path': $($_.Exception.Message)" }
}

function Test-ProductSearchDataset {
    param([Parameter(Mandatory)][object] $Dataset)

    if ($Dataset.datasetVersion -ne 'product-search-relevance-v1') { throw 'Unsupported product-search relevance dataset version.' }
    if ($Dataset.profile -ne 'ollama:nomic-embed-text:v1.5:768:retrieval-prefix-v1' -or
        $Dataset.model -ne 'nomic-embed-text:v1.5' -or
        $Dataset.modelDigest -ne '0a109f422b47e3a30ba2b10eca18548e944e8a23073ee3f3e947efcf3c45e59f' -or
        [int]$Dataset.dimensions -ne 768 -or
        $Dataset.documentRecipe -ne 'product-document-v2' -or
        $Dataset.documentInstruction -ne 'search_document: ' -or
        $Dataset.queryInstruction -ne 'search_query: ') {
        throw 'Dataset model, profile or document/query recipe does not match the pinned Products implementation.'
    }
    if ($Dataset.catalogReviewed -ne $true -or [string]::IsNullOrWhiteSpace($Dataset.catalogEligibility)) {
        throw 'The eligible seeded catalog must be explicitly reviewed and described.'
    }
    if (@($Dataset.queries).Count -lt 20 -or @($Dataset.queries).Count -gt 30) {
        throw 'The curated evaluation must contain 20 to 30 queries.'
    }
    if (@($Dataset.catalog).Count -eq 0) { throw 'The reviewed catalog snapshot is empty.' }

    $catalogSkus = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($product in $Dataset.catalog) {
        if ([string]::IsNullOrWhiteSpace($product.sku) -or
            [string]::IsNullOrWhiteSpace($product.text) -or
            $null -eq $product.price -or
            [string]::IsNullOrWhiteSpace($product.description)) {
            throw 'Every catalog snapshot entry requires a SKU, text, price and description.'
        }
        if (!$catalogSkus.Add([string]$product.sku)) { throw "Duplicate catalog SKU '$($product.sku)'." }
    }

    $queryIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($query in $Dataset.queries) {
        if ([string]::IsNullOrWhiteSpace($query.id) -or !$queryIds.Add([string]$query.id)) { throw "Missing or duplicate query ID '$($query.id)'." }
        if ([string]::IsNullOrWhiteSpace($query.query) -or [string]::IsNullOrWhiteSpace($query.intent)) { throw "Query '$($query.id)' requires query text and intent." }
        if ($query.split -notin @('tuning', 'held-out')) { throw "Query '$($query.id)' has an invalid split." }
        if ($query.kind -notin @('exact-sku', 'terminology', 'paraphrase', 'broad', 'price-constraint', 'out-of-catalog')) {
            throw "Query '$($query.id)' has an unsupported query kind."
        }
        if ($query.catalogReviewed -ne $true -or [int]$query.judgedCatalogCount -ne $catalogSkus.Count -or [int]$query.omittedSkuGrade -ne 0) {
            throw "Query '$($query.id)' must attest that all catalog entries were judged and omitted SKUs mean grade zero."
        }
        if ($query.kind -eq 'exact-sku') {
            if ([string]::IsNullOrWhiteSpace($query.expectedTop1Sku) -or !$catalogSkus.Contains([string]$query.expectedTop1Sku)) {
                throw "Exact-SKU query '$($query.id)' must name a known expected top-1 SKU."
            }
        }
        elseif ($null -ne $query.expectedTop1Sku) { throw "Only exact-SKU queries may specify expectedTop1Sku ('$($query.id)')." }

        if ($null -ne $query.constraints) {
            if ($null -eq $query.constraints.price -or $query.constraints.price.operator -notin @('eq', 'lt', 'le', 'gt', 'ge') -or $null -eq $query.constraints.price.value) {
                throw "Query '$($query.id)' has an unsupported structured constraint."
            }
            [decimal]$constraintValue = 0
            if (![decimal]::TryParse([string]$query.constraints.price.value, [Globalization.NumberStyles]::Number, [Globalization.CultureInfo]::InvariantCulture, [ref]$constraintValue)) {
                throw "Query '$($query.id)' has an invalid price constraint value."
            }
        }

        $judgedSkus = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($judgment in $query.judgments) {
            if (!$catalogSkus.Contains([string]$judgment.sku)) { throw "Query '$($query.id)' judges unknown SKU '$($judgment.sku)'." }
            if (!$judgedSkus.Add([string]$judgment.sku)) { throw "Query '$($query.id)' judges SKU '$($judgment.sku)' more than once." }
            if ([int]$judgment.grade -lt 1 -or [int]$judgment.grade -gt 3 -or [string]::IsNullOrWhiteSpace($judgment.rationale)) {
                throw "Query '$($query.id)' has an invalid grade or missing rationale for '$($judgment.sku)'."
            }
            $product = $Dataset.catalog | Where-Object sku -EQ $judgment.sku | Select-Object -First 1
            if (!(Test-ProductSearchConstraint -Product $product -Query $query)) {
                throw "Query '$($query.id)' judges SKU '$($judgment.sku)' even though it violates the query constraints."
            }
        }
        if ($query.kind -eq 'exact-sku' -and
            !(@($query.judgments | Where-Object { $_.sku -eq $query.expectedTop1Sku -and [int]$_.grade -eq 3 }).Count -eq 1)) {
            throw "Exact-SKU query '$($query.id)' must grade its expected product a direct fit."
        }
    }
    if (!(@($Dataset.queries | Where-Object split -EQ 'tuning').Count -gt 0) -or
        !(@($Dataset.queries | Where-Object split -EQ 'held-out').Count -gt 0)) {
        throw 'Both tuning and held-out queries are required.'
    }
    return $true
}

function Test-ProductSearchImplementationIdentity {
    param([Parameter(Mandatory)][object] $Dataset)
    $clientPath = Join-Path $PSScriptRoot '..\samples\src\Contoso.Products.Infrastructure\Clients\Ollama\OllamaHttpClient.cs'
    $documentPath = Join-Path $PSScriptRoot '..\samples\src\Contoso.Products.Application\Search\ProductSearchDocument.cs'
    if (!(Test-Path -LiteralPath $clientPath -PathType Leaf) -or !(Test-Path -LiteralPath $documentPath -PathType Leaf)) {
        throw 'Could not verify the checked-in Products embedding and document implementation.'
    }
    $client = Get-Content -LiteralPath $clientPath -Raw
    $document = Get-Content -LiteralPath $documentPath -Raw
    if ($client -notmatch 'ModelName\s*=\s*"([^"]+)"' -or $Matches[1] -ne $Dataset.model) {
        throw 'Dataset model does not match the checked-in Ollama client.'
    }
    if ($client -notmatch 'Dimensions\s*=\s*(\d+)' -or [int]$Matches[1] -ne [int]$Dataset.dimensions) {
        throw 'Dataset dimensions do not match the checked-in Ollama client.'
    }
    if ($client -notmatch 'Profile\s*=\s*"([^"]+)"' -or $Matches[1] -ne $Dataset.profile) {
        throw 'Dataset profile does not match the checked-in Ollama client.'
    }
    if ($document -notmatch 'TemplateVersion\s*=\s*"([^"]+)"' -or $Matches[1] -ne $Dataset.documentRecipe) {
        throw 'Dataset document recipe does not match the checked-in canonical document.'
    }
    if (!$client.Contains("EmbedDocumentAsync(string text, CancellationToken ct = default) => EmbedAsync(text, `"$($Dataset.documentInstruction)`"") -or
        !$client.Contains("EmbedQueryAsync(string text, CancellationToken ct = default) => EmbedAsync(text, `"$($Dataset.queryInstruction)`"")) {
        throw 'Dataset query/document instructions do not match the checked-in Ollama client.'
    }
    return [pscustomobject]@{
        clientSha256 = (Get-FileHash -LiteralPath $clientPath -Algorithm SHA256).Hash
        documentBuilderSha256 = (Get-FileHash -LiteralPath $documentPath -Algorithm SHA256).Hash
    }
}

function Get-ProductSearchCatalogSignature {
    param([Parameter(Mandatory)][object[]] $Products)
    $lines = foreach ($product in $Products) {
        $sku = ([string]$product.sku).Trim()
        $text = ([string]$product.text).Trim()
        $description = ([string]$product.description).Trim()
        $price = ([decimal]$product.price).ToString('0.############################', [Globalization.CultureInfo]::InvariantCulture)
        "$sku|$text|$price|$description"
    }
    [string[]]$ordered = @($lines)
    [Array]::Sort($ordered, [StringComparer]::Ordinal)
    $bytes = [Text.Encoding]::UTF8.GetBytes([string]::Join("`n", $ordered))
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))
}

function Get-ProductSearchEligibleCatalog {
    param([Parameter(Mandatory)][object] $Dataset, [Parameter(Mandatory)][object] $Query)
    $products = @($Dataset.catalog)
    if ($null -eq $Query.constraints -or $null -eq $Query.constraints.price) { return ,$products }
    $constraint = $Query.constraints.price
    [decimal]$limit = $constraint.value
    return ,@($products | Where-Object {
        [decimal]$price = $_.price
        switch ($constraint.operator) {
            'eq' { $price -eq $limit }
            'lt' { $price -lt $limit }
            'le' { $price -le $limit }
            'gt' { $price -gt $limit }
            'ge' { $price -ge $limit }
        }
    })
}

function Get-ProductSearchFilter {
    param([Parameter(Mandatory)][object] $Query)
    if ($null -eq $Query.constraints -or $null -eq $Query.constraints.price) { return $null }
    $constraint = $Query.constraints.price
    $value = ([decimal]$constraint.value).ToString('0.############################', [Globalization.CultureInfo]::InvariantCulture)
    return "price $($constraint.operator) $value"
}

function Get-ProductSearchMetrics {
    param([int[]] $RankedGrades, [int[]] $IdealGrades)
    $relevant = @($IdealGrades | Where-Object { $_ -gt 0 }).Count
    if ($relevant -eq 0) {
        return [pscustomobject]@{ HasRelevant = $false; NdcgAt5 = $null; PrecisionAt5 = $null }
    }

    $dcg = 0.0
    for ($i = 0; $i -lt [Math]::Min(5, $RankedGrades.Count); $i++) {
        $dcg += ([Math]::Pow(2, $RankedGrades[$i]) - 1) / [Math]::Log($i + 2, 2)
    }
    $idealDcg = 0.0
    $idealTop = @($IdealGrades | Sort-Object -Descending | Select-Object -First 5)
    for ($i = 0; $i -lt $idealTop.Count; $i++) {
        $idealDcg += ([Math]::Pow(2, $idealTop[$i]) - 1) / [Math]::Log($i + 2, 2)
    }
    $precision = @($RankedGrades | Select-Object -First 5 | Where-Object { $_ -ge 2 }).Count / 5.0
    return [pscustomobject]@{ HasRelevant = $true; NdcgAt5 = $dcg / $idealDcg; PrecisionAt5 = $precision }
}

function Get-ProductSearchResponse {
    param([Parameter(Mandatory)][scriptblock] $Request, [Parameter(Mandatory)][uri] $Uri)
    try { $response = & $Request $Uri }
    catch { throw "HTTP request failed for '$Uri': $($_.Exception.Message)" }
    if ($null -eq $response -or [int]$response.StatusCode -lt 200 -or [int]$response.StatusCode -ge 300) {
        $status = if ($null -eq $response) { 'no response' } else { [string]$response.StatusCode }
        throw "HTTP request to '$Uri' returned $status."
    }
    try { $data = ConvertFrom-Json -InputObject ([string]$response.Content) -NoEnumerate }
    catch { throw "Invalid JSON returned by '$Uri': $($_.Exception.Message)" }
    return [pscustomobject]@{ Data = $data; Headers = $response.Headers }
}

function Get-ProductSearchCountHeader {
    param([Parameter(Mandatory)][object] $Headers)
    foreach ($key in $Headers.Keys) {
        if ([string]$key -ieq 'X-Paging-Total-Count') {
            [int]$count = 0
            if (![int]::TryParse([string]$Headers[$key], [ref]$count)) { throw 'Products API returned an invalid X-Paging-Total-Count header.' }
            return $count
        }
    }
    foreach ($key in $Headers.AllKeys) {
        if ([string]$key -ieq 'X-Paging-Total-Count') {
            [int]$count = 0
            if (![int]::TryParse([string]$Headers[$key], [ref]$count)) { throw 'Products API returned an invalid X-Paging-Total-Count header.' }
            return $count
        }
    }
    throw 'Products API did not return X-Paging-Total-Count; cannot prove catalog and index completeness.'
}

function Test-ProductSearchConstraint {
    param([Parameter(Mandatory)][object] $Product, [Parameter(Mandatory)][object] $Query)
    if ($null -eq $Query.constraints -or $null -eq $Query.constraints.price) { return $true }
    if ($null -eq $Product.price) { return $false }
    [decimal]$price = $Product.price
    [decimal]$limit = $Query.constraints.price.value
    switch ($Query.constraints.price.operator) {
        'eq' { return $price -eq $limit }
        'lt' { return $price -lt $limit }
        'le' { return $price -le $limit }
        'gt' { return $price -gt $limit }
        'ge' { return $price -ge $limit }
    }
    return $false
}

function Invoke-ProductSearchRelevanceEvaluation {
    param(
        [Parameter(Mandatory)][object] $Dataset,
        [Parameter(Mandatory)][uri] $BaseAddress,
        [Parameter(Mandatory)][uri] $OllamaAddress,
        [Parameter(Mandatory)][scriptblock] $Request,
        [string] $DatasetSha256,
        [ValidateSet('semantic', 'fulltext')][string] $Approach = 'semantic',
        [ValidateSet('all', 'tuning', 'held-out')][string] $Split = 'all',
        [string] $BaselinePath
    )
    if ($Approach -eq 'fulltext' -and $Split -ne 'tuning') {
        throw 'This lexical milestone is tuning-only. Pass -Split tuning; held-out queries must not execute during tuning.'
    }
    if ($BaselinePath -and $Split -ne 'tuning') { throw 'Baseline comparison is tuning-only.' }
    $selectedQueries = @($Dataset.queries | Where-Object { $Split -eq 'all' -or $_.split -eq $Split })
    $requestedSplit = $Split
    if (!$selectedQueries.Count) { throw "No queries selected for split '$Split'." }
    $implementationIdentity = Test-ProductSearchImplementationIdentity $Dataset
    $catalogSignature = Get-ProductSearchCatalogSignature -Products @($Dataset.catalog)
    $seedPath = Join-Path $PSScriptRoot ("..\{0}" -f ($Dataset.seedFile -replace '/', '\'))
    if (!(Test-Path -LiteralPath $seedPath -PathType Leaf)) { throw "Pinned seed fixture not found: $seedPath" }
    $actualSeedHash = (Get-FileHash -LiteralPath $seedPath -Algorithm SHA256).Hash
    if ($actualSeedHash -ne $Dataset.seedFileSha256) {
        throw "Seed catalog drift: expected SHA-256 $($Dataset.seedFileSha256), got $actualSeedHash. Review the catalog and update the dataset intentionally."
    }
    if ([string]::IsNullOrWhiteSpace($DatasetSha256)) {
        $DatasetSha256 = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'product-search-relevance.dataset.json') -Algorithm SHA256).Hash
    }

    if ($Approach -eq 'semantic') {
        $models = (Get-ProductSearchResponse -Request $Request -Uri ([uri]::new($OllamaAddress, 'api/tags'))).Data.models
        $model = @($models | Where-Object { $_.name -eq $Dataset.model -and $_.digest -eq $Dataset.modelDigest })
        if ($model.Count -ne 1) { throw "Ollama model drift: expected $($Dataset.model) with digest $($Dataset.modelDigest)." }
    }
    else {
        $sqlPath = Join-Path $PSScriptRoot '..\samples\src\Contoso.Products.Infrastructure\Repositories\Sql'
        $repositoryPath = Split-Path $sqlPath
        $lexicalSource = Get-Content -LiteralPath (Join-Path $repositoryPath 'ProductLexicalSearchIndex.cs') -Raw
        if ($lexicalSource -notmatch 'Recipe\s*=\s*"([^"]+)"') { throw 'Cannot identify the lexical recipe.' }
        $lexicalRecipe = $Matches[1]
        $implementationIdentity | Add-Member -NotePropertyName lexicalImplementationSha256 -NotePropertyValue @(
            @('ProductLexicalSearchIndex.cs', 'ProductRepository.cs', 'ProductQueryArgsConfig.cs') | ForEach-Object {
                [pscustomobject]@{ file = $_; sha256 = (Get-FileHash -LiteralPath (Join-Path $repositoryPath $_) -Algorithm SHA256).Hash }
            })
        $implementationIdentity | Add-Member -NotePropertyName lexicalSqlSha256 -NotePropertyValue @(
            Get-ChildItem -LiteralPath $sqlPath -Filter 'ProductLexicalSearch*.pgsql' | Sort-Object Name | ForEach-Object {
                [pscustomobject]@{ file = $_.Name; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
            })
    }

    $schema = (Get-ProductSearchResponse -Request $Request -Uri ([uri]::new($BaseAddress, 'api/products/$query'))).Data
    if ($schema.search.mode -ne $Approach -or $schema.search.ranked -ne $true) {
        throw "Products API drift: this evaluation requires $Approach ranked search."
    }

    $catalogResponse = Get-ProductSearchResponse -Request $Request -Uri ([uri]::new($BaseAddress, 'api/products?$take=100&$count=true'))
    $liveCatalog = @($catalogResponse.Data)
    $liveCatalogCount = Get-ProductSearchCountHeader $catalogResponse.Headers
    if ($liveCatalogCount -ne @($Dataset.catalog).Count -or $liveCatalog.Count -ne @($Dataset.catalog).Count) {
        $expectedSkus = @($Dataset.catalog | ForEach-Object sku)
        $actualSkus = @($liveCatalog | ForEach-Object sku)
        $extraSkus = @($actualSkus | Where-Object { $_ -notin $expectedSkus })
        $missingSkus = @($expectedSkus | Where-Object { $_ -notin $actualSkus })
        throw "Catalog drift: expected $(@($Dataset.catalog).Count) eligible seeded products; API returned $liveCatalogCount total and $($liveCatalog.Count) rows. Extra SKUs: $($extraSkus -join ', '); missing SKUs: $($missingSkus -join ', ')."
    }
    $liveCatalogSignature = Get-ProductSearchCatalogSignature -Products $liveCatalog
    if ($liveCatalogSignature -ne $catalogSignature) {
        throw "Catalog drift: live Products API catalog signature $liveCatalogSignature does not match reviewed dataset signature $catalogSignature."
    }

    $firstQuery = $selectedQueries[0]
    $firstUri = [uri]::new($BaseAddress, "api/products?`$search=$([uri]::EscapeDataString([string]$firstQuery.query))&`$take=5&`$count=true")
    $firstFilter = Get-ProductSearchFilter $firstQuery
    if ($firstFilter) { $firstUri = [uri]($firstUri.AbsoluteUri + "&`$filter=$([uri]::EscapeDataString($firstFilter))") }
    $firstResponse = Get-ProductSearchResponse -Request $Request -Uri $firstUri
    $firstExpectedCount = @((Get-ProductSearchEligibleCatalog -Dataset $Dataset -Query $firstQuery)).Count
    $firstActualCount = Get-ProductSearchCountHeader $firstResponse.Headers
    if ($Approach -eq 'semantic' -and $firstActualCount -ne $firstExpectedCount) {
        throw "Search backfill is not ready or is incomplete for profile '$($Dataset.profile)' and recipe '$($Dataset.documentRecipe)': expected $firstExpectedCount indexed candidates, got $firstActualCount."
    }

    $reports = [Collections.Generic.List[object]]::new()
    foreach ($query in $selectedQueries) {
        if ($query.id -eq $firstQuery.id) {
            $response = $firstResponse
        }
        else {
            $relative = "api/products?`$search=$([uri]::EscapeDataString([string]$query.query))&`$take=5&`$count=true"
            $filter = Get-ProductSearchFilter $query
            if ($filter) { $relative += "&`$filter=$([uri]::EscapeDataString($filter))" }
            $response = Get-ProductSearchResponse -Request $Request -Uri ([uri]::new($BaseAddress, $relative))
        }

        $candidateCount = Get-ProductSearchCountHeader $response.Headers
        $expectedCandidateCount = @((Get-ProductSearchEligibleCatalog -Dataset $Dataset -Query $query)).Count
        if (($Approach -eq 'semantic' -and $candidateCount -ne $expectedCandidateCount) -or
            $candidateCount -lt 0 -or $candidateCount -gt $expectedCandidateCount) {
            throw "Search count drift for $Approach query '$($query.id)': $expectedCandidateCount eligible catalog rows, got $candidateCount candidates. Semantic must match the eligible count; lexical counts must be between zero and that count."
        }

        $judgments = @{}
        foreach ($judgment in $query.judgments) { $judgments[[string]$judgment.sku] = [int]$judgment.grade }
        $ranked = @($response.Data)
        $expectedPageCount = [Math]::Min(5, $candidateCount)
        if ($ranked.Count -ne $expectedPageCount) {
            throw "Incomplete result page for query '$($query.id)': expected $expectedPageCount rows, got $($ranked.Count)."
        }
        $knownSkus = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($product in $ranked) {
            $sku = [string]$product.sku
            if (!(@($Dataset.catalog | Where-Object sku -EQ $sku).Count)) { throw "Catalog drift: search query '$($query.id)' returned unreviewed SKU '$sku'." }
            if (!$knownSkus.Add($sku)) { throw "Products API returned duplicate SKU '$sku' for query '$($query.id)'." }
        }
        $constraintViolations = @($ranked | Where-Object { !(Test-ProductSearchConstraint -Product $_ -Query $query) })
        $grades = @($ranked | ForEach-Object { if ($judgments.ContainsKey([string]$_.sku)) { $judgments[[string]$_.sku] } else { [int]$query.omittedSkuGrade } })
        $idealGrades = @($query.judgments | ForEach-Object { [int]$_.grade })
        $metrics = Get-ProductSearchMetrics -RankedGrades $grades -IdealGrades $idealGrades
        $topFive = @($ranked | ForEach-Object {
            [pscustomobject]@{
                sku = [string]$_.sku
                text = [string]$_.text
                price = [decimal]$_.price
                grade = if ($judgments.ContainsKey([string]$_.sku)) { $judgments[[string]$_.sku] } else { [int]$query.omittedSkuGrade }
            }
        })
        $reports.Add([pscustomobject]@{
            id = [string]$query.id
            split = [string]$query.split
            kind = [string]$query.kind
            query = [string]$query.query
            intent = [string]$query.intent
            expectedTop1Sku = $query.expectedTop1Sku
            actualTop1Sku = if ($ranked.Count) { [string]$ranked[0].sku } else { $null }
            exactSkuTop1 = if ($query.kind -eq 'exact-sku') { ($ranked.Count -gt 0 -and $ranked[0].sku -eq $query.expectedTop1Sku) } else { $null }
            relevantJudgedCount = @($idealGrades | Where-Object { $_ -gt 0 }).Count
            noRelevant = !$metrics.HasRelevant
            ndcgAt5 = $metrics.NdcgAt5
            precisionAt5 = $metrics.PrecisionAt5
            eligibleCandidateCount = $expectedCandidateCount
            matchedCandidateCount = $candidateCount
            constraintViolationCount = $constraintViolations.Count
            topFive = $topFive
        })
    }

    $splitReports = @{}
    foreach ($split in @($reports | ForEach-Object split | Select-Object -Unique)) {
        $splitQueries = @($reports | Where-Object split -EQ $split)
        $relevantQueries = @($splitQueries | Where-Object { !$_.noRelevant })
        $exactQueries = @($splitQueries | Where-Object { $null -ne $_.exactSkuTop1 })
        $constraintQueries = @($splitQueries | Where-Object { $null -ne $_.eligibleCandidateCount -and $null -ne $_.kind -and $_.kind -eq 'price-constraint' })
        $constraintResultCount = 0
        foreach ($item in $constraintQueries) { $constraintResultCount += $item.topFive.Count }
        $violations = ($constraintQueries | Measure-Object -Property constraintViolationCount -Sum).Sum
        if ($null -eq $violations) { $violations = 0 }
        $splitReports[$split] = [pscustomobject]@{
            queryCount = $splitQueries.Count
            relevantQueryCount = $relevantQueries.Count
            noRelevantQueryCount = @($splitQueries | Where-Object noRelevant).Count
            noRelevantQueryIds = @($splitQueries | Where-Object noRelevant | ForEach-Object id)
            outOfCatalogQueryCount = @($splitQueries | Where-Object kind -EQ 'out-of-catalog').Count
            meanNdcgAt5 = if ($relevantQueries.Count) { ($relevantQueries | Measure-Object -Property ndcgAt5 -Average).Average } else { $null }
            meanPrecisionAt5 = if ($relevantQueries.Count) { ($relevantQueries | Measure-Object -Property precisionAt5 -Average).Average } else { $null }
            exactSkuQueryCount = $exactQueries.Count
            exactSkuTop1Accuracy = if ($exactQueries.Count) { @($exactQueries | Where-Object exactSkuTop1).Count / [double]$exactQueries.Count } else { $null }
            constrainedQueryCount = $constraintQueries.Count
            constrainedResultCount = $constraintResultCount
            constraintViolationCount = [int]$violations
            constraintViolationRate = if ($constraintResultCount) { [int]$violations / [double]$constraintResultCount } else { $null }
        }
    }

    $comparison = $null
    if ($BaselinePath) {
        $baseline = Get-Content -LiteralPath $BaselinePath -Raw | ConvertFrom-Json
        if ($baseline.status -ne 'completed' -or $baseline.datasetSha256 -ne $DatasetSha256 -or
            $baseline.profile -ne $Dataset.profile -or $baseline.documentRecipe -ne $Dataset.documentRecipe -or
            $baseline.catalogIdentity.liveApiCatalogSha256 -ne $liveCatalogSignature -or
            $baseline.catalogIdentity.seedFileSha256 -ne $actualSeedHash) {
            throw 'Baseline dataset/catalog identity does not match this evaluation.'
        }
        $baselineIds = @($baseline.queries | Where-Object split -EQ 'tuning' | ForEach-Object id | Sort-Object)
        $actualIds = @($reports | ForEach-Object id | Sort-Object)
        if (($baselineIds -join '|') -ne ($actualIds -join '|')) { throw 'Baseline tuning query IDs differ.' }
        $comparison = [pscustomobject]@{
            baselineSha256 = (Get-FileHash -LiteralPath $BaselinePath -Algorithm SHA256).Hash
            split = 'tuning'
            baseline = $baseline.splits.tuning
            meanNdcgAt5Delta = $splitReports.tuning.meanNdcgAt5 - $baseline.splits.tuning.meanNdcgAt5
            meanPrecisionAt5Delta = $splitReports.tuning.meanPrecisionAt5 - $baseline.splits.tuning.meanPrecisionAt5
            exactSkuTop1AccuracyDelta = $splitReports.tuning.exactSkuTop1Accuracy - $baseline.splits.tuning.exactSkuTop1Accuracy
        }
    }
    return [pscustomobject]@{
        evaluationVersion = 'product-search-relevance-runner-v2'
        approach = $Approach
        selectedSplit = $requestedSplit
        completedAtUtc = [DateTime]::UtcNow.ToString('o')
        status = 'completed'
        datasetVersion = [string]$Dataset.datasetVersion
        model = if ($Approach -eq 'semantic') { [string]$Dataset.model } else { $null }
        modelDigest = if ($Approach -eq 'semantic') { [string]$Dataset.modelDigest } else { $null }
        profile = if ($Approach -eq 'semantic') { [string]$Dataset.profile } else { 'postgres:english:ts_rank_cd:exact-sku-first' }
        dimensions = if ($Approach -eq 'semantic') { [int]$Dataset.dimensions } else { $null }
        documentRecipe = if ($Approach -eq 'semantic') { [string]$Dataset.documentRecipe } else { $lexicalRecipe }
        documentInstruction = if ($Approach -eq 'semantic') { [string]$Dataset.documentInstruction } else { $null }
        queryInstruction = if ($Approach -eq 'semantic') { [string]$Dataset.queryInstruction } else { $null }
        datasetSha256 = $DatasetSha256
        implementationIdentity = $implementationIdentity
        catalogIdentity = [pscustomobject]@{
            seedFile = [string]$Dataset.seedFile
            seedFileSha256 = $actualSeedHash
            eligibleCatalogCount = $liveCatalogCount
            reviewedCatalogSha256 = $catalogSignature
            liveApiCatalogSha256 = $liveCatalogSignature
            eligibility = [string]$Dataset.catalogEligibility
        }
        metrics = [pscustomobject]@{
            relevanceThresholdForPrecisionAt5 = 2
            ndcgAt5Gain = '2^grade - 1; logarithmic rank discount; judged grades 0-3'
            precisionAt5 = 'Relevant means grade >= 2; denominator is always 5.'
            noRelevantQueries = 'Reported separately and excluded from relevance-metric means.'
            exactSkuTop1 = 'Reported only for exact-SKU queries.'
            constraints = 'Returned price-filtered results are validated against the declared hard price condition; no threshold is imposed on relevance scores.'
        }
        splits = $splitReports
        tuningComparison = $comparison
        queries = @($reports)
    }
}

if ($MyInvocation.InvocationName -ne '.') {
    Test-ProductSearchDataset (Get-ProductSearchDataset $DatasetPath) | Out-Null
    $dataset = Get-ProductSearchDataset $DatasetPath
    $datasetHash = (Get-FileHash -LiteralPath $DatasetPath -Algorithm SHA256).Hash
    $rootPath = Join-Path $PSScriptRoot ("..\{0}" -f ($dataset.seedFile -replace '/', '\'))
    if (!(Test-Path -LiteralPath $rootPath -PathType Leaf)) { throw "Pinned seed fixture not found: $rootPath" }
    $actualSeedHash = (Get-FileHash -LiteralPath $rootPath -Algorithm SHA256).Hash
    if ($actualSeedHash -ne $dataset.seedFileSha256) {
        throw "Seed catalog drift: expected SHA-256 $($dataset.seedFileSha256), got $actualSeedHash. Review the catalog and update the dataset intentionally."
    }

    $request = { param([uri] $Uri) Invoke-WebRequest -Uri $Uri -TimeoutSec 120 }
    $report = Invoke-ProductSearchRelevanceEvaluation -Dataset $dataset -BaseAddress $BaseAddress -OllamaAddress $OllamaAddress -Request $request -DatasetSha256 $datasetHash -Approach $Approach -Split $Split -BaselinePath $BaselinePath
    $json = ConvertTo-Json -InputObject $report -Depth 30
    if ($OutputPath) {
        $outputFullPath = [IO.Path]::GetFullPath($OutputPath)
        [IO.File]::WriteAllText($outputFullPath, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
        Write-Host "Wrote $Approach product-search ($Split) report to $outputFullPath"
    }
    else { Write-Output $json }
}
