# AudioEffectDelay

## Meta

- Name: AudioEffectDelay
- Source: AudioEffectDelay.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectDelay -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

Adds a delay audio effect to an audio bus. Plays input signal back after a period of time. Two tap delay and feedback options.

## Description

Plays input signal back after a period of time. The delayed signal may be played back multiple times to create the sound of a repeating, decaying echo. Delay effects range from a subtle echo effect to a pronounced blending of previous sounds with new sounds.

## Quick Reference

```
[properties]
dry: float = 1.0
feedback_active: bool = false
feedback_delay_ms: float = 340.0
feedback_level_db: float = -6.0
feedback_lowpass: float = 16000.0
tap1_active: bool = true
tap1_delay_ms: float = 250.0
tap1_level_db: float = -6.0
tap1_pan: float = 0.2
tap2_active: bool = true
tap2_delay_ms: float = 500.0
tap2_level_db: float = -12.0
tap2_pan: float = -0.4
```

## Tutorials

- [Audio buses]($DOCS_URL/tutorials/audio/audio_buses.html)

## Properties

- dry: float = 1.0 [set set_dry; get get_dry]
  Output percent of original sound. At 0, only delayed sounds are output. Value can range from 0 to 1.

- feedback_active: bool = false [set set_feedback_active; get is_feedback_active]
  If true, feedback is enabled.

- feedback_delay_ms: float = 340.0 [set set_feedback_delay_ms; get get_feedback_delay_ms]
  Feedback delay time in milliseconds.

- feedback_level_db: float = -6.0 [set set_feedback_level_db; get get_feedback_level_db]
  Sound level for feedback.

- feedback_lowpass: float = 16000.0 [set set_feedback_lowpass; get get_feedback_lowpass]
  Low-pass filter for feedback, in Hz. Frequencies below this value are filtered out of the source signal.

- tap1_active: bool = true [set set_tap1_active; get is_tap1_active]
  If true, the first tap will be enabled.

- tap1_delay_ms: float = 250.0 [set set_tap1_delay_ms; get get_tap1_delay_ms]
  First tap delay time in milliseconds.

- tap1_level_db: float = -6.0 [set set_tap1_level_db; get get_tap1_level_db]
  Sound level for the first tap.

- tap1_pan: float = 0.2 [set set_tap1_pan; get get_tap1_pan]
  Pan position for the first tap. Value can range from -1 (fully left) to 1 (fully right).

- tap2_active: bool = true [set set_tap2_active; get is_tap2_active]
  If true, the second tap will be enabled.

- tap2_delay_ms: float = 500.0 [set set_tap2_delay_ms; get get_tap2_delay_ms]
  Second tap delay time in milliseconds.

- tap2_level_db: float = -12.0 [set set_tap2_level_db; get get_tap2_level_db]
  Sound level for the second tap.

- tap2_pan: float = -0.4 [set set_tap2_pan; get get_tap2_pan]
  Pan position for the second tap. Value can range from -1 (fully left) to 1 (fully right).
