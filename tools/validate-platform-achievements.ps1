param(
    [string]$ProjectPath = "",
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"

if (-not $RepoRoot) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

if (-not $ProjectPath) {
    $ProjectPath = Join-Path $RepoRoot "RoobsWorld"
}

$assets = Join-Path $ProjectPath "Assets"
$packages = Join-Path $ProjectPath "Packages"
$config = Join-Path $assets "Scripts\Core\PlatformServicesConfig.cs"
$packageLock = Join-Path $packages "packages-lock.json"
$manifest = Join-Path $RepoRoot "docs\platform-achievements-manifest.csv"

function Test-AnyFileMatching([string[]]$Roots, [string]$Pattern) {
    foreach ($root in $Roots) {
        if (-not (Test-Path $root)) { continue }
        $match = Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match $Pattern } |
            Select-Object -First 1
        if ($match) { return $true }
    }
    return $false
}

function Add-Check([string]$Name, [bool]$Passed, [string]$Detail) {
    [PSCustomObject]@{
        Check = $Name
        Status = if ($Passed) { "PASS" } else { "MISSING" }
        Detail = $Detail
    }
}

$checks = New-Object System.Collections.Generic.List[object]

$steamSdk = Test-AnyFileMatching @($assets, $packages) "Steamworks|steam_api|steamworks"
$psSdk = Test-AnyFileMatching @($assets, $packages) "PlayStation|PS5|PS4|Sony"
$xboxSdk = Test-AnyFileMatching @($assets, $packages) "Xbox|GDK|XSAPI|Microsoft\.Xbox"

$packageLockText = if (Test-Path $packageLock) { Get-Content $packageLock -Raw } else { "" }
$steamSdk = $steamSdk -or ($packageLockText -match "com\.rlabrecque\.steamworks\.net")

$configText = if (Test-Path $config) { Get-Content $config -Raw } else { "" }
$manifestExists = Test-Path $manifest

$checks.Add((Add-Check "Achievement manifest" $manifestExists "Expected docs/platform-achievements-manifest.csv"))
$checks.Add((Add-Check "Steamworks.NET package" $steamSdk "Import Steamworks.NET into Assets or Packages"))
$checks.Add((Add-Check "PlayStation SDK/plugin" $psSdk "Install Sony PlayStation Unity SDK/plugin"))
$checks.Add((Add-Check "Xbox GDK/XSAPI" $xboxSdk "Install Microsoft Xbox GDK/XSAPI Unity integration"))
$checks.Add((Add-Check "Steam App ID replaced" ($configText -notmatch "SteamAppId\s*=\s*SteamDevelopmentAppId") "Set PlatformServicesConfig.SteamAppId to real Steam App ID"))
$checks.Add((Add-Check "PlayStation service ID replaced" ($configText -notmatch "REPLACE_WITH_PLAYSTATION_TITLE_SERVICE_ID") "Set PlatformServicesConfig.PlayStationTitleServiceId"))
$checks.Add((Add-Check "Xbox title ID replaced" ($configText -notmatch "REPLACE_WITH_XBOX_TITLE_ID") "Set PlatformServicesConfig.XboxTitleId"))

$checks | Format-Table -AutoSize

$missing = $checks | Where-Object { $_.Status -ne "PASS" }
if ($missing.Count -gt 0) {
    Write-Host ""
    Write-Host "Platform achievement setup is not complete. Missing: $($missing.Count)" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Platform achievement local prerequisites look complete." -ForegroundColor Green
