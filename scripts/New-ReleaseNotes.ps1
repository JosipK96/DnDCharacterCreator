<#
.SYNOPSIS
    Writes the notes of a release: the pull requests merged since the previous tag, each with its
    labels and its "Deployment notes" section.

.DESCRIPTION
    Pull requests are squash merged, so each commit between the previous tag and this one ends in
    "(#N)". The labels (db-migration, reference-data, config-change, container-change) and the
    "## Deployment notes" section come from the pull request itself, the ones /release-labels put
    there. Without a previous tag (the first release) the list is titles only: everything built
    so far is new to the group, so a deployment note per old pull request would only be noise.
    Needs git history (fetch-depth 0) and a GH_TOKEN for gh.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Tag,
    [string] $Previous = '',
    [string] $OutFile = 'release-notes.md'
)

$ErrorActionPreference = 'Stop'

$range = if ($Previous) { "$Previous..$Tag" } else { $Tag }
$numbers = git log $range --format=%s |
    ForEach-Object { if ($_ -match '\(#(\d+)\)\s*$') { [int] $Matches[1] } } |
    Select-Object -Unique

$lines = @()
$lines += if ($Previous) { "Changes since $Previous." } else { 'The first release.' }

$blocks = @()
foreach ($number in $numbers) {
    $json = gh pr view $number --json number,title,url,labels,body
    $pr = $json | ConvertFrom-Json
    $labels = @($pr.labels | ForEach-Object { '`' + $_.name + '`' })
    $heading = "- [#$($pr.number)]($($pr.url)) $($pr.title)"
    if ($labels.Count -gt 0) {
        $heading += ' (' + ($labels -join ', ') + ')'
    }
    $blocks += $heading

    if (-not $Previous) {
        continue
    }

    # The text under "## Deployment notes", up to the next heading
    if ($pr.body -match '(?ms)^##\s+Deployment notes\s*\r?\n(.*?)(?=^##\s|\z)') {
        $notes = $Matches[1].Trim()
        if ($notes) {
            $blocks += ($notes -split '\r?\n' | ForEach-Object { "  > $_" })
        }
    }
}

if ($blocks.Count -eq 0) {
    $lines += ''
    $lines += 'No pull requests.'
} else {
    $lines += ''
    $lines += '## Pull requests'
    $lines += ''
    $lines += $blocks
}

Set-Content -Path $OutFile -Value $lines -Encoding utf8
