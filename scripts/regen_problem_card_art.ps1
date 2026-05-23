param(
    [ValidateSet("CursePower", "WeakeningField", "EternalDarkSkill")]
    [string[]]$Only,
    [ValidateSet("low", "medium", "high", "auto")]
    [string]$Quality = "high",
    [ValidateRange(30, 3600)]
    [int]$RequestTimeoutSec = 300,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$generatorScript = Join-Path $projectRoot "tools/openai_image_generate.ps1"

if (-not (Test-Path -LiteralPath $generatorScript)) {
    throw "Generator script not found: $generatorScript"
}

$styleBlock = @"
Raw game skill artwork only, not a finished card template.
Pale anime sketch with clean thin line art.
Simple watercolor shading with slightly cleaner color blocks.
Flat bright white background with strong negative space.
No text, no watermark, no logo, no UI frame, no card border.
"@

$jobs = @(
    [pscustomobject]@{
        Id = "CursePower"
        Character = "Echo"
        ReferenceImage = "asset/PlayerCharater/Echo/EchoPortrait.png"
        OutputPath = "asset/CardPicture/Echo/CursePower.png"
        Prompt = @"
Use the provided image only for character identity reference: face, hair, outfit, wing material, and color palette.
Create a completely new composition and a clearly different pose from the reference.
Do not trace or closely copy the original full-body standing pose, camera angle, silhouette, hand placement, or wing layout.
Use a close-up or half-body composition, not a full standing body.
Let the skill effect occupy a large part of the frame.
$styleBlock
Echo is channeling a compact dark-violet curse sigil near her hand.
The curse power should feel condensed, dangerous, and inward-pulling, with sharp floating fragments and thin resonance threads.
Focus on tense spellcasting energy, not idle posing.
The final image must not look like the reference with extra effects added on top.
"@
    }
    [pscustomobject]@{
        Id = "WeakeningField"
        Character = "Echo"
        ReferenceImage = "asset/PlayerCharater/Echo/EchoPortrait.png"
        OutputPath = "asset/CardPicture/Echo/WeakeningField.png"
        Prompt = @"
Use the provided image only for character identity reference: face, hair, outfit, wing material, and color palette.
Create a completely new composition and a clearly different pose from the reference.
Do not trace or closely copy the original full-body standing pose, camera angle, silhouette, hand placement, or wing layout.
Use a close-up or half-body composition, not a full standing body.
Let the skill effect occupy a large part of the frame.
$styleBlock
Echo is releasing a large suppressive weakening field.
Show broad pale-purple waveform bands, uneven circular rings, and spreading distortion pressure around her.
The image should feel like field control and debilitation, not direct attack.
Prioritize effect composition over body visibility.
The final image must not look like the reference with extra effects added on top.
"@
    }
    [pscustomobject]@{
        Id = "EternalDarkSkill"
        Character = "Nightingale"
        ReferenceImage = "asset/PlayerCharater/Nightingale/NightingalePortrait.png"
        OutputPath = "asset/CardPicture/Nightingale/EternalDarkSkill.png"
        Prompt = @"
Use the provided image only for character identity reference: face, hair, outfit, ribbons, and color palette.
Create a completely new composition and a clearly different pose from the reference.
Do not trace or closely copy the original full-body standing pose, camera angle, silhouette, arm position, or dress flow.
Use a close-up or half-body composition, not a full standing body.
Let the darkness effect occupy a large part of the frame.
$styleBlock
Nightingale is wrapped in a stable moon-dark shadow veil.
Show elegant black shadow arcs, quiet lethal pressure, and a calm but overwhelming darkness state.
The feeling should be composed, inevitable, and menacing, not explosive.
Focus on a new dramatic angle and shadow composition.
The final image must not look like the reference with extra effects added on top.
"@
    }
)

if ($Only -and $Only.Count -gt 0) {
    $jobs = $jobs | Where-Object { $_.Id -in $Only }
}

if (-not $jobs -or $jobs.Count -eq 0) {
    throw "No jobs selected."
}

foreach ($job in $jobs) {
    $referencePath = Join-Path $projectRoot $job.ReferenceImage
    $outputPath = Join-Path $projectRoot $job.OutputPath

    if (-not (Test-Path -LiteralPath $referencePath)) {
        throw "Reference image not found: $referencePath"
    }

    $outputDir = Split-Path -Parent $outputPath
    if (-not (Test-Path -LiteralPath $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    Write-Host ""
    Write-Host "[REGEN] $($job.Character) / $($job.Id)" -ForegroundColor Cyan
    Write-Host "  Ref: $referencePath"
    Write-Host "  Out: $outputPath"

    $invokeArgs = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $generatorScript,
        "-ReferenceImage", $referencePath,
        "-Size", "1024x768",
        "-Quality", $Quality,
        "-RequestTimeoutSec", $RequestTimeoutSec,
        "-OutputPath", $outputPath,
        "-Prompt", $job.Prompt
    )

    if ($DryRun) {
        $invokeArgs += "-DryRun"
    }

    & powershell @invokeArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Generation failed for $($job.Id)."
    }
}

Write-Host ""
Write-Host "All selected card art jobs finished." -ForegroundColor Green
