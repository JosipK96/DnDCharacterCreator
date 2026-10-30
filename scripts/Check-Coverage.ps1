<#
.SYNOPSIS
    Prints line and branch coverage per project from Cobertura files, and fails when Domain or
    Application is below its floor.

.DESCRIPTION
    Reads every <test project>.cobertura.xml in -Path (the name the test steps in
    .github/workflows/ci.yml give them), prints the numbers for each assembly in it, and to the
    GitHub job summary when there is one. A floor applies only to the report of that assembly's own
    unit test project, so the check needs no Docker.
#>
[CmdletBinding()]
param(
    [string] $Path = 'TestResults'
)

$ErrorActionPreference = 'Stop'

# The floors. Raise one when its coverage has gone up; never lower one to get a pull request green.
# Line coverage, in percent, of the assembly measured by its own unit test project.
$floors = @(
    @{ Report = 'CharacterCreatorApp.Domain.UnitTests'; Assembly = 'CharacterCreatorApp.Domain'; MinLine = 90 }
    @{ Report = 'CharacterCreatorApp.Application.UnitTests'; Assembly = 'CharacterCreatorApp.Application'; MinLine = 83 }
)

function Get-AssemblyCoverage($package) {
    $lines = @($package.SelectNodes('classes/class/lines/line'))
    $covered = @($lines | Where-Object { [int]$_.hits -gt 0 }).Count
    $branchesCovered = 0
    $branchesValid = 0
    foreach ($line in $lines) {
        $condition = $line.GetAttribute('condition-coverage')
        if ($condition -match '\((\d+)/(\d+)\)') {
            $branchesCovered += [int]$Matches[1]
            $branchesValid += [int]$Matches[2]
        }
    }
    [pscustomobject]@{
        Assembly        = $package.name
        LinesCovered    = $covered
        LinesValid      = $lines.Count
        BranchesCovered = $branchesCovered
        BranchesValid   = $branchesValid
    }
}

function Format-Percent($covered, $valid) {
    if ($valid -eq 0) { return 'n/a' }
    [string]::Format([cultureinfo]::InvariantCulture, '{0:N1}%', 100.0 * $covered / $valid)
}

$files = @(Get-ChildItem -Path $Path -Filter '*.cobertura.xml' -File -ErrorAction SilentlyContinue)
$summary = [System.Collections.Generic.List[string]]::new()
$summary.Add('| Test project | Assembly | Line | Branch |')
$summary.Add('|---|---|---|---|')
$failures = [System.Collections.Generic.List[string]]::new()
$measured = @{}

foreach ($file in $files) {
    $report = $file.Name -replace '\.cobertura\.xml$', ''
    $xml = [xml](Get-Content -LiteralPath $file.FullName -Raw)
    foreach ($package in $xml.coverage.packages.package) {
        $coverage = Get-AssemblyCoverage $package
        $line = Format-Percent $coverage.LinesCovered $coverage.LinesValid
        $branch = Format-Percent $coverage.BranchesCovered $coverage.BranchesValid
        $summary.Add("| $report | $($coverage.Assembly) | $line ($($coverage.LinesCovered)/$($coverage.LinesValid)) | $branch ($($coverage.BranchesCovered)/$($coverage.BranchesValid)) |")
        $measured["$report|$($coverage.Assembly)"] = $coverage
    }
}

foreach ($floor in $floors) {
    $coverage = $measured["$($floor.Report)|$($floor.Assembly)"]
    if ($null -eq $coverage -or $coverage.LinesValid -eq 0) {
        $failures.Add("$($floor.Assembly): no coverage found for it in $($floor.Report).cobertura.xml under $Path")
        continue
    }
    $actual = 100.0 * $coverage.LinesCovered / $coverage.LinesValid
    if ($actual -lt $floor.MinLine) {
        $failures.Add([string]::Format([cultureinfo]::InvariantCulture, '{0}: line coverage {1:N1}% ({2}/{3}) is below its floor of {4}%; branch coverage {5}',
            $floor.Assembly, $actual, $coverage.LinesCovered, $coverage.LinesValid, $floor.MinLine,
            (Format-Percent $coverage.BranchesCovered $coverage.BranchesValid)))
    }
}

$summary | ForEach-Object { Write-Host $_ }

if ($env:GITHUB_STEP_SUMMARY) {
    $content = @('## Test coverage', '') + $summary
    if ($failures.Count -gt 0) {
        $content += @('', '**Below the floor:**') + ($failures | ForEach-Object { "- $_" })
    }
    $content | Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY
}

foreach ($failure in $failures) {
    if ($env:GITHUB_ACTIONS) { Write-Host "::error::$failure" } else { Write-Host "FAIL $failure" }
}
if ($failures.Count -gt 0) { exit 1 }
