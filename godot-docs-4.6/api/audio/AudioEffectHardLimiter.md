# AudioEffectHardLimiter

## Meta

- Name: AudioEffectHardLimiter
- Source: AudioEffectHardLimiter.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectHardLimiter -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

Adds a hard limiter audio effect to an Audio bus.

## Description

A limiter is an effect designed to disallow sound from going over a given dB threshold. Hard limiters predict volume peaks, and will smoothly apply gain reduction when a peak crosses the ceiling threshold to prevent clipping and distortion. It preserves the waveform and prevents it from crossing the ceiling threshold. Adding one in the Master bus is recommended as a safety measure to prevent sudden volume peaks from occurring, and to prevent distortion caused by clipping.

## Quick Reference

```
[properties]
ceiling_db: float = -0.3
pre_gain_db: float = 0.0
release: float = 0.1
```

## Tutorials

- [Audio buses]($DOCS_URL/tutorials/audio/audio_buses.html)

## Properties

- ceiling_db: float = -0.3 [set set_ceiling_db; get get_ceiling_db]
  The waveform's maximum allowed value, in decibels. This value can range from -24.0 to 0.0. The default value of -0.3 prevents potential inter-sample peaks (ISP) from crossing over 0 dB, which can cause slight distortion on some older hardware.

- pre_gain_db: float = 0.0 [set set_pre_gain_db; get get_pre_gain_db]
  Gain to apply before limiting, in decibels.

- release: float = 0.1 [set set_release; get get_release]
  Time it takes in seconds for the gain reduction to fully release.
