# VideoStreamPlayer

## Meta

- Name: VideoStreamPlayer
- Source: VideoStreamPlayer.xml
- Inherits: Control
- Inheritance Chain: VideoStreamPlayer -> Control -> CanvasItem -> Node -> Object

## Brief Description

A control used for video playback.

## Description

A control used for playback of VideoStream resources. Supported video formats are [Ogg Theora](https://www.theora.org/) (.ogv, VideoStreamTheora) and any format exposed via a GDExtension plugin. **Warning:** On Web, video playback *will* perform poorly due to missing architecture-specific assembly optimizations.

## Quick Reference

```
[methods]
get_stream_length() -> float [const]
get_stream_name() -> String [const]
get_video_texture() -> Texture2D [const]
is_playing() -> bool [const]
play() -> void
stop() -> void

[properties]
audio_track: int = 0
autoplay: bool = false
buffering_msec: int = 500
bus: StringName = &"Master"
expand: bool = false
loop: bool = false
paused: bool = false
speed_scale: float = 1.0
stream: VideoStream
stream_position: float
volume: float
volume_db: float = 0.0
```

## Tutorials

- [Playing videos]($DOCS_URL/tutorials/animation/playing_videos.html)

## Methods

- get_stream_length() -> float [const]
  The length of the current stream, in seconds.

- get_stream_name() -> String [const]
  Returns the video stream's name, or "<No Stream>" if no video stream is assigned.

- get_video_texture() -> Texture2D [const]
  Returns the current frame as a Texture2D.

- is_playing() -> bool [const]
  Returns true if the video is playing. **Note:** The video is still considered playing if paused during playback.

- play() -> void
  Starts the video playback from the beginning. If the video is paused, this will not unpause the video.

- stop() -> void
  Stops the video playback and sets the stream position to 0. **Note:** Although the stream position will be set to 0, the first frame of the video stream won't become the current frame.

## Properties

- audio_track: int = 0 [set set_audio_track; get get_audio_track]
  The embedded audio track to play.

- autoplay: bool = false [set set_autoplay; get has_autoplay]
  If true, playback starts when the scene loads.

- buffering_msec: int = 500 [set set_buffering_msec; get get_buffering_msec]
  Amount of time in milliseconds to store in buffer while playing.

- bus: StringName = &"Master" [set set_bus; get get_bus]
  Audio bus to use for sound playback.

- expand: bool = false [set set_expand; get has_expand]
  If true, the video scales to the control size. Otherwise, the control minimum size will be automatically adjusted to match the video stream's dimensions.

- loop: bool = false [set set_loop; get has_loop]
  If true, the video restarts when it reaches its end.

- paused: bool = false [set set_paused; get is_paused]
  If true, the video is paused.

- speed_scale: float = 1.0 [set set_speed_scale; get get_speed_scale]
  The stream's current speed scale. 1.0 is the normal speed, while 2.0 is double speed and 0.5 is half speed. A speed scale of 0.0 pauses the video, similar to setting paused to true.

- stream: VideoStream [set set_stream; get get_stream]
  The assigned video stream. See description for supported formats.

- stream_position: float [set set_stream_position; get get_stream_position]
  The current position of the stream, in seconds.

- volume: float [set set_volume; get get_volume]
  Audio volume as a linear value.

- volume_db: float = 0.0 [set set_volume_db; get get_volume_db]
  Audio volume in dB.

## Signals

- finished()
  Emitted when playback is finished.
