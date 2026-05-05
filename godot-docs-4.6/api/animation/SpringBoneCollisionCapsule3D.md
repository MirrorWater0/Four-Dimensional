# SpringBoneCollisionCapsule3D

## Meta

- Name: SpringBoneCollisionCapsule3D
- Source: SpringBoneCollisionCapsule3D.xml
- Inherits: SpringBoneCollision3D
- Inheritance Chain: SpringBoneCollisionCapsule3D -> SpringBoneCollision3D -> Node3D -> Node -> Object

## Brief Description

A capsule shape collision that interacts with SpringBoneSimulator3D.

## Description

A capsule shape collision that interacts with SpringBoneSimulator3D.

## Quick Reference

```
[properties]
height: float = 0.5
inside: bool = false
mid_height: float
radius: float = 0.1
```

## Properties

- height: float = 0.5 [set set_height; get get_height]
  The capsule's full height, including the hemispheres. **Note:** The height of a capsule must be at least twice its radius. Otherwise, the capsule becomes a sphere. If the height is less than twice the radius, the properties adjust to a valid value.

- inside: bool = false [set set_inside; get is_inside]
  If true, the collision acts to trap the joint within the collision.

- mid_height: float [set set_mid_height; get get_mid_height]
  The capsule's height, excluding the hemispheres. This is the height of the central cylindrical part in the middle of the capsule, and is the distance between the centers of the two hemispheres. This is a wrapper for height.

- radius: float = 0.1 [set set_radius; get get_radius]
  The capsule's radius. **Note:** The radius of a capsule cannot be greater than half of its height. Otherwise, the capsule becomes a sphere. If the radius is greater than half of the height, the properties adjust to a valid value.
