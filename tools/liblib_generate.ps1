param(
    [ValidateSet("submit", "status", "wait")]
    [string]$Action = "submit",

    [string]$EnvFile = "C:\tmp\liblib.env",
    [string]$BaseUrl = "https://openapi.liblibai.cloud",
    [string]$Endpoint = "/api/generate/webui/text2img",
    [string]$StatusEndpoint = "/api/generate/webui/status",
    [string]$Img2ImgEndpoint = "/api/generate/webui/img2img",

    [string]$Prompt = "Kasiya, short white hair, amber golden eyes, elegant swordswoman, white battle dress with subtle gold accents, dynamic sword slash attack, attack skill card artwork, character illustration only, artwork only, cel shading, rough hand drawn anime line art, clean sharp outlines, flat bright colors, energetic diagonal composition, simple dark background, no card frame, no border, no UI, no text",
    [string]$NegativePrompt = "text, letters, caption, watermark, logo, signature, card frame, border, trading card layout, UI, icon, symbol, realistic photo, 3d render, thick painting, blurry, low quality, extra fingers, bad hands, deformed sword, duplicate character, animal ears, maid outfit",
    [string]$BodyFile,
    [string]$GenerateUuid,
    [string]$ReferenceImage = "asset/CardPicture/Smite.png",
    [string]$SourceImageUrl,

    [string]$TemplateUuid = "e10adc3949ba59abbe56e057f20f883e",
    [string]$Img2ImgTemplateUuid = "9c7d531dc75f476aa833b3d452b8f7ad",
    [string]$CheckPointId = "fa5e552314b59c4e4e0117e6f0b2d3b5",
    [ValidateSet("square", "portrait", "landscape")]
    [string]$AspectRatio = "portrait",
    [int]$Width = 768,
    [int]$Height = 1024,
    [ValidateRange(1, 4)]
    [int]$ImageCount = 1,
    [int]$Steps = 30,
    [int]$Seed = -1,
    [double]$CfgScale = 7,
    [string]$LoraModelId = "e2e06158a5db40deab22fad0a6cf573f",
    [double]$LoraWeight = 0.6,
    [double]$DenoisingStrength = 0.58,
    [int]$Sampler = 15,
    [int]$ClipSkip = 2,
    [int]$ResizeMode = 2,

    [string]$OutputDir = "asset/generated/liblib",
    [int]$PollSeconds = 5,
    [int]$TimeoutSeconds = 600,
    [switch]$Wait
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Read-DotEnv {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Env file not found: $Path"
    }

    $values = @{}
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

function ConvertTo-Base64Url {
    param([byte[]]$Bytes)

    return [Convert]::ToBase64String($Bytes).Replace("+", "-").Replace("/", "_").TrimEnd("=")
}

function New-SignatureNonce {
    $bytes = New-Object byte[] 8
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($bytes)
        return ([BitConverter]::ToString($bytes) -replace "-", "").ToLowerInvariant()
    }
    finally {
        $rng.Dispose()
    }
}

function New-LiblibSignature {
    param(
        [string]$Path,
        [string]$SecretKey,
        [string]$Timestamp,
        [string]$Nonce
    )

    $message = "$Path&$Timestamp&$Nonce"
    $keyBytes = [Text.Encoding]::UTF8.GetBytes($SecretKey)
    $messageBytes = [Text.Encoding]::UTF8.GetBytes($message)
    $hmac = [System.Security.Cryptography.HMACSHA1]::new($keyBytes)
    try {
        return ConvertTo-Base64Url -Bytes $hmac.ComputeHash($messageBytes)
    }
    finally {
        $hmac.Dispose()
    }
}

function Get-LiblibCredential {
    param([string]$Path)

    $envValues = Read-DotEnv -Path $Path
    $accessKey = $envValues["LIBLIB_ACCESS_KEY"]
    $secretKey = $envValues["LIBLIB_SECRET_KEY"]

    if (-not $accessKey -or -not $secretKey) {
        throw "Missing LIBLIB_ACCESS_KEY or LIBLIB_SECRET_KEY in $Path"
    }

    return @{
        AccessKey = $accessKey
        SecretKey = $secretKey
    }
}

