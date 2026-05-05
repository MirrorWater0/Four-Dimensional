# AudioStreamPlaybackPolyphonic

## Meta

- Name: AudioStreamPlaybackPolyphonic
- Source: AudioStreamPlaybackPolyphonic.xml
- Inherits: AudioStreamPlayback
- Inheritance Chain: AudioStreamPlaybackPolyphonic -> AudioStreamPlayback -> RefCounted -> Object

## Brief Description

Playback instance for AudioStreamPolyphonic.

## Description

Playback instance for AudioStreamPolyphonic. After setting the stream property of AudioStreamPlayer, AudioStreamPlayer2D, or AudioStreamPlayer3D, the playback instance can be obtained by calling AudioStreamPlayer.get_stream_playback(), AudioStreamPlayer2D.get_stream_playback() or AudioStreamPlayer3D.get_stream_playback() methods.

## Quick Reference

```
[methods]
is_stream_playing(stream: int) -> bool [const]
play_stream(stream: AudioStream, from_offset: float = 0, volume_db: float = 0, pitch_scale: float = 1.0, playback_type: int (AudioServer.PlaybackType) = 0, bus: StringName = &"Master") -> int
set_stream_pitch_scale(stream: int, pitch_scale: float) -> void
set_stream_volume(stream: int, volume_db: float) -> void
stop_stream(stream: int) -> void
```

## Methods

- is_stream_playing(stream: int) -> bool [const]
  Returns true if the stream associated with the given integer ID is still playing. Check play_stream() for information on when this ID becomes invalid.

- play_stream(stream: AudioStream, from_offset: float = 0, volume_db: float = 0, pitch_scale: float = 1.0, playback_type: int (AudioServer.PlaybackType) = 0, bus: StringName = &"Master") -> int
  Play an AudioStream at a given offset, volume, pitch scale, playback type, and bus. Playback starts immediately. The return value is a unique integer ID that is associated to this playback stream and which can be used to control it. This ID becomes invalid when the stream ends (if it does not loop), when the AudioStreamPlaybackPolyphonic is stopped, or when stop_stream() is called. This function returns INVALID_ID if the amount of streams currently playing equals AudioStreamPolyphonic.polyphony. If you need a higher amount of maximum polyphony, raise this value.

- set_stream_pitch_scale(stream: int, pitch_scale: float) -> void
  Change the stream pitch scale. The stream argument is an integer ID returned by play_stream().

- set_stream_volume(stream: int, volume_db: float) -> void
  Change the stream volume (in db). The stream argument is an integer ID returned by play_stream().

- stop_stream(stream: int) -> void
  Stop a stream. The stream argument is an integer ID returned by play_stream(), which becomes invalid after calling this function.

## Constants

- INVALID_ID = -1
  Returned by play_stream() in case it could not allocate a stream for playback.
