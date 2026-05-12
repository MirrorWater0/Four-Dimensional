# Image2 调用说明

这份文档用于在新的 Codex 对话里复用当前项目的图片生成接口。把本文档路径发给 Codex，或复制“给 Codex 的最短说明”这一段即可。

## 接口配置

- `base_url`: `http://new.xem8k5.top:3000/v1`
- `model`: `gpt-image-2`
- API key 默认放在：`C:\tmp\key.txt`
- 不要在聊天、README、提交记录或 issue 里明文粘贴 API key。

如果以后改用环境变量，也可以设置：

```powershell
[Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "你的key", "User")
```

设置后需要重启 IDE/Codex。

## 给 Codex 的最短说明

```text
请使用项目里的 docs/IMAGE2_USAGE.md 调用图片接口。
base_url = http://new.xem8k5.top:3000/v1
model = gpt-image-2
API key 在 C:\tmp\key.txt，不要打印 key。
生成图片保存到我指定的项目路径。
```

带参考图时补充：

```text
参考图：asset/PlayerCharater/Kasiya/KasiyaPortrait.png
输出：asset/CardPicture/SomeSkill.png
```

## 纯文本生成图片

接口：

```text
POST http://new.xem8k5.top:3000/v1/images/generations
```

PowerShell 示例：

```powershell
$ErrorActionPreference = "Stop"

$key = (Get-Content -Raw -LiteralPath "C:\tmp\key.txt").Trim()
$outPath = "C:\godot_project\Four-Dimensional\asset\generated\example.png"
$prompt = "clean anime game card art, pale background, no text, no watermark"

$body = @{
    model = "gpt-image-2"
    prompt = $prompt
    size = "1024x1024"
} | ConvertTo-Json -Depth 5

$response = Invoke-RestMethod `
    -Uri "http://new.xem8k5.top:3000/v1/images/generations" `
    -Method Post `
    -Headers @{ Authorization = "Bearer $key" } `
    -ContentType "application/json" `
    -Body $body `
    -TimeoutSec 180

$item = $response.data[0]
if ($item.b64_json) {
    [IO.File]::WriteAllBytes($outPath, [Convert]::FromBase64String($item.b64_json))
} elseif ($item.url) {
    Invoke-WebRequest -Uri $item.url -OutFile $outPath -TimeoutSec 180 | Out-Null
} else {
    throw "Response did not include b64_json or url"
}
```

## 参考图生成/编辑

接口：

```text
POST http://new.xem8k5.top:3000/v1/images/edits
```

单张参考图使用 multipart 字段 `image`。多张参考图使用 `image[]`。

PowerShell 单参考图示例：

```powershell
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

$key = (Get-Content -Raw -LiteralPath "C:\tmp\key.txt").Trim()
$refPath = "C:\godot_project\Four-Dimensional\asset\PlayerCharater\Kasiya\KasiyaPortrait.png"
$outPath = "C:\godot_project\Four-Dimensional\asset\CardPicture\SomeSkill.png"
$prompt = @"
Use the provided image as the only visual reference.
Create clean anime game card art.
Keep the character identity, outfit, and color palette.
No text, no watermark, no logo.
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
        "http://new.xem8k5.top:3000/v1/images/edits",
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
    } elseif ($item.url) {
        Invoke-WebRequest -Uri $item.url -OutFile $outPath -TimeoutSec 180 | Out-Null
    } else {
        throw "Response did not include b64_json or url"
    }
} finally {
    $form.Dispose()
    $client.Dispose()
    $fileStream.Dispose()
}
```

## 项目内常用路径

- 角色参考图：`asset/PlayerCharater/Kasiya/KasiyaPortrait.png`
- Kasiya 贴图：`asset/PlayerCharater/Kasiya/Kasiya.png`
- 卡图目录：`asset/CardPicture/`
- 临时/生成目录：`asset/generated/`
- 技能卡图加载规则：`SkillCard` 会按技能类名查找 `asset/CardPicture/{SkillId}.png` 等文件。

例子：`AbsouluteDefense` 技能会自动尝试加载：

```text
asset/CardPicture/AbsouluteDefense.png
asset/CardPicture/KasiyaAbsouluteDefense.png
```

注意这里项目代码里的类名拼写是 `AbsouluteDefense`，不是 `AbsoluteDefense`。

## 提示词建议

角色卡图建议明确写：

```text
Use the provided image as the only visual reference.
Keep the same character identity, outfit, palette, and simple pale anime rendering.
Clean game card illustration, bright white background, lots of negative space.
No text, no watermark, no logo.
```

如果要避免手崩，明确写：

```text
Hand correctness is top priority.
Avoid visible detailed hands whenever possible.
Use a simple closed gloved grip.
No open palms, no spread fingers, no extra fingers, no twisted wrists.
Hide the off-hand behind sleeve, hair, body, or composition.
```

如果要避免画面太乱，明确写：

```text
No random fragments, no broken glass debris, no sparks, no noisy scratch lines.
Use only subtle light arcs or a faint aura.
Keep the picture clean and readable at small card size.
```

## 输出规格

现有 `asset/CardPicture` 里的多数卡图是：

```text
980x700
```

生成时可以先用：

```text
1024x768
```

再裁剪/缩放到 `980x700`。

## 安全注意

- 不要打印 key。
- 不要把 `C:\tmp\key.txt` 加到项目或提交。
- 生成图先保存为新文件，例如 `SomeSkill_v2.png`，确认后再覆盖正式文件。
- 覆盖 Godot 正在使用的图片时可能失败，先关闭占用或保存成新文件。
