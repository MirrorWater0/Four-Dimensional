# AudioEffectCompressor

## Meta

- Name: AudioEffectCompressor
- Source: AudioEffectCompressor.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectCompressor -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

Adds a compressor audio effect to an audio bus. Reduces sounds that exceed a certain threshold level, smooths out the dynamics and increases the overall volume.

## Description

Dynamic range compressor reduces the level of the sound when the amplitude goes over a certain threshold in Decibels. One of the main uses of a compressor is to increase the dynamic range by clipping as little as possible (when sound goes over 0dB). Compressor has many uses in the mix: - In the Master bus to compress the whole output (although an AudioEffectHardLimiter is probably better). - In voice channels to ensure they sound as balanced as possible. - Sidechained. This can reduce the sound level sidechained with another audio bus for threshold detection. This technique is common in video game mixing to the level of music and SFX while voices are being heard. - Accentuates transients by using a wider attack, making effects sound more punchy.

## Quick Reference

```
[properties]
attack_us: float = 20.0
gain: float = 0.0
mix: float = 1.0
ratio: float = 4.0
release_ms: float = 250.0
sidechain: StringName = &""
threshold: float = 0.0
```

## Tutorials

- [Audio buses]($DOCS_URL/tutorials/audio/audio_buses.html)

## Properties

- attack_us: float = 20.0 [set set_attack_us; get get_attack_us]
  Compressor's reaction time when the signal exceeds the threshold, in microseconds. Value can range from 20 to 2000.

- gain: float = 0.0 [set set_gain; get get_gain]
  Gain applied to the output signal.

- mix: float = 1.0 [set set_mix; get get_mix]
  Balance between original signal and effect signal. Value can range from 0 (totally dry) to 1 (totally wet).

- ratio: float = 4.0 [set set_ratio; get get_ratio]
  Amount of compression applied to the audio once it passes the threshold level. The higher the ratio, the more the loud parts of the audio will be compressed. Value can range from 1 to 48.

- release_ms: float = 250.0 [set set_release_ms; get get_release_ms]
  Compressor's delay time to stop reducing the signal after the signal level falls below the threshold, in milliseconds. Value can range from 20 to 2000.

- sidechain: StringName = &"" [set set_sidechain; get get_sidechain]
  Reduce the sound level using another audio bus for threshold detection.

- threshold: float = 0.0 [set set_threshold; get get_threshold]
  The level above which compression is applied to the audio. Value can range from -60 to 0.
