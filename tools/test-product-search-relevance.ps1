$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\product-search-relevance.ps1"

function Assert-Equal {
    param([object] $Actual, [object] $Expected, [string] $Message)
    if ($Actual -ne $Expected) { throw "$Message Expected '$Expected', got '$Actual'." }
}

function Assert-Throws {
    param([scriptblock] $Action, [string] $Message)
    try { & $Action } catch { return }
    throw $Message
}

$dataset = Get-ProductSearchDataset "$PSScriptRoot\product-search-relevance.dataset.json"
Test-ProductSearchDataset $dataset | Out-Null
Assert-Equal (Get-ProductSearchFilter $dataset.queries[0]) "sku ne 'LABOR'" 'The shared catalog benchmark must exclude only the workshop service, not reviewed Miscellaneous products.'
$invalidReference = Get-ProductSearchDataset "$PSScriptRoot\product-search-relevance.dataset.json"
$invalidReference.referenceDataFileSha256 = 'drift'
Assert-Throws { Test-ProductSearchDataset $invalidReference } 'Canonical reference-data label drift must be rejected.'

$metrics = Get-ProductSearchMetrics -RankedGrades @(3, 2, 0) -IdealGrades @(3, 2, 1)
$expectedNdcg = (7 + 3 / [Math]::Log(3, 2)) / (7 + 3 / [Math]::Log(3, 2) + 1 / [Math]::Log(4, 2))
if ([Math]::Abs($metrics.NdcgAt5 - $expectedNdcg) -gt 0.0000001) { throw 'nDCG@5 gain/discount math is incorrect.' }
Assert-Equal $metrics.PrecisionAt5 0.4 'Precision@5 must count grade 2 or 3 in five fixed slots.'
$emptyMetrics = Get-ProductSearchMetrics -RankedGrades @(0, 0) -IdealGrades @()
Assert-Equal $emptyMetrics.HasRelevant $false 'A query with no relevant catalog items must be identified separately.'
if ($null -ne $emptyMetrics.NdcgAt5 -or $null -ne $emptyMetrics.PrecisionAt5) { throw 'Metrics for a no-relevant query must be undefined, not zero-valued quality.' }
$marginalMetrics = Get-ProductSearchMetrics -RankedGrades @(1) -IdealGrades @(1)
Assert-Equal $marginalMetrics.HasRelevant $true 'A marginal grade remains useful graded relevance for nDCG.'
Assert-Equal $marginalMetrics.PrecisionAt5 0 'Precision@5 must retain the explicit grade-2 threshold.'

$invalid = Get-ProductSearchDataset "$PSScriptRoot\product-search-relevance.dataset.json"
$invalid.queries[0].judgments[0].sku = 'NOT-IN-CATALOG'
Assert-Throws { Test-ProductSearchDataset $invalid } 'Unknown judged SKUs must be rejected.'
$invalid = Get-ProductSearchDataset "$PSScriptRoot\product-search-relevance.dataset.json"
$invalid.catalog[1].sku = $invalid.catalog[0].sku
Assert-Throws { Test-ProductSearchDataset $invalid } 'Duplicate catalog SKUs must be rejected.'
$invalid = Get-ProductSearchDataset "$PSScriptRoot\product-search-relevance.dataset.json"
$invalid.queries[0].kind = 'unreviewed-kind'
Assert-Throws { Test-ProductSearchDataset $invalid } 'Unsupported query kinds must be rejected.'
$invalid = Get-ProductSearchDataset "$PSScriptRoot\product-search-relevance.dataset.json"
$constrainedQuery = $invalid.queries | Where-Object id -EQ 'tune-derailleur-under-150'
$constrainedQuery.judgments[0].sku = 'SHIMANO-XTR-M9100-CRANK'
Assert-Throws { Test-ProductSearchDataset $invalid } 'Judgments violating hard query constraints must be rejected.'

