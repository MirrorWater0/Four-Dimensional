# AnimatedTexture

## Meta

- Name: AnimatedTexture
- Source: AnimatedTexture.xml
- Inherits: Texture2D
- Inheritance Chain: AnimatedTexture -> Texture2D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

Proxy texture for simple frame-based animations.

## Description

AnimatedTexture is a resource format for frame-based animations, where multiple textures can be chained automatically with a predefined delay for each frame. Unlike AnimationPlayer or AnimatedSprite2D, it isn't a Node, but has the advantage of being usable anywhere a Texture2D resource can be used, e.g. in a TileSet. The playback of the animation is controlled by the speed_scale property, as well as each frame's duration (see set_frame_duration()). The animation loops, i.e. it will restart at frame 0 automatically after playing the last frame. AnimatedTexture currently requires all frame textures to have the same size, otherwise the bigger ones will be cropped to match the smallest one. **Note:** AnimatedTexture doesn't support using AtlasTextures. Each frame needs to be a separate Texture2D. **Warning:** The current implementation is not efficient for the modern renderers.

## Quick Reference

```
[methods]
get_frame_duration(frame: int) -> float [const]
get_frame_texture(frame: int) -> Texture2D [const]
set_frame_duration(frame: int, duration: float) -> void
set_frame_texture(frame: int, texture: Texture2D) -> void

[properties]
current_frame: int
frames: int = 1
one_shot: bool = false
pause: bool = false
resource_local_to_scene: bool = false
speed_scale: float = 1.0
```

## Methods

- get_frame_duration(frame: int) -> float [const]
  Returns the given frame's duration, in seconds.

- get_frame_texture(frame: int) -> Texture2D [const]
  Returns the given frame's Texture2D.

- set_frame_duration(frame: int, duration: float) -> void
  Sets the duration of any given frame. The final duration is affected by the speed_scale. If set to 0, the frame is skipped during playback.

- set_frame_texture(frame: int, texture: Texture2D) -> void
  Assigns a Texture2D to the given frame. Frame IDs start at 0, so the first frame has ID 0, and the last frame of the animation has ID frames - 1. You can define any number of textures up to MAX_FRAMES, but keep in mind that only frames from 0 to frames - 1 will be part of the animation.

## Properties

- current_frame: int [set set_current_frame; get get_current_frame]
  Sets the currently visible frame of the texture. Setting this frame while playing resets the current frame time, so the newly selected frame plays for its whole configured frame duration.

- frames: int = 1 [set set_frames; get get_frames]
  Number of frames to use in the animation. While you can create the frames independently with set_frame_texture(), you need to set this value for the animation to take new frames into account. The maximum number of frames is MAX_FRAMES.

- one_shot: bool = false [set set_one_shot; get get_one_shot]
  If true, the animation will only play once and will not loop back to the first frame after reaching the end. Note that reaching the end will not set pause to true.

- pause: bool = false [set set_pause; get get_pause]
  If true, the animation will pause where it currently is (i.e. at current_frame). The animation will continue from where it was paused when changing this property to false.

- resource_local_to_scene: bool = false [set set_local_to_scene; get is_local_to_scene; override Resource]

- speed_scale: float = 1.0 [set set_speed_scale; get get_speed_scale]
  The animation speed is multiplied by this value. If set to a negative value, the animation is played in reverse.

## Constants

- MAX_FRAMES = 256
  The maximum number of frames supported by AnimatedTexture. If you need more frames in your animation, use AnimationPlayer or AnimatedSprite2D.
