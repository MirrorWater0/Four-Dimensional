[CmdletBinding(PositionalBinding = $false)]
param(
    [string[]]$SkillName,
    [switch]$DryRun,
    [switch]$StatusOnly,
    [switch]$OnlyMissingFinal,
    [switch]$UseExistingDraftReference,
    [int]$RequestTimeoutSec = 90,
    [string]$OutputDir = "asset/generated/nightingale_special_v2",
    [string]$DraftReferenceDir = "asset/generated/nightingale_special_v1",
    [string]$FinalCardDir = "asset/CardPicture/Nightingale",
    [string]$TempRawDir = "asset/generated/image2_tmp/Nightingale"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$generator = Join-Path $scriptRoot "openai_image_generate.ps1"
$refPath = Join-Path $repoRoot "asset/PlayerCharater/Nightingale/NightingalePortrait.png"
$outDirPath = Join-Path $repoRoot $OutputDir
$draftReferenceDirPath = Join-Path $repoRoot $DraftReferenceDir
$finalCardDirPath = Join-Path $repoRoot $FinalCardDir
$tempRawDirPath = Join-Path $repoRoot $TempRawDir

$specialSkillNames = @(
    "NightingaleEnergy",
    "TempoSurge",
    "LongNight",
    "RequiemBloom",
    "CurtainCallMoment",
    "SunMoonCycle",
    "ShadowForm"
)

if (-not (Test-Path -LiteralPath $generator)) {
    throw "Generator script not found: $generator"
}

if (-not (Test-Path -LiteralPath $refPath)) {
    throw "Reference image not found: $refPath"
}

function Test-AssetFile {
    param([string]$Path)

    return (Test-Path -LiteralPath $Path -PathType Leaf)
}

function Get-SkillInventory {
    param([string]$Name)

    $finalPath = Join-Path $finalCardDirPath "$Name.png"
    $draftPath = Join-Path $draftReferenceDirPath "$Name.png"
    $tempRawPath = Join-Path $tempRawDirPath "$Name.raw.png"

    [pscustomobject]@{
        Name = $Name
        FinalExists = Test-AssetFile $finalPath
        DraftExists = Test-AssetFile $draftPath
        TempRawExists = Test-AssetFile $tempRawPath
        FinalPath = $finalPath
        DraftPath = $draftPath
        TempRawPath = $tempRawPath
    }
}

function Get-InventoryLabel {
    param([object]$Inventory)

    $parts = New-Object System.Collections.Generic.List[string]
    if ($Inventory.FinalExists) {
        $parts.Add("final")
    }
    if ($Inventory.DraftExists) {
        $parts.Add("draft")
    }
    if ($Inventory.TempRawExists) {
        $parts.Add("raw")
    }

    if ($parts.Count -eq 0) {
        return "missing"
    }

    return ($parts -join ", ")
}

$inventoryByName = @{}
foreach ($skillNameEntry in $specialSkillNames) {
    $inventoryByName[$skillNameEntry] = Get-SkillInventory -Name $skillNameEntry
}

Write-Host "Nightingale special skill inventory:"
foreach ($skillNameEntry in $specialSkillNames) {
    $inventory = $inventoryByName[$skillNameEntry]
    Write-Host ("- {0}: {1}" -f $skillNameEntry, (Get-InventoryLabel -Inventory $inventory))
}

if ($StatusOnly) {
    exit 0
}

if (-not (Test-Path -LiteralPath $outDirPath)) {
    New-Item -ItemType Directory -Path $outDirPath -Force | Out-Null
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

Nightingale element constraints:
Allowed: blonde twin-tail hair, black hair bows, red eyes, black sleeveless dress, bare arms, red ribbon accent, white blade-like light effects, shadow veil or moonlike arcs when explicitly requested.
Forbidden: shield, staff, armor, wings, halo, crown, extra costume layers, gloves unless visible in the reference, musical notes unless the skill explicitly requests song or music, card frame, border, UI ornaments.
Keep the silhouette light and assassin-like. Do not add fantasy armor or holy equipment.
Shadow, moon, or song elements may appear only as translucent VFX, not as physical props, clothing, wings, or accessories.

Only draw elements explicitly visible in the character reference image or explicitly requested in this prompt.
Never invent extra weapons, armor, shields, wings, horns, halos, jewelry, UI frames, card borders, corner ornaments, stars, sparkles, or background props.
Any extra skill-themed element must be intangible VFX only: translucent light, mist, aura, abstract lines, energy arcs, glyphs, silhouettes, or symbolic shadows.
Extra elements must never become physical equipment, costume parts, held props, body parts, or solid objects attached to the character.
"@

$compositionRule = @"
Composition balance rule:
Across this batch of 7 special skill cards, keep each image visually distinct.
Cover at least these composition buckets across the batch: quiet upper-body composition, full-body dynamic composition, effect-dominant composition, symbolic-focus composition, top-down composition, low-angle composition, and one side-profile or shoulder-up composition.
Do not default every card to upper-body framing.
If one card already uses a strong portrait crop, prefer a very different bucket for the next cards.
"@

$closedGripRule = @"
Hand correctness is top priority.
If a visible hand appears, use only one clearly readable hand.
Prefer a simple closed grip, relaxed fist, or a single palm silhouette with fingers together.
No finger spread, no extra fingertips, no twisted wrist, no mirrored left-right confusion.
Hide any second hand behind the body, hair, crop, or light effect.
"@

$secondReferenceRule = @"
Image 1 is the character identity reference.
Image 2 is only an action, composition, and effect reference for the existing skill image.
Image 2 is not a style reference and not an element whitelist.
Keep the character design, palette, and element restrictions from Image 1 and this prompt.
"@

$skills = @(
    @{
        Name = "NightingaleEnergy"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Nightingale Energy.
Composition bucket: quiet upper-body composition.
Action: Nightingale gathers one unit of inner energy in a calm, restful moment.
Main visual: serene bust or upper-body view, eyes softly closed or lowered, with one hand resting lightly near the chest and pale white energy threads circling inward.
Mood: quiet, restorative, intimate, poised before action.
$closedGripRule
If a visible hand appears, keep it simple and relaxed against the chest with fingers together in one clean silhouette.
Avoid dramatic attack pose, enemy presence, or large explosive effects.
"@
    }
    @{
        Name = "TempoSurge"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Tempo Surge.
Composition bucket: speed-effect dominant close crop.
Action: Nightingale launches into a sudden accelerating surge that boosts speed and power.
Main visual: aggressive diagonal close crop showing only the head, upper torso, one leading shoulder, and one thrusting forearm cutting through the frame, with long pale speed slashes and a white blade-like light accent leading the motion.
Mood: fast, sharp, committed, explosive forward momentum.
$closedGripRule
Use only one visible hand and make it a simple closed fist or closed grip.
Do not use open palms or detailed fingers. The speed lines and body trajectory are the main subject.
Do not show a full-body sprint, both legs, or a complete standing silhouette.
Crop away most of the lower body. At most, a single partial thigh or knee fragment may appear at the frame edge.
The composition should feel like the camera is too close to capture the whole body.
"@
    }
    @{
        Name = "LongNight"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Long Night.
Composition bucket: effect-dominant composition.
Action: Nightingale spreads a long enveloping night veil that reaches across adjacent allies.
Main visual: a broad crescent shadow canopy or flowing black-red night veil dominates the card, with Nightingale small or partially hidden within it.
Mood: enveloping, solemn, expansive, protective through darkness.
Show connection and extension across space rather than a single target hit.
Avoid literal ally bodies if possible; imply protection through the veil span and layered defensive VFX.
Do not make this a close-up portrait. The spreading veil is the subject.
"@
    }
    @{
        Name = "RequiemBloom"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Requiem Bloom.
Composition bucket: symbolic-focus composition.
Action: Nightingale enters a requiem-like bloom of power and rebirth.
Main visual: pale white petals or flower-like light layers bloom around her, with a faint rebirth aura and one elegant forward-turning motion.
Mood: mournful, beautiful, elevated, miraculous.
$closedGripRule
If a hand appears, keep it secondary and simplified. The flower-like bloom and rebirth aura are the true subject.
Do not invent a physical flower prop. The bloom must be intangible VFX only.
"@
    }
    @{
        Name = "CurtainCallMoment"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Curtain Call Moment.
Composition bucket: low-angle composition.
Action: Nightingale delivers a final weakening cue and vanishes into an invisible curtain-call moment while granting a follow-up opening to an ally.
Main visual: low-angle view with stage-like shadow curtains or sweeping dark arcs closing around the frame, while Nightingale begins to disappear into pale shadow.
Mood: theatrical, final, vanishing, elegant danger.
Show weakening through an abstract hostile silhouette or collapsing light mark only, not a full enemy character.
Do not turn the curtain motif into a physical stage set. Keep it intangible and atmospheric.
"@
    }
    @{
        Name = "SunMoonCycle"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Sun Moon Cycle.
Composition bucket: top-down symbolic-focus close crop.
Action: Nightingale invokes a rotating cycle of sun and moon that renews resources.
Main visual: top-down circular composition where a large clean sun-moon ring and orbiting pale light arcs dominate almost the entire card, while Nightingale appears only as a small upper-body or shoulder-up figure near the center of the cycle.
Mood: cyclical, balanced, ritual-like, controlled.
The circle and orbit lines are the subject.
Do not create a physical platform or ornate celestial machinery.
Do not show full legs, full feet, or a complete full-body figure.
Keep the body cropped above the waist or at most mid-thigh from the top-down angle.
The celestial cycle must occupy much more visual area than the character.
"@
    }
    @{
        Name = "ShadowForm"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Shadow Form.
Composition bucket: side-profile or shoulder-up composition.
Action: Nightingale shifts into a shadow state, becoming harder to perceive while gathering dense shadow power.
Main visual: side profile or shoulder-up crop with one real figure and one or two offset shadow silhouettes phasing behind her.
Mood: hidden, predatory, poised, transformed.
Keep the shadow layers translucent and attached to her motion, not as separate clones.
Do not add armor, wings, or monster features. The transformation must stay elegant and human.
"@
    }
)

$selectedSkills = if ($SkillName) {
    $unknownNames = $SkillName | Where-Object { $_ -notin $specialSkillNames }
    if ($unknownNames) {
        throw "Unknown Nightingale special skill name(s): $($unknownNames -join ', ')"
    }

    $skills | Where-Object { $_.Name -in $SkillName }
}
else {
    $skills
}

foreach ($skill in $selectedSkills) {
    $inventory = $inventoryByName[$skill.Name]
    if ($OnlyMissingFinal -and $inventory.FinalExists) {
        Write-Host "Skipping $($skill.Name) because final card art already exists."
        continue
    }

    $referenceImages = @($refPath)
    $prompt = $skill.Prompt
    if ($UseExistingDraftReference -and $inventory.DraftExists) {
        $referenceImages += $inventory.DraftPath
        $prompt = @"
$prompt

$secondReferenceRule
"@
    }

    $outPath = Join-Path $outDirPath "$($skill.Name).png"
    Write-Host "=== Generating $($skill.Name) ==="
    Write-Host ("    inventory: {0}" -f (Get-InventoryLabel -Inventory $inventory))
    Write-Host ("    output: {0}" -f $outPath)
    if ($referenceImages.Count -gt 1) {
        Write-Host "    using draft image as second reference."
    }

    & $generator `
        -Prompt $prompt `
        -ReferenceImage $referenceImages `
        -OutputPath $outPath `
        -OutputDir $outDirPath `
        -OutputName $skill.Name `
        -Size "1024x768" `
        -Quality "high" `
        -OutputFormat "png" `
        -RequestTimeoutSec $RequestTimeoutSec `
        -DryRun:$DryRun
}

Write-Host ""
Write-Host "Nightingale special skill batch complete."
