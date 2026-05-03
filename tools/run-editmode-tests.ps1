param(
    [string]$ProjectPath = "",
    [string]$UnityExe = "",
    [string]$ResultsPath = "",
    [string]$LogPath = "",
    [switch]$NoGraphics
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

$repoRoot = Resolve-RepoRoot
if (-not $ProjectPath) {
    $ProjectPath = Resolve-DefaultProjectPath $repoRoot
} else {
    $ProjectPath = (Resolve-Path $ProjectPath).Path
}

if (-not $ResultsPath) {
    $ResultsPath = Join-Path $repoRoot "TestResults\EditMode.xml"
}
if (-not $LogPath) {
    $LogPath = Join-Path $repoRoot "TestResults\EditMode.log"
}

Ensure-DirectoryForFile $ResultsPath
Ensure-DirectoryForFile $LogPath

$unity = Resolve-UnityExe $ProjectPath $UnityExe

$args = @(
    "-batchmode",
    "-runTests",
    "-projectPath", $ProjectPath,
    "-testPlatform", "EditMode",
    "-testResults", $ResultsPath,
    "-logFile", $LogPath,
    "-quit"
)

if ($NoGraphics) {
    $args = @("-nographics") + $args
}

Write-Host "Unity: $unity"
Write-Host "Project: $ProjectPath"
Write-Host "Results: $ResultsPath"
Write-Host "Log: $LogPath"
Write-Host ""

& $unity @args

$exit = $LASTEXITCODE
if ($exit -ne 0) {
    throw "Unity test run failed (exit code $exit). See log at '$LogPath'."
}

if (-not (Test-Path $ResultsPath)) {
    $lastLines = @()
    if (Test-Path $LogPath) {
        $lastLines = Get-Content $LogPath -Tail 40
    }
    $details = ($lastLines -join [Environment]::NewLine)
    throw "Unity did not produce a test results file at '$ResultsPath'. This often happens if another Unity instance has the project open. Last log lines:`n$details"
}

Write-Host "EditMode test run succeeded."
