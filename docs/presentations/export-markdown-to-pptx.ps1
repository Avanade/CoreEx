[CmdletBinding()]
param(
    [string]$MarkdownPath = "docs/presentations/coreex-agentic-scaffolding-slides.md",
    [string]$OutputPath = "docs/presentations/coreex-agentic-scaffolding-slides.pptx",
    [string]$TemplatePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-SectionName {
    param(
        [string]$SlideId,
        [string]$Title
    )

    if (-not [string]::IsNullOrWhiteSpace($SlideId) -and $SlideId.Trim().ToUpper().StartsWith('ES')) {
        return 'Executive Summary'
    }

    if (-not [string]::IsNullOrWhiteSpace($Title) -and $Title -match '^Appendix') {
        return 'Appendix'
    }

    return 'Technical Deep Dive'
}

function New-SlideModel {
    param([string[]]$Lines)

    $slideId = $null
    $title = $null
    $subtitle = $null
    $bullets = New-Object System.Collections.Generic.List[string]
    $notes = New-Object System.Collections.Generic.List[string]

    $inNotes = $false

    foreach ($raw in $Lines) {
        $line = $raw.TrimEnd()
        $trim = $line.Trim()

        if ([string]::IsNullOrWhiteSpace($trim)) {
            if ($inNotes) { $notes.Add("") }
            continue
        }

        if ($trim -eq 'Speaker notes:') {
            $inNotes = $true
            continue
        }

        if ($inNotes) {
            $notes.Add($trim)
            continue
        }

        if (-not $title -and $trim -match '^##\s+Slide\s+([^-]+)-\s+(.+)$') {
            $slideId = $Matches[1].Trim()
            $title = $Matches[2].Trim()
            continue
        }

        if (-not $subtitle -and $trim -match '^###\s+(.+)$') {
            $subtitle = $Matches[1].Trim()
            continue
        }

        if ($trim -match '^[-]\s+(.+)$') {
            $bullets.Add($Matches[1].Trim())
            continue
        }

        if ($trim -match '^\d+[.]\s+(.+)$') {
            $bullets.Add($trim)
            continue
        }

        if ($trim -match '^(Audience|Duration):') {
            $bullets.Add($trim)
            continue
        }

        if ($trim -notmatch '^#') {
            $bullets.Add($trim)
        }
    }

    if (-not $title) { return $null }

    $section = Get-SectionName -SlideId $slideId -Title $title

    [PSCustomObject]@{
        SlideId = $slideId
        Title = $title
        Subtitle = $subtitle
        Bullets = $bullets
        Notes = $notes
        Section = $section
    }
}

$repoRoot = Get-Location
$mdFullPath = if ([System.IO.Path]::IsPathRooted($MarkdownPath)) { $MarkdownPath } else { Join-Path $repoRoot $MarkdownPath }
$pptxFullPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) { $OutputPath } else { Join-Path $repoRoot $OutputPath }
$templateFullPath = $null
$downloadedTemplatePath = $null

if (-not [string]::IsNullOrWhiteSpace($TemplatePath)) {
    if ($TemplatePath -match '^https?://') {
        $downloadedTemplatePath = Join-Path ([System.IO.Path]::GetTempPath()) ("coreex-template-{0}.potx" -f ([System.Guid]::NewGuid().ToString('N')))
        try {
            Invoke-WebRequest -Uri $TemplatePath -OutFile $downloadedTemplatePath | Out-Null
            $templateFullPath = $downloadedTemplatePath
        }
        catch {
            throw "Unable to download template from URL. Download it locally first and pass a file path. URL: $TemplatePath"
        }
    }
    else {
        $templateFullPath = if ([System.IO.Path]::IsPathRooted($TemplatePath)) { $TemplatePath } else { Join-Path $repoRoot $TemplatePath }
    }
}

if (-not (Test-Path -LiteralPath $mdFullPath)) {
    throw "Markdown file not found: $mdFullPath"
}

if ($templateFullPath -and -not (Test-Path -LiteralPath $templateFullPath)) {
    throw "Template file not found: $templateFullPath"
}

$allLines = Get-Content -LiteralPath $mdFullPath
$blocks = New-Object System.Collections.Generic.List[object]
$current = New-Object System.Collections.Generic.List[string]

