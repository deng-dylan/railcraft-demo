[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^v[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [string]$BuildDirectory,

    [string]$OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($BuildDirectory)) {
    $BuildDirectory = Join-Path $repositoryRoot 'railcraft-unity\Builds\Whitebox'
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot 'railcraft-unity\ReleasePackages'
}

$buildRoot = (Resolve-Path -LiteralPath $BuildDirectory -ErrorAction Stop).Path
$requiredPaths = @(
    'RailCraftWhitebox.exe',
    'RailCraftWhitebox_Data',
    'MonoBleedingEdge',
    'D3D12',
    'UnityPlayer.dll'
)
foreach ($requiredPath in $requiredPaths) {
    $candidate = Join-Path $buildRoot $requiredPath
    if (-not (Test-Path -LiteralPath $candidate)) {
        throw "The required Unity build path is missing: $candidate"
    }
}

$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

$releaseName = "RailCraft-$Version-windows-x64"
$stagingRoot = Join-Path $outputRoot $releaseName
$archivePath = Join-Path $outputRoot "$releaseName.zip"
$checksumsPath = Join-Path $outputRoot "$releaseName.sha256"
foreach ($target in @($stagingRoot, $archivePath, $checksumsPath)) {
    if (Test-Path -LiteralPath $target) {
        throw "Release output already exists and will not be overwritten: $target"
    }
}

New-Item -ItemType Directory -Path $stagingRoot | Out-Null
Get-ChildItem -LiteralPath $buildRoot -Force | Copy-Item -Destination $stagingRoot -Recurse -Force

$releaseNotesFile = if ($Version -match '-art-alpha(?:\.|$)') {
    'railcraft-unity\Documentation\ArtAlpha.md'
} else {
    'railcraft-unity\Documentation\Release.md'
}
$releaseNotes = Join-Path $repositoryRoot $releaseNotesFile
if (Test-Path -LiteralPath $releaseNotes) {
    Copy-Item -LiteralPath $releaseNotes -Destination (
        Join-Path $stagingRoot "ReleaseNotes-$Version.md")
}

$runtimeGuide = Join-Path $repositoryRoot 'railcraft-unity\Documentation\ThirdPersonWhitebox.md'
if (Test-Path -LiteralPath $runtimeGuide) {
    Copy-Item -LiteralPath $runtimeGuide -Destination (
        Join-Path $stagingRoot 'ThirdPersonWhitebox.md')
}

Compress-Archive -Path (Join-Path $stagingRoot '*') -DestinationPath $archivePath -CompressionLevel Optimal

$exeHash = (Get-FileHash -LiteralPath (Join-Path $buildRoot 'RailCraftWhitebox.exe') -Algorithm SHA256).Hash.ToLowerInvariant()
$zipHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
@(
    "$exeHash  RailCraftWhitebox.exe"
    "$zipHash  $([System.IO.Path]::GetFileName($archivePath))"
) | Set-Content -LiteralPath $checksumsPath -Encoding ascii

[pscustomobject]@{
    Version = $Version
    BuildDirectory = $buildRoot
    StagingDirectory = $stagingRoot
    Archive = $archivePath
    Checksums = $checksumsPath
    ExecutableSha256 = $exeHash
    ArchiveSha256 = $zipHash
}