$validMock = {
    param([uri] $Uri)
    $path = $Uri.AbsolutePath
    if ($path -eq '/api/tags') {
        $content = @{ models = @(@{ name = $dataset.model; digest = $dataset.modelDigest }) } | ConvertTo-Json -Depth 8 -Compress
        return [pscustomobject]@{ StatusCode = 200; Content = $content; Headers = @{} }
    }
    if ($path -eq '/api/products/$query') {
        $content = @{ search = @{ mode = 'semantic'; ranked = $true } } | ConvertTo-Json -Depth 8 -Compress
        return [pscustomobject]@{ StatusCode = 200; Content = $content; Headers = @{} }
    }
    if ($path -ne '/api/products') { throw "Unexpected mocked URI: $Uri" }
    if (![uri]::UnescapeDataString($Uri.Query).Contains("sku ne 'LABOR'")) { throw 'Every catalog/search request must use the reviewed MTB eligibility filter.' }
    if (!$Uri.Query.Contains('$search=')) {
        $skip = 0
        $take = 100
        if ($Uri.Query -match '\$skip=(\d+)') { $skip = [int]$Matches[1] }
        if ($Uri.Query -match '\$take=(\d+)') { $take = [int]$Matches[1] }
        $items = @($dataset.catalog | Select-Object -Skip $skip -First $take | ForEach-Object { @{ sku = $_.sku; text = $_.text; price = $_.price; description = $_.description } })
        return [pscustomobject]@{ StatusCode = 200; Content = (ConvertTo-Json -InputObject $items -Depth 8 -Compress); Headers = @{ 'X-Paging-Total-Count' = "$($dataset.catalog.Count)" } }
    }
    $queryString = $Uri.Query.TrimStart([char]'?')
    $searchPart = $queryString -split '&' | Where-Object { $_.StartsWith('$search=') } | Select-Object -First 1
    $queryText = [uri]::UnescapeDataString($searchPart.Substring(8))
    $query = $dataset.queries | Where-Object query -EQ $queryText | Select-Object -First 1
    $eligible = @($dataset.catalog)
    if ($null -ne $query.constraints -and $null -ne $query.constraints.price) {
        [decimal]$limit = $query.constraints.price.value
        $eligible = @($eligible | Where-Object {
            [decimal]$price = $_.price
            switch ($query.constraints.price.operator) {
                'eq' { $price -eq $limit }
                'lt' { $price -lt $limit }
                'le' { $price -le $limit }
                'gt' { $price -gt $limit }
                'ge' { $price -ge $limit }
            }
        })
    }
    $results = @($eligible | Select-Object -First 5 | ForEach-Object { @{ sku = $_.sku; text = $_.text; price = $_.price; description = $_.description } })
    return [pscustomobject]@{ StatusCode = 200; Content = (ConvertTo-Json -InputObject $results -Depth 8 -Compress); Headers = @{ 'X-Paging-Total-Count' = "$($eligible.Count)" } }
}.GetNewClosure()

$mockReport = Invoke-ProductSearchRelevanceEvaluation -Dataset $dataset -BaseAddress 'http://mock/' -OllamaAddress 'http://mock/' -Request $validMock
Assert-Equal $mockReport.queries.Count 60 'The mocked end-to-end evaluation must exercise all curated queries.'
Assert-Equal $mockReport.splits.'held-out'.queryCount 12 'Tuning and held-out aggregates must remain split.'
Assert-Equal $mockReport.splits.tuning.queryCount 48 'Tuning and held-out aggregates must remain split.'
Assert-Equal @($dataset.catalog | Where-Object { 'e-bike' -in $_.tags -and 'emtb' -in $_.tags }).Count 3 'E-bike aliases must be limited to the electric mountain bikes.'
Assert-Equal @($dataset.catalog | Where-Object { 'dentist-bike' -in $_.tags -and $_.brandCode -ne 'YETI' }).Count 0 'The informal dentist-bike alias must be limited to Yeti catalog entries.'
$invalid = Get-ProductSearchDataset "$PSScriptRoot\product-search-relevance.dataset.json"
$yetiAlias = $invalid.catalog | Where-Object sku -EQ 'YETI-ASR-C2-2025'
$yetiAlias.tagProvenance[0].origin = 'manufacturer'
Assert-Throws { Test-ProductSearchDataset $invalid } 'Informal search aliases must not be represented as manufacturer product facts.'
$invalid = Get-ProductSearchDataset "$PSScriptRoot\product-search-relevance.dataset.json"
$invalid.catalog | Where-Object sku -EQ 'SPECIALIZED-TURBO-LEVO' | ForEach-Object { $_.subCategoryCode = 'TR' }
Assert-Throws { Test-ProductSearchDataset $invalid } 'E-bike aliases must not be assigned to non-electric MTB categories.'

