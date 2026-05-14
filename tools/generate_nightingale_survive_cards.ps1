param(
    [string[]]$SkillName,
    [switch]$DryRun,
    [int]$RequestTimeoutSec = 90
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$generator = Join-Path $scriptRoot "openai_image_generate.ps1"
$refPath = "asset/PlayerCharater/Nightingale/NightingalePortrait.png"
$outDir = "asset/generated/nightingale_survive_v1"
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
Do not default to simple half-body composition.
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
Across this batch of 6 survival skill cards, keep each image visually distinct.
Cover at least these composition buckets across the batch: effect-dominant composition, upper-body composition, low-angle composition, top-down composition, foot close-up composition, and one cropped half-body composition.
Do not default every card to upper-body framing.
If a card already uses a strong portrait crop, prefer a different composition bucket for the next cards.
"@

$visibleSingleHandRule = @"
Hand correctness is top priority.
Show exactly one clearly readable hand.
The other hand must be fully hidden behind the body, hair, sleeve line, crop, or light effect.
Use a simple side-view or three-quarter-view hand only, never a front-facing palm toward the camera.
Show one natural hand with five fingers only.
Keep the fingers together in one clean silhouette, with the thumb clearly separated.
No finger spread, no extra fingertips, no overlapping finger confusion.
The wrist must continue naturally from the forearm with one clear bend direction only.
No twisted wrist, no reversed elbow direction, no broken forearm-to-hand connection.
If the pose becomes ambiguous, simplify the gesture instead of adding more finger detail.
"@

$rightHandednessRule = @"
Left-right rule:
The visible arm must be the character's right arm only.
The visible hand must be the character's right hand only, attached naturally to the right forearm.
Do not place a left-hand silhouette on the end of the right arm.
The thumb side must read as a right hand, not a mirrored left hand.
Keep the wrist rotation simple enough that the handedness stays obvious at first glance.
"@

$skills = @(
    @{
        Name = "VeilStep"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Veil Step.
Composition bucket: effect-dominant composition.
Action: Nightingale melts into a shadow veil while taking a silent evasive step.
Main visual: the subject can be small or partially hidden; the real subject is a flowing black-red veil trail and a pale crescent-like motion arc across the card.
Mood: stealthy, quiet, protected, evasive.
Show subtle defensive protection through veil-like shadow layers, not through a physical shield.
Do not center a portrait. Do not make this a simple standing pose.
"@
    }
    @{
        Name = "FlashOfLight"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Flash Of Light.
Composition bucket: cropped half-body composition.
Action: Nightingale releases a short-range burst of dazzling white light that exposes enemies while protecting herself with a quick guarded motion.
Main visual: bright white flare near the front of the composition, with Nightingale shown in a cropped bust or half-body view reacting through the light.
Use a sharp burst of pale white light and a few clean linear rays, but keep the image readable and not explosive.
$visibleSingleHandRule
$rightHandednessRule
Visible hand pose: one simple single-hand guarding gesture near the light burst.
The visible hand must be side-view, fingers together and slightly bent, thumb separated, palm facing sideways rather than forward.
Only one row of fingertips visible.
The hand should read as one clean silhouette first, finger details second.
Keep the visible wrist a little away from the face so the forearm-to-hand connection stays readable.
Arm anatomy rule: keep the full visible arm natural and elegant, with a clear shoulder, clear elbow, and full forearm length.
The forearm must not look shortened or compressed by perspective.
Keep the upper arm and forearm in believable anime proportions, with the forearm close to the upper arm in apparent length.
Do not place the elbow too close to the wrist or too close to the torso.
Avoid extreme foreshortening. Prefer a readable three-quarter arm angle over a camera-facing shortened arm.
Pose lock:
The right shoulder leads into the visible arm.
The right elbow bends naturally toward the light effect.
The right forearm extends into the light burst.
The right palm stays vertical in a guarding pose.
The thumb is on the character's inner side of the right hand, and must not appear mirrored.
No holy equipment, no staff, no halo, no shield.
"@
    }
    @{
        Name = "Swift"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Swift.
Composition bucket: foot close-up composition.
Action: Nightingale launches into a rapid acceleration step that boosts speed and survivability for the team.
Main visual: close focus on one foot, lower legs, trailing hem, and a sharp forward stepping motion, with thin white-red speed arcs and faint shadow streaks.
The image may crop away most of the upper body.
Make it feel fast, light, precise, and evasive rather than aggressive.
No full-body distant shot. No attack slash as the main subject.
"@
    }
    @{
        Name = "AfterimageWard"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Afterimage Ward.
Composition bucket: top-down composition.
Action: Nightingale projects protective afterimages for herself and a nearby ally.
Main visual: top-down view with one real figure and one or two faint afterimage silhouettes arranged around her, connected by pale defensive arcs or circular traces.
Keep all duplicate figures translucent and clearly ghostlike, not separate physical characters.
The focus is the layered protection pattern and spatial arrangement, not a frontal portrait.
No crowd scene. No physical barrier object.
"@
    }
    @{
        Name = "StarWard"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Star Ward.
Composition bucket: upper-body composition.
Action: Nightingale takes a poised guarding stance, gathering concentrated star-like white power around herself.
Main visual: shoulder-up crop with the face as the focal point, one forearm or shoulder line partially visible, and compact pale starlike glows with curved protective light arcs near the foreground.
Keep the power subtle and refined. The protection must read as translucent energy, not a physical shield.
Do not add stars as decorative background clutter; use only a few concentrated light points directly supporting the skill theme.
$visibleSingleHandRule
Visible hand pose: one calm guarding hand held beside the light ring, not thrust toward the camera.
Use a relaxed side-view hand with fingers together, slightly curved inward, and the thumb clearly separate.
No open palm toward viewer. No dramatic foreshortened hand. No hand larger than the face.
The face remains the focal point, while the single visible hand acts as a secondary support shape.
"@
    }
    @{
        Name = "TwilightParadox"
        Prompt = @"
$commonPrefix
$compositionRule
Skill: Twilight Paradox.
Composition bucket: symbolic-focus composition with low-angle perspective.
Action: Nightingale invokes a paradoxical guarded state that both protects and imposes a dangerous condition on a chosen enemy.
Main visual: low-angle view with Nightingale partially visible while a large twilight glyph, paradox ring, or overlapping moon-shadow arc dominates the composition.
Mood: tense, uncanny, elegant, dangerous.
Use a symbolic VFX focus with pale white light, muted red accents, and soft shadow veil layers.
Do not create physical props, floating armor, wings, or a solid magic circle platform.
"@
    }
)

foreach ($skill in $skills) {
    if ($SkillName -and ($SkillName -notcontains $skill.Name)) {
        continue
    }

    $outPath = Join-Path $outDir "$($skill.Name).png"
    Write-Host "=== Generating $($skill.Name) ==="
    & $generator `
        -Prompt $skill.Prompt `
        -ReferenceImage $refPath `
        -OutputPath $outPath `
        -OutputDir $outDir `
        -OutputName $skill.Name `
        -Size "1024x768" `
        -Quality "high" `
        -OutputFormat "png" `
        -RequestTimeoutSec $requestTimeoutSec `
        -DryRun:$DryRun
}

Write-Host ""
Write-Host "All Nightingale survival prompts submitted."
