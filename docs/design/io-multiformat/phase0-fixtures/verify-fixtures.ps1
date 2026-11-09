$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

function Check($condition, [string]$message) {
    if (-not $condition) { throw $message }
}

function ReadJson([string]$name) {
    Get-Content -LiteralPath (Join-Path $root $name) -Raw | ConvertFrom-Json -AsHashtable -DateKind String
}

function Signature($orders) {
    $normalized = @($orders | ForEach-Object {
        [ordered]@{
            Id = $_.Id
            EffectiveDate = $_.EffectiveDate
            SubmittedAt = $_.SubmittedAt
            BillingAddress = $_.BillingAddress.Code
            ShippingAddress = $_.ShippingAddress.Code
            Lines = @($_.Lines | ForEach-Object { [ordered]@{ Sku = $_.Sku; Quantity = [int]$_.Quantity } })
            Tags = @($_.Tags | ForEach-Object { $_ })
        }
    })
    ConvertTo-Json -InputObject $normalized -Depth 12 -Compress
}

$manifest = ReadJson 'expected.json'
Check ($manifest.scenarios.Count -eq 15) 'Scenario count mismatch.'
foreach ($scenario in $manifest.scenarios) {
    Check (-not $scenario.ContainsKey('shape')) "Stale JSON configuration spelling: $($scenario.id)"
    if ($scenario.format -eq 'Json') {
        Check ($scenario.kind -in @('Array', 'Object') -and $scenario.ContainsKey('unitPath')) "Missing JSON selection contract: $($scenario.id)"
        if ($scenario.kind -eq 'Object') { Check ($scenario.unitPath -ceq '') "Unsupported selected object: $($scenario.id)" }
    }
    Check ($scenario.errorCount -eq $scenario.diagnostics.Count) "Diagnostic count mismatch: $($scenario.id)"
    Check ($scenario.invalidRecords -le $scenario.recordsRead) "Invalid record invariant: $($scenario.id)"
    Check (($scenario.invalidUnits + $scenario.unitsValidated) -le $scenario.unitsRead) "Unit invariant: $($scenario.id)"
    Check ($scenario.unitsAttempted -le $scenario.unitsValidated) "Attempt invariant: $($scenario.id)"
    Check ($scenario.unitsExecuted -le $scenario.unitsAttempted) "Execution invariant: $($scenario.id)"
    if ($scenario.outcome -eq 'Rejected') {
        Check ($scenario.opens -eq 1 -and $scenario.unitsAttempted -eq 0) "Rejected execution: $($scenario.id)"
    }

    if ($scenario.ContainsKey('file')) {
        $text = [IO.File]::ReadAllText((Join-Path $root $scenario.file))
        Check ($text.EndsWith("`n") -and -not $text.EndsWith("`n`n") -and -not $text.Contains("`r")) "Line ending mismatch: $($scenario.file)"
        Check (($text.Split("`n").Count - 1) -eq $scenario.physicalLines) "Physical line mismatch: $($scenario.file)"
    } else {
        Check ($scenario.inputText.Length -eq 0) "Expected zero-byte input: $($scenario.id)"
    }
}

$canonical = ReadJson 'valid-array.json'
$expected = Signature $canonical
$envelope = ReadJson 'valid-envelope.json'
Check ($canonical.Count -eq $manifest.canonical.orderCount) 'Canonical order count differs from manifest.'
Check (($canonical.Id -join '|') -ceq ($manifest.canonical.orderIds -join '|')) 'Canonical IDs differ from manifest.'
Check (@($canonical.Lines).Count -eq $manifest.canonical.lineCount) 'Canonical line count differs from manifest.'
Check (($canonical.Lines.Quantity | Measure-Object -Sum).Sum -eq $manifest.canonical.quantityTotal) 'Canonical quantity differs from manifest.'
Check (($canonical.BillingAddress.Code -join '|') -ceq ($manifest.canonical.billingCodes -join '|')) 'Billing codes differ from manifest.'
Check (($canonical.ShippingAddress.Code -join '|') -ceq ($manifest.canonical.shippingCodes -join '|')) 'Shipping codes differ from manifest.'
Check (($canonical[0].Tags -join '|') -ceq ($manifest.canonical.firstTags -join '|')) 'Tags differ from manifest.'
Check ($envelope.batch.Id -ceq $manifest.canonical.batch.Id) 'Batch ID differs from manifest.'
Check ($envelope.batch.ExpectedOrders -eq $manifest.canonical.batch.ExpectedOrders -and $envelope.batch.ExpectedQuantity -eq $manifest.canonical.batch.ExpectedQuantity) 'Batch totals differ from manifest.'
Check ((Signature $envelope.orders) -ceq $expected) 'Envelope units differ.'
$lines = [IO.File]::ReadAllLines((Join-Path $root 'valid.jsonl'))
$jsonl = @($lines | ForEach-Object { ConvertFrom-Json -InputObject $_ -AsHashtable -DateKind String })
Check ((Signature $jsonl) -ceq $expected) 'JSONL units differ.'

