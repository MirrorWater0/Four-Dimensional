[CmdletBinding(PositionalBinding = $false)]
param(
    [string[]]$CharacterName,
    [switch]$DryRun,
    [switch]$OnlyMissingFinal,
    [int]$RequestTimeoutSec = 600,
    [ValidateSet("1024x768", "1024x1024", "1536x1024")]
    [string]$GenerationSize = "1024x768",
    [int]$FinalWidth = 980,
    [int]$FinalHeight = 700,
    [switch]$KeepRawSource
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$generator = Join-Path $scriptRoot "openai_image_generate.ps1"

if (-not (Test-Path -LiteralPath $generator)) {
    throw "Generator script not found: $generator"
}

$commonPrefix = @"
Use the provided portrait only as identity, outfit, and palette reference.
Do not copy the portrait pose, camera angle, arm placement, or centered standing composition.
Raw game skill artwork only, not a finished card template.
Skill: Basic Attack.
The image must be weapon-and-VFX dominant: the weapon strike and attack effect are the subject, while the character is cropped, small, from behind, or partly offscreen.
Pale anime sketch with clean thin line art, simple watercolor shading, bright white background, controlled negative space, readable at small card size.
No text, no watermark, no logo, no card frame, no UI ornament.
Hard composition rule: avoid centered portrait, avoid full-body standing pose, avoid copying the reference image. Show motion through weapon direction, impact lane, and effect flow.
"@

$jobs = @(
    @{
        Character = "Kasiya"
        Reference = "asset/PlayerCharater/Kasiya/KasiyaPortrait.png"
        Prompt = @"
$commonPrefix
Kasiya element constraints:
Allowed: white or silver hair, golden eyes, white dress or armor-like white outfit, black angular shoulder pieces, red chest gem, a long sword or blade-like white light, pale holy or crystal-like light effects.
Forbidden: shield, physical shield, buckler, barrier held as a shield, extra weapons, staff, wings, halo, crown, helmet, heavy armor, jewelry not visible in the reference, card frame, border, UI ornaments.

Composition bucket: sword-and-impact lane close crop.
Camera and pose: low diagonal camera very close to the sword path. Kasiya is cropped at the far left edge as a partial shoulder, hair, and forearm silhouette; her face is not the subject.
Action beat: a basic forward sword cut has just crossed the card from lower-left foreground to upper-right distance.
Main visual: one long white-silver sword or blade-like light line dominates the frame, with pale gold holy sparks and thin crystal-like stress arcs peeling away from the cutting edge.
Motion detail: the sword line must be straight, stable, and readable as a single weapon strike; add one compact impact flash near the right third.
Avoid portrait-first composition, full-body pose, shield shapes, extra blades, and decorative halo-like circles.
"@
    }
    @{
        Character = "Mariya"
        Reference = "asset/PlayerCharater/Mariya/MariyaPortrait.png"
        Prompt = @"
$commonPrefix
Mariya element constraints:
Allowed: light blue hair, green eyes, sleeveless white dress, bare arms, simple silver crescent staff or polearm motif, blue ribbon accents, pale healing or holy light effects.
Forbidden: gloves, detached sleeves, long arm coverings, heavy armor, shield, extra swords unless explicitly requested, wings, halo, crown, ornate jewelry, card frame, border, UI ornaments.
Keep Mariya's outfit simple and sleeveless. Do not add new costume layers.

Composition bucket: crescent polearm sweep close crop.
Camera and pose: tight foreground crop along the crescent polearm. Mariya is mostly offscreen on the lower-right edge; show only a stable bare forearm, a hint of white dress, blue ribbon trail, and flowing light blue hair.
Action beat: she completes a basic sweeping polearm attack, drawing the crescent head across the card in a clean arc.
Main visual: the silver crescent staff head and shaft occupy the foreground, with a pale green-white slash ribbon following the crescent path from right to left.
Motion detail: the grip must feel solid and the polearm must not float. Show exactly one clear shaft line and one crescent head; avoid extra weapons.
Avoid portrait-first composition, raised-prayer pose, detached sleeves, gloves, shield-like circles, and busy holy symbols.
"@
    }
    @{
        Character = "Nightingale"
        Reference = "asset/PlayerCharater/Nightingale/NightingalePortrait.png"
        Prompt = @"
$commonPrefix
Nightingale element constraints:
Allowed: blonde twin-tail hair, black hair bows, red eyes, black sleeveless dress, bare arms, red ribbon accent, white blade-like light effects, shadow veil or moonlike arcs when explicitly requested.
Forbidden: shield, staff, armor, wings, halo, crown, extra costume layers, gloves unless visible in the reference, musical notes unless the skill explicitly requests song/music, card frame, border, UI ornaments.
Keep the silhouette light and assassin-like. Shadow elements may appear only as translucent VFX.

Composition bucket: assassin slash afterimage lane.
Camera and pose: wide diagonal composition where Nightingale has already dashed past the camera. Show only the back edge of her black dress, one trailing blonde twin-tail, and a red ribbon accent at the far right edge.
Action beat: a basic assassin cut slices through the center after she exits the frame.
Main visual: one bright white blade-like slash dominates the middle of the card, bordered by a thin black shadow veil and small red-black speed flecks.
Motion detail: the slash should read as a single clean finishing lane, not a cluster of random blades. Character should be secondary and partly offscreen.
Avoid centered portrait, full-body standing pose, literal stage curtains, extra weapons, music notes, armor, or wings.
"@
    }
    @{
        Character = "Echo"
        Reference = "asset/PlayerCharater/Echo/EchoPortrait.png"
        Prompt = @"
$commonPrefix
Echo element constraints:
Allowed: long white hair, purple eyes, white outfit, dark blue angular shadow shapes from the reference, purple blade-like energy, pale sound-wave or resonance effects when explicitly requested.
Forbidden: shield, staff, heavy armor, wings, halo, crown, ornate jewelry, extra weapons unless explicitly requested, animal-like ears or horns beyond the reference's dark angular shapes, card frame, border, UI ornaments.
Keep the dark blue shapes abstract and angular. Do not turn them into armor, wings, or creatures.

Composition bucket: resonance blade strike.
Camera and pose: extreme horizontal crop along a purple-white blade-energy lane. Echo is barely visible at the left edge as a shoulder, hair, and dark angular shadow shape; her face can be hidden.
Action beat: a basic resonance attack is released as a clean blade-like pulse.
Main visual: one narrow purple-white energy blade streak crosses the full card from left to right, with two faint offset resonance ripples following the strike path.
Motion detail: make the central energy lane sharp and readable, with subtle sound-wave rings hugging the blade line. No broad explosion.
Avoid portrait-first composition, physical shield, staff, extra swords, wings, horns, or creature-like dark shapes.
"@
    }
)

function Convert-ToCardSize {
    param(
        [string]$SourcePath,
        [string]$DestinationPath,
        [int]$Width,
        [int]$Height
    )

    Add-Type -AssemblyName System.Drawing
    $source = [System.Drawing.Image]::FromFile($SourcePath)
    try {
        $scale = [Math]::Max($Width / $source.Width, $Height / $source.Height)
        $scaledWidth = [int][Math]::Ceiling($source.Width * $scale)
        $scaledHeight = [int][Math]::Ceiling($source.Height * $scale)
        $offsetX = [int][Math]::Floor(($scaledWidth - $Width) / 2)
        $offsetY = [int][Math]::Floor(($scaledHeight - $Height) / 2)

        $canvas = [System.Drawing.Bitmap]::new($Width, $Height)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($canvas)
            try {
                $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.DrawImage($source, -$offsetX, -$offsetY, $scaledWidth, $scaledHeight)
            }
            finally {
                $graphics.Dispose()
            }

            $parent = Split-Path -Parent $DestinationPath
            if ($parent) {
                New-Item -ItemType Directory -Path $parent -Force | Out-Null
            }

            $canvas.Save($DestinationPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $canvas.Dispose()
        }
    }
    finally {
        $source.Dispose()
    }
}

$selectedJobs = if ($CharacterName) {
    $allNames = $jobs.Character
    $unknownNames = $CharacterName | Where-Object { $_ -notin $allNames }
    if ($unknownNames) {
        throw "Unknown character name(s): $($unknownNames -join ', ')"
    }

    $jobs | Where-Object { $_.Character -in $CharacterName }
}
else {
    $jobs
}

foreach ($job in $selectedJobs) {
    $referencePath = Join-Path $repoRoot $job.Reference
    $outputDir = Join-Path $repoRoot "asset/CardPicture/$($job.Character)"
    $outPath = Join-Path $outputDir "BasicAttack.png"
    $rawDir = Join-Path $repoRoot "asset/generated/image2_tmp/BasicAttack/$($job.Character)"
    $rawPath = Join-Path $rawDir "BasicAttack.raw.png"

    if (-not (Test-Path -LiteralPath $referencePath)) {
        throw "Reference image not found: $referencePath"
    }

    if ($OnlyMissingFinal -and (Test-Path -LiteralPath $outPath -PathType Leaf)) {
        Write-Host "Skipping $($job.Character) because BasicAttack.png already exists."
        continue
    }

    Write-Host "=== Generating $($job.Character) BasicAttack ==="
    Write-Host ("    output: {0}" -f $outPath)

    & $generator `
        -Prompt $job.Prompt `
        -ReferenceImage $referencePath `
        -OutputPath $rawPath `
        -OutputDir $rawDir `
        -OutputName "BasicAttack_$($job.Character)" `
        -Size $GenerationSize `
        -Quality "high" `
        -OutputFormat "png" `
        -RequestTimeoutSec $RequestTimeoutSec `
        -DryRun:$DryRun

    if (-not $DryRun) {
        Write-Host ("    resizing to final card size: {0}x{1}" -f $FinalWidth, $FinalHeight)
        Convert-ToCardSize -SourcePath $rawPath -DestinationPath $outPath -Width $FinalWidth -Height $FinalHeight
        if (-not $KeepRawSource) {
            Remove-Item -LiteralPath $rawPath -Force
        }
    }
}

Write-Host ""
Write-Host "Basic attack card batch complete."
