param(
    [string]$ProjectPath = "",
    [string]$UnityExe = "",
    [string]$ResultsPath = "",
    [string]$LogPath = "",
    [switch]$NoGraphics,
    [switch]$Coverage,
    [string]$CoverageResultsPath = "",
    [string]$CoverageHistoryPath = "",
    [string]$CoverageOptions = ""
)

$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

function Resolve-DefaultProjectPath([string]$repoRoot) {
    return (Resolve-Path (Join-Path $repoRoot "RoobsWorld")).Path
}

function Get-UnityEditorVersion([string]$projectPath) {
    $versionFile = Join-Path $projectPath "ProjectSettings\ProjectVersion.txt"
    if (-not (Test-Path $versionFile)) {
        throw "ProjectVersion.txt not found at '$versionFile'."
    }

    $line = (Get-Content $versionFile | Where-Object { $_ -like "m_EditorVersion:*" } | Select-Object -First 1)
    if (-not $line) {
        throw "Could not read 'm_EditorVersion' from '$versionFile'."
    }

    return ($line -replace "^m_EditorVersion:\s*", "").Trim()
}

function Resolve-UnityExe([string]$projectPath, [string]$explicitUnityExe) {
    if ($explicitUnityExe) {
        $resolved = (Resolve-Path $explicitUnityExe).Path
        if (-not (Test-Path $resolved)) { throw "Unity.exe not found at '$resolved'." }
        return $resolved
    }

    if ($env:UNITY_EXE) {
        $resolved = (Resolve-Path $env:UNITY_EXE).Path
        if (-not (Test-Path $resolved)) { throw "UNITY_EXE points to missing path '$resolved'." }
        return $resolved
    }

    $version = Get-UnityEditorVersion $projectPath
    $default = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
    if (Test-Path $default) { return $default }

    throw "Unity.exe not found. Either install Unity $version via Unity Hub, pass -UnityExe, or set UNITY_EXE env var."
}

function Ensure-DirectoryForFile([string]$filePath) {
    $dir = Split-Path -Parent $filePath
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir | Out-Null
    }
}

function Resolve-OutputFilePath([string]$repoRoot, [string]$filePath) {
    if ([System.IO.Path]::IsPathRooted($filePath)) {
        return $filePath
    }

    return Join-Path $repoRoot $filePath
}

function Resolve-OutputDirectoryPath([string]$repoRoot, [string]$directoryPath) {
    if ([System.IO.Path]::IsPathRooted($directoryPath)) {
        return $directoryPath
    }

    return Join-Path $repoRoot $directoryPath
}

function Get-LogTail([string]$logPath) {
    if (-not (Test-Path $logPath)) {
        return ""
    }

    return (Get-Content $logPath -Tail 40) -join [Environment]::NewLine
}

$repoRoot = Resolve-RepoRoot
if (-not $ProjectPath) {
    $ProjectPath = Resolve-DefaultProjectPath $repoRoot
} else {
    $ProjectPath = (Resolve-Path $ProjectPath).Path
}

if (-not $ResultsPath) {
    $ResultsPath = Join-Path $repoRoot "TestResults\EditMode.xml"
} else {
    $ResultsPath = Resolve-OutputFilePath $repoRoot $ResultsPath
}
if (-not $LogPath) {
    $LogPath = Join-Path $repoRoot "TestResults\EditMode.log"
} else {
    $LogPath = Resolve-OutputFilePath $repoRoot $LogPath
}
if (-not $CoverageResultsPath) {
    $CoverageResultsPath = Join-Path $repoRoot "TestResults\Coverage"
} else {
    $CoverageResultsPath = Resolve-OutputDirectoryPath $repoRoot $CoverageResultsPath
}
if (-not $CoverageHistoryPath) {
    $CoverageHistoryPath = Join-Path $repoRoot "TestResults\CoverageHistory"
} else {
    $CoverageHistoryPath = Resolve-OutputDirectoryPath $repoRoot $CoverageHistoryPath
}
if (-not $CoverageOptions) {
    $CoverageOptions = "generateAdditionalReports;assemblyFilters:+RoobsWorld;pathFilters:-**/Tests/**,-**/Editor/**"
}

Ensure-DirectoryForFile $ResultsPath
Ensure-DirectoryForFile $LogPath
if ($Coverage) {
    if (-not (Test-Path $CoverageResultsPath)) {
        New-Item -ItemType Directory -Path $CoverageResultsPath | Out-Null
    }
    if (-not (Test-Path $CoverageHistoryPath)) {
        New-Item -ItemType Directory -Path $CoverageHistoryPath | Out-Null
    }
}

if (Test-Path $ResultsPath) {
    Remove-Item -LiteralPath $ResultsPath -Force
}
if (Test-Path $LogPath) {
    Remove-Item -LiteralPath $LogPath -Force
}

$unity = Resolve-UnityExe $ProjectPath $UnityExe
$runStartedAt = [DateTime]::UtcNow.AddSeconds(-5)

$args = @(
    "-batchmode",
    "-runTests",
    "-projectPath", $ProjectPath,
    "-testPlatform", "EditMode",
    "-testResults", $ResultsPath,
    "-logFile", $LogPath
)

if ($NoGraphics) {
    $args = @("-nographics") + $args
}
if ($Coverage) {
    $args = @("-debugCodeOptimization", "-enableCodeCoverage") + $args + @(
        "-coverageResultsPath", $CoverageResultsPath,
        "-coverageHistoryPath", $CoverageHistoryPath,
        "-coverageOptions", $CoverageOptions
    )
}

Write-Host "Unity: $unity"
Write-Host "Project: $ProjectPath"
Write-Host "Results: $ResultsPath"
Write-Host "Log: $LogPath"
if ($Coverage) {
    Write-Host "Coverage results: $CoverageResultsPath"
    Write-Host "Coverage history: $CoverageHistoryPath"
    Write-Host "Coverage options: $CoverageOptions"
}
Write-Host ""

$process = Start-Process -FilePath $unity -ArgumentList $args -Wait -PassThru -WindowStyle Hidden
$exit = $process.ExitCode
if ($exit -ne 0) {
    $details = Get-LogTail $LogPath
    throw "Unity test run failed (exit code $exit). See log at '$LogPath'. Last log lines:`n$details"
}

if (-not (Test-Path $ResultsPath)) {
    $details = Get-LogTail $LogPath
    throw "Unity did not produce a test results file at '$ResultsPath'. This often happens if another Unity instance has the project open. Last log lines:`n$details"
}

$resultsItem = Get-Item $ResultsPath
if ($resultsItem.LastWriteTimeUtc -lt $runStartedAt) {
    $details = Get-LogTail $LogPath
    throw "Unity left a stale test results file at '$ResultsPath' from $($resultsItem.LastWriteTime). Last log lines:`n$details"
}

[xml]$results = Get-Content $ResultsPath
$testRun = $results.'test-run'
if ($null -eq $testRun) {
    throw "Unity produced '$ResultsPath', but it is not a NUnit test-run XML file."
}

$failed = [int]$testRun.failed
$inconclusive = [int]$testRun.inconclusive
$skipped = [int]$testRun.skipped
$total = [int]$testRun.total
$passed = [int]$testRun.passed
if ($testRun.result -ne "Passed" -or $failed -ne 0 -or $inconclusive -ne 0 -or $skipped -ne 0) {
    throw "EditMode tests were not 100% green. Result=$($testRun.result), total=$total, passed=$passed, failed=$failed, inconclusive=$inconclusive, skipped=$skipped."
}

Write-Host "EditMode test run succeeded: $passed/$total passed, 0 failed, 0 skipped."
