# AnimationNodeAnimation

## Meta

- Name: AnimationNodeAnimation
- Source: AnimationNodeAnimation.xml
- Inherits: AnimationRootNode
- Inheritance Chain: AnimationNodeAnimation -> AnimationRootNode -> AnimationNode -> Resource -> RefCounted -> Object

## Brief Description

An input animation for an AnimationNodeBlendTree.

## Description

A resource to add to an AnimationNodeBlendTree. Only has one output port using the animation property. Used as an input for AnimationNodes that blend animations together.

## Quick Reference

```
[properties]
advance_on_start: bool = false
animation: StringName = &""
loop_mode: int (Animation.LoopMode)
play_mode: int (AnimationNodeAnimation.PlayMode) = 0
start_offset: float
stretch_time_scale: bool
timeline_length: float
use_custom_timeline: bool = false
```

## Tutorials

- [Using AnimationTree]($DOCS_URL/tutorials/animation/animation_tree.html)
- [3D Platformer Demo](https://godotengine.org/asset-library/asset/2748)
- [Third Person Shooter (TPS) Demo](https://godotengine.org/asset-library/asset/2710)

## Properties

- advance_on_start: bool = false [set set_advance_on_start; get is_advance_on_start]
  If true, on receiving a request to play an animation from the start, the first frame is not drawn, but only processed, and playback starts from the next frame. See also the notes of AnimationPlayer.play().

- animation: StringName = &"" [set set_animation; get get_animation]
  Animation to use as an output. It is one of the animations provided by AnimationTree.anim_player.

- loop_mode: int (Animation.LoopMode) [set set_loop_mode; get get_loop_mode]
  If use_custom_timeline is true, override the loop settings of the original Animation resource with the value. **Note:** If the Animation.loop_mode isn't set to looping, the Animation.track_set_interpolation_loop_wrap() option will not be respected. If you cannot get the expected behavior, consider duplicating the Animation resource and changing the loop settings.

- play_mode: int (AnimationNodeAnimation.PlayMode) = 0 [set set_play_mode; get get_play_mode]
  Determines the playback direction of the animation.

- start_offset: float [set set_start_offset; get get_start_offset]
  If use_custom_timeline is true, offset the start position of the animation. This is useful for adjusting which foot steps first in 3D walking animations.

- stretch_time_scale: bool [set set_stretch_time_scale; get is_stretching_time_scale]
  If true, scales the time so that the length specified in timeline_length is one cycle. This is useful for matching the periods of walking and running animations. If false, the original animation length is respected. If you set the loop to loop_mode, the animation will loop in timeline_length.

- timeline_length: float [set set_timeline_length; get get_timeline_length]
  The length of the custom timeline. If stretch_time_scale is true, scales the animation to this length.

- use_custom_timeline: bool = false [set set_use_custom_timeline; get is_using_custom_timeline]
  If true, AnimationNode provides an animation based on the Animation resource with some parameters adjusted.

## Constants

### Enum PlayMode

- PLAY_MODE_FORWARD = 0
  Plays animation in forward direction.

- PLAY_MODE_BACKWARD = 1
  Plays animation in backward direction.
