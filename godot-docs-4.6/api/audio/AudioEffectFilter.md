# AudioEffectFilter

## Meta

- Name: AudioEffectFilter
- Source: AudioEffectFilter.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectFilter -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

Adds a filter to the audio bus.

## Description

Allows frequencies other than the cutoff_hz to pass.

## Quick Reference

```
[properties]
cutoff_hz: float = 2000.0
db: int (AudioEffectFilter.FilterDB) = 0
gain: float = 1.0
resonance: float = 0.5
```

## Tutorials

- [Audio buses]($DOCS_URL/tutorials/audio/audio_buses.html)

## Properties

- cutoff_hz: float = 2000.0 [set set_cutoff; get get_cutoff]
  Threshold frequency for the filter, in Hz.

- db: int (AudioEffectFilter.FilterDB) = 0 [set set_db; get get_db]
  Steepness of the cutoff curve in dB per octave, also known as the order of the filter. Higher orders have a more aggressive cutoff.

- gain: float = 1.0 [set set_gain; get get_gain]
  Gain amount of the frequencies after the filter.

- resonance: float = 0.5 [set set_resonance; get get_resonance]
  Amount of boost in the frequency range near the cutoff frequency.

## Constants

### Enum FilterDB

- FILTER_6DB = 0
  Cutting off at 6dB per octave.

- FILTER_12DB = 1
  Cutting off at 12dB per octave.

- FILTER_18DB = 2
  Cutting off at 18dB per octave.

- FILTER_24DB = 3
  Cutting off at 24dB per octave.