function New-LiblibUri {
    param(
        [string]$Path,
        [hashtable]$Credential
    )

    $timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds().ToString()
    $nonce = New-SignatureNonce
    $signature = New-LiblibSignature -Path $Path -SecretKey $Credential["SecretKey"] -Timestamp $timestamp -Nonce $nonce

    $query = @{
        AccessKey = $Credential["AccessKey"]
        Signature = $signature
        Timestamp = $timestamp
        SignatureNonce = $nonce
    }.GetEnumerator() | ForEach-Object {
        "$([Uri]::EscapeDataString($_.Key))=$([Uri]::EscapeDataString([string]$_.Value))"
    }

    return "$BaseUrl${Path}?$($query -join '&')"
}

function Invoke-LiblibPost {
    param(
        [string]$Path,
        [object]$Body,
        [hashtable]$Credential
    )

    $uri = New-LiblibUri -Path $Path -Credential $Credential
    $json = $Body | ConvertTo-Json -Depth 50 -Compress
    return Invoke-RestMethod -Uri $uri -Method Post -ContentType "application/json; charset=utf-8" -Body $json
}

function Get-ContentType {
    param([string]$Path)

    $extension = [IO.Path]::GetExtension($Path).ToLowerInvariant()
    switch ($extension) {
        ".jpg" { return "image/jpeg" }
        ".jpeg" { return "image/jpeg" }
        ".webp" { return "image/webp" }
        default { return "image/png" }
    }
}

function Upload-LiblibFile {
    param(
        [string]$Path,
        [hashtable]$Credential
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Reference image not found: $Path"
    }

    $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
    $fileName = [IO.Path]::GetFileName($resolvedPath)
    $name = [IO.Path]::GetFileNameWithoutExtension($resolvedPath)
    $extension = [IO.Path]::GetExtension($resolvedPath).TrimStart(".")
    $signResponse = Invoke-LiblibPost -Path "/api/generate/upload/signature" -Body @{ name = $name; extension = $extension } -Credential $Credential

    if ($signResponse.PSObject.Properties.Name -contains "code" -and $signResponse.code -ne 0) {
        throw "Liblib upload signature failed, code $($signResponse.code): $($signResponse.msg)"
    }

    $signData = $signResponse.data
    if (-not $signData) {
        throw "Liblib upload signature response did not contain data."
    }

    $boundary = "----LiblibFormBoundary$([Guid]::NewGuid().ToString('N'))"
    $request = [Net.HttpWebRequest]::Create([string]$signData.postUrl)
    $request.Method = "POST"
    $request.ContentType = "multipart/form-data; boundary=$boundary"
    $request.AllowWriteStreamBuffering = $true

    $requestStream = $request.GetRequestStream()
    $writer = [IO.BinaryWriter]::new($requestStream)
    $encoding = [Text.Encoding]::UTF8

    function Write-MultipartText {
        param(
            [IO.BinaryWriter]$Writer,
            [Text.Encoding]$Encoding,
            [string]$Boundary,
            [string]$Name,
            [string]$Value
        )

        $Writer.Write($Encoding.GetBytes("--$Boundary`r`n"))
        $Writer.Write($Encoding.GetBytes("Content-Disposition: form-data; name=`"$Name`"`r`n`r`n"))
        $Writer.Write($Encoding.GetBytes("$Value`r`n"))
    }

    try {
        $fields = [ordered]@{
            key = [string]$signData.key
            policy = [string]$signData.policy
            "x-oss-signature-version" = [string]$signData.xOssSignatureVersion
            "x-oss-credential" = [string]$signData.xOssCredential
            "x-oss-date" = [string]$signData.xOssDate
            "x-oss-expires" = [string]$signData.xOssExpires
            "x-oss-signature" = [string]$signData.xOssSignature
        }

        foreach ($field in $fields.GetEnumerator()) {
            Write-MultipartText -Writer $writer -Encoding $encoding -Boundary $boundary -Name $field.Key -Value $field.Value
        }

        $contentType = Get-ContentType -Path $resolvedPath
        $writer.Write($encoding.GetBytes("--$boundary`r`n"))
        $writer.Write($encoding.GetBytes("Content-Disposition: form-data; name=`"file`"; filename=`"$fileName`"`r`n"))
        $writer.Write($encoding.GetBytes("Content-Type: $contentType`r`n`r`n"))
        $writer.Write([IO.File]::ReadAllBytes($resolvedPath))
        $writer.Write($encoding.GetBytes("`r`n--$boundary--`r`n"))
        $writer.Flush()

        try {
            $uploadResponse = [Net.HttpWebResponse]$request.GetResponse()
            $uploadResponse.Dispose()
        }
        catch [Net.WebException] {
            $response = $_.Exception.Response
            $body = ""
            if ($response) {
                $reader = [IO.StreamReader]::new($response.GetResponseStream())
                try {
                    $body = $reader.ReadToEnd()
                }
                finally {
                    $reader.Dispose()
                    $response.Dispose()
                }
            }

            throw "Failed to upload reference image: $body"
        }
    }
    finally {
        $writer.Dispose()
        $requestStream.Dispose()
    }

    return ([Uri]::new([Uri][string]$signData.postUrl, [string]$signData.key)).ToString()
}

