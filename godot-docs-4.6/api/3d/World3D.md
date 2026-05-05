# World3D

## Meta

- Name: World3D
- Source: World3D.xml
- Inherits: Resource
- Inheritance Chain: World3D -> Resource -> RefCounted -> Object

## Brief Description

A resource that holds all components of a 3D world, such as a visual scenario and a physics space.

## Description

Class that has everything pertaining to a world: A physics space, a visual scenario, and a sound space. 3D nodes register their resources into the current 3D world.

## Quick Reference

```
[properties]
camera_attributes: CameraAttributes
direct_space_state: PhysicsDirectSpaceState3D
environment: Environment
fallback_environment: Environment
navigation_map: RID
scenario: RID
space: RID
```

## Tutorials

- [Ray-casting]($DOCS_URL/tutorials/physics/ray-casting.html)

## Properties

- camera_attributes: CameraAttributes [set set_camera_attributes; get get_camera_attributes]
  The default CameraAttributes resource to use if none set on the Camera3D.

- direct_space_state: PhysicsDirectSpaceState3D [get get_direct_space_state]
  Direct access to the world's physics 3D space state. Used for querying current and potential collisions. When using multi-threaded physics, access is limited to Node._physics_process() in the main thread.

- environment: Environment [set set_environment; get get_environment]
  The World3D's Environment.

- fallback_environment: Environment [set set_fallback_environment; get get_fallback_environment]
  The World3D's fallback environment will be used if environment fails or is missing.

- navigation_map: RID [get get_navigation_map]
  The RID of this world's navigation map. Used by the NavigationServer3D.

- scenario: RID [get get_scenario]
  The World3D's visual scenario.

- space: RID [get get_space]
  The World3D's physics space.
