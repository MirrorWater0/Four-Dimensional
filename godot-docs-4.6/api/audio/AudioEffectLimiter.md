# AudioEffectLimiter

## Meta

- Name: AudioEffectLimiter
- Source: AudioEffectLimiter.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectLimiter -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

Adds a soft-clip limiter audio effect to an Audio bus.

## Description

A limiter is similar to a compressor, but it's less flexible and designed to disallow sound going over a given dB threshold. Adding one in the Master bus is always recommended to reduce the effects of clipping. Soft clipping starts to reduce the peaks a little below the threshold level and progressively increases its effect as the input level increases such that the threshold is never exceeded.

## Quick Reference

```
[properties]
ceiling_db: float = -0.1
soft_clip_db: float = 2.0
soft_clip_ratio: float = 10.0
threshold_db: float = 0.0
```

## Tutorials

- [Audio buses]($DOCS_URL/tutorials/audio/audio_buses.html)

## Properties

- ceiling_db: float = -0.1 [set set_ceiling_db; get get_ceiling_db]
  The waveform's maximum allowed value, in decibels. Value can range from -20 to -0.1.

- soft_clip_db: float = 2.0 [set set_soft_clip_db; get get_soft_clip_db]
  Applies a gain to the limited waves, in decibels. Value can range from 0 to 6.

- soft_clip_ratio: float = 10.0 [set set_soft_clip_ratio; get get_soft_clip_ratio]

- threshold_db: float = 0.0 [set set_threshold_db; get get_threshold_db]
  Threshold from which the limiter begins to be active, in decibels. Value can range from -30 to 0.
