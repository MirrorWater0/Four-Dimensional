# XRNode3D

## Meta

- Name: XRNode3D
- Source: XRNode3D.xml
- Inherits: Node3D
- Inheritance Chain: XRNode3D -> Node3D -> Node -> Object

## Brief Description

A 3D node that has its position automatically updated by the XRServer.

## Description

This node can be bound to a specific pose of an XRPositionalTracker and will automatically have its Node3D.transform updated by the XRServer. Nodes of this type must be added as children of the XROrigin3D node.

## Quick Reference

```
[methods]
get_has_tracking_data() -> bool [const]
get_is_active() -> bool [const]
get_pose() -> XRPose
trigger_haptic_pulse(action_name: String, frequency: float, amplitude: float, duration_sec: float, delay_sec: float) -> void

[properties]
physics_interpolation_mode: int (Node.PhysicsInterpolationMode) = 2
pose: StringName = &"default"
show_when_tracked: bool = false
tracker: StringName = &""
```

## Tutorials

- [XR documentation index]($DOCS_URL/tutorials/xr/index.html)

## Methods

- get_has_tracking_data() -> bool [const]
  Returns true if the tracker has current tracking data for the pose being tracked.

- get_is_active() -> bool [const]
  Returns true if the tracker has been registered and the pose is being tracked.

- get_pose() -> XRPose
  Returns the XRPose containing the current state of the pose being tracked. This gives access to additional properties of this pose.

- trigger_haptic_pulse(action_name: String, frequency: float, amplitude: float, duration_sec: float, delay_sec: float) -> void
  Triggers a haptic pulse on a device associated with this interface. action_name is the name of the action for this pulse. frequency is the frequency of the pulse, set to 0.0 to have the system use a default frequency. amplitude is the amplitude of the pulse between 0.0 and 1.0. duration_sec is the duration of the pulse in seconds. delay_sec is a delay in seconds before the pulse is given.

## Properties

- physics_interpolation_mode: int (Node.PhysicsInterpolationMode) = 2 [set set_physics_interpolation_mode; get get_physics_interpolation_mode; override Node]

- pose: StringName = &"default" [set set_pose_name; get get_pose_name]
  The name of the pose we're bound to. Which poses a tracker supports is not known during design time. Godot defines number of standard pose names such as aim and grip but other may be configured within a given XRInterface.

- show_when_tracked: bool = false [set set_show_when_tracked; get get_show_when_tracked]
  Enables showing the node when tracking starts, and hiding the node when tracking is lost.

- tracker: StringName = &"" [set set_tracker; get get_tracker]
  The name of the tracker we're bound to. Which trackers are available is not known during design time. Godot defines a number of standard trackers such as left_hand and right_hand but others may be configured within a given XRInterface.

## Signals

- tracking_changed(tracking: bool)
  Emitted when the tracker starts or stops receiving updated tracking data for the pose being tracked. The tracking argument indicates whether the tracker is getting updated tracking data.
