# AudioStreamRandomizer

## Meta

- Name: AudioStreamRandomizer
- Source: AudioStreamRandomizer.xml
- Inherits: AudioStream
- Inheritance Chain: AudioStreamRandomizer -> AudioStream -> Resource -> RefCounted -> Object

## Brief Description

Wraps a pool of audio streams with pitch and volume shifting.

## Description

Picks a random AudioStream from the pool, depending on the playback mode, and applies random pitch shifting and volume shifting during playback.

## Quick Reference

```
[methods]
add_stream(index: int, stream: AudioStream, weight: float = 1.0) -> void
get_stream(index: int) -> AudioStream [const]
get_stream_probability_weight(index: int) -> float [const]
move_stream(index_from: int, index_to: int) -> void
remove_stream(index: int) -> void
set_stream(index: int, stream: AudioStream) -> void
set_stream_probability_weight(index: int, weight: float) -> void

[properties]
playback_mode: int (AudioStreamRandomizer.PlaybackMode) = 0
random_pitch: float = 1.0
random_pitch_semitones: float = 0.0
random_volume_offset_db: float = 0.0
streams_count: int = 0
```

## Methods

- add_stream(index: int, stream: AudioStream, weight: float = 1.0) -> void
  Insert a stream at the specified index. If the index is less than zero, the insertion occurs at the end of the underlying pool.

- get_stream(index: int) -> AudioStream [const]
  Returns the stream at the specified index.

- get_stream_probability_weight(index: int) -> float [const]
  Returns the probability weight associated with the stream at the given index.

- move_stream(index_from: int, index_to: int) -> void
  Move a stream from one index to another.

- remove_stream(index: int) -> void
  Remove the stream at the specified index.

- set_stream(index: int, stream: AudioStream) -> void
  Set the AudioStream at the specified index.

- set_stream_probability_weight(index: int, weight: float) -> void
  Set the probability weight of the stream at the specified index. The higher this value, the more likely that the randomizer will choose this stream during random playback modes.

## Properties

- playback_mode: int (AudioStreamRandomizer.PlaybackMode) = 0 [set set_playback_mode; get get_playback_mode]
  Controls how this AudioStreamRandomizer picks which AudioStream to play next.

- random_pitch: float = 1.0 [set set_random_pitch; get get_random_pitch]
  The largest possible frequency multiplier of the random pitch variation. Pitch will be randomly chosen within a range of [code skip-lint]1.0 / random_pitch[/code] and [code skip-lint]random_pitch[/code]. A value of 1.0 means no variation. A value of 2.0 means pitch will be randomized between double and half. **Note:** Setting this property also sets random_pitch_semitones.

- random_pitch_semitones: float = 0.0 [set set_random_pitch_semitones; get get_random_pitch_semitones]
  The largest possible distance, in semitones, of the random pitch variation. A value of 0.0 means no variation. **Note:** Setting this property also sets random_pitch.

- random_volume_offset_db: float = 0.0 [set set_random_volume_offset_db; get get_random_volume_offset_db]
  The intensity of random volume variation. Volume will be increased or decreased by a random value up to [code skip-lint]random_volume_offset_db[/code]. A value of 0.0 means no variation. A value of 3.0 means volume will be randomized between -3.0 dB and +3.0 dB.

- streams_count: int = 0 [set set_streams_count; get get_streams_count]
  The number of streams in the stream pool.

## Constants

### Enum PlaybackMode

- PLAYBACK_RANDOM_NO_REPEATS = 0
  Pick a stream at random according to the probability weights chosen for each stream, but avoid playing the same stream twice in a row whenever possible. If only 1 sound is present in the pool, the same sound will always play, effectively allowing repeats to occur.

- PLAYBACK_RANDOM = 1
  Pick a stream at random according to the probability weights chosen for each stream. If only 1 sound is present in the pool, the same sound will always play.

- PLAYBACK_SEQUENTIAL = 2
  Play streams in the order they appear in the stream pool. If only 1 sound is present in the pool, the same sound will always play.
