# AudioEffectAmplify

## Meta

- Name: AudioEffectAmplify
- Source: AudioEffectAmplify.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectAmplify -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

Adds an amplifying audio effect to an audio bus.

## Description

Increases or decreases the volume being routed through the audio bus.

## Quick Reference

```
[properties]
volume_db: float = 0.0
volume_linear: float
```

## Tutorials

- [Audio buses]($DOCS_URL/tutorials/audio/audio_buses.html)

## Properties

- volume_db: float = 0.0 [set set_volume_db; get get_volume_db]
  Amount of amplification in decibels. Positive values make the sound louder, negative values make it quieter. Value can range from -80 to 24.

- volume_linear: float [set set_volume_linear; get get_volume_linear]
  Amount of amplification as a linear value. **Note:** This member modifies volume_db for convenience. The returned value is equivalent to the result of @GlobalScope.db_to_linear() on volume_db. Setting this member is equivalent to setting volume_db to the result of @GlobalScope.linear_to_db() on a value.
