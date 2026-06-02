# 项目约定 (Project Conventions)

## 工作流
- 所有功能开发通过 `openspec/changes/` 下的变更提案进行管理
- 每个变更必须包含 `proposal.md` → `design.md`（如需） → `tasks.md`
- 任务完成后更新 `- [x]` 状态

## 代码提交规范
- 提交信息使用中文或英文，保持简洁
- 重大变更需关联对应的 OpenSpec change ID

## 文件组织
- 新角色技能放在 `character/PlayerCharacter/{角色名}/`
- 新敌人放在 `character/EnemyCharacter/`
- 新特效放在 `battle/Effect/`
- UI 场景放在对应系统的 `UIScene/` 或 `UI/` 子目录

## 命名约定
- 场景文件：`.tscn`，脚本文件：`.cs`
- 类名与文件名一致（PascalCase）
- 资源文件使用小写 + 下划线（`some_texture.png`）

## Godot 特定
- 所有节点脚本必须是 `public partial class`
- 使用 `[Export]` 暴露编辑器可配置字段
- 避免在 `_Process` 中做重逻辑，优先用信号驱动

## AI 协作提示
- 修改现有文件前先检查文件头是否有特殊注释
- 不要修改 `asset/` 下的路径拼写（如 `Charater`）
- 涉及 Spine 或 Shader 的改动需确认运行时兼容性
