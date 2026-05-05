# GrooveJoint2D

## Meta

- Name: GrooveJoint2D
- Source: GrooveJoint2D.xml
- Inherits: Joint2D
- Inheritance Chain: GrooveJoint2D -> Joint2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

A physics joint that restricts the movement of two 2D physics bodies to a fixed axis.

## Description

A physics joint that restricts the movement of two 2D physics bodies to a fixed axis. For example, a StaticBody2D representing a piston base can be attached to a RigidBody2D representing the piston head, moving up and down.

## Quick Reference

```
[properties]
initial_offset: float = 25.0
length: float = 50.0
```

## Properties

- initial_offset: float = 25.0 [set set_initial_offset; get get_initial_offset]
  The body B's initial anchor position defined by the joint's origin and a local offset initial_offset along the joint's Y axis (along the groove).

- length: float = 50.0 [set set_length; get get_length]
  The groove's length. The groove is from the joint's origin towards length along the joint's local Y axis.
