[CmdletBinding(PositionalBinding = $false)]
param(
    [string[]]$SkillName,
    [switch]$DryRun,
    [switch]$StatusOnly,
    [switch]$OnlyMissingFinal,
    [int]$RequestTimeoutSec = 300,
    [string]$OutputDir = "asset/CardPicture/Mariya",
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
$refPath = Join-Path $repoRoot "asset/PlayerCharater/Mariya/MariyaPortrait.png"
$outputDirPath = Join-Path $repoRoot $OutputDir

if (-not (Test-Path -LiteralPath $generator)) {
    throw "Generator script not found: $generator"
}

if (-not (Test-Path -LiteralPath $refPath)) {
    throw "Reference image not found: $refPath"
}

$commonPrefix = @"
Use the provided image as the only visual reference.
Keep the same character identity, outfit, palette, and simple pale anime rendering.
Raw game skill artwork only, not a finished card template.
Pale anime sketch with clean thin line art.
Simple watercolor shading with slightly cleaner color blocks.
Keep a light sketch feeling, but avoid messy stray lines, broken scratch lines, noisy crosshatching, and over-fragmented hair lines.
Use cleaner continuous contour lines.
Reduce stray construction lines and broken line fragments.
Use flat bright white background, controlled negative space, and moderate visible color.
Do not over-polish: no glossy rendering, no commercial poster finish, no dense gradients, no hyper-detailed effects.
No text, no watermark, no logo.

Mariya element constraints:
Allowed: light blue hair, green eyes, sleeveless white dress, bare arms, simple silver crescent staff or polearm motif, blue ribbon accents, pale healing or holy light effects.
Forbidden: gloves, detached sleeves, long arm coverings, heavy armor, shield, extra swords unless explicitly requested, wings, halo, crown, ornate jewelry, card frame, border, UI ornaments.
Keep Mariya's outfit simple and sleeveless. Do not add new costume layers.
Holy or healing symbols may appear only as translucent VFX behind or around her, never as physical accessories attached to her body or outfit.

Only draw elements explicitly visible in the character reference image or explicitly requested in this prompt.
Never invent extra weapons, armor, shields, wings, horns, halos, jewelry, UI frames, card borders, corner ornaments, stars, sparkles, or background props.
Any extra skill-themed element must be intangible VFX only: translucent light, mist, aura, abstract lines, energy arcs, glyphs, silhouettes, or symbolic shadows.
Extra elements must never become physical equipment, costume parts, held props, body parts, or solid objects attached to the character.
"@

$compositionRule = @"
Composition balance rule:
Across this batch of Mariya skill cards, keep each image visually distinct.
Balance across these buckets: upper-body composition, shoulder crop, close hand-support crop, top-down composition, low-angle composition, effect-dominant composition, symbolic-focus composition, and dynamic half-body composition.
Do not default every skill card to upper-body framing.
If one card already uses a strong portrait crop, prefer a very different bucket for the next cards.
"@

$handRule = @"
Hand correctness is top priority.
Avoid visible detailed hands whenever possible.
If a visible hand appears, show only one clearly readable hand.
Use a simple closed grip, relaxed supporting hand, or calm side-view gesture with fingers together.
No open palm toward the camera, no spread fingers, no extra fingertips, no twisted wrist.
Hide the second hand behind hair, body, crop, or light effect.
"@

$skills = @(
    @{
        Name = "MendSlash"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Mend Slash.
Composition bucket: dynamic half-body composition.
Action: Mariya performs a graceful healing slash that mends an ally while striking.
Main visual: one flowing slash arc in pale green-white light crossing the foreground, with Mariya shown waist-up or half-body turning through the motion.
Mood: gentle, precise, restorative.
$handRule
Do not make this a violent finishing attack. The healing slash arc is more important than the weapon.
"@
    }
    @{
        Name = "SwapSlash"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Swap Slash.
Composition bucket: symbolic-focus composition.
Action: Mariya cuts through a positional gap and exchanges places through a swift support slash.
Main visual: a diagonal silver-white slash plus one faint afterimage offset behind her, linked by a soft blue relay trail.
Mood: agile, clever, repositioning.
Keep the afterimage translucent and partial, not a second full character.
"@
    }
    @{
        Name = "SiphonSlash"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Siphon Slash.
Composition bucket: close crop with effect lane.
Action: Mariya draws life back from the target through a quiet draining strike.
Main visual: a thin crimson-violet energy thread pulling inward toward Mariya from the slash path, with Mariya shown in cropped bust or shoulder view near one side.
Mood: surgical, solemn, controlled.
Avoid explosive blood-red clutter. Keep the draining line clean and readable.
"@
    }
    @{
        Name = "ShatterSlash"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Shatter Slash.
Composition bucket: effect-dominant composition.
Action: Mariya unleashes a wide shattering strike that fractures through multiple enemies.
Main visual: broad ice-blue and white fracture-like energy planes spreading from one impact line, with Mariya small or partially cropped behind the effect.
Mood: forceful, sacrificial, high-impact.
The fracture must read as energy stress, not physical glass debris.
"@
    }
    @{
        Name = "ChargedBlade"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Charged Blade.
Composition bucket: low-angle composition.
Action: Mariya gathers brilliant stored power into a charged blade before release.
Main visual: low-angle half-body pose with the charged silver-white weapon line and compact gold energy packed around it, plus a few restrained lightning-like arcs.
Mood: focused, rising, compressed power.
$handRule
Do not turn the weapon into a giant oversized sword prop.
"@
    }
    @{
        Name = "CrescentWind"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Crescent Wind.
Composition bucket: wide effect-dominant composition.
Action: Mariya sends crescent-like wind slashes across both enemy rows while weakening them.
Main visual: multiple pale crescent wind arcs sweeping across the frame with Mariya small or cropped at one edge.
Mood: elegant, airy, suppressive.
Do not center a portrait. The crossing crescents are the real subject.
"@
    }
    @{
        Name = "ConcordSlash"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Concord Slash.
Composition bucket: weapon-grip action close crop.
Camera and pose: tight diagonal action crop from Mariya's front-right side, waist-up only. Her torso twists left while the polearm runs firmly from lower-right foreground toward upper-left background.
Action beat: she is completing a controlled support slash, not posing. The body weight pulls backward, the weapon shaft stays straight and stable, and the slash trail follows the polearm line.
Hand and grip correction: Mariya's own right hand is the main visible hand gripping the polearm near the lower-right foreground. The right wrist must connect naturally to her right forearm. Show exactly one clear gripping hand with five fingers wrapped around the shaft. The left hand must be hidden behind the shaft, hair, body, or slash light.
Main visual: one strong silver-white slash lane crosses the foreground along the stable polearm, and one softer pale green-gold echo arc follows behind it at a different angle.
Motion detail: the grip must look solid and load-bearing; do not let the polearm float, wobble, or pass behind an impossible hand.
Mood: coordinated, balanced, uplifting, a support attack rather than a solo execution.
Avoid left-hand-looking grip, reversed thumb, extra hand, loose fingertips, centered portrait framing, extra characters, literal musical notes, and many unrelated slash marks.
"@
    }
    @{
        Name = "FinalGuard"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Final Guard.
Composition bucket: shoulder crop.
Action: Mariya braces in a final guarding stance while sending empowering protection toward an ally.
Main visual: close shoulder-up composition with a pale protective light plane in the foreground and a soft power-giving glow extending outward.
Mood: steadfast, composed, protective.
The defense must read as translucent holy VFX, not a physical shield.
"@
    }
    @{
        Name = "CrystalGuard"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Crystal Guard.
Composition bucket: top-down composition.
Action: Mariya invokes a group-wide crystal-like protection.
Main visual: top-down view with Mariya near the center and several translucent crystal-light facets or arcs radiating outward to imply all allies are covered.
Mood: broad, clear, supportive.
Do not create physical crystal walls or shields. Keep the facets airy and symbolic.
"@
    }
    @{
        Name = "QuietVeil"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Quiet Veil.
Composition bucket: intimate upper-body composition.
Action: Mariya wraps herself in a soft invisible protective veil that also restores vitality.
Main visual: bust or upper-body crop with a thin pale veil of healing light crossing in front of her and one or two subtle green-white recovery glows near the torso.
Mood: quiet, hidden, restorative.
Keep the veil mist-like and protective, not theatrical curtains.
"@
    }
    @{
        Name = "EnergyTransfer"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Energy Transfer.
Composition bucket: hand-support close crop.
Action: Mariya blocks while channeling energy toward an ally.
Main visual: a close half-body or forearm-led composition where one calm guiding gesture sends a bright gold-white energy stream toward the open side of the card.
Mood: supportive, directed, efficient.
$handRule
The outgoing energy stream is the subject, not a frontal portrait.
"@
    }
    @{
        Name = "EnergyRelay"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Energy Relay.
Composition bucket: top-down symbolic composition.
Action: Mariya relays energy from one ally position to another through herself.
Main visual: Mariya centered in a simple top-down or slightly elevated view, with one energy lane entering from one side and another leaving from the other side.
Mood: connective, technical, supportive.
Avoid extra characters. Show the relay structure through abstract energy paths only.
"@
    }
    @{
        Name = "TouchOfGod"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Touch Of God.
Composition bucket: low-angle symbolic-focus composition.
Camera and pose: low camera looking upward from near the ground. Mariya is small in the lower center or lower-left, standing steady with the simple crescent staff or polearm motif angled beside her; hands can be hidden by the crop, staff, or light.
Action beat: she plants her stance and calls protection down from above rather than attacking forward.
Main visual: one huge translucent golden hand-shaped holy VFX forms from the upper-right sky area and reaches down toward the center of the card. The palm and fingers are made of layered light bands, mist, and glyph-like lines, never solid flesh.
Motion detail: the descending hand should cast soft protective rays around Mariya, with a clear vertical flow from top-right to lower-center.
Mood: solemn, sacred, protective, quiet miracle.
Avoid portrait-first composition, physical giant limbs, extra hands attached to Mariya, halos, wings, ornate jewelry, shields, or background architecture.
"@
    }
    @{
        Name = "RebirthPrayer"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Rebirth Prayer.
Composition bucket: calm upper-body composition.
Action: Mariya offers a rebirth prayer that heals, shields, and restores life.
Main visual: serene bust or half-body pose with layered pale green-gold prayer light and a gentle circular protection aura.
Mood: compassionate, sacred, renewing.
Avoid complex hand poses. Keep the prayer readable and calm.
"@
    }
    @{
        Name = "Sacrifice"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Sacrifice.
Composition bucket: low-angle symbolic composition.
Action: Mariya unleashes a powerful sacrificial holy attack at great cost to her team.
Main visual: low-angle composition with Mariya partly silhouetted against a broad crimson-gold sacrificial flare and a heavy descending strike line.
Mood: severe, tragic, powerful.
Keep the effect abstract and holy. Do not add gore or demonic props.
"@
    }
    @{
        Name = "RearlineRevival"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Rearline Revival.
Composition bucket: vertical revival-beam composition.
Action: Mariya calls restorative light down onto fallen allies in the back line.
Main visual: a descending pillar of warm gold-white light from above with Mariya shown as a dynamic lower-third half-body or three-quarter figure reaching into it from the side, not standing still beneath it.
Mood: miraculous, hopeful, distant support.
Do not show multiple ally bodies. Let the revival beam carry the meaning.
Do not make Mariya tiny in the center of a mostly empty card.
Do not reuse the portrait pose. Give her a reaching, guiding motion that feels like active invocation.
"@
    }
    @{
        Name = "GroupHealing"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Group Healing.
Composition bucket: broad top-down composition.
Action: Mariya spreads a group-wide healing bath of sacred light.
Main visual: circular waves of pale green-gold light radiating outward from Mariya, with faint healing rings or arcs expanding across the card.
Mood: warm, abundant, enveloping.
Avoid crowd scenes. The healing field should imply the team-wide effect by itself.
"@
    }
    @{
        Name = "Ragnarok"
        ReferenceImage = $false
        Prompt = @"
Skill: Ragnarok.
Raw game skill artwork only, not a finished card template.
Pale anime sketch with clean thin line art and simple watercolor shading, but this card is pure VFX with a sunset backdrop.
Use a wide horizontal 980x700 card-art composition with no character.
Composition bucket: pure effect-dominant sunset apocalypse.
Scene: a bright sunset horizon over a mostly white, pale gold sky. Warm orange light sits low near the horizon, fading upward into clean white negative space.
Action beat: a catastrophic holy-war rupture opens in the sky at sunset, as if Ragnarok is happening far away beyond the battlefield.
Main visual: one huge white-gold solar core near the horizon, surrounded by broad crimson-red crescent pressure waves and dark red sacred turbulence. One heavy diagonal white-gold strike line tears across the sky from upper-left to lower-right.
Motion detail: use large clean VFX shapes, controlled arcs, and a readable central flare. The image should feel like a divine disaster at sunset, not a portrait.
Color palette: sunset gold, pale orange, crimson red, white-gold light, small accents of dark red. Keep the background bright and readable.
No human figure, no Mariya, no face, no body, no hands, no weapon, no staff, no armor, no wings, no halo, no city, no landscape detail, no gore, no monsters, no text, no watermark, no logo, no card frame.
"@
    }
    @{
        Name = "SanctuaryForm"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Sanctuary Form.
Composition bucket: field-unfolding back-view composition.
Camera and pose: elevated three-quarter back view. Mariya is small and placed near the bottom-left third, seen from behind or side-back with head slightly bowed. Her arms are folded close to the chest or hidden by hair and light; no raised arm, no reaching hand, no copied portrait pose.
Action beat: she silently releases the sanctuary outward from the ground, like a field opening beneath her rather than power coming from her raised hand.
Main visual: three separated VFX layers dominate the card: an inner pale green healing circle blooming under her feet, a middle band of warm gold sanctuary arcs curving around her from left to right, and a high translucent canopy ring sweeping overhead from the upper-left edge to the upper-right edge.
Motion detail: the rings must spiral away from Mariya and fill the image as the real subject. Her figure should support the field, not become a centered character pose.
Mood: quiet, protective, transcendent, broad team shelter implied without showing other allies.
Hard avoid: do not copy the reference pose, do not show Mariya holding the staff upward, do not show one arm stretched high, do not draw an open palm gesture, do not place her centered inside a simple circle.
Avoid portrait-first composition, physical dome, shield, armor, wings, halo, crown, buildings, extra characters, or solid crystal walls. The sanctuary must be entirely intangible holy VFX.
"@
    }
)

function Test-AssetFile {
    param([string]$Path)

    return (Test-Path -LiteralPath $Path -PathType Leaf)
}

function Get-InventoryLabel {
    param([string]$Name)

    $path = Join-Path $outputDirPath "$Name.png"
    if (Test-AssetFile $path) {
        return "final"
    }

    return "missing"
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

Write-Host "Mariya skill card inventory:"
foreach ($skill in $skills) {
    Write-Host ("- {0}: {1}" -f $skill.Name, (Get-InventoryLabel -Name $skill.Name))
}

if ($StatusOnly) {
    exit 0
}

if (-not (Test-Path -LiteralPath $outputDirPath)) {
    New-Item -ItemType Directory -Path $outputDirPath -Force | Out-Null
}

$selectedSkills = if ($SkillName) {
    $allNames = $skills.Name
    $unknownNames = $SkillName | Where-Object { $_ -notin $allNames }
    if ($unknownNames) {
        throw "Unknown Mariya skill name(s): $($unknownNames -join ', ')"
    }

    $skills | Where-Object { $_.Name -in $SkillName }
}
else {
    $skills
}

foreach ($skill in $selectedSkills) {
    $outPath = Join-Path $outputDirPath "$($skill.Name).png"
    if ($OnlyMissingFinal -and (Test-AssetFile $outPath)) {
        Write-Host "Skipping $($skill.Name) because final card art already exists."
        continue
    }

    Write-Host "=== Generating $($skill.Name) ==="
    Write-Host ("    output: {0}" -f $outPath)
    $rawDir = Join-Path $repoRoot "asset/generated/image2_tmp/Mariya"
    $rawPath = Join-Path $rawDir "$($skill.Name).raw.png"
    $useReferenceImage = -not ($skill.ContainsKey("ReferenceImage") -and $skill.ReferenceImage -eq $false)

    if ($useReferenceImage) {
        & $generator `
            -Prompt $skill.Prompt `
            -ReferenceImage $refPath `
            -OutputPath $rawPath `
            -OutputDir $rawDir `
            -OutputName $skill.Name `
            -Size $GenerationSize `
            -Quality "high" `
            -OutputFormat "png" `
            -RequestTimeoutSec $RequestTimeoutSec `
            -DryRun:$DryRun
    }
    else {
        & $generator `
            -Prompt $skill.Prompt `
            -OutputPath $rawPath `
            -OutputDir $rawDir `
            -OutputName $skill.Name `
            -Size $GenerationSize `
            -Quality "high" `
            -OutputFormat "png" `
            -RequestTimeoutSec $RequestTimeoutSec `
            -DryRun:$DryRun
    }

    if (-not $DryRun) {
        Write-Host ("    resizing to final card size: {0}x{1}" -f $FinalWidth, $FinalHeight)
        Convert-ToCardSize -SourcePath $rawPath -DestinationPath $outPath -Width $FinalWidth -Height $FinalHeight
        if (-not $KeepRawSource) {
            Remove-Item -LiteralPath $rawPath -Force
        }
    }
}

Write-Host ""
Write-Host "Mariya skill card batch complete."
