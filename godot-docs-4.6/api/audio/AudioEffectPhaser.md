# AudioEffectPhaser

## Meta

- Name: AudioEffectPhaser
- Source: AudioEffectPhaser.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectPhaser -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

Adds a phaser audio effect to an audio bus. Combines the original signal with a copy that is slightly out of phase with the original.

## Description

Combines phase-shifted signals with the original signal. The movement of the phase-shifted signals is controlled using a low-frequency oscillator.

## Quick Reference

```
[properties]
depth: float = 1.0
feedback: float = 0.7
range_max_hz: float = 1600.0
range_min_hz: float = 440.0
rate_hz: float = 0.5
```

## Tutorials

- [Audio buses]($DOCS_URL/tutorials/audio/audio_buses.html)

## Properties

- depth: float = 1.0 [set set_depth; get get_depth]
  Determines how high the filter frequencies sweep. Low value will primarily affect bass frequencies. High value can sweep high into the treble. Value can range from 0.1 to 4.0.

- feedback: float = 0.7 [set set_feedback; get get_feedback]
  Output percent of modified sound. Value can range from 0.1 to 0.9.

- range_max_hz: float = 1600.0 [set set_range_max_hz; get get_range_max_hz]
  Determines the maximum frequency affected by the LFO modulations, in Hz. Value can range from 10 to 10000.

- range_min_hz: float = 440.0 [set set_range_min_hz; get get_range_min_hz]
  Determines the minimum frequency affected by the LFO modulations, in Hz. Value can range from 10 to 10000.

- rate_hz: float = 0.5 [set set_rate_hz; get get_rate_hz]
  Adjusts the rate in Hz at which the effect sweeps up and down across the frequency range.
