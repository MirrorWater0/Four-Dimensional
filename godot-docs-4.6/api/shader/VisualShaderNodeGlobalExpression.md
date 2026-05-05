# VisualShaderNodeGlobalExpression

## Meta

- Name: VisualShaderNodeGlobalExpression
- Source: VisualShaderNodeGlobalExpression.xml
- Inherits: VisualShaderNodeExpression
- Inheritance Chain: VisualShaderNodeGlobalExpression -> VisualShaderNodeExpression -> VisualShaderNodeGroupBase -> VisualShaderNodeResizableBase -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A custom global visual shader graph expression written in Godot Shading Language.

## Description

Custom Godot Shader Language expression, which is placed on top of the generated shader. You can place various function definitions inside to call later in VisualShaderNodeExpressions (which are injected in the main shader functions). You can also declare varyings, uniforms and global constants.
