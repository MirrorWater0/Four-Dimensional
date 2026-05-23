$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http

$key = (Get-Content -Raw -LiteralPath 'C:\tmp\key.txt').Trim()
$refPath = 'C:\godot_project\Four-Dimensional\asset\PlayerCharater\Echo\EchoPortrait.png'
$outPath = 'C:\godot_project\Four-Dimensional\asset\CardPicture\Echo\Shelter.png'

$prompt = @"
Use the provided Echo portrait only for identity and palette.
Raw game skill artwork only, not a finished card template.
Follow Echo element constraints exactly.
Pale anime sketch, thin rough line art, simple watercolor shading, flat bright white background, moderate visible color.
Prefer close-up, cropped bust, shoulder crop, or half-body composition.
All extra shapes must be intangible VFX only: translucent sound waves, resonance rings, mist, aura, abstract lines.
No physical shield, armor, staff, card frame, border, UI ornaments, text, watermark, or logo.

Skill: Resonance Shelter.
Composition: close bust or shoulder crop, with a soft resonance veil passing in front of Echo like a curtain.
Action: a soft resonance canopy folds around Echo, suggesting broad protection and card refresh without showing other allies.
Main visual: a thin translucent veil or curtain of pale purple sound waves, with a few gentle circular ripples near the foreground.
Mood: soft cover, safe space, quiet recovery.
Avoid full-body framing, other characters, solid dome, physical tent, shield, roof, or armor. The shelter must be mist-like VFX.
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
        Write-Host 'Shelter saved'
    } elseif ($item.url) {
        Invoke-WebRequest -Uri $item.url -OutFile $outPath -TimeoutSec 180 | Out-Null
        Write-Host 'Shelter saved'
    } else {
        throw 'No b64_json or url'
    }
} finally {
    $form.Dispose()
    $client.Dispose()
    $fileStream.Dispose()
}
