$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http

$key = (Get-Content -Raw -LiteralPath 'C:\tmp\key.txt').Trim()
$refPath = 'C:\godot_project\Four-Dimensional\asset\PlayerCharater\Echo\EchoPortrait.png'
$outPath = 'C:\godot_project\Four-Dimensional\asset\CardPicture\Echo\Purity.png'

$prompt = @"
Use the provided Echo portrait only for identity and palette.
Raw game skill artwork only, not a finished card template.
Follow Echo element constraints exactly.
Pale anime sketch, thin rough line art, simple watercolor shading, flat bright white background, moderate visible color.
Prefer close-up, cropped bust, shoulder crop, or half-body composition.
All extra shapes must be intangible VFX only: translucent light, resonance rings, mist, aura, abstract lines, energy planes.
No physical shield, armor, staff, card frame, border, UI ornaments, text, watermark, or logo.

Skill: Purity.
Composition: close-up of Echo's hand or chest with a bright pure energy sphere forming at center.
Action: Echo channels pure energy, a luminous white-violet light gathering and radiating outward in soft pulses.
Main visual: a clean glowing energy orb with pale white and light purple resonance rings expanding outward, suggesting clarity and energy gain.
Mood: pure, focused, rejuvenating, minimal.
Avoid attack beams, slashes, chaotic energy, or explosions. Keep the energy orb as the central subject and keep the picture clean.
"@

$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromMinutes(5)
$client.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $key)

$form = [System.Net.Http.MultipartFormDataContent]::new()
$form.Add([System.Net.Http.StringContent]::new('gpt-image-2'), 'model')
$form.Add([System.Net.Http.StringContent]::new($prompt), 'prompt')
$form.Add([System.Net.Http.StringContent]::new('1024x768'), 'size')

$fileStream = [System.IO.File]::OpenRead($refPath)
try {
    $fileContent = [System.Net.Http.StreamContent]::new($fileStream)
    $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse('image/png')
    $form.Add($fileContent, 'image', [System.IO.Path]::GetFileName($refPath))

    $response = $client.PostAsync('https://api.traxnode.com/v1/images/edits', $form).GetAwaiter().GetResult()
    $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    if (-not $response.IsSuccessStatusCode) {
        throw "HTTP $($response.StatusCode): $text"
    }

    $json = $text | ConvertFrom-Json
    $item = $json.data[0]
    if ($item.b64_json) {
        [IO.File]::WriteAllBytes($outPath, [Convert]::FromBase64String($item.b64_json))
        Write-Host 'Purity saved'
    } elseif ($item.url) {
        Invoke-WebRequest -Uri $item.url -OutFile $outPath -TimeoutSec 180 | Out-Null
        Write-Host 'Purity saved'
    } else {
        throw 'No b64_json or url'
    }
} finally {
    $form.Dispose()
    $client.Dispose()
    $fileStream.Dispose()
}