$csvOrders = [Collections.Generic.List[object]]::new()
$csvQuantity = 0
foreach ($line in [IO.File]::ReadAllLines((Join-Path $root 'valid.csv'))) {
    $f = $line.Split(',')
    switch ($f[0]) {
        'H' { $batchId = $f[1] }
        'O' {
            $current = @{
                Id = $f[1]
                EffectiveDate = [DateOnly]::ParseExact($f[2], 'yyyyMMdd', [Globalization.CultureInfo]::InvariantCulture).ToString('yyyy-MM-dd')
                SubmittedAt = $f[3]
                Lines = [Collections.Generic.List[object]]::new()
                Tags = [Collections.Generic.List[string]]::new()
            }
            $csvOrders.Add($current)
        }
        'B' { $current.BillingAddress = @{ Code = $f[1] } }
        'S' { $current.ShippingAddress = @{ Code = $f[1] } }
        'L' { $current.Lines.Add(@{ Sku = $f[1]; Quantity = [int]$f[2] }); $csvQuantity += [int]$f[2] }
        'G' { $current.Tags.Add($f[1]) }
        'T' { Check ([int]$f[1] -eq 2 -and [int]$f[2] -eq $csvQuantity) 'CSV trailer mismatch.' }
        default { throw 'Unexpected fixture route.' }
    }
}
Check ((Signature $csvOrders) -ceq $expected) 'CSV conceptual units differ.'
Check ($batchId -ceq $envelope.batch.Id) 'CSV metadata differs.'

$settings = [Xml.XmlReaderSettings]::new()
$settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
$settings.XmlResolver = $null
$reader = [Xml.XmlReader]::Create((Join-Path $root 'valid.xml'), $settings)
$xml = [Xml.XmlDocument]::new()
try { $xml.Load($reader) } finally { $reader.Dispose() }
$ns = [Xml.XmlNamespaceManager]::new($xml.NameTable)
$ns.AddNamespace('p', 'urn:coreex:phase0')
$xmlOrders = @($xml.SelectNodes('/p:Batch/p:Orders/p:Order', $ns) | ForEach-Object {
    $node = $_
    @{
        Id = $node.GetAttribute('Id')
        EffectiveDate = $node.SelectSingleNode('p:EffectiveDate', $ns).InnerText
        SubmittedAt = $node.SelectSingleNode('p:SubmittedAt', $ns).InnerText
        BillingAddress = @{ Code = $node.SelectSingleNode('p:BillingAddress/p:Code', $ns).InnerText }
        ShippingAddress = @{ Code = $node.SelectSingleNode('p:ShippingAddress/p:Code', $ns).InnerText }
        Lines = @($node.SelectNodes('p:Lines/p:Line', $ns) | ForEach-Object {
            @{ Sku = $_.SelectSingleNode('p:Sku', $ns).InnerText; Quantity = [int]$_.SelectSingleNode('p:Quantity', $ns).InnerText }
        })
        Tags = @($node.SelectNodes('p:Tags/p:Tag', $ns) | ForEach-Object { $_.InnerText })
    }
})
Check ((Signature $xmlOrders) -ceq $expected) 'XML conceptual units differ.'
Check ($xml.SelectSingleNode('/p:Batch/p:Metadata', $ns).GetAttribute('Id') -ceq $batchId) 'XML metadata differs.'
Check (($canonical.Lines.Quantity | Measure-Object -Sum).Sum -eq 6) 'Expected quantity total differs.'
Check ($canonical.Count -eq 2 -and $canonical[1].Tags.Count -eq 0) 'Canonical counts differ.'
foreach ($order in $canonical) {
    $parsed = [DateTimeOffset]::ParseExact($order.SubmittedAt, "yyyy-MM-dd'T'HH:mm:ssK", [Globalization.CultureInfo]::InvariantCulture)
    if ($order.Id -eq 'A') { Check ($parsed.Offset.TotalMinutes -eq 600) 'Offset was lost.' }
}

