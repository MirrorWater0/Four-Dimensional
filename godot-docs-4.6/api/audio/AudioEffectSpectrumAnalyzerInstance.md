# AudioEffectSpectrumAnalyzerInstance

## Meta

- Name: AudioEffectSpectrumAnalyzerInstance
- Source: AudioEffectSpectrumAnalyzerInstance.xml
- Inherits: AudioEffectInstance
- Inheritance Chain: AudioEffectSpectrumAnalyzerInstance -> AudioEffectInstance -> RefCounted -> Object

## Brief Description

Queryable instance of an AudioEffectSpectrumAnalyzer.

## Description

The runtime part of an AudioEffectSpectrumAnalyzer, which can be used to query the magnitude of a frequency range on its host bus. An instance of this class can be obtained with AudioServer.get_bus_effect_instance().

## Quick Reference

```
[methods]
get_magnitude_for_frequency_range(from_hz: float, to_hz: float, mode: int (AudioEffectSpectrumAnalyzerInstance.MagnitudeMode) = 1) -> Vector2 [const]
```

## Tutorials

- [Audio Spectrum Visualizer Demo](https://godotengine.org/asset-library/asset/2762)

## Methods

- get_magnitude_for_frequency_range(from_hz: float, to_hz: float, mode: int (AudioEffectSpectrumAnalyzerInstance.MagnitudeMode) = 1) -> Vector2 [const]
  Returns the magnitude of the frequencies from from_hz to to_hz in linear energy as a Vector2. The x component of the return value represents the left stereo channel, and y represents the right channel. mode determines how the frequency range will be processed.

## Constants

### Enum MagnitudeMode

- MAGNITUDE_AVERAGE = 0
  Use the average value across the frequency range as magnitude.

- MAGNITUDE_MAX = 1
  Use the maximum value of the frequency range as magnitude.
