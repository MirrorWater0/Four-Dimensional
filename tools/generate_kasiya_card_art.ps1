[CmdletBinding(PositionalBinding = $false)]
param(
    [string[]]$SkillName,
    [switch]$Overwrite,
    [int]$RequestTimeoutSec = 900,
    [ValidateSet("low", "medium", "high", "auto")]
    [string]$Quality = "medium",
    [ValidateSet("1024x768", "1024x1024", "1536x1024")]
    [string]$GenerationSize = "1024x768",
    [int]$FinalWidth = 980,
    [int]$FinalHeight = 700,
    [switch]$TextOnly,
    [switch]$KeepRawSource,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$generator = Join-Path $scriptRoot "openai_image_generate.ps1"

if (-not (Test-Path -LiteralPath $generator)) {
    throw "Generator script not found: $generator"
}

$referencePath = Join-Path $repoRoot "asset/PlayerCharater/Kasiya/KasiyaPortrait.png"
if ((-not $TextOnly) -and (-not (Test-Path -LiteralPath $referencePath -PathType Leaf))) {
    throw "Reference image not found: $referencePath"
}

$commonPrompt = @"
Use the provided Kasiya portrait as the required identity, outfit, palette, hair, face, and costume reference.
Do not copy the portrait pose, camera angle, arm placement, centered standing posture, or expression.
Raw anime RPG skill card artwork for Kasiya.
Kasiya identity must match the reference: white or silver hair, golden eyes, white dress or armor-like white outfit, black angular shoulder pieces, small red chest gem, long sword or blade-like white light.
Allowed effects: pale holy light, white-gold sword light, translucent crystal facets, abstract guard planes, amber vulnerable-mark lines.
Forbidden elements: physical shield, buckler, staff, wings, halo, crown, helmet, heavy armor, extra weapons, jewelry not described, card frame, border, UI ornament, text, watermark, logo.
Composition rule: do not make a centered portrait, full-body standing pose, or copied character reference pose. The skill action, sword line, guard field, or special VFX must be the subject. Kasiya should be cropped, from behind, at an edge, partially offscreen, or secondary to the effect.
Style: pale anime sketch, clean thin line art, simple watercolor shading, bright white background, controlled negative space, readable at small card size. No random debris, no broken glass, no noisy scratch clutter.
"@

$skillPrompts = [ordered]@{
    Determination = @"
Skill: Determination / Jian Yi Yi Jue.
Composition bucket: sword-edge resolve close crop.
Camera and pose: low diagonal camera along the sword edge. Kasiya is cropped to one shoulder, flowing white hair, black shoulder piece, red chest gem, and the sword hand at the left edge; her face is mostly hidden.
Action beat: she commits to a decisive thrust while a thin self-protection aura locks around her body.
Main visual: one bright white-gold sword line pierces from lower-left to upper-right, with a compact translucent guard sheath wrapping the red chest gem and wrist.
Motion detail: the sword path and defensive aura are the subject. Show resolve through forward pressure, not a static portrait.
Avoid: shield shapes, frontal bust pose, full-body stance, multiple swords, halo rings.
"@
    ReNewedSpirit = @"
Skill: ReNewed Spirit.
Composition bucket: recovery core and planted blade.
Camera and pose: close crop at waist and chest height. The sword is planted diagonally in the foreground; Kasiya is only visible as hair falling forward, red chest gem, one shoulder piece, and part of the white outfit.
Action beat: she steadies herself, draws breath, and pulls strength back into her body.
Main visual: white-gold energy rises from the planted blade into the red chest gem, then spreads as soft angular light veins through the frame.
Motion detail: make the renewal flow readable as inward recovery and power-up, not an attack slash.
Avoid: prayer pose, centered face, full-body kneeling, shield, halo, extra ornaments.
"@
    Smite = @"
Skill: Smite / Jue Yu Jian Sha.
Composition bucket: top-down execution strike.
Camera and pose: high top-down angle. Kasiya is a small cropped figure at the upper-left edge, sword extended downward into the composition.
Action beat: a vertical holy sword impact lands on an unseen target and crushes its survivability before the hit.
Main visual: one narrow white-gold sword beam strikes the center-right, surrounded by pale pressure rings and a clean amber fracture mark where the target would be.
Motion detail: the beam and impact point dominate. No visible enemy body is needed.
Avoid: portrait, broad random slash cluster, physical debris, extra weapons, halo.
"@
    VulnerablePurge = @"
Skill: Vulnerable Purge.
Composition bucket: effect-dominant sweeping weakpoint purge.
Camera and pose: wide horizontal composition. Kasiya is mostly offscreen at the far left; only hair, shoulder, and sword hilt may appear.
Action beat: a holy sword sweep purges exposed weakpoints across the enemy line.
Main visual: a broad white-gold sweep crosses the card and passes through several translucent amber weakpoint marks arranged at different depths.
Motion detail: weakpoint marks should look like intangible debuff glyphs and thin cracks in light, not physical objects or enemies.
Avoid: enemy figures, centered character, shield, many separate blades, messy explosions.
"@
    VulnerabilityStrike = @"
Skill: Vulnerability Strike.
Composition bucket: precise double-hit weakpoint close-up.
Camera and pose: extreme diagonal close crop. Kasiya's sword arm and a strip of white hair enter from the lower-right edge; her face is not centered.
Action beat: the first slash opens a weakpoint, then a delayed second sword-light line strikes the same point.
Main visual: exactly two white-gold slash trails crossing near a single amber weakpoint core, one solid foreground cut and one thinner delayed after-cut behind it.
Motion detail: make the repeated strike timing clear, controlled, and surgical.
Avoid: more than two slash trails, full-body pose, random sparks, shield, enemy body.
"@
    AbsouluteDefense = @"
Skill: Absoulute Defense.
Composition bucket: low-angle crystal guard planes.
Camera and pose: low side camera close to the ground, looking up along several translucent guard planes. Kasiya is cropped behind the planes, visible as hair, red chest gem, black shoulder piece, and sword angled downward.
Action beat: she braces and pulls enemy attention while an overwhelming defensive wall rises around her.
Main visual: layered white-gold crystal light planes dominate the center, with one thin taunt-like amber pulse radiating from the red chest gem.
Motion detail: the defense must be intangible holy crystal VFX, not a physical shield or buckler.
Avoid: shield silhouette held on arm, frontal portrait, heavy armor, halo, fortress props.
"@
    Vower = @"
Skill: Vower / Oath Bearer.
Composition bucket: oath chain and carried sword line.
Camera and pose: three-quarter back crop from behind Kasiya's right shoulder. Her sword extends forward out of frame while her hair and black shoulder piece sweep across the left side.
Action beat: she swears an oath and carries momentum from a previous action into the next strike.
Main visual: one long white-gold sword line continues forward, linked to a red-gold oath ribbon that loops once around the red chest gem and trails toward the previous-position afterglow behind her.
Motion detail: show continuity and carry-over through the ribbon and afterglow, not through extra characters.
Avoid: centered face, full-body stance, physical chains, shield, wings, halo, extra swords.
"@
}

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

$selectedSkills = if ($SkillName) {
    $unknownSkills = $SkillName | Where-Object { -not $skillPrompts.Contains($_) }
    if ($unknownSkills) {
        throw "Unknown Kasiya skill name(s): $($unknownSkills -join ', ')"
    }
    $skillPrompts.Keys | Where-Object { $_ -in $SkillName }
}
else {
    $skillPrompts.Keys
}

foreach ($skill in $selectedSkills) {
    $outputDir = Join-Path $repoRoot "asset/CardPicture/Kasiya"
    $outPath = Join-Path $outputDir "$skill.png"
    $rawDir = Join-Path $repoRoot "asset/generated/image2_tmp/Kasiya"
    $rawPath = Join-Path $rawDir "$skill.raw.png"

    if ((-not $Overwrite) -and (Test-Path -LiteralPath $outPath -PathType Leaf)) {
        Write-Host "Skipping Kasiya $skill because final card art already exists."
        continue
    }

    $prompt = @"
$commonPrompt
$($skillPrompts[$skill])
"@

    Write-Host "=== Generating Kasiya $skill ==="
    Write-Host ("    output: {0}" -f $outPath)

    $generatorArgs = @{
        Prompt = $prompt
        OutputPath = $rawPath
        OutputDir = $rawDir
        OutputName = "$($skill)_Kasiya"
        Size = $GenerationSize
        Quality = $Quality
        OutputFormat = "png"
        RequestTimeoutSec = $RequestTimeoutSec
        DryRun = $DryRun
    }

    if (-not $TextOnly) {
        $generatorArgs.ReferenceImage = $referencePath
    }

    & $generator @generatorArgs

    if (-not $DryRun) {
        Write-Host ("    resizing to final card size: {0}x{1}" -f $FinalWidth, $FinalHeight)
        Convert-ToCardSize -SourcePath $rawPath -DestinationPath $outPath -Width $FinalWidth -Height $FinalHeight
        if (-not $KeepRawSource) {
            Remove-Item -LiteralPath $rawPath -Force
        }
    }
}

Write-Host ""
Write-Host "Kasiya card art batch complete."
