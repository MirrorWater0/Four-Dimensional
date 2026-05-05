# AudioStreamPlaybackResampled

## Meta

- Name: AudioStreamPlaybackResampled
- Source: AudioStreamPlaybackResampled.xml
- Inherits: AudioStreamPlayback
- Inheritance Chain: AudioStreamPlaybackResampled -> AudioStreamPlayback -> RefCounted -> Object

## Quick Reference

```
[methods]
_get_stream_sampling_rate() -> float [virtual required const]
_mix_resampled(dst_buffer: AudioFrame*, frame_count: int) -> int [virtual required]
begin_resample() -> void
```

## Methods

- _get_stream_sampling_rate() -> float [virtual required const]

- _mix_resampled(dst_buffer: AudioFrame*, frame_count: int) -> int [virtual required]

- begin_resample() -> void
