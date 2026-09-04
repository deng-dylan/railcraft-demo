param(
    [string]$BuildRoot = (Join-Path $PSScriptRoot '..\railcraft-unity\Builds\InternalTest')
)

$resolvedRoot = [System.IO.Path]::GetFullPath($BuildRoot)
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\railcraft-unity'))
$allowedRoot = [System.IO.Path]::GetFullPath((Join-Path $projectRoot 'Builds\InternalTest'))
$allowedPrefix = $allowedRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
$isAllowed = $resolvedRoot.Equals($allowedRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
    $resolvedRoot.StartsWith($allowedPrefix, [System.StringComparison]::OrdinalIgnoreCase)
if (-not $isAllowed) {
    throw "Refusing to launch an executable outside Builds/InternalTest: $resolvedRoot"
}

$executable = Join-Path $resolvedRoot 'RailCraftInternalTest.exe'
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw "Internal test build is missing: $executable"
}

Start-Process -FilePath $executable -ArgumentList '-railcraft-internal-debug' -WorkingDirectory $resolvedRoot
Write-Host 'Internal build started. Use Ctrl+Alt+Shift+F10 on the main menu to unlock debug mode.'