function New-TextToImageBody {
    if (-not $Prompt) {
        throw "Prompt is required when BodyFile is not provided."
    }

    $generateParams = [ordered]@{
        prompt = $Prompt
        negativePrompt = $NegativePrompt
        sampler = $Sampler
        clipSkip = $ClipSkip
        randnSource = 0
        restoreFaces = 0
        aspectRatio = $AspectRatio
        imageSize = @{
            width = $Width
            height = $Height
        }
        imgCount = $ImageCount
        steps = $Steps
        cfgScale = $CfgScale
    }

    if ($Seed -ge 0) {
        $generateParams["seed"] = $Seed
    }

    if ($CheckPointId) {
        $generateParams["checkPointId"] = $CheckPointId
    }

    if ($LoraModelId) {
        $generateParams["additionalNetwork"] = @(
            [ordered]@{
                modelId = $LoraModelId
                weight = $LoraWeight
            }
        )
    }

    return [ordered]@{
        templateUuid = $TemplateUuid
        generateParams = $generateParams
    }
}

function New-ImageToImageBody {
    param([string]$ImageUrl)

    $body = New-TextToImageBody
    $body.templateUuid = $Img2ImgTemplateUuid
    $body.generateParams["sourceImage"] = $ImageUrl
    $body.generateParams["resizeMode"] = $ResizeMode
    $body.generateParams["resizedWidth"] = $Width
    $body.generateParams["resizedHeight"] = $Height
    $body.generateParams["mode"] = 0
    $body.generateParams["denoisingStrength"] = $DenoisingStrength
    $body.generateParams.Remove("aspectRatio")
    $body.generateParams.Remove("imageSize")

    return $body
}

function Read-BodyJson {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Body file not found: $Path"
    }

    return Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
}

function Find-FirstPropertyValue {
    param(
        [object]$Value,
        [string[]]$Names
    )

    if ($null -eq $Value) {
        return $null
    }

    if ($Value -is [System.Collections.IEnumerable] -and $Value -isnot [string] -and $Value -isnot [pscustomobject]) {
        foreach ($item in $Value) {
            $found = Find-FirstPropertyValue -Value $item -Names $Names
            if ($null -ne $found) {
                return $found
            }
        }
        return $null
    }

    if ($Value -is [pscustomobject]) {
        foreach ($prop in $Value.PSObject.Properties) {
            if ($Names -contains $prop.Name) {
                return $prop.Value
            }

            $found = Find-FirstPropertyValue -Value $prop.Value -Names $Names
            if ($null -ne $found) {
                return $found
            }
        }
    }

    return $null
}

function Find-ImageUrls {
    param(
        [object]$Value,
        [string]$PropertyName = ""
    )

    $urls = New-Object System.Collections.Generic.List[string]

    if ($null -eq $Value) {
        return $urls
    }

    if ($Value -is [string]) {
        $looksLikeImageProperty = $PropertyName -match "(?i)image|url"
        $looksLikeImageUrl = $Value -match "^https?://" -and $Value -match "(?i)\.(png|jpg|jpeg|webp)(\?|$)"
        if (($looksLikeImageProperty -or $looksLikeImageUrl) -and $Value -match "^https?://") {
            $urls.Add($Value)
        }
        return $urls
    }

    if ($Value -is [System.Collections.IEnumerable] -and $Value -isnot [pscustomobject]) {
        foreach ($item in $Value) {
            $child = Find-ImageUrls -Value $item -PropertyName $PropertyName
            foreach ($url in $child) {
                $urls.Add($url)
            }
        }
        return $urls
    }

    if ($Value -is [pscustomobject]) {
        foreach ($prop in $Value.PSObject.Properties) {
            $child = Find-ImageUrls -Value $prop.Value -PropertyName $prop.Name
            foreach ($url in $child) {
                $urls.Add($url)
            }
        }
    }

    return $urls
}

