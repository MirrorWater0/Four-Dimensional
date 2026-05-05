# AudioStreamPlayback

## Meta

- Name: AudioStreamPlayback
- Source: AudioStreamPlayback.xml
- Inherits: RefCounted
- Inheritance Chain: AudioStreamPlayback -> RefCounted -> Object

## Brief Description

Meta class for playing back audio.

## Description

Can play, loop, pause a scroll through audio. See AudioStream and AudioStreamOggVorbis for usage.

## Quick Reference

```
[methods]
_get_loop_count() -> int [virtual const]
_get_parameter(name: StringName) -> Variant [virtual const]
_get_playback_position() -> float [virtual required const]
_is_playing() -> bool [virtual required const]
_mix(buffer: AudioFrame*, rate_scale: float, frames: int) -> int [virtual required]
_seek(position: float) -> void [virtual]
_set_parameter(name: StringName, value: Variant) -> void [virtual]
_start(from_pos: float) -> void [virtual required]
_stop() -> void [virtual required]
_tag_used_streams() -> void [virtual]
get_loop_count() -> int [const]
get_playback_position() -> float [const]
get_sample_playback() -> AudioSamplePlayback [const]
is_playing() -> bool [const]
mix_audio(rate_scale: float, frames: int) -> PackedVector2Array
seek(time: float = 0.0) -> void
set_sample_playback(playback_sample: AudioSamplePlayback) -> void
start(from_pos: float = 0.0) -> void
stop() -> void
```

## Tutorials

- [Audio Generator Demo](https://godotengine.org/asset-library/asset/2759)

## Methods

- _get_loop_count() -> int [virtual const]
  Overridable method. Should return how many times this audio stream has looped. Most built-in playbacks always return 0.

- _get_parameter(name: StringName) -> Variant [virtual const]
  Return the current value of a playback parameter by name (see AudioStream._get_parameter_list()).

- _get_playback_position() -> float [virtual required const]
  Overridable method. Should return the current progress along the audio stream, in seconds.

- _is_playing() -> bool [virtual required const]
  Overridable method. Should return true if this playback is active and playing its audio stream.

- _mix(buffer: AudioFrame*, rate_scale: float, frames: int) -> int [virtual required]
  Override this method to customize how the audio stream is mixed. This method is called even if the playback is not active. **Note:** It is not useful to override this method in GDScript or C#. Only GDExtension can take advantage of it.

- _seek(position: float) -> void [virtual]
  Override this method to customize what happens when seeking this audio stream at the given position, such as by calling AudioStreamPlayer.seek().

- _set_parameter(name: StringName, value: Variant) -> void [virtual]
  Set the current value of a playback parameter by name (see AudioStream._get_parameter_list()).

- _start(from_pos: float) -> void [virtual required]
  Override this method to customize what happens when the playback starts at the given position, such as by calling AudioStreamPlayer.play().

- _stop() -> void [virtual required]
  Override this method to customize what happens when the playback is stopped, such as by calling AudioStreamPlayer.stop().

- _tag_used_streams() -> void [virtual]
  Overridable method. Called whenever the audio stream is mixed if the playback is active and AudioServer.set_enable_tagging_used_audio_streams() has been set to true. Editor plugins may use this method to "tag" the current position along the audio stream and display it in a preview.

- get_loop_count() -> int [const]
  Returns the number of times the stream has looped.

- get_playback_position() -> float [const]
  Returns the current position in the stream, in seconds.

- get_sample_playback() -> AudioSamplePlayback [const]
  Returns the AudioSamplePlayback associated with this AudioStreamPlayback for playing back the audio sample of this stream.

- is_playing() -> bool [const]
  Returns true if the stream is playing.

- mix_audio(rate_scale: float, frames: int) -> PackedVector2Array
  Mixes up to frames of audio from the stream from the current position, at a rate of rate_scale, advancing the stream. Returns a PackedVector2Array where each element holds the left and right channel volume levels of each frame. **Note:** Can return fewer frames than requested, make sure to use the size of the return value.

- seek(time: float = 0.0) -> void
  Seeks the stream at the given time, in seconds.

- set_sample_playback(playback_sample: AudioSamplePlayback) -> void
  Associates AudioSamplePlayback to this AudioStreamPlayback for playing back the audio sample of this stream.

- start(from_pos: float = 0.0) -> void
  Starts the stream from the given from_pos, in seconds.

- stop() -> void
  Stops the stream.
