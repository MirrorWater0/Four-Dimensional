# AudioEffectStereoEnhance

## Meta

- Name: AudioEffectStereoEnhance
- Source: AudioEffectStereoEnhance.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectStereoEnhance -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

An audio effect that can be used to adjust the intensity of stereo panning.

## Description

An audio effect that can be used to adjust the intensity of stereo panning.

## Quick Reference

```
[properties]
pan_pullout: float = 1.0
surround: float = 0.0
time_pullout_ms: float = 0.0
```

## Tutorials

- [Audio buses]($DOCS_URL/tutorials/audio/audio_buses.html)

## Properties

- pan_pullout: float = 1.0 [set set_pan_pullout; get get_pan_pullout]
  Amplifies the difference between stereo channels, increasing or decreasing existing panning. A value of 0.0 will downmix stereo to mono. Does not affect a mono signal.

- surround: float = 0.0 [set set_surround; get get_surround]
  Widens sound stage through phase shifting in conjunction with time_pullout_ms. Just pans sound to the left channel if time_pullout_ms is 0.

- time_pullout_ms: float = 0.0 [set set_time_pullout; get get_time_pullout]
  Widens sound stage through phase shifting in conjunction with surround. Just delays the right channel if surround is 0.
