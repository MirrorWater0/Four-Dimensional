param(
    [int]$Tail = 200,
    [string[]]$ProjectNames = @("Four-Dimensional", "tower"),
    [switch]$Follow
)

$ErrorActionPreference = "Stop"

$logRoot = Join-Path $env:APPDATA "Godot\app_userdata"
$latestLog = $null

foreach ($projectName in $ProjectNames) {
    $logDir = Join-Path $logRoot (Join-Path $projectName "logs")
    if (!(Test-Path -LiteralPath $logDir)) {
        continue
    }

    $candidate = Get-ChildItem -LiteralPath $logDir -Filter "*.log" -File |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($candidate -ne $null -and ($latestLog -eq $null -or $candidate.LastWriteTime -gt $latestLog.LastWriteTime)) {
        $latestLog = $candidate
    }
}

if ($latestLog -eq $null) {
    throw "No Godot log file found under $logRoot for: $($ProjectNames -join ', ')"
}

Write-Host "Reading Godot log: $($latestLog.FullName)"

if ($Follow) {
    Get-Content -LiteralPath $latestLog.FullName -Tail $Tail -Wait
} else {
    Get-Content -LiteralPath $latestLog.FullName -Tail $Tail
}
