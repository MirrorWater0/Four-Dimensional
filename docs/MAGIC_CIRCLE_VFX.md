# 法阵展开特效

在 Godot 中打开 `res://tools/MagicCirclePreview.tscn`，按 F6 预览。左侧是俯视图，右侧是地面投影；支持拖动时间轴、空格暂停和 R 重播。

特效场景：`res://battle/Effect/MagicCircleUnfoldVfx.tscn`。

默认时序：1.05 秒展开 → 1.25 秒维持 → 0.65 秒消散。圆环扩张并沿圆周绘制，24 枚几何符文依次点亮，六角星与外围小印记随后出现。成阵时产生短暂闪光和向外扩散的波纹；维持阶段缓慢旋转，伴随程序化光点。

## 在技能中调用

```csharp
// localPosition 使用 effectParent 的局部坐标；实际外缘半径约 220 像素。
MagicCircleUnfoldVfx.Spawn(effectParent, localPosition, 220f);
```

需要改色、俯视、循环或自定义时序时，在加入场景树之前配置：

```csharp
var circle = GD.Load<PackedScene>("res://battle/Effect/MagicCircleUnfoldVfx.tscn")
    .Instantiate<MagicCircleUnfoldVfx>();
circle.Position = localPosition;
circle.PrimaryColor = new Color("7be5ff");
circle.AccentColor = new Color("b68aff");
circle.GroundRatio = 1f; // 1 = 俯视；默认 0.48 = 地面椭圆投影
circle.HoldSeconds = 2f;
effectParent.AddChild(circle);
```

- `Radius`：法阵外缘大小；`GroundRatio`：纵向压缩比例。
- `UnfoldSeconds / HoldSeconds / FadeSeconds`：三个阶段时长。
- `Brightness / RotationSpeed`：亮度、旋转速度；符文环与中心几何反向转动。
- `AutoPlay`：进入场景树时自动播放。
- `AutoFree`：单次播放完成时释放节点；`Finished` 信号在释放前发出。
- `Loop / LoopDelaySeconds`：循环与间隔；循环时不会自动释放或每圈发送 `Finished`。
- `Play()` 重播、`Stop()` 停止并隐藏、`Seek(seconds)` 定格。运行中修改外观属性后调用 `RefreshAppearance()`。

所有线条、符文、光晕和光点均由 `canvas_item` shader 绘制，无图片素材、无真实光照或粒子节点依赖。使用加法混合，自带柔光，兼容没有启用全屏 glow 的 2D 场景。每个实例独立持有材质和动画时钟。默认 `z_index = 4`；地面投影的前后遮挡需要按战斗场景层级调整。

## 保存五个阶段的截图

```powershell
& 'C:/Users/86189/Desktop/Godot_v4.6-stable_mono_win64/Godot_v4.6-stable_mono_win64_console.exe' --path . --resolution 1280x720 --windowed res://tools/MagicCirclePreview.tscn -- --capture-magic-circle
```

截图保存到 `asset/generated/magic_circle_preview/`（项目已忽略此目录）。
