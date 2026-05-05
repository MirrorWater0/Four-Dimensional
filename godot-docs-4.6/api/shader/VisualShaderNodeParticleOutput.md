# VisualShaderNodeParticleOutput

## Meta

- Name: VisualShaderNodeParticleOutput
- Source: VisualShaderNodeParticleOutput.xml
- Inherits: VisualShaderNodeOutput
- Inheritance Chain: VisualShaderNodeParticleOutput -> VisualShaderNodeOutput -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

Visual shader node that defines output values for particle emitting.

## Description

This node defines how particles are emitted. It allows to customize e.g. position and velocity. Available ports are different depending on which function this node is inside (start, process, collision) and whether custom data is enabled.
