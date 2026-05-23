$ErrorActionPreference = "Stop"

$key = (Get-Content -Raw -LiteralPath "C:\tmp\key.txt").Trim()
$baseUrl = "https://api.traxnode.com/v1"
$model = "gpt-image-2"
$size = "1024x768"

$portrait = "C:\godot_project\Four-Dimensional\asset\PlayerCharater\Echo\EchoPortrait.png"
$outPath = "C:\godot_project\Four-Dimensional\asset\CardPicture\Echo\BasicDefense.png"

$prompt = @"
Use the provided image as the only visual reference. Keep the same character identity, outfit, palette. Raw game skill artwork only.
Echo element constraints: long white hair, purple eyes, white outfit, dark blue angular shadow shapes, purple blade-like energy. No shield, staff, armor, wings, halo, crown, jewelry, card frame, border, UI ornaments.
Pale anime sketch, thin line art, simple watercolor, flat bright white background, moderate visible color. No text, no watermark, no logo.
Skill: Basic Defense. Close-up bust. Guarded stance. Translucent purple-white waveform bands or soft resonance rings as intangible sound barrier. No physical shield.
"@

Write-Host "[GENERATE] Echo / BasicDefense (short prompt) ..." -NoNewline

$jsonFile = [System.IO.Path]::GetTempFileName()
$curlArgs = @(
    "-s", "-X", "POST",
    "--max-time", "300",
    "-H", "Authorization: Bearer $key",
    "-F", "model=$model",
    "-F", "prompt=$prompt",
    "-F", "size=$size",
    "-F", "image=@$portrait",
    "-o", $jsonFile,
    "-w", "%{http_code}",
    "$baseUrl/images/edits"
)

$httpCode = & curl.exe @curlArgs

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