foreach ($line in $allLines) {
    if ($line.Trim() -eq '---') {
        if ($current.Count -gt 0) {
            $blocks.Add(@($current))
            $current = New-Object System.Collections.Generic.List[string]
        }
        continue
    }

    $current.Add($line)
}

if ($current.Count -gt 0) {
    $blocks.Add(@($current))
}

$slideModels = New-Object System.Collections.Generic.List[object]
foreach ($block in $blocks) {
    $model = New-SlideModel -Lines $block
    if ($null -ne $model) { $slideModels.Add($model) }
}

if ($slideModels.Count -eq 0) {
    throw "No slides were parsed from markdown."
}

$parent = Split-Path -Parent $pptxFullPath
if (-not (Test-Path -LiteralPath $parent)) {
    New-Item -ItemType Directory -Path $parent | Out-Null
}

$ppSaveAsOpenXMLPresentation = 24
$ppLayoutText = 2
$readableTextColorRgb = 0

$powerPoint = $null
$presentation = $null

try {
    $powerPoint = New-Object -ComObject PowerPoint.Application
    $powerPoint.Visible = $true
    $presentation = $powerPoint.Presentations.Add()

    if ($templateFullPath) {
        $presentation.ApplyTemplate($templateFullPath)
    }

    while ($presentation.Slides.Count -gt 0) {
        $presentation.Slides.Item(1).Delete()
    }

    foreach ($m in $slideModels) {
        $slide = $presentation.Slides.Add($presentation.Slides.Count + 1, $ppLayoutText)

        $titleText = if ([string]::IsNullOrWhiteSpace($m.Subtitle)) { $m.Title } else { "{0}`n{1}" -f $m.Title, $m.Subtitle }
        $titleRange = $slide.Shapes.Title.TextFrame.TextRange
        $titleRange.Text = $titleText
        $titleRange.Font.Color.RGB = $readableTextColorRgb

        $content = if ($m.Bullets.Count -gt 0) {
            ($m.Bullets | ForEach-Object { [string]::Format([char]0x2022 + ' {0}', $_) }) -join "`r`n"
        } else {
            ""
        }

        $body = $slide.Shapes.Placeholders.Item(2).TextFrame.TextRange
        $body.Text = $content
        $body.Font.Color.RGB = $readableTextColorRgb

        if ($m.Notes.Count -gt 0) {
            $notesText = ($m.Notes -join "`r`n")
            $slide.NotesPage.Shapes.Placeholders.Item(2).TextFrame.TextRange.Text = $notesText
        }
    }

    # Create native PowerPoint sections from parsed markdown section metadata.
    if ($slideModels.Count -gt 0) {
        $sectionProperties = $presentation.SectionProperties
        $firstSectionName = [string]$slideModels[0].Section

        try {
            if ($sectionProperties.Count -ge 1) {
                $sectionProperties.Rename(1, $firstSectionName) | Out-Null
            }
            else {
                $sectionProperties.AddBeforeSlide(1, $firstSectionName) | Out-Null
            }
        }
        catch {
            # Ignore section rename/create issues and continue exporting slides.
        }

        $previousSectionName = $firstSectionName
        for ($i = 2; $i -le $slideModels.Count; $i++) {
            $currentSectionName = [string]$slideModels[$i - 1].Section
            if ($currentSectionName -ne $previousSectionName) {
                try {
                    $sectionProperties.AddBeforeSlide($i, $currentSectionName) | Out-Null
                }
                catch {
                    # Ignore section insertion issues and continue exporting slides.
                }
            }

            $previousSectionName = $currentSectionName
        }
    }

    $presentation.SaveAs($pptxFullPath, $ppSaveAsOpenXMLPresentation)
    Write-Host "Created: $pptxFullPath"
}
finally {
    if ($presentation) { $presentation.Close() }
    if ($powerPoint) { $powerPoint.Quit() }

    if ($null -ne $presentation) { [System.Runtime.InteropServices.Marshal]::ReleaseComObject($presentation) | Out-Null }
    if ($null -ne $powerPoint) { [System.Runtime.InteropServices.Marshal]::ReleaseComObject($powerPoint) | Out-Null }

    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()

    if ($downloadedTemplatePath -and (Test-Path -LiteralPath $downloadedTemplatePath)) {
        Remove-Item -LiteralPath $downloadedTemplatePath -Force -ErrorAction SilentlyContinue
    }
}
