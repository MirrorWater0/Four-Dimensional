# 技术设计：示例功能

## 方案概述
使用 OpenSpec 规范驱动开发流程管理后续功能迭代。

## 目录结构
```
openspec/
├── AGENTS.md          # 项目知识库（自动注入 AI 上下文）
├── project.md         # 项目约定（编码规范、提交规范）
├── changes/
│   └── {change-id}/
│       ├── proposal.md    # 变更提案
│       ├── design.md      # 技术设计（本文件）
│       └── tasks.md       # 任务清单（与 AI 交互入口）
└── specs/             # 长期规格文档
```

## 集成点
- VS Code OpenSpec 扩展会扫描 `openspec/changes/*/tasks.md`
- 点击 CodeLens 按钮时自动注入 `AGENTS.md` + `project.md` + `proposal.md` + `design.md` + 当前任务
- 如果 AI Chat API 不可用，上下文会复制到剪贴板

## 无运行时影响
本变更 purely additive，不修改任何现有 C# 脚本、场景文件或资源。
