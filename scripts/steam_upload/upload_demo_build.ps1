param(
    [string]$SteamCmdPath = "C:\godot_project\steamworks_sdk_164\sdk\tools\ContentBuilder\builder\steamcmd.exe",

    [string]$SteamUser = "chaosheng35",

    [string]$DepotId = "4778371",

    [string]$SetLiveBranch = "",

    [string]$GodotPath = "C:\Users\86189\Desktop\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64.exe",

    [switch]$SkipExport
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$appBuildTemplatePath = Join-Path $scriptDir "app_build_4778370.vdf"
$depotBuildTemplatePath = Join-Path $scriptDir "depot_build_4778371.vdf"
$generatedDir = Join-Path $scriptDir "generated"
$appBuildPath = Join-Path $generatedDir "app_build_4778370.generated.vdf"
$depotBuildPath = Join-Path $generatedDir "depot_build_$DepotId.generated.vdf"
$exportPath = "C:\godot_project\export\Four-Dimensional"
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)
$releaseExportScriptPath = Join-Path $scriptDir "export_demo_release.ps1"
$exportPresetsPath = Join-Path $projectRoot "export_presets.cfg"
$spineSidecars = @(
    [PSCustomObject]@{
        Source = Join-Path $projectRoot "bin\spine_godot_extension.gdextension"
        Destination = Join-Path $exportPath "bin\spine_godot_extension.gdextension"
    },
    [PSCustomObject]@{
        Source = Join-Path $projectRoot "bin\windows\libspine_godot.windows.template_release.x86_64.dll"
        Destination = Join-Path $exportPath "bin\windows\libspine_godot.windows.template_release.x86_64.dll"
    },
    [PSCustomObject]@{
        Source = Join-Path $projectRoot ".godot\extension_list.cfg"
        Destination = Join-Path $exportPath ".godot\extension_list.cfg"
    }
)
$requiredFiles = @(
    "Four-Dimensional.exe",
    "Four-Dimensional.pck",
    "data_tower_windows_x86_64",
    "libspine_godot.windows.template_release.x86_64.dll",
    "bin\spine_godot_extension.gdextension",
    "bin\windows\libspine_godot.windows.template_release.x86_64.dll",
    ".godot\extension_list.cfg"
)

if (-not (Test-Path -LiteralPath $SteamCmdPath)) {
    throw "steamcmd.exe not found: $SteamCmdPath"
}

if (-not (Test-Path -LiteralPath $appBuildTemplatePath)) {
    throw "App build template not found: $appBuildTemplatePath"
}

if (-not (Test-Path -LiteralPath $depotBuildTemplatePath)) {
    throw "Depot build template not found: $depotBuildTemplatePath"
}

if (-not (Test-Path -LiteralPath $exportPath)) {
    New-Item -ItemType Directory -Path $exportPath -Force | Out-Null
}

if (-not (Test-Path -LiteralPath $exportPresetsPath)) {
    throw "Export presets not found: $exportPresetsPath"
}

$exportPresetsText = Get-Content -LiteralPath $exportPresetsPath -Raw
foreach ($requiredExportResource in @(
    ".godot/extension_list.cfg",
    "bin/spine_godot_extension.gdextension"
)) {
    if ($exportPresetsText -notmatch [regex]::Escape($requiredExportResource)) {
        throw "Windows export preset must include this resource in the PCK: $requiredExportResource"
    }
}

if ($SkipExport) {
    Write-Host "Skipping release export by request; validating the existing export."
}
else {
    if (-not (Test-Path -LiteralPath $releaseExportScriptPath)) {
        throw "Release export script not found: $releaseExportScriptPath"
    }

    Write-Host "Exporting the current project before upload..."
    & $releaseExportScriptPath -GodotPath $GodotPath -OutputPath (Join-Path $exportPath "Four-Dimensional.exe")
}

foreach ($sidecar in $spineSidecars) {
    if (-not (Test-Path -LiteralPath $sidecar.Source)) {
        throw "Spine release sidecar is missing: $($sidecar.Source)"
    }

    New-Item -ItemType Directory -Path (Split-Path -Parent $sidecar.Destination) -Force | Out-Null
    Copy-Item -LiteralPath $sidecar.Source -Destination $sidecar.Destination -Force
}

foreach ($item in $requiredFiles) {
    $path = Join-Path $exportPath $item
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Export is missing required item: $path"
    }
}

$pckPath = Join-Path $exportPath "Four-Dimensional.pck"
if ((Get-Item -LiteralPath $pckPath).Length -lt 1MB) {
    throw "The external PCK is unexpectedly small: $pckPath"
}

New-Item -ItemType Directory -Path "C:\godot_project\steam_build_output" -Force | Out-Null
New-Item -ItemType Directory -Path $generatedDir -Force | Out-Null

$appBuildText = Get-Content -LiteralPath $appBuildTemplatePath -Raw
$appBuildText = $appBuildText.Replace('"4778371"', '"' + $DepotId + '"')
$appBuildText = $appBuildText.Replace('"SetLive" ""', '"SetLive" "' + $SetLiveBranch + '"')
$appBuildText = $appBuildText.Replace(
    '"C:/godot_project/Four-Dimensional/scripts/steam_upload/depot_build_4778371.vdf"',
    '"' + ($depotBuildPath -replace "\\", "/") + '"'
)
$appBuildText | Set-Content -LiteralPath $appBuildPath -Encoding ASCII

$depotBuildText = Get-Content -LiteralPath $depotBuildTemplatePath -Raw
$depotBuildText = $depotBuildText.Replace('"DepotID" "4778371"', '"DepotID" "' + $DepotId + '"')
$depotBuildText | Set-Content -LiteralPath $depotBuildPath -Encoding ASCII

Write-Host "Uploading Four-Dimensional Demo build..."
Write-Host "AppID: 4778370"
Write-Host "DepotID: $DepotId"
if ($SetLiveBranch) {
    Write-Host "SetLive: $SetLiveBranch"
} else {
    Write-Host "SetLive: disabled; set the build live manually in Steamworks after upload."
}
Write-Host "ContentRoot: $exportPath"
Write-Host "Prepared Spine release sidecars for the exported game."
Write-Host "Generated app build script: $appBuildPath"
Write-Host ""

$steamOutput = & $SteamCmdPath `
    +login $SteamUser `
    +run_app_build $appBuildPath `
    +quit 2>&1 | Out-String
$steamExitCode = $LASTEXITCODE
Write-Host $steamOutput

if ($steamExitCode -ne 0) {
    if ($SetLiveBranch) {
        throw "SteamCMD uploaded/scanned the content but Steam rejected the commit while setting branch '$SetLiveBranch' live. Run this script without -SetLiveBranch, then set the build live in Steamworks > SteamPipe > Builds."
    }

    throw "steamcmd failed with exit code $steamExitCode. See C:\godot_project\steamworks_sdk_164\sdk\tools\ContentBuilder\builder\logs\console_log.txt for the full response."
}

Write-Host ""
Write-Host "Upload command completed. Check Steamworks > SteamPipe > Builds for the new build."