function Save-ImageUrls {
    param(
        [string[]]$Urls,
        [string]$Directory
    )

    if (-not $Urls -or $Urls.Count -eq 0) {
        return @()
    }

    New-Item -ItemType Directory -Force -Path $Directory | Out-Null

    $saved = New-Object System.Collections.Generic.List[string]
    $index = 1
    foreach ($url in ($Urls | Select-Object -Unique)) {
        $extension = ".png"
        try {
            $pathPart = ([Uri]$url).AbsolutePath
            $candidate = [IO.Path]::GetExtension($pathPart)
            if ($candidate -match "^\.(png|jpg|jpeg|webp)$") {
                $extension = $candidate
            }
        }
        catch {
            $extension = ".png"
        }

        $fileName = "liblib_{0:yyyyMMdd_HHmmss}_{1:00}{2}" -f (Get-Date), $index, $extension
        $path = Join-Path $Directory $fileName
        Invoke-WebRequest -Uri $url -OutFile $path
        $saved.Add((Resolve-Path -LiteralPath $path).Path)
        $index += 1
    }

    return $saved
}

function Invoke-Submit {
    param([hashtable]$Credential)

    $submitEndpoint = $Endpoint
    $body = if ($BodyFile) {
        Read-BodyJson -Path $BodyFile
    }
    elseif ($SourceImageUrl -or $ReferenceImage) {
        $submitEndpoint = $Img2ImgEndpoint
        $imageUrl = if ($SourceImageUrl) { $SourceImageUrl } else { Upload-LiblibFile -Path $ReferenceImage -Credential $Credential }
        New-ImageToImageBody -ImageUrl $imageUrl
    }
    else {
        New-TextToImageBody
    }

    $response = Invoke-LiblibPost -Path $submitEndpoint -Body $body -Credential $Credential
    if ($response.PSObject.Properties.Name -contains "code" -and $response.code -ne 0) {
        $message = if ($response.PSObject.Properties.Name -contains "msg") { $response.msg } else { "Unknown Liblib error." }
        throw "Liblib submit failed, code $($response.code): $message"
    }

    $uuid = Find-FirstPropertyValue -Value $response -Names @("generateUuid")

    [pscustomobject]@{
        response = $response
        generateUuid = $uuid
    }
}

function Invoke-Status {
    param(
        [hashtable]$Credential,
        [string]$Uuid
    )

    if (-not $Uuid) {
        throw "GenerateUuid is required."
    }

    return Invoke-LiblibPost -Path $StatusEndpoint -Body @{ generateUuid = $Uuid } -Credential $Credential
}

function Wait-LiblibResult {
    param(
        [hashtable]$Credential,
        [string]$Uuid
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $response = Invoke-Status -Credential $Credential -Uuid $Uuid
        if ($response.PSObject.Properties.Name -contains "code" -and $response.code -ne 0) {
            $message = if ($response.PSObject.Properties.Name -contains "msg") { $response.msg } else { "Unknown Liblib error." }
            throw "Liblib status failed, code $($response.code): $message"
        }

        $urls = @(Find-ImageUrls -Value $response)
        if ($urls.Count -gt 0) {
            $saved = Save-ImageUrls -Urls $urls -Directory $OutputDir
            return [pscustomobject]@{
                response = $response
                imageUrls = $urls
                savedFiles = $saved
            }
        }

        $json = $response | ConvertTo-Json -Depth 50 -Compress
        if ($json -match "(?i)fail|failed|error|exception") {
            throw "Liblib generation failed or returned an error: $json"
        }

        $status = Find-FirstPropertyValue -Value $response -Names @("generateStatus", "status", "state")
        if ($status) {
            Write-Host "Waiting for $Uuid, status: $status"
        }
        else {
            Write-Host "Waiting for $Uuid..."
        }

        Start-Sleep -Seconds $PollSeconds
    }

    throw "Timed out waiting for Liblib result: $Uuid"
}

$credential = Get-LiblibCredential -Path $EnvFile

switch ($Action) {
    "submit" {
        $submitted = Invoke-Submit -Credential $credential
        $submitted.response | ConvertTo-Json -Depth 50

        if ($Wait) {
            if (-not $submitted.generateUuid) {
                throw "Submit response did not contain generateUuid."
            }
            Wait-LiblibResult -Credential $credential -Uuid $submitted.generateUuid | ConvertTo-Json -Depth 50
        }
    }
    "status" {
        Invoke-Status -Credential $credential -Uuid $GenerateUuid | ConvertTo-Json -Depth 50
    }
    "wait" {
        Wait-LiblibResult -Credential $credential -Uuid $GenerateUuid | ConvertTo-Json -Depth 50
    }
}
