# AudioEffectRecord

## Meta

- Name: AudioEffectRecord
- Source: AudioEffectRecord.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectRecord -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

Audio effect used for recording the sound from an audio bus.

## Description

Allows the user to record the sound from an audio bus into an AudioStreamWAV. When used on the "Master" audio bus, this includes all audio output by Godot. Unlike AudioEffectCapture, this effect encodes the recording with the given format (8-bit, 16-bit, or compressed) instead of giving access to the raw audio samples. Can be used (with an AudioStreamMicrophone) to record from a microphone. **Note:** ProjectSettings.audio/driver/enable_input must be true for audio input to work. See also that setting's description for caveats related to permissions and operating system privacy settings.

## Quick Reference

```
[methods]
get_recording() -> AudioStreamWAV [const]
is_recording_active() -> bool [const]
set_recording_active(record: bool) -> void

[properties]
format: int (AudioStreamWAV.Format) = 1
```

## Tutorials

- [Recording with microphone]($DOCS_URL/tutorials/audio/recording_with_microphone.html)
- [Audio Microphone Record Demo](https://godotengine.org/asset-library/asset/2760)

## Methods

- get_recording() -> AudioStreamWAV [const]
  Returns the recorded sample.

- is_recording_active() -> bool [const]
  Returns whether the recording is active or not.

- set_recording_active(record: bool) -> void
  If true, the sound will be recorded. Note that restarting the recording will remove the previously recorded sample.

## Properties

- format: int (AudioStreamWAV.Format) = 1 [set set_format; get get_format]
  Specifies the format in which the sample will be recorded.
