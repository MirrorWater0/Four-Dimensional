# STS2 斩击素材来源

来源为用户指定的本地参考目录 `D:\Slay the Spire 2`。

- `AttackCut.png`：由 `images/vfx/vfx_attack_slash/vfx_attack_slash_00.png` 至 `03.png` 四张 512×512 原图依次拼成 2×2 图集，保留原始 RGBA。播放顺序为左上、右上、左下、右下。
- `SwordEnergy.png`：来自 `images/vfx/slash/slash_flipbook.png`，保留原始 3×2 图集及 RGBA，逐行播放。红色通道用于材质颜色映射。

时序参考：

- `scenes/vfx/vfx_attack_slash.tscn` 与 `src/Core/Nodes/Vfx/Utilities/NSpriteAnimator.cs`：普通斩击四帧，默认 15 FPS，时长约 0.267 秒。
- `scenes/vfx/slash/vfx_slash_core.tscn`、`materials/vfx/slash/vfx_slash_core.tres`：六帧、0.3 秒、后半段淡出、少量向前位移。

本项目使用自己的播放控制与换色材质，不依赖 STS2 的游戏代码。以上两张 PNG 使用了 STS2 本地资源中的美术素材。
