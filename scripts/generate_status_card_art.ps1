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
Fill the entire image area with visual content. The effect must extend close to all four edges.
Leave no large blank white margins. White may appear only as small gaps between effect shapes.
No text, no watermark, no logo, no UI frame, no card border.
No character, no face, no body, no portrait.
"@

$jobs = @(
    [pscustomobject]@{
        Id = "VoidStatus"
        ReferenceImage = "asset/CardPicture/Echo/CursePower.png"
        OutputPath = "asset/CardPicture/Status/VoidStatus.png"
        Prompt = @"
Use the provided image only as style reference for this game's pale anime skill card look.
Do not copy its character, pose, composition, or exact layout.
$styleBlock
Skill: Void Call (虚空之唤) — a cursed void status card that drains energy when drawn.
Composition: pure effect-dominant abstract symbol.
Main visual: a collapsing dark-violet void rift with inward-pulling energy threads, hollow negative-space rings, and faint drained light particles being swallowed.
Mood: empty, hungry, unsettling, ethereal.
Purple-black void tones with pale drained highlights. Fill the frame densely with void VFX.
"@
    }
    [pscustomobject]@{
        Id = "WoundStatus"
        ReferenceImage = "asset/CardPicture/Echo/DisasterImpact.png"
        OutputPath = "asset/CardPicture/Status/WoundStatus.png"
        Prompt = @"
Use the provided image only as style reference for this game's pale anime skill card look.
Do not copy its character, pose, composition, or exact layout.
$styleBlock
Skill: Wound (伤口) — an unplayable injury status card.
Composition: symbolic close-up effect, not a character injury portrait.
Main visual: an abstract bleeding wound sigil made of cracked crimson brush strokes, pale pain ripples, and a few sharp stress lines radiating outward.
Mood: painful, stagnant, harmful but still clean and readable.
Use restrained red accents on pale white-gray forms. Fill the frame with the wound symbol and surrounding pain VFX.
"@
    }
    [pscustomobject]@{
        Id = "DazeStatus"
        ReferenceImage = "asset/CardPicture/Nightingale/StasisBlade.png"
        OutputPath = "asset/CardPicture/Status/DazeStatus.png"
        Prompt = @"
Use the provided image only as style reference for this game's pale anime skill card look.
Do not copy its character, pose, composition, or exact layout.
$styleBlock
Skill: Daze (晕眩) — an unplayable stun status card that fades at turn end.
Composition: disorienting abstract effect filling the whole frame.
Main visual: overlapping spiral rings, off-center starbursts, and drifting motion lines that suggest dizziness and confusion.
Mood: dizzy, unstable, light-headed, ethereal.
Use pale gold, soft gray, and faint violet accents. Dense readable shapes, no empty corners.
"@
    }
    [pscustomobject]@{
        Id = "PlagueStatus"
        ReferenceImage = "asset/CardPicture/Echo/WeakeningField.png"
        OutputPath = "asset/CardPicture/Status/PlagueStatus.png"
        Prompt = @"
Use the provided image only as style reference for this game's pale anime skill card look.
Do not copy its character, pose, composition, or exact layout.
$styleBlock
Skill: Plague (瘟疫) — a contagious sickness status card that harms the whole team if kept in hand.
Composition: pestilent miasma effect dominating the entire card.
Main visual: sickly green-purple plague fog, floating spore motes, rotting vapor curls, and a few jagged corruption veins spreading across the frame.
Mood: toxic, spreading, suffocating, dangerous.
Keep surfaces graphic and readable at small size. Fill all edges with plague mist and particles.
"@
    }
)

foreach ($job in $jobs) {
    $referencePath = Join-Path $projectRoot $job.ReferenceImage
    $outputPath = Join-Path $projectRoot $job.OutputPath

    if (Test-Path -LiteralPath $outputPath) {
        Write-Host "[SKIP] $($job.Id) already exists." -ForegroundColor Yellow
        continue
    }

    if (-not (Test-Path -LiteralPath $referencePath)) {
        throw "Reference image not found: $referencePath"
    }

    $outputDir = Split-Path -Parent $outputPath
    if (-not (Test-Path -LiteralPath $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    Write-Host ""
    Write-Host "[GENERATE] $($job.Id)" -ForegroundColor Cyan
    Write-Host "  Ref: $referencePath"
    Write-Host "  Out: $outputPath"

    & powershell -NoProfile -ExecutionPolicy Bypass -File $generatorScript `
        -ReferenceImage $referencePath `
        -Size "1024x768" `
        -Quality "high" `
        -RequestTimeoutSec 300 `
        -OutputPath $outputPath `
        -Prompt $job.Prompt

    if ($LASTEXITCODE -ne 0) {
        throw "Generation failed for $($job.Id)."
    }
}

Write-Host ""
Write-Host "All status card art jobs finished." -ForegroundColor Green
