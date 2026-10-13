[CmdletBinding()]
param(
    [uri] $BaseAddress = 'http://localhost:11434/'
)

$ErrorActionPreference = 'Stop'
$model = 'nomic-embed-text:v1.5'
$expectedDigest = '0a109f422b47e3a30ba2b10eca18548e944e8a23073ee3f3e947efcf3c45e59f'
$dimensions = 768

$models = Invoke-RestMethod -Uri ([uri]::new($BaseAddress, 'api/tags')) -TimeoutSec 30
$installed = @($models.models | Where-Object name -EQ $model)
if ($installed.Count -ne 1 -or $installed[0].digest -ne $expectedDigest) {
    throw "Expected $model with digest $expectedDigest. Pull the documented model and verify its identity before evaluating."
}

function Get-Embedding([string] $Text) {
    $body = @{ model = $model; input = $Text; truncate = $false; keep_alive = '5m' } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri ([uri]::new($BaseAddress, 'api/embed')) -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 120
    if ($response.model -ne $model -or $response.embeddings.Count -ne 1 -or $response.embeddings[0].Count -ne $dimensions) {
        throw 'Ollama returned an incompatible embedding response.'
    }

    $vector = [double[]] $response.embeddings[0]
    $norm = 0.0
    foreach ($value in $vector) {
        if ([double]::IsNaN($value) -or [double]::IsInfinity($value)) {
            throw 'Ollama returned a non-finite vector.'
        }
        $norm += $value * $value
    }
    if ($norm -eq 0) {
        throw 'Ollama returned a zero vector.'
    }
    return ,$vector
}

function Get-CosineDistance([double[]] $Query, [double[]] $Document) {
    $dot = 0.0
    $queryNorm = 0.0
    $documentNorm = 0.0
    for ($i = 0; $i -lt $dimensions; $i++) {
        $dot += $Query[$i] * $Document[$i]
        $queryNorm += $Query[$i] * $Query[$i]
        $documentNorm += $Document[$i] * $Document[$i]
    }
    return 1 - $dot / [math]::Sqrt($queryNorm * $documentNorm)
}

# These names, labels and tags come from the existing product and reference-data seeds.
$products = @(
    @{ Sku = 'MUCOFF-NANO-CLEANER'; Text = 'Muc-Off Nano Tech Bike Cleaner'; Brand = 'Muc-Off'; Category = 'Accessories'; Subcategory = 'Cleaner'; Tags = '' },
    @{ Sku = 'SHIMANO-XT-M8100-RD'; Text = 'Shimano Deore XT M8100 Rear Derailleur'; Brand = 'Shimano'; Category = 'Parts'; Subcategory = 'Derailleur'; Tags = '' },
    @{ Sku = 'YETI-ASR-C2-2025'; Text = 'Yeti ASR C2'; Brand = 'Yeti Cycles'; Category = 'Bikes'; Subcategory = 'Cross country'; Tags = 'cross-country, full-suspension' }
)
foreach ($product in $products) {
    $document = "Name: $($product.Text)`nSKU: $($product.Sku)`nBrand: $($product.Brand)`nCategory: $($product.Category)`nSubcategory: $($product.Subcategory)`nTags: $($product.Tags)"
    $product.Vector = Get-Embedding "search_document: $document"
}

$queries = @(
    @{ Text = 'something to clean mud off my bike'; Expected = 'MUCOFF-NANO-CLEANER' },
    @{ Text = 'a component for changing gears'; Expected = 'SHIMANO-XT-M8100-RD' }
)
$failures = [Collections.Generic.List[string]]::new()
foreach ($query in $queries) {
    $vector = Get-Embedding "search_query: $($query.Text)"
    $ranked = @($products | ForEach-Object {
        [pscustomobject]@{
            Sku = $_.Sku
            Distance = Get-CosineDistance $vector $_.Vector
        }
    } | Sort-Object Distance)
    Write-Host "`nQuery: $($query.Text)"
    $ranked | Format-Table Sku, @{Label = 'Cosine distance (lower is closer)'; Expression = { '{0:F4}' -f $_.Distance }}
    if ($ranked[0].Sku -ne $query.Expected) {
        $failures.Add("Query '$($query.Text)' expected $($query.Expected) first but retrieved $($ranked[0].Sku).")
    }
}

if ($failures.Count -gt 0) {
    throw ($failures -join "`n")
}

Write-Host "`nVerified $dimensions-dimensional real embeddings and two illustrative rankings."
Write-Host 'This is an in-memory learning smoke check, not PostgreSQL retrieval or a comprehensive relevance evaluation.'
