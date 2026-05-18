param(
    [string]$ListenPrefix = "http://127.0.0.1:17891/",
    [string]$UpstreamBaseUrl = "https://api.traxnode.com/v1",
    [string]$KeyFile = "C:\tmp\key.txt",
    [string]$Model = "gpt-image-2",
    [ValidateRange(30, 3600)]
    [int]$TimeoutSec = 300
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Log {
    param([string]$Message)

    $timestamp = Get-Date -Format "HH:mm:ss"
    Write-Host "[cherry-image2 $timestamp] $Message"
}

function Get-ApiKey {
    if ($env:OPENAI_API_KEY) {
        return $env:OPENAI_API_KEY
    }

    if (Test-Path -LiteralPath $KeyFile) {
        $key = (Get-Content -Raw -LiteralPath $KeyFile).Trim()
        if ($key) {
            return $key
        }
    }

    throw "Missing API key. Put it in $KeyFile or set OPENAI_API_KEY."
}

function Send-Json {
    param(
        [System.Net.HttpListenerResponse]$Response,
        [int]$StatusCode,
        [object]$Body
    )

    $json = $Body | ConvertTo-Json -Depth 50 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $Response.StatusCode = $StatusCode
    $Response.ContentType = "application/json; charset=utf-8"
    $Response.ContentLength64 = $bytes.Length
    $Response.Headers["Access-Control-Allow-Origin"] = "*"
    $Response.OutputStream.Write($bytes, 0, $bytes.Length)
}

function Send-Text {
    param(
        [System.Net.HttpListenerResponse]$Response,
        [int]$StatusCode,
        [string]$Text
    )

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
    $Response.StatusCode = $StatusCode
    $Response.ContentType = "text/plain; charset=utf-8"
    $Response.ContentLength64 = $bytes.Length
    $Response.Headers["Access-Control-Allow-Origin"] = "*"
    $Response.OutputStream.Write($bytes, 0, $bytes.Length)
}

function Read-RequestBytes {
    param([System.Net.HttpListenerRequest]$Request)

    $memory = [System.IO.MemoryStream]::new()
    try {
        $Request.InputStream.CopyTo($memory)
        return $memory.ToArray()
    }
    finally {
        $memory.Dispose()
    }
}

function Convert-ImageUrlsToBase64 {
    param([object]$Payload)

    if (-not ($Payload.PSObject.Properties.Name -contains "data") -or -not $Payload.data) {
        return $Payload
    }

    foreach ($item in $Payload.data) {
        $hasBase64 = ($item.PSObject.Properties.Name -contains "b64_json") -and $item.b64_json
        $hasUrl = ($item.PSObject.Properties.Name -contains "url") -and $item.url
        if ($hasBase64 -or -not $hasUrl) {
            continue
        }

        Write-Log "Converting returned URL image to b64_json."
        $downloadClient = [System.Net.Http.HttpClient]::new()
        $downloadClient.Timeout = [TimeSpan]::FromSeconds($TimeoutSec)
        try {
            $bytes = $downloadClient.GetByteArrayAsync([string]$item.url).GetAwaiter().GetResult()
        }
        finally {
            $downloadClient.Dispose()
        }

        $item | Add-Member -NotePropertyName "b64_json" -NotePropertyValue ([Convert]::ToBase64String($bytes)) -Force
        $item.PSObject.Properties.Remove("url")
    }

    return $Payload
}

function Invoke-Upstream {
    param(
        [string]$Path,
        [string]$Method,
        [byte[]]$BodyBytes,
        [string]$ContentType,
        [string]$ApiKey
    )

    $uri = "$($UpstreamBaseUrl.TrimEnd('/'))/$($Path.TrimStart('/'))"
    $client = [System.Net.Http.HttpClient]::new()
    $client.Timeout = [TimeSpan]::FromSeconds($TimeoutSec)
    $client.DefaultRequestHeaders.Authorization =
        [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $ApiKey)

    try {
        $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method), $uri)
        if ($BodyBytes -and $BodyBytes.Length -gt 0) {
            $content = [System.Net.Http.ByteArrayContent]::new($BodyBytes)
            if ($ContentType) {
                $content.Headers.TryAddWithoutValidation("Content-Type", $ContentType) | Out-Null
            }
            $request.Content = $content
        }

        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        return [pscustomobject]@{
            StatusCode = [int]$response.StatusCode
            Body = $text
            ContentType = $response.Content.Headers.ContentType
        }
    }
    finally {
        $client.Dispose()
    }
}

$apiKey = Get-ApiKey
$listener = [System.Net.HttpListener]::new()
$listener.Prefixes.Add($ListenPrefix)
$listener.Start()

Write-Log "Listening on $ListenPrefix"
Write-Log "Use Cherry base URL: $($ListenPrefix.TrimEnd('/'))/v1"
Write-Log "Upstream: $UpstreamBaseUrl"

try {
    while ($listener.IsListening) {
        $context = $listener.GetContext()
        $request = $context.Request
        $response = $context.Response

        try {
            $response.Headers["Access-Control-Allow-Origin"] = "*"
            $response.Headers["Access-Control-Allow-Headers"] = "authorization, content-type"
            $response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS"

            if ($request.HttpMethod -eq "OPTIONS") {
                $response.StatusCode = 204
                continue
            }

            $path = $request.Url.AbsolutePath.TrimStart("/")
            if ($path -eq "v1/models") {
                Send-Json -Response $response -StatusCode 200 -Body @{
                    object = "list"
                    data = @(
                        @{
                            id = $Model
                            object = "model"
                            created = 0
                            owned_by = "traxnode"
                        }
                    )
                }
                continue
            }

            if ($path -in @("v1/images/generations", "v1/images/edits")) {
                Write-Log "$($request.HttpMethod) /$path"
                $bodyBytes = Read-RequestBytes -Request $request
                $upstream = Invoke-Upstream `
                    -Path $path.Substring(3) `
                    -Method $request.HttpMethod `
                    -BodyBytes $bodyBytes `
                    -ContentType $request.ContentType `
                    -ApiKey $apiKey

                $payload = $upstream.Body | ConvertFrom-Json
                $payload = Convert-ImageUrlsToBase64 -Payload $payload
                Send-Json -Response $response -StatusCode $upstream.StatusCode -Body $payload
                continue
            }

            Send-Json -Response $response -StatusCode 404 -Body @{
                error = @{
                    message = "Unsupported path: /$path"
                    type = "not_found"
                }
            }
        }
        catch {
            Send-Json -Response $response -StatusCode 500 -Body @{
                error = @{
                    message = $_.Exception.Message
                    type = "proxy_error"
                }
            }
        }
        finally {
            $response.Close()
        }
    }
}
finally {
    $listener.Stop()
    $listener.Close()
}
