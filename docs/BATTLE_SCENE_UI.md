# 战斗场景 UI 编辑入口

战斗主画面的固定节点、位置、尺寸、字体、样式盒、材质和布局参数由 `battle/Battle.tscn` 及它引用的 `.tscn` / `.tres` 定义。运行时代码负责绑定节点、填入战斗数据、按实际卡牌数量排布，以及播放交互动画。

## 主画面

打开 `battle/Battle.tscn`，在 1920 × 1080 的设计画布中编辑：

| 内容 | 场景节点 / 资源 | 调整方式 |
| --- | --- | --- |
| 手牌装饰线和底部渐暗 | `CharacterControlLayer/BattleHudChrome` | 修改子节点的 Layout 和颜色 |
| 能量区 | `CharacterControlLayer/EnergyBG`、`EnergyLabel` | 修改 Layout、字体及 ShaderMaterial 参数 |
| 结束回合按钮 | `CharacterControlLayer/BattleActionButtons/EndTurnButton` | 修改 Layout、Theme Overrides、`CrystalSurface` 材质和快捷键提示 |
| 三个牌堆按钮、图标和数量 | `CharacterControlLayer/BattleActionButtons/*PileButton` | 修改节点 Layout、图标、数量背景材质和主题 |
| 手牌区域 | `CharacterControlLayer/CharacterControl/ActionAreaRoot/CardRow` | 修改区域 Layout 和 `BattleHandLayout` 的导出参数 |
| 选牌、弃牌和选目标时的操作提示 | `CharacterControlLayer/CharacterControl/ActionAreaRoot/StatusLabel` | 修改 Layout 和 Theme Overrides；普通回合不显示文字 |
| 战报按钮和面板 | `UI` | 修改节点 Layout、字体和面板资源 |
| 弃牌选择遮罩和隐藏按钮 | `CardPlayOverlay` | 修改遮罩 Color 和按钮 Layout；动画恢复此处定义的颜色与透明度 |
| 弃牌选择区域 | `CharacterControlLayer/DiscardSelectionOverlay` | 调整区域；选中卡牌的间距和高度在 CharacterControl 的 Selection Layout 中调整 |
| 手动选目标箭头、提示和取消按钮 | `LiftedCardOverlay/ManualTargetArrowPicker` | 修改节点 Layout 和箭头场景资源 |
| 角色站位 | `Left/Slot1…5`、`Right/Slot1…4` | 移动 Marker2D；初始化、召唤和换位使用同一组标记 |
| 超立方体背景 | `bg/BattleAtmosphere` | 调整 Structure Center、Structure Size、Projection Height、Orbit Radius、Glow Radius、色调和 Glow Texture |

顶部 72px 留给 Map 的全局 HUD，道具栏在战斗中继续可见、可用。调整战报按钮时保留这段空间。

## 手牌的编辑器预览

`CardRow` 上的 `BattleHandLayout` 是 `[Tool]` 脚本，编辑器和运行时共用水平排布公式，卡牌保持竖直，所有手牌处于同一高度。

- `Preview Card Count`：编辑器预览的牌数（1–10）；运行时使用实际手牌数量。
- `Card Scale`、`Card Y Offset`、`Side Padding`：卡牌整体大小、垂直位置和手牌两侧留白。
- `Min Step Ratio`、`Overlap Step Ratio`：牌数变化时的最小和最大横向间距。
- `Hover Lift`、`Hover Scale`、`Hover Spread Ratio/Base/Max`：悬停动画的目标布局。

请通过这些导出参数调整手牌；运行时会根据牌数重排各个 CardSlot，单独拖动 CardSlot 不会成为运行时的固定位置。

超立方体同样支持编辑器预览，`Animate Editor Preview` 可开启旋转预览。首次添加或修改 C# 导出属性后，需要在 Godot 中重新构建 C# 项目。

## 弹层与重复组件

