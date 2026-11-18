# 行动牌布局预览

在 Godot 打开 `res://tools/ActionCardsUiPreview.tscn`，按 F6 运行当前场景。

预览以提供的战斗截图作为背景，三个角色脚下显示示例行动牌。左上可切换每个角色 2～5 张牌、模拟触发高亮、显示或隐藏。悬停时小卡保持原尺寸、小幅抬起，并显示独立的完整卡牌详情，移开鼠标后复原。

卡牌直接实例化项目现有的 `BattleHandCard.tscn` / `SkillCard`，缩放为 29%，根据牌数调整重叠间距；小卡悬停复用手牌的蓝色遮罩。抬起距离与详情预览尺寸由行动牌组件的导出参数控制，抓起手牌期间行动牌不处理悬停或点击。场景布局保存在 TSCN 中。

本版本是 UI 原型，示例卡名和效果不接入战斗结算。可复用的组件是 `res://battle/UIScene/ActionCardRack.tscn`；运行时通过 `SetCards(CardEntry[])` 提供实际 Skill，调用 `Pulse(index)` 显示触发反馈。当前显示容量为五张，不代表玩法槽位限制。

正式 Battle 已接入实际行动牌：攻击牌和生存牌手动安装，每轮结束自动执行。布局参数在 `Battle.tscn` 的 `ActionCardsLayer/ActionCardsUi` 导出，可直接在 Inspector 中调整。完整规则及消耗 X 说明见 `docs/行动卡牌战斗系统.md`；正式战斗功能检查运行 `tools/ActionCardRegression.tscn`。

预览独立于正式 Battle 场景。背景中的手牌和角色来自静态截图，只有新增的行动牌与预览控件可以操作。
