# AudioEffectPanner

## Meta

- Name: AudioEffectPanner
- Source: AudioEffectPanner.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectPanner -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

Adds a panner audio effect to an audio bus. Pans sound left or right.

## Description

Determines how much of an audio signal is sent to the left and right buses.

## Quick Reference

```
[properties]
pan: float = 0.0
```

## Tutorials

- [Audio buses]($DOCS_URL/tutorials/audio/audio_buses.html)

## Properties

- pan: float = 0.0 [set set_pan; get get_pan]
  Pan position. Value can range from -1 (fully left) to 1 (fully right).
