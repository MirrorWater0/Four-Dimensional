# 鼠标光标与拖尾

全局入口为 `MouseTrail/MouseTrail.tscn`，作为 Autoload 在所有界面使用。桌面端显示冰蓝与银白切面的箭形光标、短光带及点击时的淡金色分段涟漪；移动端不显示。界面暂停时仍响应鼠标。

在场景的 `TrailVisual` 节点上调整：

| 参数 | 默认值 | 含义 |
| --- | --- | --- |
| Trail Lifetime | 0.22 秒 | 历史轨迹的存活时间，停下后自然消散 |
| Max Trail Length | 150 像素 | 设计画布坐标下的最大长度 |
| Point Spacing | 4 像素 | 移动路径采样间距 |
| Trail Color | 淡蓝 | 光带底色 |
| Highlight Color | 银白 | 光带前端及微光颜色 |
| Click Color | 淡金 | 点击涟漪的强调色 |

`Cursor` 材质的 Face / Facet / Edge / Accent Color 控制光标切面、深色轮廓与金色脊线。光标画布为 48 × 48，实际几何约 21 × 25 像素；尖端位于局部坐标 (8, 8)，与根节点的 Cursor Hotspot 对齐。改变几何尖端时需同步修改热点。按下时围绕尖端轻微旋转、收缩。

`MouseTrailVisual.cs` 使用有上限的复用数组、按时间过期的路径点及连续折线绘制，不实例化粒子节点。光带最长保留 96 个路径点，点击涟漪最多同时保留 3 个。时间衰减与帧率无关，路径宽度随速度略微变化；极大的位置跳变、长帧间隔、失焦、越出窗口或恢复窗口焦点时清除旧轨迹。滚轮不触发点击涟漪。

`MouseTrail.SetUseSystemCursor()` 切换系统光标时会清理历史光带与点击效果。原有 `ResetPointerTracking()`、鼠标帧追踪参数 `--trace-mouse-frames` 及卡顿诊断接口继续可用，诊断文字默认隐藏。