$shortPageMock = {
    param([uri] $Uri)
    if ($Uri.AbsolutePath -eq '/api/products' -and $Uri.Query.Contains('$search=')) {
        $items = @($dataset.catalog | Select-Object -First 1 | ForEach-Object { @{ sku = $_.sku; text = $_.text; price = $_.price; description = $_.description } })
        return [pscustomobject]@{ StatusCode = 200; Content = (ConvertTo-Json -InputObject $items -Depth 8 -Compress); Headers = @{ 'X-Paging-Total-Count' = "$($dataset.catalog.Count)" } }
    }
    & $validMock $Uri
}.GetNewClosure()
Assert-Throws { Invoke-ProductSearchRelevanceEvaluation -Dataset $dataset -BaseAddress 'http://mock/' -OllamaAddress 'http://mock/' -Request $shortPageMock } 'Incomplete search result pages must be rejected.'

$driftMock = {
    param([uri] $Uri)
    if ($Uri.AbsolutePath -eq '/api/products' -and !$Uri.Query.Contains('$search=')) {
        $skip = 0
        $take = 100
        if ($Uri.Query -match '\$skip=(\d+)') { $skip = [int]$Matches[1] }
        if ($Uri.Query -match '\$take=(\d+)') { $take = [int]$Matches[1] }
        $items = @($dataset.catalog | Select-Object -Skip $skip -First $take | ForEach-Object { @{ sku = $_.sku; text = $_.text; price = $_.price; description = $_.description } })
        $items[0].text = 'Changed catalog item'
        return [pscustomobject]@{ StatusCode = 200; Content = (ConvertTo-Json -InputObject $items -Depth 8 -Compress); Headers = @{ 'X-Paging-Total-Count' = "$($dataset.catalog.Count)" } }
    }
    & $validMock $Uri
}.GetNewClosure()
Assert-Throws { Invoke-ProductSearchRelevanceEvaluation -Dataset $dataset -BaseAddress 'http://mock/' -OllamaAddress 'http://mock/' -Request $driftMock } 'Catalog drift must stop evaluation.'

$backfillMock = {
    param([uri] $Uri)
    if ($Uri.AbsolutePath -eq '/api/products' -and $Uri.Query.Contains('$search=')) {
        return [pscustomobject]@{ StatusCode = 200; Content = '[]'; Headers = @{ 'X-Paging-Total-Count' = '28' } }
    }
    & $validMock $Uri
}.GetNewClosure()
Assert-Throws { Invoke-ProductSearchRelevanceEvaluation -Dataset $dataset -BaseAddress 'http://mock/' -OllamaAddress 'http://mock/' -Request $backfillMock } 'A ready flag with an incomplete search projection must fail the full-catalog count check.'

