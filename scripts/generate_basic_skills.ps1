$ErrorActionPreference = "Stop"

$key = (Get-Content -Raw -LiteralPath "C:\tmp\key.txt").Trim()
$baseUrl = "https://api.traxnode.com/v1"
$model = "gpt-image-2"
$size = "1024x768"

$characters = @(
    @{
        Name = "Kasiya"
        Portrait = "C:\godot_project\Four-Dimensional\asset\PlayerCharater\Kasiya\KasiyaPortrait.png"
        Constraints = @"
Kasiya element constraints:
Allowed: white or silver hair, golden eyes, white dress or armor-like white outfit, black angular shoulder pieces, red chest gem, a long sword or blade-like white light, pale holy or crystal-like light effects.
Forbidden: shield, physical shield, buckler, barrier held as a shield, extra weapons, staff, wings, halo, crown, helmet, heavy armor, jewelry not visible in the reference, card frame, border, UI ornaments.
For defense skills, use abstract light planes, crystal facets, guard stance, or white/gold energy arcs instead of a shield. These effects must stay translucent and intangible, not held equipment.
"@
    },
    @{
        Name = "Mariya"
        Portrait = "C:\godot_project\Four-Dimensional\asset\PlayerCharater\Mariya\MariyaPortrait.png"
        Constraints = @"
Mariya element constraints:
Allowed: light blue hair, green eyes, sleeveless white dress, bare arms, simple silver crescent staff or polearm motif, blue ribbon accents, pale healing or holy light effects.
Forbidden: gloves, detached sleeves, long arm coverings, heavy armor, shield, extra swords unless explicitly requested, wings, halo, crown, ornate jewelry, card frame, border, UI ornaments.
Keep Mariya's outfit simple and sleeveless. Do not add new costume layers.
Holy/healing symbols may appear only as translucent VFX behind or around her, never as physical accessories attached to her body or outfit.
"@
    },
    @{
        Name = "Nightingale"
        Portrait = "C:\godot_project\Four-Dimensional\asset\PlayerCharater\Nightingale\NightingalePortrait.png"
        Constraints = @"
Nightingale element constraints:
Allowed: blonde twin-tail hair, black hair bows, red eyes, black sleeveless dress, bare arms, red ribbon accent, white blade-like light effects, shadow veil or moonlike arcs when explicitly requested.
Forbidden: shield, staff, armor, wings, halo, crown, extra costume layers, gloves unless visible in the reference, musical notes unless the skill explicitly requests song/music, card frame, border, UI ornaments.
Keep the silhouette light and assassin-like. Do not add fantasy armor or holy equipment.
Shadow, moon, or song elements may appear only as translucent VFX, not as physical props, clothing, wings, or accessories.
"@
    },
    @{
        Name = "Echo"
        Portrait = "C:\godot_project\Four-Dimensional\asset\PlayerCharater\Echo\EchoPortrait.png"
        Constraints = @"
Echo element constraints:
Allowed: long white hair, purple eyes, white outfit, dark blue angular shadow shapes from the reference, purple blade-like energy, pale sound-wave or resonance effects when explicitly requested.
Forbidden: shield, staff, heavy armor, wings, halo, crown, ornate jewelry, extra weapons unless explicitly requested, animal-like ears or horns beyond the reference's dark angular shapes, card frame, border, UI ornaments.
Keep the dark blue shapes abstract and angular. Do not turn them into armor, wings, or creatures.
Sound, resonance, void, or echo elements may appear only as translucent VFX, not as physical equipment, body parts, or solid attached objects.
"@
    }
)

