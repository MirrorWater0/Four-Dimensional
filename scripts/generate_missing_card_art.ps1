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
Flat bright white background. Fill the composition with meaningful visual elements.
Avoid large empty blank background areas.
No text, no watermark, no logo, no UI frame, no card border.
Hand correctness is top priority. Avoid visible detailed hands whenever possible.
Hide hands behind sleeve, hair, body, or composition when possible.
"@

$jobs = @(
    [pscustomobject]@{
        Id = "ExhaustBulwark"
        Character = "Kasiya"
        ReferenceImage = "asset/PlayerCharater/Kasiya/KasiyaPortrait.png"
        OutputPath = "asset/CardPicture/Kasiya/ExhaustBulwark.png"
        Prompt = @"
Use the provided image only for Kasiya identity reference: white hair, golden eyes, white outfit, black shoulder pieces, red chest gem, and palette.
Create a completely new effect-dominant composition. Do not copy the reference pose.
$styleBlock
Kasiya element constraints:
Allowed: white or silver hair, golden eyes, white dress or armor-like white outfit, black angular shoulder pieces, red chest gem, pale holy or crystal-like light effects.
Forbidden: shield, physical shield, buckler, extra weapons, staff, wings, halo, crown, helmet, heavy armor, card frame, border, UI ornaments.
Skill: Exhaust Bulwark (ash oath shield).
Composition: effect-dominant. Kasiya may appear only as a small partial silhouette at one edge.
Main visual: a broad translucent ash-and-ember protective veil spreading outward, like smoke hardened into a holy bulwark.
Show pale gold-white light mixed with soft gray ash particles and gentle ember sparks.
The shield must be intangible VFX, not physical armor or a held shield.
Mood: solemn team protection through sacrifice and burn.
"@
    }
    [pscustomobject]@{
        Id = "Resistance"
        Character = "Kasiya"
        ReferenceImage = "asset/PlayerCharater/Kasiya/KasiyaPortrait.png"
        OutputPath = "asset/CardPicture/Kasiya/Resistance.png"
        Prompt = @"
Use the provided image only for Kasiya identity reference: white hair, golden eyes, white outfit, black shoulder pieces, red chest gem, and palette.
Create a completely new close crop composition. Do not copy the reference pose.
$styleBlock
Kasiya element constraints:
Allowed: white or silver hair, golden eyes, white dress or armor-like white outfit, black angular shoulder pieces, red chest gem, pale holy light effects.
Forbidden: shield, physical shield, buckler, extra weapons, staff, wings, halo, crown, heavy armor, card frame, border, UI ornaments.
Skill: Resistance (defensive endure while discarding spent power).
Composition: shoulder crop or upper-torso close-up, not a full portrait.
Main visual: a clean pale gold defensive light plane in the foreground while two translucent card silhouettes dissolve into gray ash smoke at the side.
Mood: steady endurance, controlled sacrifice, guarded resolve.
All defensive elements must be intangible VFX only.
"@
    }
    [pscustomobject]@{
        Id = "Foresight"
        Character = "Mariya"
        ReferenceImage = "asset/PlayerCharater/Mariya/MariyaPortrait.png"
        OutputPath = "asset/CardPicture/Mariya/Foresight.png"
        Prompt = @"
Use the provided image only for Mariya identity reference: light blue hair, green eyes, sleeveless white dress, bare arms, and palette.
Create a completely new partial close-up composition. Do not copy the reference pose.
$styleBlock
Mariya element constraints:
Allowed: light blue hair, green eyes, sleeveless white dress, bare arms, blue ribbon accents, pale healing or holy light effects.
Forbidden: gloves, detached sleeves, long arm coverings, heavy armor, shield, wings, halo, crown, ornate jewelry, card frame, border, UI ornaments.
Skill: Foresight (see the future draw).
Composition: eye close-up or partial face with one side of the frame filled by prophetic VFX.
Main visual: soft blue-green holy ripples, faint translucent future card silhouettes, and quiet predictive light paths branching forward.
The foresight effect should feel calm, perceptive, and supportive, not explosive.
Avoid full-body portrait. Prioritize mystical partial framing and effect readability.
"@
    }
    [pscustomobject]@{
        Id = "BladeSalvo"
        Character = "Nightingale"
        ReferenceImage = "asset/PlayerCharater/Nightingale/NightingalePortrait.png"
        OutputPath = "asset/CardPicture/Nightingale/BladeSalvo.png"
        Prompt = @"
Use the provided image only for Nightingale identity reference: blonde twin-tail hair, black hair bows, red eyes, black sleeveless dress, and palette.
Create a completely new effect-dominant composition. Do not copy the reference pose.
$styleBlock
Nightingale element constraints:
Allowed: blonde twin-tail hair, black hair bows, red eyes, black sleeveless dress, bare arms, red ribbon accent, white blade-like light effects, shadow veil when needed.
Forbidden: shield, staff, armor, wings, halo, crown, extra costume layers, card frame, border, UI ornaments.
Skill: Blade Salvo (multiple blade lights launched together).
Composition: effect-dominant with Nightingale only as a small cropped edge silhouette or partial shoulder.
Main visual: two or three sharp white blade-light slashes radiating outward in a salvo pattern, with thin shadow accents.
All blades must be translucent energy VFX, not physical weapons.
Mood: fast, lethal, coordinated release.
Avoid centered half-body portrait.
"@
    }
    [pscustomobject]@{
        Id = "BladeDance"
        Character = "Nightingale"
        ReferenceImage = "asset/PlayerCharater/Nightingale/NightingalePortrait.png"
        OutputPath = "asset/CardPicture/Nightingale/BladeDance.png"
        Prompt = @"
Use the provided image only for Nightingale identity reference: blonde twin-tail hair, black hair bows, red eyes, black sleeveless dress, and palette.
Create a completely new dynamic partial composition. Do not copy the reference pose.
$styleBlock
Nightingale element constraints:
Allowed: blonde twin-tail hair, black hair bows, red eyes, black sleeveless dress, bare arms, red ribbon accent, white blade-like light effects, shadow veil when needed.
Forbidden: shield, staff, armor, wings, halo, crown, extra costume layers, card frame, border, UI ornaments.
Skill: Blade Dance (spinning blade-light dance while defending).
Composition: foot close-up, skirt hem motion, or lower-body crop with blade trails orbiting around the movement.
Main visual: three clean white blade-light arcs circling in a dance pattern, with a faint defensive shadow ripple beneath.
All blades must be translucent energy VFX, not physical weapons.
Mood: agile, rhythmic, dangerous elegance.
Avoid full-body portrait and avoid showing detailed hands.
"@
    }
    [pscustomobject]@{
        Id = "Blade"
        Character = "Colorless"
        ReferenceImage = "asset/CardPicture/Nightingale/StasisBlade.png"
        OutputPath = "asset/CardPicture/Colorless/Blade.png"
        Prompt = @"
Use the provided image only as style reference for this game's pale anime skill card look.
Do not copy its character, pose, composition, or exact blade layout.
$styleBlock
Skill: Blade (generic colorless attack slash).
Composition: pure effect-dominant. No character, no face, no body, no portrait.
Main visual: one decisive white blade-light slash crossing the frame diagonally with a faint afterimage trail.
Keep it minimal, sharp, and instantly readable at small card size.
This is a neutral colorless skill card, not tied to any specific character.
"@
    }
    [pscustomobject]@{
        Id = "Calmness"
        Character = "Colorless"
        ReferenceImage = "asset/CardPicture/Mariya/BasicSpecial.png"
        OutputPath = "asset/CardPicture/Colorless/Calmness.png"
        Prompt = @"
Use the provided image only as style reference for this game's pale anime skill card look.
Do not copy its character, pose, composition, or exact layout.
$styleBlock
Skill: Calmness (draw cards through tranquil focus).
Composition: abstract effect-dominant. No character, no face, no body, no portrait.
Main visual: soft blue-white calm mist with two floating translucent card-shaped light panels and gentle ripples spreading outward.
Mood: peaceful, focused, restorative card draw.
Keep the image clean, airy, and readable at small card size.
This is a neutral colorless skill card, not tied to any specific character.
"@
    }
)

foreach ($job in $jobs) {
    $referencePath = Join-Path $projectRoot $job.ReferenceImage
    $outputPath = Join-Path $projectRoot $job.OutputPath

    if (Test-Path -LiteralPath $outputPath) {
        Write-Host "[SKIP] $($job.Character) / $($job.Id) already exists." -ForegroundColor Yellow
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
    Write-Host "[GENERATE] $($job.Character) / $($job.Id)" -ForegroundColor Cyan
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
Write-Host "All missing card art jobs finished." -ForegroundColor Green
