$ErrorActionPreference = "Stop"

$key = (Get-Content -Raw -LiteralPath "C:\tmp\key.txt").Trim()
$baseUrl = "https://www.traxnode.com/v1"
$model = "gpt-image-2"
$size = "1024x768"

$outPath = "C:\godot_project\Four-Dimensional\asset\CardPicture\Echo\BasicDefense.png"

$prompt = @"
Clean anime game card art, pale background, no text, no watermark.
Character: a girl with long white hair, purple eyes, wearing a white outfit. Dark blue angular abstract shadow shapes appear around her. She has purple blade-like energy effects.
Skill: Basic Defense. Close-up bust composition. Guarded stance.
Main visual: translucent purple-white waveform bands and soft resonance rings forming an intangible sound barrier in front of her. No physical shield, no armor, no staff, no wings, no halo, no crown, no jewelry.
Style: pale anime sketch, thin rough line art, simple watercolor shading, flat bright white background, moderate visible color. No glossy rendering, no dense gradients.
No card frame, no border, no UI ornaments, no logo.
"@

Write-Host "[GENERATE] Echo / BasicDefense (text-only) ..." -NoNewline

$jsonFile = [System.IO.Path]::GetTempFileName()
$body = @{ model = $model; prompt = $prompt; size = $size } | ConvertTo-Json -Depth 5

$httpCode = & curl.exe -s -X POST `
    -H "Authorization: Bearer $key" `
    -H "Content-Type: application/json" `
    -d $body `
    --max-time 300 `
    -o $jsonFile `
    -w "%{http_code}" `
    "$baseUrl/images/generations"

if ($httpCode -ne "200") {
    Write-Host " FAILED (HTTP $httpCode)" -ForegroundColor Red
    Remove-Item $jsonFile -ErrorAction SilentlyContinue
    exit 1
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
        exit 1
    }
    Write-Host " DONE -> $outPath" -ForegroundColor Green
} catch {
    Write-Host " FAILED ($_))" -ForegroundColor Red
} finally {
    Remove-Item $jsonFile -ErrorAction SilentlyContinue
}
