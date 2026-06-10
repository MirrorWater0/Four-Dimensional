param(
    [string]$SteamCmdPath = "C:\godot_project\steamworks_sdk_164\sdk\tools\ContentBuilder\builder\steamcmd.exe",

    [Parameter(Mandatory = $true)]
    [string]$SteamUser,

    [string]$DepotId = "4778371",

    [string]$SetLiveBranch = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$appBuildTemplatePath = Join-Path $scriptDir "app_build_4778370.vdf"
$depotBuildTemplatePath = Join-Path $scriptDir "depot_build_4778371.vdf"
$generatedDir = Join-Path $scriptDir "generated"
$appBuildPath = Join-Path $generatedDir "app_build_4778370.generated.vdf"
$depotBuildPath = Join-Path $generatedDir "depot_build_$DepotId.generated.vdf"
$exportPath = "C:\godot_project\export\Four-Dimensional"
$requiredFiles = @(
    "Four-Dimensional.exe",
    "data_tower_windows_x86_64",
    "libspine_godot.windows.template_debug.x86_64.dll"
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
    throw "Export folder not found: $exportPath"
}

foreach ($item in $requiredFiles) {
    $path = Join-Path $exportPath $item
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Export is missing required item: $path"
    }
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
Write-Host "Generated app build script: $appBuildPath"
Write-Host ""

& $SteamCmdPath `
    +login $SteamUser `
    +run_app_build $appBuildPath `
    +quit

if ($LASTEXITCODE -ne 0) {
    throw "steamcmd failed with exit code $LASTEXITCODE"
}

Write-Host ""
Write-Host "Upload command completed. Check Steamworks > SteamPipe > Builds for the new build."