| 内容 | 编辑文件 |
| --- | --- |
| 战斗手牌外框、序号 | `battle/UIScene/BattleHandCard.tscn` |
| 手牌槽模板 | `battle/UIScene/BattleHandSlot.tscn` |
| 通用卡牌内部布局 | `battle/UIScene/Reward/SkillCard.tscn` |
| 牌堆遮罩、滚动范围、确认按钮 | `battle/UIScene/BattlePileOverlay.tscn` |
| 牌堆标题、空状态、网格列数和间隔 | `battle/UIScene/PileSection.tscn` |
| 按角色分组的标题和网格 | `battle/UIScene/PileCharacterGroup.tscn` |
| 普通及虚拟列表的卡牌尺寸、内偏移 | `battle/UIScene/PileCardHolder.tscn` |
| 牌堆计数徽章 | `battle/UIScene/PileCountBadge.tscn` |
| 战斗教程 | `battle/UIScene/BattleTutorialOverlay.tscn` |
| 文本提示 | `battle/UIScene/Tip.tscn` |
| 关联卡牌预览的缩放、留白和间距 | `battle/UIScene/SkillRelatedCardPreview.tscn` |
| 效果预览的位置参数 | `battle/UIScene/EffectPreviewPanel.tscn` |
| 效果预览的背景、字号和图标间距 | `battle/UIScene/EffectPreviewRow.tscn`、`EffectPreviewPlainRow.tscn` |
| 效果图标的尺寸和材质 | `battle/UIScene/EffectPreviewIconHolder.tscn`、`EffectPreview*Icon.tscn` |
| 伤害、治疗等效果的语义颜色 | `battle/UIScene/EffectPreviewPalette.tres` |
| 角色名、无背景框血条、敌方意图和伤害汇总 | `character/CharacterTemplate.tscn` |

弹层默认隐藏。可在编辑器临时打开 Visible 检查布局，保存前恢复初始隐藏状态。重复组件的数量由数据决定，运行时实例化以上场景模板。

`SkillCard.tscn` 根节点的 Card Appearance 提供耗能数字字号、可用/能量不足颜色、稀有度边框亮度和说明最小字号。卡牌文本、插画、稀有度和角色标识由实际技能数据填充。

## 公共样式

`battle/UIScene/BattleUiTheme.tres` 管理字体、按钮状态样式和牌堆状态颜色；`BattleUiFont.tres` 管理字体回退。单个控件可以使用 Theme Overrides。

牌堆按钮读取 Theme 中的 `Button/colors/pile_icon_enabled`、`pile_icon_hover`、`pile_icon_disabled`、`pile_count_enabled`、`pile_count_disabled`。悬停和收牌动画基于场景图标的原始 Scale 播放。

主画面移除了品牌、回合标题、遭遇类型、能量标题和常驻行动提示。三个牌堆按钮各状态均使用 `SceneEmptyStyle`，只显示图标和数量，通过悬停放大、提亮及 Tooltip 说明用途，不显示边框、底色和常驻标题。左下角的帧率调试浮层默认关闭，可在 `MouseTrail` 的 Show Stutter Overlay 中重新开启。

材质的基础颜色和几何参数在场景资源中编辑；运行时只更新时间、悬停、按下、禁用、受击等状态参数。

角色血条使用无背景框的线条样式：生命条保持红色并提高对比度，受伤缓冲条保持浅色并提高亮度；拥有护盾时保持蓝色系。血条外层面板和 ProgressBar 背景轨道均不绘制，保留角色名、生命数值、护盾图标和缓冲动画。

`EndTurnButton/CrystalSurface` 使用透明底切角细线轮廓，角部略微提亮；悬停时线条增亮，按下时偏淡青，禁用时降低透明度，各状态均无面板填充。材质的 `line_color`、`line_width`、`corner_cut` 控制线色、线宽和切角大小，`EndTurnKeyHint` 的样式盒关闭 Draw Center，保留空心快捷键提示。

`EnergyBG` 使用白色细线原子轨道，中间只有无填充圆环和能量数字。三条倾斜椭圆轨道缓慢转动，微小亮点及短光迹沿轨道环绕；后侧较淡，并在中央圆环内淡出；前侧轨道和亮点完整掠过圆环前方，交叉处圆环略微断开以表现遮挡。能量数字仍在最上层保持可读。`line_color` 控制颜色，`core_radius` 控制圆环半径，`orbit_radius` / `orbit_tilt` 控制轨道大小与倾斜投影，`line_width` / `orbit_opacity` 控制线宽和透明度，`rotation_speed` 控制亮点速度，`precession_speed` 控制轨道整体转速，`glow_strength` 控制亮点光晕。动画使用 Shader 的 `TIME`，编辑器和运行时共用；将 `animation_speed` 设为 0 可以停住预览，`phase_offset` 可以选择动画阶段。能量增减触发轨道轻微外扩和圆环增亮，零能量时平滑降低亮度。

## 编辑器和运行时的预期差异

编辑器显示样例内容和默认状态。实际能量、卡牌内容、角色阵营、敌人意图、伤害/治疗颜色、按钮可用状态、鼠标跟随和动画随战斗数据变化；无障碍字号设置和长文本自适应仍生效。这些变化不重新定义主画面的固定布局。

本次迁移覆盖 `Battle.tscn` 战斗主画面及其关联组件。`BattleReady`、战前预览教程和奖励页面属于独立界面，不在这份主画面迁移范围内。
