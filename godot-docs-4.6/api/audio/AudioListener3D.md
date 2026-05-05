# AudioListener3D

## Meta

- Name: AudioListener3D
- Source: AudioListener3D.xml
- Inherits: Node3D
- Inheritance Chain: AudioListener3D -> Node3D -> Node -> Object

## Brief Description

Overrides the location sounds are heard from.

## Description

Once added to the scene tree and enabled using make_current(), this node will override the location sounds are heard from. This can be used to listen from a location different from the Camera3D.

## Quick Reference

```
[methods]
clear_current() -> void
get_listener_transform() -> Transform3D [const]
is_current() -> bool [const]
make_current() -> void

[properties]
doppler_tracking: int (AudioListener3D.DopplerTracking) = 0
```

## Methods

- clear_current() -> void
  Disables the listener to use the current camera's listener instead.

- get_listener_transform() -> Transform3D [const]
  Returns the listener's global orthonormalized Transform3D.

- is_current() -> bool [const]
  Returns true if the listener was made current using make_current(), false otherwise. **Note:** There may be more than one AudioListener3D marked as "current" in the scene tree, but only the one that was made current last will be used.

- make_current() -> void
  Enables the listener. This will override the current camera's listener.

## Properties

- doppler_tracking: int (AudioListener3D.DopplerTracking) = 0 [set set_doppler_tracking; get get_doppler_tracking]
  If not DOPPLER_TRACKING_DISABLED, this listener will simulate the [Doppler effect](https://en.wikipedia.org/wiki/Doppler_effect) for objects changed in particular _process methods. **Note:** The Doppler effect will only be heard on AudioStreamPlayer3Ds if AudioStreamPlayer3D.doppler_tracking is not set to AudioStreamPlayer3D.DOPPLER_TRACKING_DISABLED.

## Constants

### Enum DopplerTracking

- DOPPLER_TRACKING_DISABLED = 0
  Disables [Doppler effect](https://en.wikipedia.org/wiki/Doppler_effect) simulation (default).

- DOPPLER_TRACKING_IDLE_STEP = 1
  Simulate [Doppler effect](https://en.wikipedia.org/wiki/Doppler_effect) by tracking positions of objects that are changed in _process. Changes in the relative velocity of this listener compared to those objects affect how audio is perceived (changing the audio's AudioStreamPlayer3D.pitch_scale).

- DOPPLER_TRACKING_PHYSICS_STEP = 2
  Simulate [Doppler effect](https://en.wikipedia.org/wiki/Doppler_effect) by tracking positions of objects that are changed in _physics_process. Changes in the relative velocity of this listener compared to those objects affect how audio is perceived (changing the audio's AudioStreamPlayer3D.pitch_scale).