$badChildren = @([IO.File]::ReadAllLines((Join-Path $root 'invalid-child.jsonl')) | ForEach-Object {
    ConvertFrom-Json -InputObject $_ -AsHashtable -DateKind String
})
Check ($badChildren[0].Lines[0].Quantity -eq 0) 'Invalid child absent.'
Check ((Signature @($badChildren[1])) -ceq (Signature @($canonical[1]))) 'Valid recovery unit changed.'
$badCount = 0
foreach ($line in [IO.File]::ReadAllLines((Join-Path $root 'malformed-line.jsonl'))) {
    try { $doc = [Text.Json.JsonDocument]::Parse([string]$line); $doc.Dispose() } catch [System.Management.Automation.MethodInvocationException] {
        if ($_.Exception.InnerException -isnot [Text.Json.JsonException]) { throw }
        $badCount++
    }
}
Check ($badCount -eq 1) 'Malformed JSONL fixture must contain exactly one syntax failure.'
$trailingRejected = $false
try {
    $doc = [Text.Json.JsonDocument]::Parse([IO.File]::ReadAllText((Join-Path $root 'trailing-garbage.json')))
    $doc.Dispose()
} catch [System.Management.Automation.MethodInvocationException] {
    if ($_.Exception.InnerException -isnot [Text.Json.JsonException]) { throw }
    $trailingRejected = $true
}
Check $trailingRejected 'Trailing garbage was unexpectedly accepted.'
$late = ReadJson 'late-total.json'
Check ((Signature $late.orders) -ceq $expected) 'Late-total changed unit content.'
Check ($late.batch.ExpectedOrders -eq 3 -and $late.batch.ExpectedQuantity -eq 6) 'Late-total fixture does not isolate count mismatch.'
$missing = ReadJson 'missing-child.jsonl'
Check (-not $missing.ContainsKey('Lines')) 'Missing-child fixture has Lines.'
Check ((Signature @(ReadJson 'single-object.json')) -ceq (Signature @($canonical[0]))) 'Single-object fixture differs.'
Check ((ReadJson 'empty-array.json').Count -eq 0) 'Empty array is not empty.'

$execution = ReadJson 'execution-expectations.json'
Check ($execution.scenarios.Count -eq 8) 'Execution scenario count mismatch.'
foreach ($scenario in $execution.scenarios) {
    Check (Test-Path -LiteralPath (Join-Path $root $scenario.file)) "Missing fixture: $($scenario.file)"
    Check ($scenario.unitsAttempted -le $scenario.unitsValidated -and $scenario.unitsExecuted -le $scenario.unitsAttempted) "Execution invariants: $($scenario.id)"
    Check (($scenario.invalidUnits + $scenario.unitsValidated) -le $scenario.unitsRead) "Unit invariants: $($scenario.id)"
    if ($scenario.mode -eq 'ValidateAndExecute') { Check ($scenario.opens -eq 1) "Unexpected replay: $($scenario.id)" }
    if ($scenario.outcome -eq 'ExecutedWithErrors') { Check ($scenario.errorCount -gt 0) "Missing content errors: $($scenario.id)" }
    if ($scenario.outcome -eq 'ExecutionFailed') { Check (($scenario.unitsAttempted - $scenario.unitsExecuted) -eq 1 -and $scenario.preserveOriginalResult) "Business failure contract: $($scenario.id)" }
    if ($scenario.outcome -eq 'Executed') { Check $scenario.acceptedMetadataAtCompletion "Missing accepted completion view: $($scenario.id)" }
}
$routed = @([IO.File]::ReadAllLines((Join-Path $root 'routed.jsonl')) | ForEach-Object { ConvertFrom-Json -InputObject $_ -AsHashtable -DateKind String })
Check (($routed.type -join '|') -ceq 'header|order|order|trailer') 'Routed fixture order differs.'
Check (($routed | Where-Object type -eq 'order').Count -eq $routed[3].orderCount) 'Routed count differs.'
Check ((($routed | Where-Object type -eq 'order').quantity | Measure-Object -Sum).Sum -eq $routed[3].quantityTotal) 'Routed quantity differs.'

'PASS: 15 baseline and 8 execution/routing expectation scenarios; five-format conceptual parity; offsets, totals, and negative deltas. This checks fixture data, not unimplemented runtime behavior.'
