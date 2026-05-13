param(
    [string]$CoverageResultsPath = "",
    [double]$MinimumLineCoverage = 100.0,
    [double]$MinimumBranchCoverage = 100.0
)

$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

function Resolve-DirectoryPath([string]$repoRoot, [string]$directoryPath) {
    if ([string]::IsNullOrWhiteSpace($directoryPath)) {
        return Join-Path $repoRoot "TestResults\Coverage"
    }

    if ([System.IO.Path]::IsPathRooted($directoryPath)) {
        return $directoryPath
    }

    return Join-Path $repoRoot $directoryPath
}

function Get-CoberturaCoverage([string]$root) {
    $files = Get-ChildItem -Path $root -Recurse -File -Filter *.xml |
        Where-Object { $_.Name -match "cobertura|coverage" }

    foreach ($file in $files) {
        try {
            [xml]$xml = Get-Content $file.FullName
            $coverage = $xml.coverage
            if ($null -ne $coverage -and $coverage.HasAttribute("line-rate")) {
                $line = [double]$coverage.GetAttribute("line-rate") * 100.0
                $branch = if ($coverage.HasAttribute("branch-rate")) {
                    [double]$coverage.GetAttribute("branch-rate") * 100.0
                } else {
                    100.0
                }

                return [pscustomobject]@{
                    Line = $line
                    Branch = $branch
                    Source = $file.FullName
                }
            }
        } catch {
            continue
        }
    }

    return $null
}

function Get-LcovCoverage([string]$root) {
    $files = Get-ChildItem -Path $root -Recurse -File -Include *.info,*.lcov
    foreach ($file in $files) {
        $linesFound = 0
        $linesHit = 0
        $branchesFound = 0
        $branchesHit = 0

        foreach ($line in Get-Content $file.FullName) {
            if ($line.StartsWith("LF:")) {
                $linesFound += [int]$line.Substring(3)
            } elseif ($line.StartsWith("LH:")) {
                $linesHit += [int]$line.Substring(3)
            } elseif ($line.StartsWith("BRF:")) {
                $branchesFound += [int]$line.Substring(4)
            } elseif ($line.StartsWith("BRH:")) {
                $branchesHit += [int]$line.Substring(4)
            }
        }

        if ($linesFound -gt 0) {
            $linePercent = ($linesHit / $linesFound) * 100.0
            $branchPercent = if ($branchesFound -gt 0) {
                ($branchesHit / $branchesFound) * 100.0
            } else {
                100.0
            }

            return [pscustomobject]@{
                Line = $linePercent
                Branch = $branchPercent
                Source = $file.FullName
            }
        }
    }

    return $null
}

$repoRoot = Resolve-RepoRoot
$CoverageResultsPath = Resolve-DirectoryPath $repoRoot $CoverageResultsPath

if (-not (Test-Path $CoverageResultsPath)) {
    throw "Coverage results path '$CoverageResultsPath' does not exist. Run .\tools\run-editmode-tests.ps1 -Coverage first."
}

$coverage = Get-CoberturaCoverage $CoverageResultsPath
if ($null -eq $coverage) {
    $coverage = Get-LcovCoverage $CoverageResultsPath
}

if ($null -eq $coverage) {
    throw "No Cobertura or LCOV coverage report was found under '$CoverageResultsPath'. Run coverage with 'generateAdditionalReports'."
}

Write-Host ("Coverage source: {0}" -f $coverage.Source)
Write-Host ("Line coverage: {0:N2}%" -f $coverage.Line)
Write-Host ("Branch coverage: {0:N2}%" -f $coverage.Branch)

if ($coverage.Line + 0.0001 -lt $MinimumLineCoverage) {
    throw ("Line coverage {0:N2}% is below required {1:N2}%." -f $coverage.Line, $MinimumLineCoverage)
}

if ($coverage.Branch + 0.0001 -lt $MinimumBranchCoverage) {
    throw ("Branch coverage {0:N2}% is below required {1:N2}%." -f $coverage.Branch, $MinimumBranchCoverage)
}

Write-Host "Coverage threshold satisfied."
