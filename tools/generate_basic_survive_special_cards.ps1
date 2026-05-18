[CmdletBinding(PositionalBinding = $false)]
param(
    [string[]]$CharacterName,
    [string[]]$SkillName,
    [switch]$DryRun,
    [switch]$OnlyMissingFinal,
    [int]$RequestTimeoutSec = 600,
    [ValidateSet("low", "medium", "high", "auto")]
    [string]$Quality = "high",
    [ValidateSet("1024x768", "1024x1024", "1536x1024")]
    [string]$GenerationSize = "1024x768",
    [int]$FinalWidth = 980,
    [int]$FinalHeight = 700,
    [switch]$TextOnly,
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

$commonPrefix = if ($TextOnly) {
@"
Use the written character identity and constraints as the only identity source.
Do not make a centered portrait, full-body standing pose, or quiet character showcase.
Raw game skill artwork only, not a finished card template.
Pale anime sketch with clean thin line art, simple watercolor shading, bright white background, controlled negative space, readable at small card size.
No text, no watermark, no logo, no card frame, no UI ornament.
Hard composition rule: the skill action, weapon line, guard field, or special VFX must be the subject; the character should be cropped, small, from behind, or partly offscreen.
"@
}
else {
@"
Use the provided portrait only as identity, outfit, and palette reference.
Do not copy the portrait pose, camera angle, arm placement, standing posture, or centered portrait composition.
Raw game skill artwork only, not a finished card template.
Pale anime sketch with clean thin line art, simple watercolor shading, bright white background, controlled negative space, readable at small card size.
No text, no watermark, no logo, no card frame, no UI ornament.
Hard composition rule: avoid centered portrait, avoid full-body standing pose, avoid copying the reference image. The skill action, weapon line, guard field, or special VFX must be the subject; the character should be cropped, small, from behind, or partly offscreen.
"@
}

$characterData = @{
    Kasiya = @{
        Reference = "asset/PlayerCharater/Kasiya/KasiyaPortrait.png"
        Constraints = @"
Kasiya element constraints:
Allowed: white or silver hair, golden eyes, white dress or armor-like white outfit, black angular shoulder pieces, red chest gem, a long sword or blade-like white light, pale holy or crystal-like light effects.
Forbidden: shield, physical shield, buckler, barrier held as a shield, extra weapons, staff, wings, halo, crown, helmet, heavy armor, jewelry not visible in the reference, card frame, border, UI ornaments.
For defense skills, use abstract light planes, crystal facets, guard stance, or white/gold energy arcs instead of a shield. Effects must stay translucent and intangible, not held equipment.
"@
        Prompts = @{
            BasicDefense = @"
Skill: Basic Defense.
Composition bucket: crystal guard plane close crop.
Camera and pose: low side camera close to the defensive line. Kasiya is cropped on the far left as a shoulder, hair, black angular shoulder piece, and a hint of sword hilt; her face is mostly hidden.
Action beat: she braces and angles her sword downward while a protective plane opens in front of her.
Main visual: a broad translucent white-gold crystal light plane cuts diagonally across the center, with thin polygon facets and a compact impact glow pressing into it from the right.
Motion detail: make the guard plane the subject; it should look like a wall of holy light, not a physical shield.
Avoid shield silhouettes, full-body stance, centered portrait, halo rings, and heavy armor.
"@
            BasicGuard = @"
Skill: Basic Guard.
Composition bucket: ally-covering guard lane.
Camera and pose: three-quarter back view from behind Kasiya's sword arm, cropped at waist and shoulder. She steps across the frame from left to right to cover an offscreen ally.
Action beat: the sword draws a vertical white-gold guard line between the camera and an unseen incoming strike.
Main visual: one tall blade-like light line dominates the near foreground, with crystal facets fanning outward toward the lower-right as protective VFX.
Motion detail: show a directional cover motion, not a static defense pose. The ally is implied only by a warm glow behind the guard line; do not draw another character.
Avoid physical shields, crowd scenes, centered portrait, extra swords, wings, or halo.
"@
            BasicSpecial = @"
Skill: Basic Special.
Composition bucket: sword-core power shift.
Camera and pose: close crop on Kasiya's sword and red chest gem area; only part of her torso, hair, and sword hand are visible at the left edge.
Action beat: power gathers into the sword while an enemy's defense is weakened by the same holy pressure.
Main visual: a bright white-gold energy core runs along the sword line, while thin pale crystal cracks and amber debuff lines spread outward from the right side.
Motion detail: make the sword-core and outward weakening wave dominate; the character is secondary.
Avoid portrait pose, shield, physical crystal debris, armor buildup, and decorative halos.
"@
        }
    }
    Mariya = @{
        Reference = "asset/PlayerCharater/Mariya/MariyaPortrait.png"
        Constraints = @"
Mariya element constraints:
Allowed: light blue hair, green eyes, sleeveless white dress, bare arms, simple silver crescent staff or polearm motif, blue ribbon accents, pale healing or holy light effects.
Forbidden: gloves, detached sleeves, long arm coverings, heavy armor, shield, extra swords unless explicitly requested, wings, halo, crown, ornate jewelry, card frame, border, UI ornaments.
Keep Mariya's outfit simple and sleeveless. Do not add new costume layers.
Holy or healing symbols may appear only as translucent VFX behind or around her, never as physical accessories attached to her body or outfit.
"@
        Prompts = @{
            BasicDefense = @"
Skill: Basic Defense.
Composition bucket: crescent ward sweep.
Camera and pose: elevated diagonal crop focused on Mariya's crescent staff crossing the foreground. Mariya is at the lower-right edge, mostly cropped to bare arm, blue hair, ribbon, and a sliver of white dress.
Action beat: she sweeps the staff in a short defensive arc to soften incoming damage.
Main visual: a pale green-white crescent ward arc fills the left and upper portions of the card, with soft healing motes and one subtle impact ripple.
Motion detail: the staff shaft and crescent head must feel connected to the ward arc; no floating weapon.
Avoid centered portrait, prayer pose, shields, gloves, detached sleeves, and busy holy symbols.
"@
            BasicGuard = @"
Skill: Basic Guard.
Composition bucket: support protection ribbon.
Camera and pose: side-back crop from behind Mariya's shoulder. She leans forward from the right edge, guiding the crescent staff diagonally toward the lower-left.
Action beat: a protective healing ribbon leaves the staff and bends toward an offscreen ally.
Main visual: one pale gold-green ribbon of light curves through the center as the guard lane, with small translucent crescent fragments around it.
Motion detail: imply the ally only as a faint warm light at the far left; do not draw another character. Make the ribbon the subject.
Avoid portrait-first composition, shield shapes, full-body standing pose, and ornate divine accessories.
"@
            BasicSpecial = @"
Skill: Basic Special.
Composition bucket: energy transfer and weakening split.
Camera and pose: close crop on Mariya's crescent staff head near the center-left, with her face mostly outside the right edge. One bare forearm and blue ribbon trail are visible.
Action beat: the staff pulls pale blue-green energy inward, then sends a small gold pulse outward to empower the next action while weakening the target.
Main visual: a two-way VFX flow: soft blue-green energy threads entering the crescent head from the left, and a compact gold-white pulse leaving toward the upper-right.
Motion detail: keep the flows clean and readable, with the crescent head as the pivot.
Avoid open palm spellcasting, prayer pose, shield, halo, wings, and extra weapons.
"@
        }
    }
    Nightingale = @{
        Reference = "asset/PlayerCharater/Nightingale/NightingalePortrait.png"
        Constraints = @"
Nightingale element constraints:
Allowed: blonde twin-tail hair, black hair bows, red eyes, black sleeveless dress, bare arms, red ribbon accent, white blade-like light effects, shadow veil or moonlike arcs when explicitly requested.
Forbidden: shield, staff, armor, wings, halo, crown, extra costume layers, gloves unless visible in the reference, musical notes unless the skill explicitly requests song/music, card frame, border, UI ornaments.
Keep the silhouette light and assassin-like. Shadow elements may appear only as translucent VFX.
"@
        Prompts = @{
            BasicDefense = @"
Skill: Basic Defense.
Composition bucket: shadow deflection close crop.
Camera and pose: tight side crop with Nightingale sliding backward at the far right edge; only her twin-tail, black bow, shoulder, and red ribbon accent are visible.
Action beat: she avoids the hit by letting a shadow veil bend the incoming force aside.
Main visual: a translucent black crescent veil curves through the center, redirecting one thin pale attack streak away toward the upper-left.
Motion detail: the deflection bend is the subject; Nightingale remains secondary and partially offscreen.
Avoid shield, armor, centered portrait, full-body pose, stage curtains, and extra weapons.
"@
            BasicGuard = @"
Skill: Basic Guard.
Composition bucket: afterimage cover.
Camera and pose: diagonal crop from above. Nightingale's solid figure is low and off to the right, already moving away, while a pale afterimage remains near the center-left.
Action beat: the afterimage intercepts pressure for an offscreen ally.
Main visual: a ghostly black-white afterimage silhouette and a small curved shadow ward absorb a thin incoming streak.
Motion detail: the afterimage is incomplete and translucent, not a second character. The ally is implied only by a small warm glow behind the ward.
Avoid shield, full-body pair pose, centered portrait, physical barrier, and literal stage props.
"@
            BasicSpecial = @"
Skill: Basic Special.
Composition bucket: shadow tempo power shift.
Camera and pose: low diagonal close crop along a white blade-like light line. Nightingale is mostly offscreen on the lower-right, visible only as hair ribbon, red ribbon accent, and a dark dress edge.
Action beat: a shadow pulse weakens the target's defense while a white tempo arc feeds power forward.
Main visual: a black-violet shadow lane moves from left to center, crossed by a thin white blade-like arc that surges toward the right.
Motion detail: show two readable flows: dark weakening pressure inward, white power surge outward. No random slash cluster.
Avoid portrait pose, music notes, armor, staff, wings, and literal curtains.
"@
        }
    }
    Echo = @{
        Reference = "asset/PlayerCharater/Echo/EchoPortrait.png"
        Constraints = @"
Echo element constraints:
Allowed: long white hair, purple eyes, white outfit, dark blue angular shadow shapes from the reference, purple blade-like energy, pale sound-wave or resonance effects when explicitly requested.
Forbidden: shield, staff, heavy armor, wings, halo, crown, ornate jewelry, extra weapons unless explicitly requested, animal-like ears or horns beyond the reference's dark angular shapes, card frame, border, UI ornaments.
Keep the dark blue shapes abstract and angular. Do not turn them into armor, wings, or creatures.
"@
        Prompts = @{
            BasicDefense = @"
Skill: Basic Defense.
Composition bucket: resonance wall close crop.
Camera and pose: horizontal crop with Echo barely visible at the far left as white hair, shoulder, and dark angular shadow shapes.
Action beat: she emits a controlled resonance wall to reduce incoming harm.
Main visual: stacked pale purple-white waveform bands form a translucent vertical-leaning sound wall across the center, catching a faint impact ripple from the right.
Motion detail: the wall is transparent VFX, not a physical shield or glass plate. Keep it clean and readable.
Avoid portrait, armor, shield, staff, wings, horns, and creature-like dark shapes.
"@
            BasicGuard = @"
Skill: Basic Guard.
Composition bucket: relay shelter line.
Camera and pose: close side crop from behind Echo's shoulder, with her face hidden by white hair at the left edge.
Action beat: a resonance line expands from Echo toward an offscreen ally to cover them.
Main visual: one curved purple-white relay line runs from left to lower-right, then blooms into a soft circular ward at the far end.
Motion detail: imply the ally only through the endpoint glow. The relay line and ward are the subject.
Avoid physical shield, extra character, full-body stance, staff, and broad explosion.
"@
            BasicSpecial = @"
Skill: Basic Special.
Composition bucket: resonance debuff and power pulse.
Camera and pose: effect-dominant horizontal composition. Echo is a tiny cropped shoulder and hair silhouette at the far left edge.
Action beat: a purple resonance pulse weakens the target's survivability while a bright inner line feeds power forward.
Main visual: a narrow purple-white beam with two concentric sound rings along the path, plus dark blue angular VFX fragments peeling away from the beam.
Motion detail: the beam should be precise and surgical, not explosive. One clear line, two ring pulses.
Avoid portrait-first composition, shield, staff, extra weapons, wings, horns, and creature silhouettes.
"@
        }
    }
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

$allCharacterNames = @("Kasiya", "Mariya", "Nightingale", "Echo")
$allSkillNames = @("BasicDefense", "BasicGuard", "BasicSpecial")

$selectedCharacters = if ($CharacterName) {
    $unknownCharacters = $CharacterName | Where-Object { $_ -notin $allCharacterNames }
    if ($unknownCharacters) {
        throw "Unknown character name(s): $($unknownCharacters -join ', ')"
    }
    $allCharacterNames | Where-Object { $_ -in $CharacterName }
}
else {
    $allCharacterNames
}

$selectedSkills = if ($SkillName) {
    $unknownSkills = $SkillName | Where-Object { $_ -notin $allSkillNames }
    if ($unknownSkills) {
        throw "Unknown skill name(s): $($unknownSkills -join ', ')"
    }
    $allSkillNames | Where-Object { $_ -in $SkillName }
}
else {
    $allSkillNames
}

foreach ($character in $selectedCharacters) {
    $data = $characterData[$character]
    $referencePath = Join-Path $repoRoot $data.Reference
    if (-not (Test-Path -LiteralPath $referencePath)) {
        throw "Reference image not found: $referencePath"
    }

    foreach ($skill in $selectedSkills) {
        $outputDir = Join-Path $repoRoot "asset/CardPicture/$character"
        $outPath = Join-Path $outputDir "$skill.png"
        $rawDir = Join-Path $repoRoot "asset/generated/image2_tmp/BasicSurviveSpecial/$character"
        $rawPath = Join-Path $rawDir "$skill.raw.png"

        if ($OnlyMissingFinal -and (Test-Path -LiteralPath $outPath -PathType Leaf)) {
            Write-Host "Skipping $character $skill because final card art already exists."
            continue
        }

        $prompt = @"
$commonPrefix
$($data.Constraints)
$($data.Prompts[$skill])
"@

        Write-Host "=== Generating $character $skill ==="
        Write-Host ("    output: {0}" -f $outPath)

        $generatorArgs = @{
            Prompt = $prompt
            OutputPath = $rawPath
            OutputDir = $rawDir
            OutputName = "$($skill)_$character"
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
}

Write-Host ""
Write-Host "Basic survive/special card batch complete."