$violationMock = {
    param([uri] $Uri)
    if ($Uri.AbsolutePath -eq '/api/products' -and $Uri.Query.Contains('$search=')) {
        $queryString = $Uri.Query.TrimStart([char]'?')
        $searchPart = $queryString -split '&' | Where-Object { $_.StartsWith('$search=') } | Select-Object -First 1
        $queryText = [uri]::UnescapeDataString($searchPart.Substring(8))
        if ($queryText -eq 'mountain bike rear derailleur under $150') {
            $crank = $dataset.catalog | Where-Object sku -EQ 'SHIMANO-XTR-M9100-CRANK'
            $query = $dataset.queries | Where-Object id -EQ 'tune-derailleur-under-150'
            $eligibleCatalog = @($dataset.catalog | Where-Object { [decimal]$_.price -lt [decimal]$query.constraints.price.value })
            $eligible = @($eligibleCatalog | Select-Object -First 4)
            $results = @(@($crank) + $eligible | ForEach-Object { @{ sku = $_.sku; text = $_.text; price = $_.price; description = $_.description } })
            return [pscustomobject]@{ StatusCode = 200; Content = (ConvertTo-Json -InputObject $results -Depth 8 -Compress); Headers = @{ 'X-Paging-Total-Count' = "$($eligibleCatalog.Count)" } }
        }
    }
    & $validMock $Uri
}.GetNewClosure()
$violationReport = Invoke-ProductSearchRelevanceEvaluation -Dataset $dataset -BaseAddress 'http://mock/' -OllamaAddress 'http://mock/' -Request $violationMock
$violationQuery = $violationReport.queries | Where-Object id -EQ 'tune-derailleur-under-150'
Assert-Equal $violationQuery.constraintViolationCount 1 'Hard price-filter violations must be measured rather than hidden.'
Assert-Equal $violationReport.splits.tuning.constraintViolationCount 1 'Constraint violations must be included in split aggregates.'

$failedMock = { param([uri] $Uri) throw 'Mocked HTTP outage.' }.GetNewClosure()
Assert-Throws { Invoke-ProductSearchRelevanceEvaluation -Dataset $dataset -BaseAddress 'http://mock/' -OllamaAddress 'http://mock/' -Request $failedMock } 'HTTP failures must stop evaluation.'
$unavailableMock = { param([uri] $Uri) [pscustomobject]@{ StatusCode = 503; Content = '{"title":"Unavailable"}'; Headers = @{} } }.GetNewClosure()
Assert-Throws { Invoke-ProductSearchRelevanceEvaluation -Dataset $dataset -BaseAddress 'http://mock/' -OllamaAddress 'http://mock/' -Request $unavailableMock } 'Service-unavailable responses must stop evaluation.'

$tuningOnlyMock = {
    param([uri] $Uri)
    if ($Uri.AbsolutePath -eq '/api/tags') { throw 'Full-text must not contact Ollama.' }
    if ($Uri.AbsolutePath -eq '/api/products/$query') {
        return [pscustomobject]@{ StatusCode = 200; Content = '{"search":{"mode":"fulltext","ranked":true}}'; Headers = @{} }
    }
    if ($Uri.Query.Contains('$search=')) {
        $searchPart = $Uri.Query.TrimStart([char]'?') -split '&' | Where-Object { $_.StartsWith('$search=') } | Select-Object -First 1
        $queryText = [uri]::UnescapeDataString($searchPart.Substring(8))
        $query = $dataset.queries | Where-Object query -EQ $queryText | Select-Object -First 1
        if ($query.split -ne 'tuning') { throw "Non-tuning query executed: $queryText" }
        return [pscustomobject]@{ StatusCode = 200; Content = '[]'; Headers = @{ 'X-Paging-Total-Count' = '0' } }
    }
    & $validMock $Uri
}.GetNewClosure()
$lexical = Invoke-ProductSearchRelevanceEvaluation -Dataset $dataset -BaseAddress 'http://mock/' -OllamaAddress 'http://mock/' -Request $tuningOnlyMock -Approach fulltext -Split tuning
Assert-Equal $lexical.queries.Count 48 'Tuning-only must execute exactly 48 queries.'
Assert-Equal $lexical.splits.ContainsKey('held-out') $false 'Held-out metrics must not be generated while tuning.'
Assert-Equal $lexical.model $null 'Full-text must not claim model inference.'
Assert-Equal $lexical.countSemantics 'Eligible lexical matches; not the full eligible catalog.' 'Full-text count semantics must not imply complete catalog coverage.'
Assert-Equal $lexical.splits.tuning.meanNdcgAt5 0 'Zero matching results are legitimate lexical results and must score zero.'
Assert-Throws { Invoke-ProductSearchRelevanceEvaluation -Dataset $dataset -BaseAddress 'http://mock/' -OllamaAddress 'http://mock/' -Request $tuningOnlyMock -Approach fulltext -Split tuning -BaselinePath "$PSScriptRoot\..\samples\docs\product-search-relevance-baseline.json" } 'The historical v1 baseline must not be compared directly with the v2 catalog and query set.'
Assert-Equal $mockReport.selectedSplit 'all' 'The original semantic full evaluation remains supported.'
Assert-Throws { Invoke-ProductSearchRelevanceEvaluation -Dataset $dataset -BaseAddress 'http://mock/' -OllamaAddress 'http://mock/' -Request $tuningOnlyMock -Approach fulltext } 'Full-text must require an explicit tuning-only split.'
Assert-Throws { Invoke-ProductSearchRelevanceEvaluation -Dataset $dataset -BaseAddress 'http://mock/' -OllamaAddress 'http://mock/' -Request $tuningOnlyMock -Approach fulltext -Split held-out } 'Held-out lexical execution must be rejected during this tuning milestone.'

