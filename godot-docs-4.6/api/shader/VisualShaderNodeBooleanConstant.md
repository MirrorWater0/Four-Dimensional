# VisualShaderNodeBooleanConstant

## Meta

- Name: VisualShaderNodeBooleanConstant
- Source: VisualShaderNodeBooleanConstant.xml
- Inherits: VisualShaderNodeConstant
- Inheritance Chain: VisualShaderNodeBooleanConstant -> VisualShaderNodeConstant -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A boolean constant to be used within the visual shader graph.

## Description

Has only one output port and no inputs. Translated to [code skip-lint]bool[/code] in the shader language.

## Quick Reference

```
[properties]
constant: bool = false
```

## Properties

- constant: bool = false [set set_constant; get get_constant]
  A boolean constant which represents a state of this node.
