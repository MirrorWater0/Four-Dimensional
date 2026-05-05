# JointLimitationCone3D

## Meta

- Name: JointLimitationCone3D
- Source: JointLimitationCone3D.xml
- Inherits: JointLimitation3D
- Inheritance Chain: JointLimitationCone3D -> JointLimitation3D -> Resource -> RefCounted -> Object

## Brief Description

A cone shape limitation that interacts with ChainIK3D.

## Description

A cone shape limitation that interacts with ChainIK3D.

## Quick Reference

```
[properties]
angle: float = 1.5707964
```

## Properties

- angle: float = 1.5707964 [set set_angle; get get_angle]
  The radius range of the hole made by the cone. 0 degrees makes a sphere without hole, 180 degrees makes a hemisphere, and 360 degrees become empty (no limitation).
