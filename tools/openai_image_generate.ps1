param(
    [string]$EnvFile = "C:\tmp\openai.env",
    [string]$KeyFile = "C:\tmp\key.txt",
    [string]$BaseUrl = "http://new.xem8k5.top:3000/v1",
    [string]$Model = "gpt-image-2",

    [Parameter(Mandatory = $true)]
    [string]$Prompt,

    [ValidateSet("1024x1024", "1024x1536", "1536x1024", "1024x768", "980x700", "auto")]
    [string]$Size = "1024x1024",
    [ValidateSet("low", "medium", "high", "auto")]
    [string]$Quality = "high",
    [ValidateSet("png", "webp", "jpeg")]
    [string]$OutputFormat = "png",
    [ValidateSet("transparent", "opaque", "auto")]
    [string]$Background = "auto",
    [ValidateRange(1, 4)]
    [int]$ImageCount = 1,
    [string[]]$ReferenceImage,
    [string]$OutputPath,
    [string]$OutputDir = "asset/generated/openai",
    [string]$OutputName,
    [switch]$DryRun,
    [switch]$OpenAfterSave
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Read-DotEnv {
    param([string]$Path)

    $values = @{}
    if (-not (Test-Path -LiteralPath $Path)) {
        return $values
    }

    Get-Content -LiteralPath $Path | ForEach-Object {
        $line = $_.Trim()
        if (-not $line -or $line.StartsWith("#")) {
            return
        }

        $idx = $line.IndexOf("=")
        if ($idx -le 0) {
            return
        }

        $name = $line.Substring(0, $idx).Trim()
        $value = $line.Substring($idx + 1).Trim()
        if (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'"))) {
            $value = $value.Substring(1, $value.Length - 2)
        }

        $values[$name] = $value
    }

    return $values
}

function Get-OpenAIApiKey {
    param(
        [string]$EnvPath,
        [string]$KeyPath
    )

    if ($env:OPENAI_API_KEY) {
        return $env:OPENAI_API_KEY
    }

    $envValues = Read-DotEnv -Path $EnvPath
    if ($envValues.ContainsKey("OPENAI_API_KEY") -and $envValues["OPENAI_API_KEY"]) {
        return $envValues["OPENAI_API_KEY"]
    }

    if (Test-Path -LiteralPath $KeyPath) {
        $key = (Get-Content -Raw -LiteralPath $KeyPath).Trim()
        if ($key) {
            return $key
        }
    }

    throw "Missing API key. Put it in $KeyPath or set OPENAI_API_KEY."
}

function ConvertTo-SafeFileName {
    param([string]$Value)

    if (-not $Value) {
        return "openai_image"
    }

    $safe = $Value -replace '[\\/:*?"<>|]', "_" -replace "\s+", "_"
    $safe = $safe.Trim("_")
    if ($safe.Length -gt 48) {
        $safe = $safe.Substring(0, 48).Trim("_")
    }

    if (-not $safe) {
        return "openai_image"
    }

    return $safe
}

function Save-ImageItem {
    param(
        [object]$Item,
        [string]$Directory,
        [string]$NamePrefix,
        [string]$Format,
        [int]$Index,
        [string]$ExplicitPath
    )

    $extension = if ($Format -eq "jpeg") { "jpg" } else { $Format }
    $path = $ExplicitPath
    if (-not $path) {
        New-Item -ItemType Directory -Force -Path $Directory | Out-Null
        $fileName = "{0}_{1:yyyyMMdd_HHmmss}_{2:00}.{3}" -f $NamePrefix, (Get-Date), $Index, $extension
        $path = Join-Path $Directory $fileName
    }

    $parent = Split-Path -Parent $path
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    if ($Item.b64_json) {
        [IO.File]::WriteAllBytes($path, [Convert]::FromBase64String($Item.b64_json))
    }
    elseif ($Item.url) {
        Invoke-WebRequest -Uri $Item.url -OutFile $path -TimeoutSec 180 | Out-Null
    }
    else {
        throw "Image response did not contain b64_json or url."
    }

    return (Resolve-Path -LiteralPath $path).Path
}

function Get-ApiUri {
    param(
        [string]$Base,
        [string]$Endpoint
    )

    $baseValue = $Base.TrimEnd("/")
    if ($baseValue -notmatch "/v1$") {
        $baseValue = "$baseValue/v1"
    }

    return "$baseValue/$($Endpoint.TrimStart('/'))"
}

function Invoke-OpenAIImageGeneration {
    param(
        [string]$ApiKey,
        [hashtable]$Body
    )

    $headers = @{
        Authorization = "Bearer $ApiKey"
    }
    $json = $Body | ConvertTo-Json -Depth 20 -Compress
    $uri = Get-ApiUri -Base $BaseUrl -Endpoint "images/generations"

    try {
        return Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -ContentType "application/json" -Body $json
    }
    catch {
        $response = $_.Exception.Response
        if ($response) {
            $reader = [IO.StreamReader]::new($response.GetResponseStream())
            try {
                $body = $reader.ReadToEnd()
            }
            finally {
                $reader.Dispose()
                $response.Dispose()
            }

            throw "OpenAI image request failed: $body"
        }

        throw
    }
}

function Get-ImageContentType {
    param([string]$Path)

    $extension = [IO.Path]::GetExtension($Path).ToLowerInvariant()
    switch ($extension) {
        ".jpg" { return "image/jpeg" }
        ".jpeg" { return "image/jpeg" }
        ".webp" { return "image/webp" }
        default { return "image/png" }
    }
}

function Invoke-OpenAIImageEdit {
    param(
        [string]$ApiKey,
        [string[]]$Images
    )

    Add-Type -AssemblyName System.Net.Http

    $client = [System.Net.Http.HttpClient]::new()
    $client.Timeout = [TimeSpan]::FromMinutes(5)
    $client.DefaultRequestHeaders.Authorization =
        [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $ApiKey)

    $form = [System.Net.Http.MultipartFormDataContent]::new()
    $streams = New-Object System.Collections.Generic.List[System.IO.FileStream]
    try {
        $form.Add([System.Net.Http.StringContent]::new($Model), "model")
        $form.Add([System.Net.Http.StringContent]::new($Prompt), "prompt")
        $form.Add([System.Net.Http.StringContent]::new($Size), "size")
        $form.Add([System.Net.Http.StringContent]::new($Quality), "quality")
        $form.Add([System.Net.Http.StringContent]::new($OutputFormat), "output_format")
        if ($Background -ne "auto") {
            $form.Add([System.Net.Http.StringContent]::new($Background), "background")
        }

        $fieldName = if ($Images.Count -gt 1) { "image[]" } else { "image" }
        foreach ($image in $Images) {
            if (-not (Test-Path -LiteralPath $image)) {
                throw "Reference image not found: $image"
            }

            $resolvedImage = (Resolve-Path -LiteralPath $image).Path
            $stream = [System.IO.File]::OpenRead($resolvedImage)
            $streams.Add($stream)
            $fileContent = [System.Net.Http.StreamContent]::new($stream)
            $fileContent.Headers.ContentType =
                [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse((Get-ImageContentType -Path $resolvedImage))
            $form.Add($fileContent, $fieldName, [System.IO.Path]::GetFileName($resolvedImage))
        }

        $uri = Get-ApiUri -Base $BaseUrl -Endpoint "images/edits"
        $response = $client.PostAsync($uri, $form).GetAwaiter().GetResult()
        $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if (-not $response.IsSuccessStatusCode) {
            throw "OpenAI image edit failed, HTTP $([int]$response.StatusCode): $text"
        }

        return $text | ConvertFrom-Json
    }
    finally {
        foreach ($stream in $streams) {
            $stream.Dispose()
        }
        $form.Dispose()
        $client.Dispose()
    }
}

$body = @{
    model = $Model
    prompt = $Prompt
    size = $Size
    quality = $Quality
    output_format = $OutputFormat
}

if ($Background -ne "auto") {
    $body["background"] = $Background
}

if ($ImageCount -gt 1) {
    $body["n"] = $ImageCount
}

if ($DryRun) {
    [pscustomobject]@{
        endpoint = if ($ReferenceImage) { Get-ApiUri -Base $BaseUrl -Endpoint "images/edits" } else { Get-ApiUri -Base $BaseUrl -Endpoint "images/generations" }
        body = $body
        referenceImage = $ReferenceImage
        outputPath = $OutputPath
    } | ConvertTo-Json -Depth 20
    exit 0
}

$apiKey = Get-OpenAIApiKey -EnvPath $EnvFile -KeyPath $KeyFile
$response = if ($ReferenceImage) {
    Invoke-OpenAIImageEdit -ApiKey $apiKey -Images $ReferenceImage
}
else {
    Invoke-OpenAIImageGeneration -ApiKey $apiKey -Body $body
}

if (-not $response.data -or $response.data.Count -eq 0) {
    $response | ConvertTo-Json -Depth 20
    throw "OpenAI response did not contain image data."
}

$namePrefix = ConvertTo-SafeFileName -Value $(if ($OutputName) { $OutputName } else { $Prompt })
$savedFiles = New-Object System.Collections.Generic.List[string]
$index = 1
foreach ($image in $response.data) {
    $explicitPath = if ($OutputPath -and $response.data.Count -eq 1) { $OutputPath } else { $null }
    $savedFiles.Add((Save-ImageItem -Item $image -Directory $OutputDir -NamePrefix $namePrefix -Format $OutputFormat -Index $index -ExplicitPath $explicitPath))
    $index += 1
}

[pscustomobject]@{
    model = $Model
    size = $Size
    quality = $Quality
    outputFormat = $OutputFormat
    referenceImage = $ReferenceImage
    outputDir = if (Test-Path -LiteralPath $OutputDir) { (Resolve-Path -LiteralPath $OutputDir).Path } else { $OutputDir }
    savedFiles = $savedFiles
    usage = if ($response.PSObject.Properties.Name -contains "usage") { $response.usage } else { $null }
} | ConvertTo-Json -Depth 20

if ($OpenAfterSave) {
    foreach ($path in $savedFiles) {
        Invoke-Item -LiteralPath $path
    }
}
