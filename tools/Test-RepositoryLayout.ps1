[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:Failures = New-Object System.Collections.Generic.List[string]
$script:PassedChecks = 0

function Add-Pass {
    param([Parameter(Mandatory = $true)][string]$Message)

    $script:PassedChecks++
    Write-Host "[PASS] $Message" -ForegroundColor Green
}

function Add-Failure {
    param([Parameter(Mandatory = $true)][string]$Message)

    $script:Failures.Add($Message)
    Write-Host "[FAIL] $Message" -ForegroundColor Red
}

function Format-PathSample {
    param(
        [Parameter(Mandatory = $true)][object[]]$Paths,
        [int]$Limit = 8
    )

    $values = @($Paths | Select-Object -First $Limit)
    $suffix = if ($Paths.Count -gt $Limit) { " (+$($Paths.Count - $Limit) more)" } else { "" }
    return ($values -join ", ") + $suffix
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$previousLocation = Get-Location

try {
    Set-Location -LiteralPath $repositoryRoot

    $insideWorkTree = & git rev-parse --is-inside-work-tree 2>$null
    if ($LASTEXITCODE -ne 0 -or $insideWorkTree -ne "true") {
        throw "Repository layout check must run inside a Git working tree."
    }

    $requiredFiles = @(
        ".gitattributes",
        ".gitignore",
        "apps/railcraft-unity/Packages/manifest.json",
        "apps/railcraft-unity/Packages/packages-lock.json",
        "apps/railcraft-unity/ProjectSettings/ProjectVersion.txt",
        "apps/railcraft-unity/ProjectSettings/EditorBuildSettings.asset",
        "apps/railcraft-unity/Assets/RailCraft/Scenes/Bootstrap.unity",
        "apps/railcraft-unity/Assets/RailCraft/Scenes/Factory.unity",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson.meta",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Scenes/ThirdPersonWhitebox.unity",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Editor/RailCraft.ThirdPerson.Editor.asmdef",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Editor/WhiteboxSceneBuilder.cs",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Editor/WhiteboxWindowsBuild.cs",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Runtime/Domain/RailCraft.ThirdPerson.Domain.asmdef",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Runtime/Domain/WhiteboxGameCatalog.cs",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Runtime/Domain/WhiteboxGameSession.cs",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Runtime/Domain/WhiteboxQuestionBank.cs",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Runtime/Player/RailCraft.ThirdPerson.Player.asmdef",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Runtime/UI/RailCraft.ThirdPerson.UI.asmdef",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Runtime/UI/WhiteboxAutomatedSmokeRunner.cs",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Runtime/World/RailCraft.ThirdPerson.World.asmdef",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Runtime/World/WhiteboxGameSessionHost.cs",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Tests/EditMode/Domain/RailCraft.ThirdPerson.Domain.EditModeTests.asmdef",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Tests/EditMode/Domain/WhiteboxGameCatalogTests.cs",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Tests/EditMode/Domain/WhiteboxGameSessionTests.cs",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Tests/EditMode/Player/RailCraft.ThirdPerson.Player.EditModeTests.asmdef",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Tests/EditMode/World/RailCraft.ThirdPerson.World.EditModeTests.asmdef",
        "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Tests/EditMode/World/WhiteboxWorldInteractionTests.cs"
    )
    $missingRequiredFiles = @($requiredFiles | Where-Object {
        -not (Test-Path -LiteralPath (Join-Path $repositoryRoot $_) -PathType Leaf)
    })
    if ($missingRequiredFiles.Count -eq 0) {
        Add-Pass "All $($requiredFiles.Count) Unity mainline anchor files are present."
    }
    else {
        Add-Failure "Missing Unity mainline anchor files: $(Format-PathSample $missingRequiredFiles)."
    }

    $thirdPersonRelativeRoot = "apps/railcraft-unity/Assets/RailCraft/ThirdPerson"
    $thirdPersonRoot = Join-Path $repositoryRoot $thirdPersonRelativeRoot
    $thirdPersonRootMeta = "$thirdPersonRoot.meta"
    $missingMetaFiles = New-Object System.Collections.Generic.List[string]
    if (-not (Test-Path -LiteralPath $thirdPersonRootMeta -PathType Leaf)) {
        $missingMetaFiles.Add("$thirdPersonRelativeRoot.meta")
    }
    if (Test-Path -LiteralPath $thirdPersonRoot -PathType Container) {
        foreach ($item in Get-ChildItem -LiteralPath $thirdPersonRoot -Recurse -Force) {
            if (-not $item.Name.EndsWith(".meta", [StringComparison]::OrdinalIgnoreCase)) {
                $expectedMeta = "$($item.FullName).meta"
                if (-not (Test-Path -LiteralPath $expectedMeta -PathType Leaf)) {
                    $relativeItem = $item.FullName.Substring($repositoryRoot.Length + 1).Replace('\', '/')
                    $missingMetaFiles.Add("$relativeItem.meta")
                }
            }
        }
    }

    $metaFiles = @()
    if (Test-Path -LiteralPath $thirdPersonRootMeta -PathType Leaf) {
        $metaFiles += Get-Item -LiteralPath $thirdPersonRootMeta
    }
    if (Test-Path -LiteralPath $thirdPersonRoot -PathType Container) {
        $metaFiles += Get-ChildItem -LiteralPath $thirdPersonRoot -Recurse -File -Filter "*.meta"
    }
    $invalidMetaFiles = New-Object System.Collections.Generic.List[string]
    $guidOwners = @{}
    foreach ($metaFile in $metaFiles) {
        $metaContent = Get-Content -Raw -Encoding utf8 -LiteralPath $metaFile.FullName
        $guidMatch = [regex]::Match($metaContent, '(?m)^guid:[ \t]*(?<guid>[0-9a-fA-F]{32})[ \t]*\r?$')
        $relativeMeta = $metaFile.FullName.Substring($repositoryRoot.Length + 1).Replace('\', '/')
        if (-not $guidMatch.Success) {
            $invalidMetaFiles.Add($relativeMeta)
            continue
        }

        $guid = $guidMatch.Groups['guid'].Value.ToLowerInvariant()
        if (-not $guidOwners.ContainsKey($guid)) {
            $guidOwners[$guid] = New-Object System.Collections.Generic.List[string]
        }
        $guidOwners[$guid].Add($relativeMeta)
    }
    $duplicateGuidGroups = @($guidOwners.GetEnumerator() | Where-Object { $_.Value.Count -gt 1 })

    if ($missingMetaFiles.Count -eq 0 -and $invalidMetaFiles.Count -eq 0 -and $duplicateGuidGroups.Count -eq 0) {
        Add-Pass "Unity ThirdPerson assets have complete metadata and $($metaFiles.Count) unique GUIDs."
    }
    else {
        if ($missingMetaFiles.Count -gt 0) {
            Add-Failure "Unity assets or folders missing .meta files: $(Format-PathSample $missingMetaFiles.ToArray())."
        }
        if ($invalidMetaFiles.Count -gt 0) {
            Add-Failure "Unity .meta files with missing or invalid GUIDs: $(Format-PathSample $invalidMetaFiles.ToArray())."
        }
        if ($duplicateGuidGroups.Count -gt 0) {
            $duplicateDescriptions = @($duplicateGuidGroups | ForEach-Object { $_.Value -join ' = ' })
            Add-Failure "Duplicate Unity GUIDs: $(Format-PathSample $duplicateDescriptions)."
        }
    }

    $buildSettingsPath = Join-Path $repositoryRoot "apps/railcraft-unity/ProjectSettings/EditorBuildSettings.asset"
    if (Test-Path -LiteralPath $buildSettingsPath -PathType Leaf) {
        $buildSettings = Get-Content -Raw -Encoding utf8 -LiteralPath $buildSettingsPath
        $sceneMatches = [regex]::Matches(
            $buildSettings,
            '(?m)^[ \t]*-[ \t]+enabled:[ \t]*(?<enabled>[01])[ \t]*\r?\n[ \t]+path:[ \t]*(?<path>[^\r\n]+)')
        $enabledScenes = @($sceneMatches | Where-Object {
            $_.Groups['enabled'].Value -eq '1'
        } | ForEach-Object {
            $_.Groups['path'].Value.Trim()
        })
        $expectedScenes = @(
            "Assets/RailCraft/ThirdPerson/Scenes/ThirdPersonWhitebox.unity"
        )
        $sceneLayoutMatches = $enabledScenes.Count -eq $expectedScenes.Count
        if ($sceneLayoutMatches) {
            for ($index = 0; $index -lt $expectedScenes.Count; $index++) {
                if ($enabledScenes[$index] -cne $expectedScenes[$index]) {
                    $sceneLayoutMatches = $false
                    break
                }
            }
        }

        if ($sceneLayoutMatches) {
            Add-Pass "EditorBuildSettings uses ThirdPersonWhitebox as the single default scene."
        }
        else {
            Add-Failure "EditorBuildSettings must enable only ThirdPersonWhitebox; found: $($enabledScenes -join ', ')."
        }
    }

    $packageManifestPath = Join-Path $repositoryRoot "apps/railcraft-unity/Packages/manifest.json"
    $packageLockPath = Join-Path $repositoryRoot "apps/railcraft-unity/Packages/packages-lock.json"
    $qualitySettingsPath = Join-Path $repositoryRoot "apps/railcraft-unity/ProjectSettings/QualitySettings.asset"
    $mobileAssetPaths = @(
        "apps/railcraft-unity/Assets/Settings/Mobile_RPAsset.asset",
        "apps/railcraft-unity/Assets/Settings/Mobile_RPAsset.asset.meta",
        "apps/railcraft-unity/Assets/Settings/Mobile_Renderer.asset",
        "apps/railcraft-unity/Assets/Settings/Mobile_Renderer.asset.meta"
    )
    $mobilePipelineGuid = "5e6cbd92db86f4b18aec3ed561671858"
    $desktopPipelineGuid = "4b83569d67af61e458304325a23e5dfd"
    $releaseTargetIssues = New-Object System.Collections.Generic.List[string]

    $packageManifest = Get-Content -Raw -Encoding utf8 -LiteralPath $packageManifestPath | ConvertFrom-Json
    $packageLock = Get-Content -Raw -Encoding utf8 -LiteralPath $packageLockPath | ConvertFrom-Json
    if ($packageManifest.dependencies.PSObject.Properties.Name -contains "com.unity.modules.androidjni") {
        $releaseTargetIssues.Add("manifest retains com.unity.modules.androidjni")
    }
    if ($packageLock.dependencies.PSObject.Properties.Name -contains "com.unity.modules.androidjni") {
        $releaseTargetIssues.Add("packages-lock retains com.unity.modules.androidjni")
    }

    $presentMobileAssets = @($mobileAssetPaths | Where-Object {
        Test-Path -LiteralPath (Join-Path $repositoryRoot $_)
    })
    foreach ($assetPath in $presentMobileAssets) {
        $releaseTargetIssues.Add("mobile render asset remains: $assetPath")
    }

    $qualitySettings = Get-Content -Raw -Encoding utf8 -LiteralPath $qualitySettingsPath
    if ($qualitySettings.Contains($mobilePipelineGuid, [StringComparison]::OrdinalIgnoreCase)) {
        $releaseTargetIssues.Add("QualitySettings still references the mobile render pipeline")
    }
    if (-not $qualitySettings.Contains($desktopPipelineGuid, [StringComparison]::OrdinalIgnoreCase)) {
        $releaseTargetIssues.Add("QualitySettings does not reference the desktop render pipeline")
    }

    if ($releaseTargetIssues.Count -eq 0) {
        Add-Pass "Unity package and render configuration exposes Windows as the only release target."
    }
    else {
        Add-Failure "Non-Windows release configuration remains: $(Format-PathSample $releaseTargetIssues.ToArray())."
    }

    $questionBankPath = Join-Path $repositoryRoot "apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Runtime/Domain/WhiteboxQuestionBank.cs"
    if (Test-Path -LiteralPath $questionBankPath -PathType Leaf) {
        $questionBank = Get-Content -Raw -Encoding utf8 -LiteralPath $questionBankPath
        $definitionMatches = [regex]::Matches(
            $questionBank,
            'new\s+QuizQuestionDefinition\s*\(\s*"(?<id>[^"]+)"',
            [System.Text.RegularExpressions.RegexOptions]::Singleline)
        $questionIds = @($definitionMatches | ForEach-Object { $_.Groups['id'].Value })
        $duplicateQuestionIds = @($questionIds | Group-Object | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name })
        $expectedQuestionIds = @(
            (1..50 | ForEach-Object { "bank_mc{0:D2}" -f $_ }),
            (1..8 | ForEach-Object { "bank_tf{0:D2}" -f $_ })
        )
        $expectedQuestionIds = @($expectedQuestionIds | ForEach-Object { $_ })
        $missingQuestionIds = @($expectedQuestionIds | Where-Object { $_ -notin $questionIds })
        $unexpectedQuestionIds = @($questionIds | Where-Object { $_ -notin $expectedQuestionIds })
        $choiceCount = @($questionIds | Where-Object { $_ -match '^bank_mc\d{2}$' }).Count
        $judgmentCount = @($questionIds | Where-Object { $_ -match '^bank_tf\d{2}$' }).Count

        if ($questionIds.Count -eq 58 -and $choiceCount -eq 50 -and $judgmentCount -eq 8 -and
            $duplicateQuestionIds.Count -eq 0 -and $missingQuestionIds.Count -eq 0 -and
            $unexpectedQuestionIds.Count -eq 0) {
            Add-Pass "Question bank has 58 unique IDs: 50 choice and 8 judgment questions."
        }
        else {
            Add-Failure (
                "Question bank IDs are invalid (total=$($questionIds.Count), choice=$choiceCount, " +
                "judgment=$judgmentCount, duplicates=$($duplicateQuestionIds.Count), " +
                "missing=$($missingQuestionIds.Count), unexpected=$($unexpectedQuestionIds.Count)).")
        }
    }

    $trackedPaths = @(& git -c core.quotepath=false ls-files)
    if ($LASTEXITCODE -ne 0) {
        throw "git ls-files failed with exit code $LASTEXITCODE."
    }
    $trackedPaths = @($trackedPaths | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object {
        $_.Replace('\', '/')
    })
    $trackedGeneratedFiles = @($trackedPaths | Where-Object {
        $_ -match '^apps/railcraft-unity/(?:Library|Temp|Obj|Logs|UserSettings|TestResults|Builds|MemoryCaptures|Recordings|\.vs)(?:/|$)'
    })
    if ($trackedGeneratedFiles.Count -eq 0) {
        Add-Pass "No Unity generated directories are tracked."
    }
    else {
        Add-Failure "Tracked Unity generated files: $(Format-PathSample $trackedGeneratedFiles)."
    }

    $trackedReleaseFiles = @($trackedPaths | Where-Object {
        $_ -match '^deliveries(?:/[^/]+)*/release(?:/|$)'
    })
    if ($trackedReleaseFiles.Count -eq 0) {
        Add-Pass "No deliveries/**/release/** files are tracked."
    }
    else {
        Add-Failure "Tracked delivery release files: $(Format-PathSample $trackedReleaseFiles)."
    }

    $quarantinedPrototypeRoot =
        "prototypes/high-speed-rail-factory-godot-4.6.3/source/"
    $trackedQuarantinedFiles = @($trackedPaths | Where-Object {
        $_.StartsWith($quarantinedPrototypeRoot, [StringComparison]::OrdinalIgnoreCase)
    })
    if ($trackedQuarantinedFiles.Count -eq 0) {
        Add-Pass "The unlicensed high-speed-rail Godot source snapshot remains outside Git."
    }
    else {
        Add-Failure "Tracked quarantined prototype files: $(Format-PathSample $trackedQuarantinedFiles)."
    }

    $maximumOrdinaryBlobBytes = 95MB
    $stageLines = @(& git -c core.quotepath=false ls-files --stage)
    if ($LASTEXITCODE -ne 0) {
        throw "git ls-files --stage failed with exit code $LASTEXITCODE."
    }
    $indexEntries = New-Object System.Collections.Generic.List[object]
    foreach ($line in $stageLines) {
        $match = [regex]::Match($line, '^(?<mode>\d+)\s+(?<hash>[0-9a-f]+)\s+(?<stage>\d+)\t(?<path>.*)$')
        if ($match.Success -and $match.Groups['stage'].Value -eq '0') {
            $indexEntries.Add([pscustomobject]@{
                Mode = $match.Groups['mode'].Value
                Hash = $match.Groups['hash'].Value
                Path = $match.Groups['path'].Value.Replace('\', '/')
            })
        }
    }

    $objectHashes = @($indexEntries | ForEach-Object { $_.Hash } | Sort-Object -Unique)
    $objectSizes = @{}
    if ($objectHashes.Count -gt 0) {
        $batchArgument = '--batch-check=%(objectname) %(objecttype) %(objectsize)'
        $batchLines = @($objectHashes | & git cat-file $batchArgument)
        if ($LASTEXITCODE -ne 0) {
            throw "git cat-file --batch-check failed with exit code $LASTEXITCODE."
        }
        foreach ($line in $batchLines) {
            $match = [regex]::Match($line, '^(?<hash>[0-9a-f]+)\s+(?<type>\S+)\s+(?<size>\d+)$')
            if ($match.Success -and $match.Groups['type'].Value -eq 'blob') {
                $objectSizes[$match.Groups['hash'].Value] = [int64]$match.Groups['size'].Value
            }
        }
    }

    $largeOrdinaryFiles = New-Object System.Collections.Generic.List[string]
    $largestOrdinaryFile = $null
    foreach ($entry in $indexEntries) {
        if (-not $entry.Mode.StartsWith('100', [StringComparison]::Ordinal) -or
            -not $objectSizes.ContainsKey($entry.Hash)) {
            continue
        }

        $size = [int64]$objectSizes[$entry.Hash]
        if ($null -eq $largestOrdinaryFile -or $size -gt $largestOrdinaryFile.Size) {
            $largestOrdinaryFile = [pscustomobject]@{ Path = $entry.Path; Size = $size }
        }
        if ($size -gt $maximumOrdinaryBlobBytes) {
            $largeOrdinaryFiles.Add("$($entry.Path) ($([math]::Round($size / 1MB, 2)) MiB)")
        }
    }

    if ($largeOrdinaryFiles.Count -eq 0) {
        $largestDescription = if ($null -eq $largestOrdinaryFile) {
            "no ordinary files"
        }
        else {
            "$($largestOrdinaryFile.Path), $([math]::Round($largestOrdinaryFile.Size / 1MB, 2)) MiB"
        }
        Add-Pass "No tracked ordinary file exceeds 95 MiB (largest: $largestDescription)."
    }
    else {
        Add-Failure "Tracked ordinary files exceed 95 MiB: $(Format-PathSample $largeOrdinaryFiles.ToArray())."
    }
}
catch {
    Add-Failure "Unexpected repository check error: $($_.Exception.Message)"
}
finally {
    Set-Location -LiteralPath $previousLocation
}

if ($script:Failures.Count -gt 0) {
    Write-Host ""
    Write-Host "REPOSITORY_LAYOUT_CHECK_FAILED: $($script:Failures.Count) failure(s), $script:PassedChecks check(s) passed." -ForegroundColor Red
    foreach ($failure in $script:Failures) {
        Write-Host " - $failure" -ForegroundColor Red
    }
    exit 1
}

Write-Host ""
Write-Host "REPOSITORY_LAYOUT_CHECK_SUCCEEDED: $script:PassedChecks check(s) passed." -ForegroundColor Green
exit 0
