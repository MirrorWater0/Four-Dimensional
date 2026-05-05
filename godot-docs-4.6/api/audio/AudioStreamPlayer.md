# AudioStreamPlayer

## Meta

- Name: AudioStreamPlayer
- Source: AudioStreamPlayer.xml
- Inherits: Node
- Inheritance Chain: AudioStreamPlayer -> Node -> Object

## Brief Description

A node for audio playback.

## Description

The AudioStreamPlayer node plays an audio stream non-positionally. It is ideal for user interfaces, menus, or background music. To use this node, stream needs to be set to a valid AudioStream resource. Playing more than one sound at the same time is also supported, see max_polyphony. If you need to play audio at a specific position, use AudioStreamPlayer2D or AudioStreamPlayer3D instead.

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
autoplay: bool = false
bus: StringName = &"Master"
max_polyphony: int = 1
mix_target: int (AudioStreamPlayer.MixTarget) = 0
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
- [2D Dodge The Creeps Demo](https://godotengine.org/asset-library/asset/2712)
- [Audio Device Changer Demo](https://godotengine.org/asset-library/asset/2758)
- [Audio Generator Demo](https://godotengine.org/asset-library/asset/2759)
- [Audio Microphone Record Demo](https://godotengine.org/asset-library/asset/2760)
- [Audio Spectrum Visualizer Demo](https://godotengine.org/asset-library/asset/2762)

## Methods

- get_playback_position() -> float
  Returns the position in the AudioStream of the latest sound, in seconds. Returns 0.0 if no sounds are playing. **Note:** The position is not always accurate, as the AudioServer does not mix audio every processed frame. To get more accurate results, add AudioServer.get_time_since_last_mix() to the returned position. **Note:** This method always returns 0.0 if the stream is an AudioStreamInteractive, since it can have multiple clips playing at once.

- get_stream_playback() -> AudioStreamPlayback
  Returns the latest AudioStreamPlayback of this node, usually the most recently created by play(). If no sounds are playing, this method fails and returns an empty playback.

- has_stream_playback() -> bool
  Returns true if any sound is active, even if stream_paused is set to true. See also playing and get_stream_playback().

- play(from_position: float = 0.0) -> void
  Plays a sound from the beginning, or the given from_position in seconds.

- seek(to_position: float) -> void
  Restarts all sounds to be played from the given to_position, in seconds. Does nothing if no sounds are playing.

- stop() -> void
  Stops all sounds from this node.

## Properties

- autoplay: bool = false [set set_autoplay; get is_autoplay_enabled]
  If true, this node calls play() when entering the tree.

- bus: StringName = &"Master" [set set_bus; get get_bus]
  The target bus name. All sounds from this node will be playing on this bus. **Note:** At runtime, if no bus with the given name exists, all sounds will fall back on "Master". See also AudioServer.get_bus_name().

- max_polyphony: int = 1 [set set_max_polyphony; get get_max_polyphony]
  The maximum number of sounds this node can play at the same time. Calling play() after this value is reached will cut off the oldest sounds.

- mix_target: int (AudioStreamPlayer.MixTarget) = 0 [set set_mix_target; get get_mix_target]
  The mix target channels. Has no effect when two speakers or less are detected (see AudioServer.SpeakerMode).

- pitch_scale: float = 1.0 [set set_pitch_scale; get get_pitch_scale]
  The audio's pitch and tempo, as a multiplier of the stream's sample rate. A value of 2.0 doubles the audio's pitch, while a value of 0.5 halves the pitch.

- playback_type: int (AudioServer.PlaybackType) = 0 [set set_playback_type; get get_playback_type]
  The playback type of the stream player. If set other than to the default value, it will force that playback type.

- playing: bool = false [set set_playing; get is_playing]
  If true, this node is playing sounds. Setting this property has the same effect as play() and stop().

- stream: AudioStream [set set_stream; get get_stream]
  The AudioStream resource to be played. Setting this property stops all currently playing sounds. If left empty, the AudioStreamPlayer does not work.

- stream_paused: bool = false [set set_stream_paused; get get_stream_paused]
  If true, the sounds are paused. Setting stream_paused to false resumes all sounds. **Note:** This property is automatically changed when exiting or entering the tree, or this node is paused (see Node.process_mode).

- volume_db: float = 0.0 [set set_volume_db; get get_volume_db]
  Volume of sound, in decibels. This is an offset of the stream's volume. **Note:** To convert between decibel and linear energy (like most volume sliders do), use volume_linear, or @GlobalScope.db_to_linear() and @GlobalScope.linear_to_db().

- volume_linear: float [set set_volume_linear; get get_volume_linear]
  Volume of sound, as a linear value. **Note:** This member modifies volume_db for convenience. The returned value is equivalent to the result of @GlobalScope.db_to_linear() on volume_db. Setting this member is equivalent to setting volume_db to the result of @GlobalScope.linear_to_db() on a value.

## Signals

- finished()
  Emitted when a sound finishes playing without interruptions. This signal is *not* emitted when calling stop(), or when exiting the tree while sounds are playing.

## Constants

### Enum MixTarget

- MIX_TARGET_STEREO = 0
  The audio will be played only on the first channel. This is the default.

- MIX_TARGET_SURROUND = 1
  The audio will be played on all surround channels.

- MIX_TARGET_CENTER = 2
  The audio will be played on the second channel, which is usually the center.
