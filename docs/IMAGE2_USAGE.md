# Image2 调用说明

这份文档用于在新的 Codex 对话里复用当前项目的图片生成接口。把本文档路径发给 Codex，或复制“给 Codex 的最短说明”这一段即可。

## 接口配置

- `base_url`: `https://www.traxnode.com/v1`
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
base_url = https://www.traxnode.com/v1
model = gpt-image-2
API key 在 C:\tmp\key.txt，不要打印 key。
生成图片保存到我指定的项目路径。
```

带参考图时补充：

```text
参考图：asset/PlayerCharater/Kasiya/KasiyaPortrait.png
输出：asset/CardPicture/Kasiya/SomeSkill.png
```

## 纯文本生成图片

接口：

```text
POST https://www.traxnode.com/v1/images/generations
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
    -Uri "https://www.traxnode.com/v1/images/generations" `
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
POST https://www.traxnode.com/v1/images/edits
```

单张参考图使用 multipart 字段 `image`。多张参考图使用 `image[]`。

PowerShell 单参考图示例：

```powershell
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

$key = (Get-Content -Raw -LiteralPath "C:\tmp\key.txt").Trim()
$refPath = "C:\godot_project\Four-Dimensional\asset\PlayerCharater\Kasiya\KasiyaPortrait.png"
$outPath = "C:\godot_project\Four-Dimensional\asset\CardPicture\Kasiya\SomeSkill.png"
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
- 卡图目录：`asset/CardPicture/{CharacterName}/`
- 临时/生成目录：`asset/generated/`
- 技能卡图加载规则：`SkillCard` 会优先按角色子目录查找 `asset/CardPicture/{CharacterName}/{SkillId}.png`，找不到时再回退旧的平铺路径 `asset/CardPicture/{SkillId}.png`。

例子：`AbsouluteDefense` 技能会自动尝试加载：

```text
asset/CardPicture/Kasiya/AbsouluteDefense.png
asset/CardPicture/KasiyaAbsouluteDefense.png
```

注意这里项目代码里的类名拼写是 `AbsouluteDefense`，不是 `AbsoluteDefense`。

## 提示词建议

角色卡图建议明确写：

```text
Use the provided image as the only visual reference.
Keep the same character identity, outfit, palette, and simple pale anime rendering.
Raw game skill artwork only, not a finished card template.
Bright white background, lots of negative space.
No text, no watermark, no logo.
```

当前推荐风格提示词：

```text
Pale anime sketch with clean thin line art.
Simple watercolor shading with slightly cleaner color blocks.
Keep a light sketch feeling, but avoid messy stray lines, broken scratch lines, noisy crosshatching, and over-fragmented hair lines.
Use flat bright white background, controlled negative space, and moderate visible color.
Do not default to simple half-body composition.
Do not over-polish: no glossy rendering, no commercial poster finish, no dense gradients, no hyper-detailed effects.
```

如果画面乱线太多，追加：

```text
Use cleaner continuous contour lines.
Reduce stray construction lines and broken line fragments.
Keep color fills simple and readable with only light watercolor texture.
```

技能卡图生成时，必须额外写明构图平衡规则，不要把“角色半身像”当成默认解：

```text
Composition balance rule:
Across a batch of skill cards, balance the compositions across these buckets:
upper-body composition, top-down composition, low-angle composition, hand close-up, foot close-up, effect-dominant composition, object-dominant composition.
Do not default every skill card to upper-body framing.
For a batch of 6 to 8 skill cards, cover at least 4 or 5 different composition buckets, and use upper-body composition at most 2 times unless explicitly requested.
If one card already uses a strong upper-body portrait, prefer a different composition bucket for the next cards.
If the character constraints do not allow a physical object, replace object-dominant composition with a symbolic-focus or VFX-focus composition explicitly allowed by the prompt.
```

构图桶说明：

- `上半身构图`：角色胸像、肩部、半身，适合表达表情、姿态、静态护持。
- `俯视构图`：从上向下看角色或特效场，适合范围技、场域技、地面扩散类技能。
- `仰视构图`：从下向上看角色或冲击波，适合压迫感、爆发、抬升、结界展开。
- `手部特写`：重点放在手势、指尖、施法动作、接触点，适合精密、防反、引导、抽取。
- `脚部特写`：重点放在踏步、位移、起跳、后撤、落点，适合移动、防御步法、站姿调整。
- `特效主体构图`：角色可以很小甚至只露局部，真正主体是波纹、斩痕、屏障、冲击环、雾幕等特效。
- `物品主体构图`：只有当提示词明确允许、且角色设定里确实能出现某个实体物件时才使用；否则不要为了凑构图强行发明道具。

批量生成同一角色的一组技能卡图时，建议先分配构图桶，再写每张提示词。例如 7 张卡图不要做成 7 张上半身，至少应覆盖：

- `上半身构图`
- `俯视构图`
- `仰视构图`
- `手部特写`
- `脚部特写`
- `特效主体构图`
- `物品主体构图` 或其替代的 `符号主体 / VFX 主体构图`

严格元素规则：

```text
Only draw elements explicitly visible in the character reference image or explicitly requested in this prompt.
Never invent extra weapons, armor, shields, wings, horns, halos, jewelry, UI frames, card borders, corner ornaments, stars, sparkles, or background props.
Any extra skill-themed element must be intangible VFX only: translucent light, mist, aura, abstract lines, energy arcs, glyphs, silhouettes, or symbolic shadows.
Extra elements must never become physical equipment, costume parts, held props, body parts, or solid objects attached to the character.
If an old skill image is provided as an action or element reference, use only the requested pose/composition/effect idea from it.
Do not copy any old-card element that conflicts with the character element constraints.
Do not add symbolic objects just because the skill name suggests them unless the prompt explicitly allows them.
```

如果要使用旧卡面做参考，建议给每张输入图标明角色：

```text
Image 1 is the character identity reference.
Image 2 is only an action/composition/effect reference for the existing skill image.
Image 2 is not a style reference and not an element whitelist.
```

## 角色元素限制提示词

生成任何角色技能图时，把对应角色的限制段落复制进提示词。这里的 `Allowed` 是可以出现的元素；`Forbidden` 是即使技能名容易联想到也不要自动添加的元素。

Kasiya：

```text
Kasiya element constraints:
Allowed: white or silver hair, golden eyes, white dress or armor-like white outfit, black angular shoulder pieces, red chest gem, a long sword or blade-like white light, pale holy or crystal-like light effects.
Forbidden: shield, physical shield, buckler, barrier held as a shield, extra weapons, staff, wings, halo, crown, helmet, heavy armor, jewelry not visible in the reference, card frame, border, UI ornaments.
For defense skills, use abstract light planes, crystal facets, guard stance, or white/gold energy arcs instead of a shield. These effects must stay translucent and intangible, not held equipment.
```

Mariya：

```text
Mariya element constraints:
Allowed: light blue hair, green eyes, sleeveless white dress, bare arms, simple silver crescent staff or polearm motif, blue ribbon accents, pale healing or holy light effects.
Forbidden: gloves, detached sleeves, long arm coverings, heavy armor, shield, extra swords unless explicitly requested, wings, halo, crown, ornate jewelry, card frame, border, UI ornaments.
Keep Mariya's outfit simple and sleeveless. Do not add new costume layers.
Holy/healing symbols may appear only as translucent VFX behind or around her, never as physical accessories attached to her body or outfit.
```

Nightingale：

```text
Nightingale element constraints:
Allowed: blonde twin-tail hair, black hair bows, red eyes, black sleeveless dress, bare arms, red ribbon accent, white blade-like light effects, shadow veil or moonlike arcs when explicitly requested.
Forbidden: shield, staff, armor, wings, halo, crown, extra costume layers, gloves unless visible in the reference, musical notes unless the skill explicitly requests song/music, card frame, border, UI ornaments.
Keep the silhouette light and assassin-like. Do not add fantasy armor or holy equipment.
Shadow, moon, or song elements may appear only as translucent VFX, not as physical props, clothing, wings, or accessories.
```

Echo：

```text
Echo element constraints:
Allowed: long white hair, purple eyes, white outfit, dark blue angular shadow shapes from the reference, purple blade-like energy, pale sound-wave or resonance effects when explicitly requested.
Forbidden: shield, staff, heavy armor, wings, halo, crown, ornate jewelry, extra weapons unless explicitly requested, animal-like ears or horns beyond the reference's dark angular shapes, card frame, border, UI ornaments.
Keep the dark blue shapes abstract and angular. Do not turn them into armor, wings, or creatures.
Sound, resonance, void, or echo elements may appear only as translucent VFX, not as physical equipment, body parts, or solid attached objects.
```

## Echo 攻击技能提示词草案

这组提示词用于解决 Echo 攻击卡图同质化问题。每张图都要有明确构图差异，不必每张都画完整人物；如果人物不是重点，可以只画局部、背影、剪影，甚至只画技能特效。通用风格和 Echo 元素限制仍然要一起使用。
这一组必须遵守上面的构图平衡规则，不要连续多张都落成上半身正面或侧面出招图。

通用前缀：

```text
Use the provided Echo portrait only for identity and palette.
Raw game skill artwork only, not a finished card template.
Follow Echo element constraints exactly.
Pale anime sketch, thin rough line art, simple watercolor shading, flat bright white background, large empty space, moderate visible color.
Do not make every image a same half-body portrait. Prioritize visual distinction between skills.
No card frame, no border, no UI ornaments, no text, no watermark, no logo.
```

SacredOnslaught：

```text
Skill: Sacred Onslaught.
Composition: wide horizontal impact composition, Echo placed small on the left third or shown only as a pale silhouette behind the effect.
Action: a broad sanctuary-like resonance wave surges from left to right, sweeping across multiple invisible targets.
Main visual: layered white-purple shock arcs, a faint holy circular pressure front, and small translucent block-like light plates forming near Echo.
Do not use a close-up portrait. Do not center Echo's face. The attack wave is the subject.
Keep all plates and arcs as intangible VFX, not physical shields or equipment.
```

ResonantSlash：

```text
Skill: Resonant Slash.
Composition: diagonal crossing layout with large empty corners; Echo may be shown from behind or cropped at shoulder level.
Action: two sequential resonance slashes cross in an X shape at different depths, suggesting a double hit.
Main visual: one pale violet slash in the foreground and one thinner delayed echo-slash behind it, with subtle sound-wave ripples along the cut path.
Avoid frontal half-body pose. Avoid many random blades. Exactly two main slash trails.
All slash trails are translucent energy VFX, not held weapons.
```

EchoPuncture：

```text
Skill: Echo Puncture.
Composition: extreme horizontal negative-space composition; the focal point is a thin piercing beam crossing the card.
Action: Echo's hand, eye, or shoulder can be barely visible at one edge while a narrow purple-white beam pierces forward.
Main visual: one sharp central beam plus one faint offset echo beam, like a delayed after-puncture.
The image may omit most of Echo's body. Make it minimal, precise, and fast.
Avoid broad slashes, circular explosions, or centered portrait composition.
```

Extract：

```text
Skill: Extract.
Composition: quiet close-up or abstract non-character composition; Echo's face can be small, off-center, or partially hidden.
Action: a thin violet thread pulls energy inward from an unseen weakened target toward Echo.
Main visual: delicate energy thread, faint empty silhouette or negative-space mark at the far end, and a small glow near Echo's chest or fingertips.
Mood: quiet, surgical, draining, not explosive.
Avoid big attack arcs. Avoid showing a full enemy. Avoid making it look like a slash skill.
The energy thread is intangible VFX only.
```

BladeOfSlaughter：

```text
Skill: Blade of Slaughter.
Composition: dramatic close crop with Echo's eye or profile on one side and a single dark violet crescent energy blade dominating the opposite side.
Action: a decisive execution-like cut passes through the foreground.
Main visual: one dark purple crescent slash with a pale inner edge, and a faint resonance mark behind it to imply a conditional follow-up.
Mood: severe and focused, more dangerous than Resonant Slash.
Avoid multiple crisscross slashes. Avoid wide resonance rings. The crescent is VFX, not a physical weapon.
```

DisasterImpact：

```text
Skill: Disaster Impact.
Composition: mostly effect-focused, top-down or low-angle burst composition; Echo can be a small silhouette above or behind the impact.
Action: a compressed disaster pulse erupts outward from a single point.
Main visual: pale purple pressure rings, fractured abstract VFX lines, and a central impact bloom. The fracture lines should look like energy stress, not physical debris.
Mood: unstable and catastrophic but still clean and readable.
Avoid portrait-first composition. Avoid solid rocks, broken glass, or physical fragments.
```

## Echo 生存技能提示词草案

这组用于 Echo 生存卡图。整体原则：优先局部或半身构图，不要全身小人、远景大场面或多人群像。每张要用不同防御语汇表达：墙、偏转、调律、护佑、失谐、位移、护幕。所有防御元素都必须是虚的特效，不能变成实体盾、实体护甲或角色携带装备。
这一组也必须遵守上面的构图平衡规则：虽然可以用近景，但不能把所有卡都画成同一种上半身角度。

通用前缀：

```text
Use the provided Echo portrait only for identity and palette.
Raw game skill artwork only, not a finished card template.
Follow Echo element constraints exactly.
Pale anime sketch, thin rough line art, simple watercolor shading, flat bright white background, moderate visible color.
Prefer close-up, cropped bust, shoulder crop, hand crop, side profile, or half-body composition.
Fill the card with character crop plus foreground VFX. Avoid distant full-body framing, tiny character, large empty blank areas, or group shots.
Do not make every image a same half-body portrait. Prioritize visual distinction between survival skills.
All defensive objects must be intangible VFX only: translucent sound waves, resonance rings, mist, aura, abstract lines, or symbolic shadows.
No physical shield, no armor, no extra equipment, no card frame, no border, no UI ornaments, no text, no watermark, no logo.
```

SoundBarrier：

```text
Skill: Sound Barrier.
Composition: cropped half-body or shoulder crop; Echo stands close behind the foreground sound wall.
Action: a translucent vertical sound wall rises close to the camera, partly covering Echo's body.
Main visual: stacked pale purple-white waveform bands and thin resonance rings crossing the foreground.
The sound wall is transparent VFX, not a physical shield or glass wall.
Avoid distant full-body framing. Avoid attack slashes.
```

SonicDeflection：

```text
Skill: Sonic Deflection.
Composition: cropped bust or hand crop with a diagonal incoming-force line bending away near the foreground.
Action: a sound ripple redirects the attack aside.
Main visual: one thin hostile streak curving away, a small deflection ripple, and Echo's focused face or hand near the edge.
Mood: precise, reactive, evasive.
Avoid full-body pose, large barrier walls, impact explosions, and making the deflection line into a weapon.
```

TuningStance：

```text
Skill: Tuning Stance.
Composition: centered upper-body composition with strong symmetry.
Action: Echo holds a quiet tuning posture inside balanced resonance rings, like tuning an instrument before battle.
Main visual: two or three thin circular sound rings around the torso and a faint horizontal waveform crossing behind her shoulders.
Mood: prepared, balanced, quiet.
Avoid full-body framing, attack beams, slashes, or chaotic energy. Keep the pose simple and iconic.
```

ResonantWard：

```text
Skill: Resonant Ward.
Composition: intimate protective close-up or shoulder crop, with the ward symbol occupying the open side of the card.
Action: a small resonance ward forms beside Echo, protecting her from debuffs.
Main visual: a compact pale sigil made of concentric rings and soft purple sound marks.
Mood: gentle protection, clean and minimal.
The ward is a floating translucent glyph, not a shield, armor plate, halo, or physical object.
```

DissonantField：

```text
Skill: Dissonant Field.
Composition: cropped side profile or waist-up composition, with the warped sound field filling the foreground.
Action: uneven resonance waves distort the space immediately around Echo and weaken unseen enemies.
Main visual: offset waveform bands, slightly broken purple rings, and a subtle visual imbalance around the torso and face.
Mood: uncomfortable, suppressive, but still clean.
Avoid distant full-body framing, physical debris, enemies, explosions, and attack slashes.
```

RelayShift：

```text
Skill: Relay Shift.
Composition: dynamic half-body crop with one solid Echo and one partial afterimage cropped at the edge.
Action: Echo shifts backward while a pale resonance trail links the previous and next positions.
Main visual: one translucent afterimage fragment, a curved relay line, and a small energy handoff pulse near the shoulder or hand.
Mood: agile repositioning, supportive movement.
Do not draw extra characters. The afterimage is only partial VFX, not a separate full-body person.
```

ResonanceShelter / Shelter：

```text
Skill: Resonance Shelter.
Composition: close bust or shoulder crop, with a soft resonance veil passing in front of Echo like a curtain.
Action: a soft resonance canopy folds around Echo, suggesting broad protection and card refresh without showing other allies.
Main visual: a thin translucent veil or curtain of pale purple sound waves, with a few gentle circular ripples near the foreground.
Mood: soft cover, safe space, quiet recovery.
Avoid full-body framing, other characters, solid dome, physical tent, shield, roof, or armor. The shelter must be mist-like VFX.
```

## Echo 特殊技能提示词草案

这组用于 Echo 特殊卡图。整体原则：比攻击卡更概念化、比生存卡更高能，但仍然保持干净、克制、可读。优先用“共鸣层叠、重复释放、相位分裂、虚无化、回响复制”这些语汇，不要画成普通斩击卡，也不要变成实体装备或召唤物。
这一组同样必须遵守上面的构图平衡规则，优先把“特效主体、俯视、仰视、局部特写”分散开，而不是重复半身像。

通用前缀：

```text
Use the provided Echo portrait only for identity and palette.
Raw game skill artwork only, not a finished card template.
Follow Echo element constraints exactly.
Pale anime sketch, thin rough line art, simple watercolor shading, flat bright white background, moderate visible color.
Special-skill images may be more abstract than attack or survival cards, but must stay clean and readable at small card size.
Prefer close crop, half-body, side profile, silhouette layering, or effect-dominant composition instead of a standard centered portrait.
All extra shapes must be intangible VFX only: resonance rings, waveform layers, phase afterimages, void haze, symbolic shadows, translucent glyphs, or purple-white energy planes.
Do not add physical shields, armor, staffs, creatures, floating machines, card frame, border, UI ornaments, text, watermark, or logo.
```

EchonicResonance：

```text
Skill: Echonic Resonance.
Composition: strong forward-facing or three-quarter half-body composition with layered resonance bands expanding outward from Echo.
Action: Echo releases a first resonance strike, then feeds more energy into the same wave again and again, making the attack intensify in stacked pulses.
Main visual: one central purple-white resonance lane with several nested delayed pulse layers, each slightly larger or brighter than the last, plus subtle sound-wave rings around the torso or hand.
Mood: escalating, deliberate, accumulating force.
Show clear repeated amplification, not many unrelated projectiles.
Avoid wide battlefield scene, enemy figures, or messy explosion clutter.
```

SonicBoom：

```text
Skill: Sonic Boom.
Composition: broad effect-dominant composition, with Echo small or partially cropped near one edge while the blast fills most of the card.
Action: a violent sound burst detonates outward and hits multiple targets at once in repeated shock pulses.
Main visual: one large circular shock front and two thinner follow-up shock rings echoing behind it, with compressed white-purple pressure lines radiating across the frame.
Mood: loud, sudden, overwhelming, but still clean.
The repeating hits should read as layered sonic pressure, not fire, lightning, or debris.
Avoid centered portrait pose or solid physical explosion fragments.
```

PhaseEcho：

```text
Skill: Phase Echo.
Composition: layered split-image composition with one main Echo and one or two offset phase silhouettes overlapping at different depths.
Action: Echo shifts through multiple phases at once, releasing a broad destructive wave while part of herself seems to fall out of sync.
Main visual: translucent offset afterimages, warped purple-white phase seams, and a broad unstable resonance wash spreading across the card.
Mood: unstable, costly, high-output, self-straining.
The afterimages are phase VFX only, not separate characters or clones.
Avoid neat symmetry, grounded props, or normal slash composition.
```

ReverbChain：

```text
Skill: Reverb Chain.
Composition: long flowing diagonal or S-curve composition that suggests chained repeats across time.
Action: one attack impulse repeats again and again as a linked resonance phrase, as if Echo is continuing a combo through accumulated allied rhythm.
Main visual: several connected purple-white strike marks or pulse nodes arranged along one continuous path, each one smaller or later than the previous, like a musical reverb chain.
Mood: rhythmic, continuous, clever, accumulative.
Make the repeated pattern readable as a chain, not random scattered hits.
Avoid full enemy group, literal music instruments, or dense overlapping slashes.
```

VoidForm：

```text
Skill: Void Form.
Composition: iconic upper-body or close bust transformation image, with Echo partially dissolving into pale void haze.
Action: Echo enters a quiet void state, becoming harder to affect and less physically anchored.
Main visual: white clothing and hair fading into soft negative-space mist, dark blue angular shapes widening around her like abstract void planes, and thin purple resonance traces fading into silence.
Mood: cold, detached, untouchable, quiet power.
Keep the void effect airy and abstract, not monstrous, not cosmic horror, not black armor.
Avoid extra body parts, wings, horns, or creature silhouettes.
```

EchoForm：

```text
Skill: Echo Form.
Composition: close or half-body composition with one clear Echo and one faint mirrored echo-offset overlapping behind or beside her.
Action: Echo enters a state where her next actions can repeat, so the image should suggest duplication, replay, and delayed follow-through.
Main visual: one main figure plus one translucent echo layer with slightly shifted pose timing, connected by thin resonance arcs and circular replay ripples.
Mood: elegant, uncanny, precise repetition.
The echo layer must remain translucent VFX, not a solid clone or second person.
Avoid chaotic motion blur, crowding, or turning the card into an attack scene.
```

如果是某个角色已经有明确服装特征，提示词里要直接钉死，避免模型自己补设定。比如 Mariya 当前设定是：

```text
Keep Mariya's sleeveless white dress and bare arms.
No gloves, no detached sleeves, no long arm coverings.
```

如果要避免手崩，明确写：

```text
Hand correctness is top priority.
Avoid visible detailed hands whenever possible.
Use a simple closed gloved grip.
No open palms, no spread fingers, no extra fingers, no twisted wrists.
Hide the off-hand behind sleeve, hair, body, or composition.
```

如果是修现有图里的手，不要泛泛地说“修好手”，而是按局部编辑写法明确限制修改范围：

```text
Edit the first image.
Change only the hand and wrist near the effect. Keep everything else the same.
Replace the current hand with a simpler side-view gesture.
Show exactly one natural hand with five fingers only.
No extra fingertips, no overlapping finger confusion, no twisted wrist.
```

如果不是要藏手，而是要真的把可见手画对，最好不要让模型自由发挥“复杂施法手势”，而是把手势压成单手、单方向、单轮廓：

```text
Hand correctness is top priority.
Show exactly one clearly readable hand.
The other hand must be fully hidden.
Use a simple side-view or three-quarter-view hand only, never a front-facing palm toward the camera.
Show one natural hand with five fingers only.
Keep the fingers together in one clean silhouette, with the thumb clearly separated.
No finger spread, no extra fingertips, no overlapping finger confusion.
The wrist must continue naturally from the forearm with one clear bend direction only.
No twisted wrist, no reversed elbow direction, no broken forearm-to-hand connection.
If the pose becomes ambiguous, simplify the hand gesture instead of adding more finger detail.
```

更稳一点的防御/施法可见手写法可以直接钉死成这类低风险动作：

```text
Use a simple single-hand guarding gesture.
Visible hand pose: side-view hand, fingers together and slightly bent, thumb separated, palm facing sideways rather than forward.
Only one row of fingertips visible.
The hand should read as one clean silhouette first, finger details second.
```

祈祷手、双手合十这类高风险姿势，尽量压成单一轮廓：

```text
Replace the current hands with two palms pressed flat together vertically.
Fingers straight and aligned into one clean silhouette.
Only one row of fingertips visible.
No interlocked fingers, no overlapping finger confusion.
```

如果要避免画面太乱，明确写：

```text
No random fragments, no broken glass debris, no sparks, no noisy scratch lines.
Use only subtle light arcs or a faint aura.
Keep the picture clean and readable at small card size.
```

## 输出规格

现有 `asset/CardPicture/{CharacterName}` 里的多数卡图是：

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

## 当前卡图分组

当前项目内建议按角色拆目录：

```text
asset/CardPicture/Kasiya/
asset/CardPicture/Mariya/
asset/CardPicture/Nightingale/
asset/CardPicture/Echo/
```

新图默认直接输出到对应角色目录，不再往 `asset/CardPicture/` 根目录堆文件。
