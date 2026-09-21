param(
    [string]$GodotPath = "C:\Users\86189\Desktop\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64.exe",

    [string]$PresetName = "Windows Desktop",

    [string]$OutputPath = "C:\godot_project\export\Four-Dimensional\Four-Dimensional.exe"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)
$projectSettingsPath = Join-Path $projectRoot "project.godot"

if (-not (Test-Path -LiteralPath $GodotPath)) {
    throw "Godot executable not found: $GodotPath"
}

if (-not (Test-Path -LiteralPath $projectSettingsPath)) {
    throw "Project settings not found: $projectSettingsPath"
}

$runningGodotProcesses = Get-Process -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -eq $GodotPath
}
if ($runningGodotProcesses) {
    throw "Close the Godot editor before exporting. Exporting while it is open can make the release startup check miss Spine resource loaders; the script blocks that package to prevent missing battle characters on Steam."
}

try {
    New-Item -ItemType Directory -Path (Split-Path -Parent $OutputPath) -Force | Out-Null
    $exportLogPath = Join-Path (Split-Path -Parent $OutputPath) "godot-export.log"
    # Start Godot through .NET instead of a PowerShell pipeline. Windows PowerShell
    # 5.1 turns any native stderr line into NativeCommandError when ErrorAction is Stop,
    # even if the executable itself returns success.
    $godotProcessStartInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $godotProcessStartInfo.FileName = $GodotPath
    $godotProcessStartInfo.UseShellExecute = $false
    $godotProcessStartInfo.RedirectStandardOutput = $true
    $godotProcessStartInfo.RedirectStandardError = $true
    foreach ($argument in @(
        "--headless",
        "--path",
        $projectRoot,
        "--export-release",
        $PresetName,
        $OutputPath
    )) {
        [void]$godotProcessStartInfo.ArgumentList.Add($argument)
    }

    $godotProcess = [System.Diagnostics.Process]::new()
    $godotProcess.StartInfo = $godotProcessStartInfo
    try {
        if (-not $godotProcess.Start()) {
            throw "Could not start Godot export process: $GodotPath"
        }
        $godotStdoutTask = $godotProcess.StandardOutput.ReadToEndAsync()
        $godotStderrTask = $godotProcess.StandardError.ReadToEndAsync()
        $godotProcess.WaitForExit()
        $godotExportOutput = $godotStdoutTask.GetAwaiter().GetResult() + $godotStderrTask.GetAwaiter().GetResult()
        $godotExportExitCode = $godotProcess.ExitCode
    }
    finally {
        $godotProcess.Dispose()
    }
    $godotExportOutput | Set-Content -LiteralPath $exportLogPath -Encoding UTF8

    if ($godotExportExitCode -ne 0) {
        Write-Host $godotExportOutput
        throw "Godot export failed with exit code $godotExportExitCode. Full output: $exportLogPath"
    }
    Write-Host "Godot export completed. Detailed log: $exportLogPath"

    if (-not (Test-Path -LiteralPath $OutputPath)) {
        throw "Godot reported success but did not produce: $OutputPath"
    }

    $pckPath = [System.IO.Path]::ChangeExtension($OutputPath, ".pck")
    if (-not (Test-Path -LiteralPath $pckPath)) {
        throw "Release export must produce a separate PCK so GDExtension can load Spine: $pckPath"
    }

    $startupLogPath = Join-Path (Split-Path -Parent $OutputPath) "spine-startup-check.log"
    Push-Location (Split-Path -Parent $OutputPath)
    try {
        $smokeTestOutput = & $OutputPath --headless --quit-after 300 --verbose --log-file $startupLogPath 2>&1 | Out-String
        $smokeTestExitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
    if ($smokeTestExitCode -ne 0) {
        throw "The exported game failed its startup check with exit code $smokeTestExitCode."
    }

    if (-not (Test-Path -LiteralPath $startupLogPath)) {
        throw "The exported game did not produce the Spine startup log: $startupLogPath"
    }

    $spineCheckOutput = $smokeTestOutput + [Environment]::NewLine + (Get-Content -LiteralPath $startupLogPath -Raw)
    if ($spineCheckOutput -match '(?im)(Could not find base class\s+"SpineSprite"|Cannot get class\s+[\x27"]SpineSprite[\x27"]|No loader found for resource.*Spine|GDExtension.*(?:error|fail)|(?:error|fail).*GDExtension)') {
        throw "The exported game failed the Spine startup check. The package was not accepted."
    }

    Write-Host "External-PCK export and Spine startup check passed."
    Write-Host "Steam release export completed: $OutputPath"
}
finally {
}
