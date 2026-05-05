# VisualShaderNodeScreenUVToSDF

## Meta

- Name: VisualShaderNodeScreenUVToSDF
- Source: VisualShaderNodeScreenUVToSDF.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeScreenUVToSDF -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A function to convert screen UV to an SDF (signed-distance field), to be used within the visual shader graph.

## Description

Translates to screen_uv_to_sdf(uv) in the shader language. If the UV port isn't connected, SCREEN_UV is used instead.
