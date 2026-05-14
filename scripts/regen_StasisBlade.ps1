$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

$key = (Get-Content -Raw -LiteralPath "C:\tmp\key.txt").Trim()
$refPath = "C:\godot_project\Four-Dimensional\asset\PlayerCharater\Nightingale\NightingalePortrait.png"
$outPath = "C:\godot_project\Four-Dimensional\asset\CardPicture\Nightingale\StasisBlade_v2.png"

$prompt = @"
Use the provided image as the only visual reference.
Keep the same character identity, outfit, palette, and simple pale anime rendering.
Raw game skill artwork only, not a finished card template.
Bright white background, lots of negative space.
No text, no watermark, no logo.

Nightingale element constraints:
Allowed: blonde twin-tail hair, black hair bows, red eyes, black sleeveless dress, bare arms, red ribbon accent, white blade-like light effects, shadow veil or moonlike arcs when explicitly requested.
Forbidden: shield, staff, armor, wings, halo, crown, extra costume layers, gloves unless visible in the reference, musical notes unless the skill explicitly requests song/music, card frame, border, UI ornaments.
Keep the silhouette light and assassin-like. Do not add fantasy armor or holy equipment.
Shadow, moon, or song elements may appear only as translucent VFX, not as physical props, clothing, wings, or accessories.

Skill: Stasis Blade.
Composition: low-angle composition looking slightly upward at Nightingale. The blade effect dominates the foreground while the character occupies the upper portion.
Action: Nightingale delivers a slashing strike that leaves a lingering stasis trail.
Main visual: one sweeping white blade arc with pale slowing rings or delayed afterimages behind it, suggesting reduced speed. Thin translucent wave lines follow the cut path.

Hand correctness is top priority.
Avoid visible detailed hands whenever possible.
If a hand must appear, use a simple side-view closed silhouette with fingers together; hide the other hand behind the body or hair.
No open palms, no spread fingers, no extra fingers, no twisted wrists.

No random fragments, no broken glass debris, no sparks, no noisy scratch lines.
Use only subtle light arcs or a faint aura.
Keep the picture clean and readable at small card size.
"@

$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromMinutes(5)
$client.DefaultRequestHeaders.Authorization =
    [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $key)

$form = [System.Net.Http.MultipartFormDataContent]::new()
$form.Add([System.Net.Http.StringContent]::new("gpt-image-2"), "model")
$form.Add([System.Net.Http.StringContent]::new($prompt), "prompt")
$form.Add([System.Net.Http.StringContent]::new("1024x768"), "size")

$fileStream = [System.IO.File]::OpenRead($refPath)
try {
    $fileContent = [System.Net.Http.StreamContent]::new($fileStream)
    $fileContent.Headers.ContentType =
        [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("image/png")
    $form.Add($fileContent, "image", [System.IO.Path]::GetFileName($refPath))

    $response = $client.PostAsync(
        "https://www.traxnode.com/v1/images/edits",
        $form
    ).GetAwaiter().GetResult()

    $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    if (-not $response.IsSuccessStatusCode) {
        throw "HTTP $([int]$response.StatusCode): $text"
    }

    $json = $text | ConvertFrom-Json
    $item = $json.data[0]
    if ($item.b64_json) {
        [IO.File]::WriteAllBytes($outPath, [Convert]::FromBase64String($item.b64_json))
        Write-Host "Saved to $outPath"
    } elseif ($item.url) {
        Invoke-WebRequest -Uri $item.url -OutFile $outPath -TimeoutSec 180 | Out-Null
        Write-Host "Saved to $outPath from URL"
    } else {
        throw "Response did not include b64_json or url"
    }
} finally {
    $form.Dispose()
    $client.Dispose()
    $fileStream.Dispose()
}
