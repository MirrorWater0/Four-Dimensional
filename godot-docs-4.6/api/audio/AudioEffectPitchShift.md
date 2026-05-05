# AudioEffectPitchShift

## Meta

- Name: AudioEffectPitchShift
- Source: AudioEffectPitchShift.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectPitchShift -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

Adds a pitch-shifting audio effect to an audio bus. Raises or lowers the pitch of original sound.

## Description

Allows modulation of pitch independently of tempo. All frequencies can be increased/decreased with minimal effect on transients.

## Quick Reference

```
[properties]
fft_size: int (AudioEffectPitchShift.FFTSize) = 3
oversampling: int = 4
pitch_scale: float = 1.0
```

## Tutorials

- [Audio buses]($DOCS_URL/tutorials/audio/audio_buses.html)

## Properties

- fft_size: int (AudioEffectPitchShift.FFTSize) = 3 [set set_fft_size; get get_fft_size]
  The size of the [Fast Fourier transform](https://en.wikipedia.org/wiki/Fast_Fourier_transform) buffer. Higher values smooth out the effect over time, but have greater latency. The effects of this higher latency are especially noticeable on sounds that have sudden amplitude changes.

- oversampling: int = 4 [set set_oversampling; get get_oversampling]
  The oversampling factor to use. Higher values result in better quality, but are more demanding on the CPU and may cause audio cracking if the CPU can't keep up.

- pitch_scale: float = 1.0 [set set_pitch_scale; get get_pitch_scale]
  The pitch scale to use. 1.0 is the default pitch and plays sounds unaffected. pitch_scale can range from 0.0 (infinitely low pitch, inaudible) to 16 (16 times higher than the initial pitch).

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
