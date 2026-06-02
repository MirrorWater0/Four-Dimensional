# 任务清单：示例功能

## Phase 1 - 初始化

### 1.1 创建 OpenSpec 目录结构
- [x] 创建 `openspec/` 根目录
- [x] 创建 `openspec/changes/` 和 `openspec/specs/`
- [x] 复制现有 `AGENTS.md` 到 `openspec/AGENTS.md`

### 1.2 编写项目约定文档
- [x] 创建 `openspec/project.md`，包含编码规范和文件组织约定

## Phase 2 - 验证扩展

### 2.1 验证按钮显示
- [-] 打开本文件，确认每个任务上方出现 ▶️ Start task 按钮
- [ ] 点击按钮测试上下文注入（需要安装 GitHub Copilot）

### 2.2 验证状态图标
- [ ] 进行中任务显示 🔵（使用 `- [-]` 或 `- [~]`）
- [ ] 未开始任务显示 ⚪（使用 `- [ ]`）
- [x] 已完成任务显示 ✅（使用 `- [x]`）

## Phase 3 - 下一个真实功能（示例）

### 3.1 实现新功能
- [ ] 在 `openspec/changes/` 下创建真实的功能变更目录
- [ ] 编写 `proposal.md` 描述需求
- [ ] 编写 `tasks.md` 拆解开发步骤
- [ ] 使用 ▶️ Start task 按钮让 AI 协助实现
