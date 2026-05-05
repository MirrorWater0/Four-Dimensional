# AudioEffectDistortion

## Meta

- Name: AudioEffectDistortion
- Source: AudioEffectDistortion.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectDistortion -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

Adds a distortion audio effect to an Audio bus. Modifies the sound to make it distorted.

## Description

Different types are available: clip, tan, lo-fi (bit crushing), overdrive, or waveshape. By distorting the waveform the frequency content changes, which will often make the sound "crunchy" or "abrasive". For games, it can simulate sound coming from some saturated device or speaker very efficiently.

## Quick Reference

```
[properties]
drive: float = 0.0
keep_hf_hz: float = 16000.0
mode: int (AudioEffectDistortion.Mode) = 0
post_gain: float = 0.0
pre_gain: float = 0.0
```

## Tutorials

- [Audio buses]($DOCS_URL/tutorials/audio/audio_buses.html)

## Properties

- drive: float = 0.0 [set set_drive; get get_drive]
  Distortion power. Value can range from 0 to 1.

- keep_hf_hz: float = 16000.0 [set set_keep_hf_hz; get get_keep_hf_hz]
  High-pass filter, in Hz. Frequencies higher than this value will not be affected by the distortion. Value can range from 1 to 20000.

- mode: int (AudioEffectDistortion.Mode) = 0 [set set_mode; get get_mode]
  Distortion type.

- post_gain: float = 0.0 [set set_post_gain; get get_post_gain]
  Increases or decreases the volume after the effect, in decibels. Value can range from -80 to 24.

- pre_gain: float = 0.0 [set set_pre_gain; get get_pre_gain]
  Increases or decreases the volume before the effect, in decibels. Value can range from -60 to 60.

## Constants

### Enum Mode

- MODE_CLIP = 0
  Digital distortion effect which cuts off peaks at the top and bottom of the waveform.

- MODE_ATAN = 1

- MODE_LOFI = 2
  Low-resolution digital distortion effect (bit depth reduction). You can use it to emulate the sound of early digital audio devices.

- MODE_OVERDRIVE = 3
  Emulates the warm distortion produced by a field effect transistor, which is commonly used in solid-state musical instrument amplifiers. The drive property has no effect in this mode.

- MODE_WAVESHAPE = 4
  Waveshaper distortions are used mainly by electronic musicians to achieve an extra-abrasive sound.
