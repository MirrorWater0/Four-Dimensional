# AudioEffectSpectrumAnalyzer

## Meta

- Name: AudioEffectSpectrumAnalyzer
- Source: AudioEffectSpectrumAnalyzer.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectSpectrumAnalyzer -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

Audio effect that can be used for real-time audio visualizations.

## Description

This audio effect does not affect sound output, but can be used for real-time audio visualizations. This resource configures an AudioEffectSpectrumAnalyzerInstance, which performs the actual analysis at runtime. An instance can be obtained with AudioServer.get_bus_effect_instance(). See also AudioStreamGenerator for procedurally generating sounds.

## Quick Reference

```
[properties]
buffer_length: float = 2.0
fft_size: int (AudioEffectSpectrumAnalyzer.FFTSize) = 2
tap_back_pos: float = 0.01
```

## Tutorials

- [Audio Spectrum Visualizer Demo](https://godotengine.org/asset-library/asset/2762)

## Properties

- buffer_length: float = 2.0 [set set_buffer_length; get get_buffer_length]
  The length of the buffer to keep (in seconds). Higher values keep data around for longer, but require more memory.

- fft_size: int (AudioEffectSpectrumAnalyzer.FFTSize) = 2 [set set_fft_size; get get_fft_size]
  The size of the [Fast Fourier transform](https://en.wikipedia.org/wiki/Fast_Fourier_transform) buffer. Higher values smooth out the spectrum analysis over time, but have greater latency. The effects of this higher latency are especially noticeable with sudden amplitude changes.

- tap_back_pos: float = 0.01 [set set_tap_back_pos; get get_tap_back_pos]

## Constants

### Enum FFTSize

- FFT_SIZE_256 = 0
  Use a buffer of 256 samples for the Fast Fourier transform. Lowest latency, but least stable over time.

- FFT_SIZE_512 = 1
  Use a buffer of 512 samples for the Fast Fourier transform. Low latency, but less stable over time.

- FFT_SIZE_1024 = 2
  Use a buffer of 1024 samples for the Fast Fourier transform. This is a compromise between latency and stability over time.

- FFT_SIZE_2048 = 3
  Use a buffer of 2048 samples for the Fast Fourier transform. High latency, but stable over time.

- FFT_SIZE_4096 = 4
  Use a buffer of 4096 samples for the Fast Fourier transform. Highest latency, but most stable over time.

- FFT_SIZE_MAX = 5
  Represents the size of the FFTSize enum.
