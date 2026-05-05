# AudioEffectChorus

## Meta

- Name: AudioEffectChorus
- Source: AudioEffectChorus.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectChorus -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

Adds a chorus audio effect.

## Description

Adds a chorus audio effect. The effect applies a filter with voices to duplicate the audio source and manipulate it through the filter.

## Quick Reference

```
[methods]
get_voice_cutoff_hz(voice_idx: int) -> float [const]
get_voice_delay_ms(voice_idx: int) -> float [const]
get_voice_depth_ms(voice_idx: int) -> float [const]
get_voice_level_db(voice_idx: int) -> float [const]
get_voice_pan(voice_idx: int) -> float [const]
get_voice_rate_hz(voice_idx: int) -> float [const]
set_voice_cutoff_hz(voice_idx: int, cutoff_hz: float) -> void
set_voice_delay_ms(voice_idx: int, delay_ms: float) -> void
set_voice_depth_ms(voice_idx: int, depth_ms: float) -> void
set_voice_level_db(voice_idx: int, level_db: float) -> void
set_voice_pan(voice_idx: int, pan: float) -> void
set_voice_rate_hz(voice_idx: int, rate_hz: float) -> void

[properties]
dry: float = 1.0
voice/1/cutoff_hz: float = 8000.0
voice/1/delay_ms: float = 15.0
voice/1/depth_ms: float = 2.0
voice/1/level_db: float = 0.0
voice/1/pan: float = -0.5
voice/1/rate_hz: float = 0.8
voice/2/cutoff_hz: float = 8000.0
voice/2/delay_ms: float = 20.0
voice/2/depth_ms: float = 3.0
voice/2/level_db: float = 0.0
voice/2/pan: float = 0.5
voice/2/rate_hz: float = 1.2
voice/3/cutoff_hz: float
voice/3/delay_ms: float
voice/3/depth_ms: float
voice/3/level_db: float
voice/3/pan: float
voice/3/rate_hz: float
voice/4/cutoff_hz: float
voice/4/delay_ms: float
voice/4/depth_ms: float
voice/4/level_db: float
voice/4/pan: float
voice/4/rate_hz: float
voice_count: int = 2
wet: float = 0.5
```

## Tutorials

- [Audio buses]($DOCS_URL/tutorials/audio/audio_buses.html)

## Methods

- get_voice_cutoff_hz(voice_idx: int) -> float [const]

- get_voice_delay_ms(voice_idx: int) -> float [const]

- get_voice_depth_ms(voice_idx: int) -> float [const]

- get_voice_level_db(voice_idx: int) -> float [const]

- get_voice_pan(voice_idx: int) -> float [const]

- get_voice_rate_hz(voice_idx: int) -> float [const]

- set_voice_cutoff_hz(voice_idx: int, cutoff_hz: float) -> void

- set_voice_delay_ms(voice_idx: int, delay_ms: float) -> void

- set_voice_depth_ms(voice_idx: int, depth_ms: float) -> void

- set_voice_level_db(voice_idx: int, level_db: float) -> void

- set_voice_pan(voice_idx: int, pan: float) -> void

- set_voice_rate_hz(voice_idx: int, rate_hz: float) -> void

## Properties

- dry: float = 1.0 [set set_dry; get get_dry]
  The effect's raw signal.

- voice/1/cutoff_hz: float = 8000.0 [set set_voice_cutoff_hz; get get_voice_cutoff_hz]
  The voice's cutoff frequency.

- voice/1/delay_ms: float = 15.0 [set set_voice_delay_ms; get get_voice_delay_ms]
  The voice's signal delay.

- voice/1/depth_ms: float = 2.0 [set set_voice_depth_ms; get get_voice_depth_ms]
  The voice filter's depth.

- voice/1/level_db: float = 0.0 [set set_voice_level_db; get get_voice_level_db]
  The voice's volume.

- voice/1/pan: float = -0.5 [set set_voice_pan; get get_voice_pan]
  The voice's pan level.

- voice/1/rate_hz: float = 0.8 [set set_voice_rate_hz; get get_voice_rate_hz]
  The voice's filter rate.

- voice/2/cutoff_hz: float = 8000.0 [set set_voice_cutoff_hz; get get_voice_cutoff_hz]
  The voice's cutoff frequency.

- voice/2/delay_ms: float = 20.0 [set set_voice_delay_ms; get get_voice_delay_ms]
  The voice's signal delay.

- voice/2/depth_ms: float = 3.0 [set set_voice_depth_ms; get get_voice_depth_ms]
  The voice filter's depth.

- voice/2/level_db: float = 0.0 [set set_voice_level_db; get get_voice_level_db]
  The voice's volume.

- voice/2/pan: float = 0.5 [set set_voice_pan; get get_voice_pan]
  The voice's pan level.

- voice/2/rate_hz: float = 1.2 [set set_voice_rate_hz; get get_voice_rate_hz]
  The voice's filter rate.

- voice/3/cutoff_hz: float [set set_voice_cutoff_hz; get get_voice_cutoff_hz]
  The voice's cutoff frequency.

- voice/3/delay_ms: float [set set_voice_delay_ms; get get_voice_delay_ms]
  The voice's signal delay.

- voice/3/depth_ms: float [set set_voice_depth_ms; get get_voice_depth_ms]
  The voice filter's depth.

- voice/3/level_db: float [set set_voice_level_db; get get_voice_level_db]
  The voice's volume.

- voice/3/pan: float [set set_voice_pan; get get_voice_pan]
  The voice's pan level.

- voice/3/rate_hz: float [set set_voice_rate_hz; get get_voice_rate_hz]
  The voice's filter rate.

- voice/4/cutoff_hz: float [set set_voice_cutoff_hz; get get_voice_cutoff_hz]
  The voice's cutoff frequency.

- voice/4/delay_ms: float [set set_voice_delay_ms; get get_voice_delay_ms]
  The voice's signal delay.

- voice/4/depth_ms: float [set set_voice_depth_ms; get get_voice_depth_ms]
  The voice filter's depth.

- voice/4/level_db: float [set set_voice_level_db; get get_voice_level_db]
  The voice's volume.

- voice/4/pan: float [set set_voice_pan; get get_voice_pan]
  The voice's pan level.

- voice/4/rate_hz: float [set set_voice_rate_hz; get get_voice_rate_hz]
  The voice's filter rate.

- voice_count: int = 2 [set set_voice_count; get get_voice_count]
  The number of voices in the effect.

- wet: float = 0.5 [set set_wet; get get_wet]
  The effect's processed signal.
