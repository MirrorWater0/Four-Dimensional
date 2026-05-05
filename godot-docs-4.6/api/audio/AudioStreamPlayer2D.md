# AudioStreamPlayer2D

## Meta

- Name: AudioStreamPlayer2D
- Source: AudioStreamPlayer2D.xml
- Inherits: Node2D
- Inheritance Chain: AudioStreamPlayer2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

Plays positional sound in 2D space.

## Description

Plays audio that is attenuated with distance to the listener. By default, audio is heard from the screen center. This can be changed by adding an AudioListener2D node to the scene and enabling it by calling AudioListener2D.make_current() on it. See also AudioStreamPlayer to play a sound non-positionally. **Note:** Hiding an AudioStreamPlayer2D node does not disable its audio output. To temporarily disable an AudioStreamPlayer2D's audio output, set volume_db to a very low value like -100 (which isn't audible to human hearing).

## Quick Reference

```
[methods]
get_playback_position() -> float
get_stream_playback() -> AudioStreamPlayback
has_stream_playback() -> bool
play(from_position: float = 0.0) -> void
seek(to_position: float) -> void
stop() -> void

[properties]
area_mask: int = 1
attenuation: float = 1.0
autoplay: bool = false
bus: StringName = &"Master"
max_distance: float = 2000.0
max_polyphony: int = 1
panning_strength: float = 1.0
pitch_scale: float = 1.0
playback_type: int (AudioServer.PlaybackType) = 0
playing: bool = false
stream: AudioStream
stream_paused: bool = false
volume_db: float = 0.0
volume_linear: float
```

## Tutorials

- [Audio streams]($DOCS_URL/tutorials/audio/audio_streams.html)

## Methods

- get_playback_position() -> float
  Returns the position in the AudioStream.

- get_stream_playback() -> AudioStreamPlayback
  Returns the AudioStreamPlayback object associated with this AudioStreamPlayer2D.

- has_stream_playback() -> bool
  Returns whether the AudioStreamPlayer can return the AudioStreamPlayback object or not.

- play(from_position: float = 0.0) -> void
  Queues the audio to play on the next physics frame, from the given position from_position, in seconds.

- seek(to_position: float) -> void
  Sets the position from which audio will be played, in seconds.

- stop() -> void
  Stops the audio.

## Properties

- area_mask: int = 1 [set set_area_mask; get get_area_mask]
  Determines which Area2D layers affect the sound for reverb and audio bus effects. Areas can be used to redirect AudioStreams so that they play in a certain audio bus. An example of how you might use this is making a "water" area so that sounds played in the water are redirected through an audio bus to make them sound like they are being played underwater.

- attenuation: float = 1.0 [set set_attenuation; get get_attenuation]
  The volume is attenuated over distance with this as an exponent.

- autoplay: bool = false [set set_autoplay; get is_autoplay_enabled]
  If true, audio plays when added to scene tree.

- bus: StringName = &"Master" [set set_bus; get get_bus]
  Bus on which this audio is playing. **Note:** When setting this property, keep in mind that no validation is performed to see if the given name matches an existing bus. This is because audio bus layouts might be loaded after this property is set. If this given name can't be resolved at runtime, it will fall back to "Master".

- max_distance: float = 2000.0 [set set_max_distance; get get_max_distance]
  Maximum distance from which audio is still hearable.

- max_polyphony: int = 1 [set set_max_polyphony; get get_max_polyphony]
  The maximum number of sounds this node can play at the same time. Playing additional sounds after this value is reached will cut off the oldest sounds.

- panning_strength: float = 1.0 [set set_panning_strength; get get_panning_strength]
  Scales the panning strength for this node by multiplying the base ProjectSettings.audio/general/2d_panning_strength with this factor. Higher values will pan audio from left to right more dramatically than lower values.

- pitch_scale: float = 1.0 [set set_pitch_scale; get get_pitch_scale]
  The pitch and the tempo of the audio, as a multiplier of the audio sample's sample rate.

- playback_type: int (AudioServer.PlaybackType) = 0 [set set_playback_type; get get_playback_type]
  The playback type of the stream player. If set other than to the default value, it will force that playback type.

- playing: bool = false [set set_playing; get is_playing]
  If true, audio is playing or is queued to be played (see play()).

- stream: AudioStream [set set_stream; get get_stream]
  The AudioStream object to be played.

- stream_paused: bool = false [set set_stream_paused; get get_stream_paused]
  If true, the playback is paused. You can resume it by setting stream_paused to false.

- volume_db: float = 0.0 [set set_volume_db; get get_volume_db]
  Base volume before attenuation, in decibels.

- volume_linear: float [set set_volume_linear; get get_volume_linear]
  Base volume before attenuation, as a linear value. **Note:** This member modifies volume_db for convenience. The returned value is equivalent to the result of @GlobalScope.db_to_linear() on volume_db. Setting this member is equivalent to setting volume_db to the result of @GlobalScope.linear_to_db() on a value.

## Signals

- finished()
  Emitted when the audio stops playing.
