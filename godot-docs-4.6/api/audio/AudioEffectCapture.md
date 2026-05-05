# AudioEffectCapture

## Meta

- Name: AudioEffectCapture
- Source: AudioEffectCapture.xml
- Inherits: AudioEffect
- Inheritance Chain: AudioEffectCapture -> AudioEffect -> Resource -> RefCounted -> Object

## Brief Description

Captures audio from an audio bus in real-time.

## Description

AudioEffectCapture is an AudioEffect which copies all audio frames from the attached audio effect bus into its internal ring buffer. Application code should consume these audio frames from this ring buffer using get_buffer() and process it as needed, for example to capture data from an AudioStreamMicrophone, implement application-defined effects, or to transmit audio over the network. When capturing audio data from a microphone, the format of the samples will be stereo 32-bit floating-point PCM. Unlike AudioEffectRecord, this effect only returns the raw audio samples instead of encoding them into an AudioStream.

## Quick Reference

```
[methods]
can_get_buffer(frames: int) -> bool [const]
clear_buffer() -> void
get_buffer(frames: int) -> PackedVector2Array
get_buffer_length_frames() -> int [const]
get_discarded_frames() -> int [const]
get_frames_available() -> int [const]
get_pushed_frames() -> int [const]

[properties]
buffer_length: float = 0.1
```

## Tutorials

- [Audio buses]($DOCS_URL/tutorials/audio/audio_buses.html)

## Methods

- can_get_buffer(frames: int) -> bool [const]
  Returns true if at least frames audio frames are available to read in the internal ring buffer.

- clear_buffer() -> void
  Clears the internal ring buffer. **Note:** Calling this during a capture can cause the loss of samples which causes popping in the playback.

- get_buffer(frames: int) -> PackedVector2Array
  Gets the next frames audio samples from the internal ring buffer. Returns a PackedVector2Array containing exactly frames audio samples if available, or an empty PackedVector2Array if insufficient data was available. The samples are signed floating-point PCM between -1 and 1. You will have to scale them if you want to use them as 8 or 16-bit integer samples. (v = 0x7fff * samples0.x)

- get_buffer_length_frames() -> int [const]
  Returns the total size of the internal ring buffer in frames.

- get_discarded_frames() -> int [const]
  Returns the number of audio frames discarded from the audio bus due to full buffer.

- get_frames_available() -> int [const]
  Returns the number of frames available to read using get_buffer().

- get_pushed_frames() -> int [const]
  Returns the number of audio frames inserted from the audio bus.

## Properties

- buffer_length: float = 0.1 [set set_buffer_length; get get_buffer_length]
  Length of the internal ring buffer, in seconds. Setting the buffer length will have no effect if already initialized.
