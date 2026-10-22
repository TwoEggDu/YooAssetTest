param([Parameter(Mandatory)][ValidatePattern('^[a-zA-Z0-9][a-zA-Z0-9_.-]*$')][string]$Version)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $projectRoot "Bundles\StandaloneWindows64\DefaultPackage\$Version"
$destinationRoot = Join-Path $projectRoot 'Server\html\CDN\PC\v1.0'
$versionFile = Join-Path $sourceRoot 'DefaultPackage.version'
if (-not (Test-Path -LiteralPath $versionFile)) { throw "Build version first; version file missing: $versionFile" }
if ((Get-Content -LiteralPath $versionFile -Raw).Trim() -ne $Version) { throw 'Build version file content does not match requested version.' }
$files = @(Get-ChildItem -LiteralPath $sourceRoot -File)
if (-not ($files | Where-Object Extension -eq '.bundle')) { throw 'No bundles in source package.' }
New-Item -ItemType Directory -Path $destinationRoot -Force | Out-Null
foreach ($file in $files | Where-Object Name -ne 'DefaultPackage.version') {
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $destinationRoot $file.Name) -Force
}
# Publish the version pointer last; keep older bundles for clients still using them.
$pendingVersion = Join-Path $destinationRoot 'DefaultPackage.version.pending'
Copy-Item -LiteralPath $versionFile -Destination $pendingVersion -Force
Move-Item -LiteralPath $pendingVersion -Destination (Join-Path $destinationRoot 'DefaultPackage.version') -Force
Write-Output "Published $Version ($($files.Count) files) to $destinationRoot"