$skills = @(
    @{
        Id = "BasicAttack"
        Name = "Basic Attack"
        ComposeHint = @"
Composition: upper-body composition or dynamic side-profile.
Action: a simple decisive attack pose.
Main visual: the character's signature weapon or energy effect in a clean slash or strike motion.
"@
    },
    @{
        Id = "BasicDefense"
        Name = "Basic Defense"
        ComposeHint = @"
Composition: close-up bust or shoulder crop.
Action: a guarded or evasive stance.
Main visual: translucent intangible defensive VFX in front of the character. No physical shield, no armor, no equipment.
"@
    },
    @{
        Id = "BasicSpecial"
        Name = "Basic Special"
        ComposeHint = @"
Composition: centered upper-body composition.
Action: a focused power-up or buff stance.
Main visual: eyes glowing faintly, a soft aura or energy gathering around the character. Suggest empowerment without explosive effects.
"@
    }
)

$stylePrefix = @"
Pale anime sketch with clean thin line art. Simple watercolor shading with slightly cleaner color blocks. Keep a light sketch feeling, but avoid messy stray lines, broken scratch lines, noisy crosshatching, and over-fragmented hair lines. Use flat bright white background, controlled negative space, and moderate visible color. Do not default to simple half-body composition. Do not over-polish: no glossy rendering, no commercial poster finish, no dense gradients, no hyper-detailed effects. No card frame, no border, no UI ornaments, no text, no watermark, no logo.
Hand correctness is top priority. Avoid visible detailed hands whenever possible. Hide the off-hand behind sleeve, hair, body, or composition. Use a simple side-view or three-quarter-view hand only if visible. Show one natural hand with five fingers only. Keep the fingers together in one clean silhouette, with the thumb clearly separated. No finger spread, no extra fingertips, no overlapping finger confusion. No twisted wrist, no reversed elbow direction, no broken forearm-to-hand connection.
"@

foreach ($char in $characters) {
    $outDir = "C:\godot_project\Four-Dimensional\asset\CardPicture\$($char.Name)"
    if (!(Test-Path $outDir)) {
        New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    }

    foreach ($skill in $skills) {
        $outPath = Join-Path $outDir "$($skill.Id).png"
        if (Test-Path $outPath) {
            Write-Host "[SKIP] $($char.Name) / $($skill.Id).png already exists." -ForegroundColor Yellow
            continue
        }

        $prompt = @"
Use the provided image as the only visual reference.
Keep the same character identity, outfit, palette, and simple pale anime rendering.
Raw game skill artwork only, not a finished card template.
Follow $($char.Name) element constraints exactly.
$stylePrefix
Skill: $($skill.Name).
$($skill.ComposeHint)
"@

        Write-Host "[GENERATE] $($char.Name) / $($skill.Id) ..." -NoNewline

        $jsonFile = [System.IO.Path]::GetTempFileName()
        $curlArgs = @(
            "-s", "-X", "POST",
            "--max-time", "300",
            "-H", "Authorization: Bearer $key",
            "-F", "model=$model",
            "-F", "prompt=$prompt",
            "-F", "size=$size",
            "-F", "image=@$($char.Portrait)",
            "-o", $jsonFile,
            "-w", "%{http_code}",
            "$baseUrl/images/edits"
        )

        $httpCode = & curl.exe @curlArgs

        if ($httpCode -ne "200") {
            Write-Host " FAILED (HTTP $httpCode)" -ForegroundColor Red
            Remove-Item $jsonFile -ErrorAction SilentlyContinue
            continue
        }

        try {
            $json = Get-Content -Raw $jsonFile | ConvertFrom-Json
            $item = $json.data[0]
            if ($item.b64_json) {
                [IO.File]::WriteAllBytes($outPath, [Convert]::FromBase64String($item.b64_json))
            } elseif ($item.url) {
                & curl.exe -s -L -o $outPath $item.url
            } else {
                Write-Host " FAILED (no image data)" -ForegroundColor Red
                continue
            }
            Write-Host " DONE -> $outPath" -ForegroundColor Green
        } catch {
            Write-Host " FAILED ($_))" -ForegroundColor Red
        } finally {
            Remove-Item $jsonFile -ErrorAction SilentlyContinue
        }
    }
}

Write-Host "`nAll generations complete." -ForegroundColor Cyan
