# 刀光剑气特效

根据用户指定的 `D:\Slay the Spire 2` 重新制作，采用该目录中的斩击帧图与短促时序。

- `AttackCut`：四帧普通斩击，基础时序为 15 FPS、约 0.267 秒。白色刀心配猩红边缘，从小斩点瞬间拉成斜向斩痕，再逐帧收掉。
- `SwordEnergy`：六帧弧形剑气，基础时长 0.3 秒。厚实、形状不断变化的弧形斩面，带少量前移，在后半段淡出。

帧图来源及转换记录在 `asset/Effect/SwordSlash/SOURCE.md`。普通斩击保留白色刀心并对红色边缘换色；弧形剑气依据原材质的红色通道阈值映射刀心与边缘颜色。

## 随机与连击

单次攻击会小幅改变角度、长度、粗细、位置、速度和亮度，并随机选择正手或反手。变化围绕同一命中点，不会大幅偏离角色。

同一组连击共用一个随机种子，以“左下→右上→右下→左上→左下”的折返路线连续接刀，第三刀穿过第一刀形成交叉。整组可镜像或掉转起手，并小幅改变倾斜、横向跨度和落点；每刀都保留明显的折回，不再逐刀沿圆周转向。普通斩击按源帧图的首尾位置规划，让上一刀的收势衔接下一刀的起手；斩击端点始终约束在目标周围，长连击不会累积漂移。三连及以上的最后一刀会略长、略厚、略亮，速度稍收，形成收尾。弧形剑气也使用这组角度与位置变化，其宽弧帧图保留自身的前移。

实际时长由场景中的 `PlaybackSpeed` 决定，并叠加每刀的小幅速度变化；连击不再额外固定加速约 1.3 倍。随机数仅用于视觉，不消耗战斗、抽牌或目标选择的随机序列。同一个种子、总刀数和刀序号会得到相同效果。

## 预览

在 Godot 打开 `res://tools/SwordSlashPreview.tscn`，按 F6 运行当前场景。左侧是独立刀光，右侧叠在 Evil 立绘上。可选择单刀、二连斩、四连斩，点“换一组”生成新动作。重播、切换样式和拖动时间轴都会保留当前种子，便于观察同一组动作。

预览默认使用正常速度，读取特效场景中的长度、粗细、角度、镜像、样式及配色。右下方可切换普通斩击和弧形剑气，时间轴显示当前刀数与帧号；支持暂停、重播、慢放、方向、反向斩击，以及场景原配色／猩红／冰蓝／幽紫配色。底部的长度、粗细滑杆独立控制两个方向，调整长度不会使刀光变粗。预览界面里的调整只用于当前预览，不会保存到特效场景。空格暂停，R 重播。

所有布局与特效实例都在 TSCN 中。特效脚本为 `[Tool]`，在编辑器中修改 `EditorPreviewProgress`（0 到 1）可以查看阶段；编辑器中不会自动播放或释放节点。

## 调用

```csharp
// 沿用特效场景中的外观参数，每次生成一组新的视觉变化。
SwordSlashVfx.Spawn(parent, localHitPosition);

// 弧形剑气，可旋转及镜像。
SwordSlashVfx.Spawn(parent, localHitPosition,
    length: 360f, angleDegrees: -15f, mirror: false,
    style: SwordSlashVfx.SlashStyle.SwordEnergy);

// 连击：在本次攻击开始时创建，随后每次命中都复用同一组种子。
var sequence = new SwordSlashSequence(hitCount: 4);
for (int i = 0; i < sequence.HitCount; i++) {
    SwordSlashVfx.Spawn(parent, localHitPosition, length: 280f,
        sequenceSeed: sequence.Seed, hitIndex: i, hitCount: sequence.HitCount);
    // 此处沿用调用方的命中间隔；特效本身不调度伤害。
    await Task.Delay(100);
}
```

- `Style`：四帧普通斩击／六帧弧形剑气。
- `Length`：只沿斩线方向拉长或缩短；普通斩击沿素材对角线拉伸，弧形剑气沿弧面纵向拉伸。由 360 调到 720 时，斩线长度加倍，宽度不变。
- `Thickness`：垂直于斩线方向的粗细倍率，以长度为 360 时的原始宽度为基准，与 `Length` 和每刀的长度变化独立。
- `AngleDegrees`、`Mirror`：旋转、镜像。
- `BladeColor`、`EnergyColor`、`Brightness`：刀心、边缘颜色及亮度。
- `TravelDistance`：弧形剑气向前位移占 `Length` 的比例，普通斩击不位移。
- `PlaybackSpeed`：播放倍率；`Duration`、`GetDuration()` 返回当前效果完整时长。
- `RandomizeVariation`：默认开启，关闭后使用基础尺寸、角度和时序。
- `VariationSeed`：非零时固定动作；为零时每次 `Play()` 使用新种子。编辑器自动预览使用固定种子，避免闪动。
- `HitIndex`、`HitCount`：从零开始的刀序号、整组刀数；同组实例须共用种子和总刀数。
- `SetSequenceVariant(seed, hitIndex, hitCount)`：更新已有实例的连击参数，适用于预览及复用。
- `AutoPlay`、`AutoFree`、`Loop`、`LoopDelaySeconds`：自动播放、结束释放及循环设置。
- `Play()`、`Pause()`、`Resume()`、`Stop()`、`Seek(seconds)`：播放控制。单次结束发出 `Finished`；暂停和时间轴定位不会触发结束或释放。`Seek()` 保留当前随机动作。

每个实例拥有独立材质和时钟，默认层级为 `z_index = 10`。已接入 `Skill` 的普通单体攻击、技能计划多段攻击和范围攻击入口，按目标视觉中心生成刀光；同组范围攻击的各目标共用每刀的动作。伤害数值和原有命中等待时序保持不变。

战斗外观统一读取 `battle/Effect/SwordSlashVfx.tscn` 根节点的导出参数。战斗入口不再重算长度或重置角度、样式；敌方的朝向翻转叠加在场景的 `Mirror` 设置上。`Spawn()` 的长度、角度、镜像和样式参数只有显式传入时才覆盖场景值。目标位置、随机种子和连击序号仍由本次攻击提供。

请修改根节点上的参数；`Surface` 的材质参数及尺寸由脚本根据根节点设置计算。保存后重新启动游戏来加载新配置：已经运行的游戏会缓存场景资源，编辑器中的改动不会自动热更新到该进程。预览的“速度”是整段时间轴的调试倍率；战斗中每刀的持续时间使用特效根节点的 `PlaybackSpeed`。

本次仅检查代码、场景引用和源素材，未运行构建、测试或 Godot 场景，实际游戏中的渲染效果尚未验证。
