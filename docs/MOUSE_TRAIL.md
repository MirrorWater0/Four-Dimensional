# 鼠标光标与拖尾

全局入口为 `MouseTrail/MouseTrail.tscn`，作为 Autoload 在所有界面使用。桌面端显示轮廓清晰的金黄箭形光标，使用一条克制的浅金切面高光、短金色光带及点击时的分段涟漪；移动端不显示。界面暂停时仍响应鼠标。

在场景的 `TrailVisual` 节点上调整：

| 参数 | 默认值 | 含义 |
| --- | --- | --- |
| Trail Lifetime | 0.22 秒 | 历史轨迹的存活时间，停下后自然消散 |
| Max Trail Length | 150 像素 | 设计画布坐标下的最大长度 |
| Point Spacing | 4 像素 | 移动路径采样间距 |
| Trail Color | 琥珀金 | 光带底色 |
| Highlight Color | 浅金 | 光带前端及微光颜色 |
| Click Color | 明黄 | 点击涟漪的强调色 |

`Cursor` 材质的 Face / Facet / Edge / Accent Color 控制金黄主面、琥珀暗面、棕黑细轮廓与浅金高光。光标画布为 48 × 48，实际几何约 24 × 34 像素；尖端位于局部坐标 (7, 5)，与根节点的 Cursor Hotspot 对齐。改变几何尖端时需同步修改热点。按下时围绕尖端轻微旋转、收缩，箭头头部和握柄只保留少量高光。

`MouseTrailVisual.cs` 使用有上限的复用数组、按时间过期的路径点及连续折线绘制，不实例化粒子节点。光带最长保留 96 个路径点，点击涟漪最多同时保留 3 个。时间衰减与帧率无关，路径宽度随速度略微变化；极大的位置跳变、长帧间隔、失焦、越出窗口或恢复窗口焦点时清除旧轨迹。滚轮不触发点击涟漪。

`MouseTrail.SetUseSystemCursor()` 切换系统光标时会清理历史光带与点击效果。原有 `ResetPointerTracking()`、鼠标帧追踪参数 `--trace-mouse-frames` 及卡顿诊断接口继续可用，诊断文字默认隐藏。
