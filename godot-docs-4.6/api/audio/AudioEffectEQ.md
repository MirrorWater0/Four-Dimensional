# AudioEffectEQ

## Meta

- Name: AudioEffectEQ
- Source: AudioEffectEQ.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectEQ -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

Base class for audio equalizers. Gives you control over frequencies. Use it to create a custom equalizer if AudioEffectEQ6, AudioEffectEQ10 or AudioEffectEQ21 don't fit your needs.

## Description

AudioEffectEQ gives you control over frequencies. Use it to compensate for existing deficiencies in audio. AudioEffectEQs are useful on the Master bus to completely master a mix and give it more character. They are also useful when a game is run on a mobile device, to adjust the mix to that kind of speakers (it can be added but disabled when headphones are plugged).

## Quick Reference

```
[methods]
get_band_count() -> int [const]
get_band_gain_db(band_idx: int) -> float [const]
set_band_gain_db(band_idx: int, volume_db: float) -> void
```

## Tutorials

- [Audio buses]($DOCS_URL/tutorials/audio/audio_buses.html)

## Methods

- get_band_count() -> int [const]
  Returns the number of bands of the equalizer.

- get_band_gain_db(band_idx: int) -> float [const]
  Returns the band's gain at the specified index, in dB.

- set_band_gain_db(band_idx: int, volume_db: float) -> void
  Sets band's gain at the specified index, in dB.