$hybridMock = {
    param([uri] $Uri)
    if ($Uri.AbsolutePath -eq '/api/products/$query') {
        return [pscustomobject]@{ StatusCode = 200; Content = '{"search":{"mode":"hybrid","ranked":true}}'; Headers = @{} }
    }
    & $validMock $Uri
}.GetNewClosure()
$hybrid = Invoke-ProductSearchRelevanceEvaluation -Dataset $dataset -BaseAddress 'http://mock/' -OllamaAddress 'http://mock/' -Request $hybridMock -Approach hybrid -Split tuning
Assert-Equal $hybrid.queries.Count 48 'Hybrid tuning must execute only the tuning split.'
Assert-Equal $hybrid.model $dataset.model 'Hybrid search must retain the semantic model identity.'
Assert-Equal $hybrid.candidatePool.candidatesPerBranch 100 'Hybrid reports must identify the per-branch candidate budget.'
Assert-Equal $hybrid.candidatePool.maximumUniqueCandidates 200 'Hybrid reports must identify the maximum unique candidate pool.'
Assert-Equal $hybrid.candidatePool.rrfK 60 'Hybrid reports must identify the fixed RRF constant.'
Assert-Equal $hybrid.countSemantics 'Deduplicated bounded union of eligible lexical and semantic candidates; not the full eligible catalog.' 'Hybrid count must describe the bounded union.'
$hybridHeldOut = Invoke-ProductSearchRelevanceEvaluation -Dataset $dataset -BaseAddress 'http://mock/' -OllamaAddress 'http://mock/' -Request $hybridMock -Approach hybrid -Split held-out
Assert-Equal $hybridHeldOut.queries.Count 12 'Hybrid held-out evaluation must remain available after tuning is finalized.'
Assert-Equal $hybridHeldOut.splits.ContainsKey('tuning') $false 'Hybrid held-out runs must not execute tuning queries.'
Assert-Throws { Invoke-ProductSearchRelevanceEvaluation -Dataset $dataset -BaseAddress 'http://mock/' -OllamaAddress 'http://mock/' -Request $hybridMock -Approach hybrid -Split held-out -BaselinePath "$PSScriptRoot\..\samples\docs\product-search-relevance-baseline.json" } 'The historical v1 baseline must not be compared directly with v2 held-out queries.'

Write-Host 'Product-search relevance tests passed: metric math, dataset validation, catalog drift, count scope, split isolation, constraint violations, and mocked HTTP failures.'
