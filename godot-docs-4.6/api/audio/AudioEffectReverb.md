# AudioEffectReverb

## Meta

- Name: AudioEffectReverb
- Source: AudioEffectReverb.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectReverb -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

Adds a reverberation audio effect to an Audio bus.

## Description

Simulates the sound of acoustic environments such as rooms, concert halls, caverns, or an open spaces.

## Quick Reference

```
[properties]
damping: float = 0.5
dry: float = 1.0
hipass: float = 0.0
predelay_feedback: float = 0.4
predelay_msec: float = 150.0
room_size: float = 0.8
spread: float = 1.0
wet: float = 0.5
```

## Tutorials

- [Audio buses]($DOCS_URL/tutorials/audio/audio_buses.html)
- [Third Person Shooter (TPS) Demo](https://godotengine.org/asset-library/asset/2710)

## Properties

- damping: float = 0.5 [set set_damping; get get_damping]
  Defines how reflective the imaginary room's walls are. Value can range from 0 to 1.

- dry: float = 1.0 [set set_dry; get get_dry]
  Output percent of original sound. At 0, only modified sound is outputted. Value can range from 0 to 1.

- hipass: float = 0.0 [set set_hpf; get get_hpf]
  High-pass filter passes signals with a frequency higher than a certain cutoff frequency and attenuates signals with frequencies lower than the cutoff frequency. Value can range from 0 to 1.

- predelay_feedback: float = 0.4 [set set_predelay_feedback; get get_predelay_feedback]
  Output percent of predelay. Value can range from 0 to 1.

- predelay_msec: float = 150.0 [set set_predelay_msec; get get_predelay_msec]
  Time between the original signal and the early reflections of the reverb signal, in milliseconds.

- room_size: float = 0.8 [set set_room_size; get get_room_size]
  Dimensions of simulated room. Bigger means more echoes. Value can range from 0 to 1.

- spread: float = 1.0 [set set_spread; get get_spread]
  Widens or narrows the stereo image of the reverb tail. 1 means fully widens. Value can range from 0 to 1.

- wet: float = 0.5 [set set_wet; get get_wet]
  Output percent of modified sound. At 0, only original sound is outputted. Value can range from 0 to 1.
