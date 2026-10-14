[CmdletBinding()]
param(
    [uri] $BaseAddress = 'http://localhost:5207/'
)

$ErrorActionPreference = 'Stop'
$schema = Invoke-RestMethod -Uri ([uri]::new($BaseAddress, 'api/products/$query')) -TimeoutSec 30
if ($schema.search.mode -ne 'semantic' -or !$schema.search.ranked) {
    throw 'This smoke check requires the semantic Products API.'
}

$queries = @(
    @{ Text = 'something to clean mud off my bike'; Expected = 'MUCOFF-NANO-CLEANER' },
    @{ Text = 'a component for changing gears'; Expected = 'SHIMANO-XT-M8100-RD' }
)
$failures = [Collections.Generic.List[string]]::new()
foreach ($query in $queries) {
    $path = 'api/products?$search=' + [uri]::EscapeDataString($query.Text) + '&$take=5&$count=true'
    $response = Invoke-WebRequest -Uri ([uri]::new($BaseAddress, $path)) -TimeoutSec 120
    $products = @($response.Content | ConvertFrom-Json)
    Write-Host "`nQuery: $($query.Text)"
    $products | Format-Table sku, text, price
    if ($products.Count -eq 0 -or $products[0].sku -ne $query.Expected) {
        $failures.Add("Query '$($query.Text)' expected $($query.Expected) first.")
    }
    if (!$response.Headers.ContainsKey('X-Paging-Total-Count')) {
        $failures.Add("Query '$($query.Text)' returned no eligible-product count.")
    }
}

$filtered = Invoke-RestMethod -Uri ([uri]::new($BaseAddress, 'api/products?$search=clean%20mud%20off%20my%20bike&$filter=price%20lt%2020&$take=1')) -TimeoutSec 120
if (@($filtered).Count -ne 1 -or $filtered[0].sku -ne 'MUCOFF-NANO-CLEANER' -or $filtered[0].price -ge 20) {
    $failures.Add('The price-constrained cleaning query did not return the cleaner under 20.')
}

$body = @{ query = '{ products(search: "a component for changing gears", first: 1) { edges { node { sku } } } }' } | ConvertTo-Json
$graphql = Invoke-RestMethod -Uri ([uri]::new($BaseAddress, 'api/query')) -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 120
if ($graphql.errors -or $graphql.data.products.edges[0].node.sku -ne 'SHIMANO-XT-M8100-RD') {
    $failures.Add('GraphQL-lite did not return the same first gear-changing candidate as REST.')
}

if ($failures.Count -gt 0) {
    throw ($failures -join "`n")
}
Write-Host "`nVerified illustrative full-catalog pgvector rankings, a live price constraint and GraphQL-lite parity."
Write-Host 'Nearest candidates are not guaranteed relevant; this is a learning smoke check, not a broad quality benchmark.'
