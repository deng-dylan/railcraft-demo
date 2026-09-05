[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^v[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [string]$BuildDirectory,

    [string]$OutputDirectory,

    [ValidateSet('Standard', 'InternalTest')]
    [string]$PackageKind = 'Standard'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($BuildDirectory)) {
    $relativeBuildDirectory = if ($PackageKind -eq 'InternalTest') {
        'railcraft-unity\Builds\InternalTest'
    } else {
        'railcraft-unity\Builds\Whitebox'
    }
    $BuildDirectory = Join-Path $repositoryRoot $relativeBuildDirectory
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot 'railcraft-unity\ReleasePackages'
}

$buildRoot = (Resolve-Path -LiteralPath $BuildDirectory -ErrorAction Stop).Path
$executableName = if ($PackageKind -eq 'InternalTest') {
    'RailCraftInternalTest.exe'
} else {
    'RailCraftWhitebox.exe'
}
$dataDirectoryName = [System.IO.Path]::GetFileNameWithoutExtension($executableName) + '_Data'
$requiredPaths = @(
    $executableName,
    $dataDirectoryName,
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

$releaseNotesFile = if ($PackageKind -eq 'InternalTest') {
    'railcraft-unity\Documentation\InternalTest.md'
} elseif ($Version -match '-art-alpha(?:\.|$)') {
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

# ART-004: CC BY attribution must travel with the compiled model in the ZIP.
$operationTableNotices = Join-Path $repositoryRoot 'railcraft-unity\Assets\RailCraft\ThirdPerson\Art\ThirdParty\Components\RebelHideoutOperationTable'
$operationTableNoticeTarget = Join-Path $stagingRoot 'ThirdPartyNotices\RebelHideoutOperationTable'
New-Item -ItemType Directory -Force -Path $operationTableNoticeTarget | Out-Null
foreach ($noticeName in @('README.md', 'LICENSE.txt', 'asset-manifest.json')) {
    Copy-Item -LiteralPath (Join-Path $operationTableNotices $noticeName) -Destination $operationTableNoticeTarget
}
if ($PackageKind -eq 'InternalTest') {
    @('@echo off', 'start "" "%~dp0RailCraftInternalTest.exe" -railcraft-internal-debug') |
        Set-Content -LiteralPath (Join-Path $stagingRoot 'StartInternalDebug.cmd') -Encoding ascii
}

Compress-Archive -Path (Join-Path $stagingRoot '*') -DestinationPath $archivePath -CompressionLevel Optimal

$exeHash = (Get-FileHash -LiteralPath (Join-Path $buildRoot $executableName) -Algorithm SHA256).Hash.ToLowerInvariant()
$zipHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
@(
    "$exeHash  $executableName"
    "$zipHash  $([System.IO.Path]::GetFileName($archivePath))"
) | Set-Content -LiteralPath $checksumsPath -Encoding ascii

[pscustomobject]@{
    Version = $Version
    PackageKind = $PackageKind
    BuildDirectory = $buildRoot
    StagingDirectory = $stagingRoot
    Archive = $archivePath
    Checksums = $checksumsPath
    ExecutableSha256 = $exeHash
    ArchiveSha256 = $zipHash
}
