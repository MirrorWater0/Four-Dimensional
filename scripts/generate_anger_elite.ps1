param(
    [ValidateSet("low", "medium", "high", "auto")]
    [string]$Quality = "high",
    [ValidateRange(30, 3600)]
    [int]$RequestTimeoutSec = 300,
    [switch]$ReplaceTarget,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path -Parent $PSScriptRoot
$generatorScript = Join-Path $projectRoot "tools/openai_image_generate.ps1"
$removeChromaScript = "C:\Users\86189\.codex\skills\.system\imagegen\scripts\remove_chroma_key.py"

if (-not (Test-Path -LiteralPath $generatorScript)) {
    throw "Generator script not found: $generatorScript"
}

if (-not (Test-Path -LiteralPath $removeChromaScript)) {
    throw "Chroma-key helper not found: $removeChromaScript"
}

$referenceImages = @(
    "asset/EnemyCharater/GraveWraith.png",
    "asset/EnemyCharater/HollowBulwark.png",
    "asset/EnemyCharater/VoidAcolyte.png",
    "asset/EnemyCharater/MarrowReaver.png",
    "asset/EnemyCharater/FearWorm.png"
)

$referenceLabels = @(
    "Image 1 GraveWraith: style reference only.",
    "Image 2 HollowBulwark: style reference only.",
    "Image 3 VoidAcolyte: style reference only.",
    "Image 4 MarrowReaver: style reference only.",
    "Image 5 FearWorm: style reference only."
)

$prompt = @"
Use the provided images only as style references for this game's enemy portrait look.
Do not copy their identities, silhouettes, exact weapons, or armor.
$(($referenceLabels -join "`n"))
Create a new enemy standing portrait named Anger.
Elite enemy complexity, stronger and more dangerous than a normal enemy, but not boss complexity.
Pale anime sketch with clean thin line art.
Simple watercolor shading with slightly cleaner color blocks.
Perfectly flat solid #00ff00 chroma-key background for local background removal.
The background must be one uniform green color with no shadow, gradient, texture, floor plane, or lighting variation.
Do not use #00ff00 or green rim light anywhere in the enemy itself.
Raw game enemy portrait only, not a finished card template.
No text, no watermark, no logo, no UI frame, no card border.

Keep the project's established enemy look: pale bone-like outer shell, dark inner body mass, restrained red crack light accents, readable silhouette, and moderate detail density.
Do not make it look like a clean mecha, superhero armor, glossy sci-fi suit, or sleek robot.
Avoid smooth plated power armor and avoid symmetrical heroic posing.

Anger should feel violent, unstable, and oppressive.
Use a broad aggressive silhouette with one dominant heavy cleaver-like arm or blade limb, plus one secondary claw or hook limb.
The body should feel like a wrath-driven monster with cracked bone shell over a dark core, not a human knight in armor.
Prefer asymmetry, forward lean, tension, and an imminent lunge feeling.
One or two strong shape motifs only: cleaver limb, broken horned head, torn tendrils, or jagged rib-like shell edges.
The weirdness should come from the outer silhouette, not dense tiny decorations.
No wings, no cape, no halo, no gun, no shield, no elegant knight proportions.
"@

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$generatedDir = Join-Path $projectRoot "asset/generated/enemy_refine"
if (-not (Test-Path -LiteralPath $generatedDir)) {
    New-Item -ItemType Directory -Path $generatedDir -Force | Out-Null
}

$rawGreenPath = Join-Path $generatedDir "Anger_ref_green_$timestamp.png"
$alphaPath = Join-Path $generatedDir "Anger_ref_alpha_$timestamp.png"
$crop2500Path = Join-Path $generatedDir "Anger_ref_alpha_2500_$timestamp.png"
$targetPath = Join-Path $projectRoot "asset/EnemyCharater/Anger.png"

function Resize-ToSquare2500 {
    param(
        [string]$InputPath,
        [string]$OutputPath
    )

    $src = [System.Drawing.Bitmap]::new($InputPath)
    try {
        $dst = [System.Drawing.Bitmap]::new(2500, 2500, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($dst)
            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality

                $scale = [Math]::Min(2500.0 / $src.Width, 2500.0 / $src.Height)
                $drawWidth = [int][Math]::Round($src.Width * $scale)
                $drawHeight = [int][Math]::Round($src.Height * $scale)
                $offsetX = [int][Math]::Floor((2500 - $drawWidth) / 2.0)
                $offsetY = [int][Math]::Floor((2500 - $drawHeight) / 2.0)

                $graphics.DrawImage($src, $offsetX, $offsetY, $drawWidth, $drawHeight)
            }
            finally {
                $graphics.Dispose()
            }

            $dst.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $dst.Dispose()
        }
    }
    finally {
        $src.Dispose()
    }
}

Write-Host "[ANGER] Reference images:" -ForegroundColor Cyan
foreach ($ref in $referenceImages) {
    Write-Host "  $ref"
}
Write-Host "[ANGER] Raw green output: $rawGreenPath"
Write-Host "[ANGER] Alpha output: $alphaPath"
Write-Host "[ANGER] Final 2500 output: $crop2500Path"
if ($ReplaceTarget) {
    Write-Host "[ANGER] Will replace target: $targetPath" -ForegroundColor Yellow
}

$resolvedRefs = $referenceImages | ForEach-Object { Join-Path $projectRoot $_ }
$generatorParams = @{
    ReferenceImage = $resolvedRefs
    Size = "1024x1024"
    Quality = $Quality
    RequestTimeoutSec = $RequestTimeoutSec
    OutputPath = $rawGreenPath
    Prompt = $prompt
}

if ($DryRun) {
    $generatorParams["DryRun"] = $true
}

& $generatorScript @generatorParams

if ($DryRun) {
    Write-Host "[ANGER] Dry run complete." -ForegroundColor Green
    return
}

& python $removeChromaScript `
    --input $rawGreenPath `
    --out $alphaPath `
    --auto-key border `
    --soft-matte `
    --transparent-threshold 12 `
    --opaque-threshold 220 `
    --despill `
    --force
if ($LASTEXITCODE -ne 0) {
    throw "Chroma-key removal failed."
}

Resize-ToSquare2500 -InputPath $alphaPath -OutputPath $crop2500Path

if ($ReplaceTarget) {
    Copy-Item -LiteralPath $crop2500Path -Destination $targetPath -Force
    Write-Host "[ANGER] Replaced target: $targetPath" -ForegroundColor Green
}
else {
    Write-Host "[ANGER] Target was not replaced. Use -ReplaceTarget if you want to overwrite asset/EnemyCharater/Anger.png." -ForegroundColor Yellow
}

Write-Host "[ANGER] Finished." -ForegroundColor Green
