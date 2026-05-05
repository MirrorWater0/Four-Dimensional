# AudioStreamWAV

## Meta

- Name: AudioStreamWAV
- Source: AudioStreamWAV.xml
- Inherits: AudioStream
- Inheritance Chain: AudioStreamWAV -> AudioStream -> Resource -> RefCounted -> Object

## Brief Description

Stores audio data loaded from WAV files.

## Description

AudioStreamWAV stores sound samples loaded from WAV files. To play the stored sound, use an AudioStreamPlayer (for non-positional audio) or AudioStreamPlayer2D/AudioStreamPlayer3D (for positional audio). The sound can be looped. This class can also be used to store dynamically-generated PCM audio data. See also AudioStreamGenerator for procedural audio generation.

## Quick Reference

```
[methods]
load_from_buffer(stream_data: PackedByteArray, options: Dictionary = {}) -> AudioStreamWAV [static]
load_from_file(path: String, options: Dictionary = {}) -> AudioStreamWAV [static]
save_to_wav(path: String) -> int (Error)

[properties]
data: PackedByteArray = PackedByteArray()
format: int (AudioStreamWAV.Format) = 0
loop_begin: int = 0
loop_end: int = 0
loop_mode: int (AudioStreamWAV.LoopMode) = 0
mix_rate: int = 44100
stereo: bool = false
tags: Dictionary = {}
```

## Tutorials

- [Runtime file loading and saving]($DOCS_URL/tutorials/io/runtime_file_loading_and_saving.html)

## Methods

- load_from_buffer(stream_data: PackedByteArray, options: Dictionary = {}) -> AudioStreamWAV [static]
  Creates a new AudioStreamWAV instance from the given buffer. The buffer must contain WAV data. The keys and values of options match the properties of ResourceImporterWAV. The usage of options is identical to AudioStreamWAV.load_from_file().

- load_from_file(path: String, options: Dictionary = {}) -> AudioStreamWAV [static]
  Creates a new AudioStreamWAV instance from the given file path. The file must be in WAV format. The keys and values of options match the properties of ResourceImporterWAV. **Example:** Load the first file dropped as a WAV and play it:


```
  @onready var audio_player = $AudioStreamPlayer

  func _ready():
      get_window().files_dropped.connect(_on_files_dropped)

  func _on_files_dropped(files):
      if files0.get_extension() == "wav":
          audio_player.stream = AudioStreamWAV.load_from_file(files0, {
                  "force/max_rate": true,
                  "force/max_rate_hz": 11025
              })
          audio_player.play()

```

- save_to_wav(path: String) -> int (Error)
  Saves the AudioStreamWAV as a WAV file to path. Samples with IMA ADPCM or Quite OK Audio formats can't be saved. **Note:** A .wav extension is automatically appended to path if it is missing.

## Properties

- data: PackedByteArray = PackedByteArray() [set set_data; get get_data]
  Contains the audio data in bytes. **Note:** If format is set to FORMAT_8_BITS, this property expects signed 8-bit PCM data. To convert from unsigned 8-bit PCM, subtract 128 from each byte. **Note:** If format is set to FORMAT_QOA, this property expects data from a full QOA file.

- format: int (AudioStreamWAV.Format) = 0 [set set_format; get get_format]
  Audio format.

- loop_begin: int = 0 [set set_loop_begin; get get_loop_begin]
  The loop start point (in number of samples, relative to the beginning of the stream).

- loop_end: int = 0 [set set_loop_end; get get_loop_end]
  The loop end point (in number of samples, relative to the beginning of the stream).

- loop_mode: int (AudioStreamWAV.LoopMode) = 0 [set set_loop_mode; get get_loop_mode]
  The loop mode.

- mix_rate: int = 44100 [set set_mix_rate; get get_mix_rate]
  The sample rate for mixing this audio. Higher values require more storage space, but result in better quality. In games, common sample rates in use are 11025, 16000, 22050, 32000, 44100, and 48000. According to the [Nyquist-Shannon sampling theorem](https://en.wikipedia.org/wiki/Nyquist%E2%80%93Shannon_sampling_theorem), there is no quality difference to human hearing when going past 40,000 Hz (since most humans can only hear up to ~20,000 Hz, often less). If you are using lower-pitched sounds such as voices, lower sample rates such as 32000 or 22050 may be usable with no loss in quality.

- stereo: bool = false [set set_stereo; get is_stereo]
  If true, audio is stereo.

- tags: Dictionary = {} [set set_tags; get get_tags]
  Contains user-defined tags if found in the WAV data. Commonly used tags include title, artist, album, tracknumber, and date (date does not have a standard date format). **Note:** No tag is *guaranteed* to be present in every file, so make sure to account for the keys not always existing. **Note:** Only WAV files using a LIST chunk with an identifier of INFO to encode the tags are currently supported.

## Constants

### Enum Format

- FORMAT_8_BITS = 0
  8-bit PCM audio codec.

- FORMAT_16_BITS = 1
  16-bit PCM audio codec.

- FORMAT_IMA_ADPCM = 2
  Audio is lossily compressed as IMA ADPCM.

- FORMAT_QOA = 3
  Audio is lossily compressed as [Quite OK Audio](https://qoaformat.org/).

### Enum LoopMode

- LOOP_DISABLED = 0
  Audio does not loop.

- LOOP_FORWARD = 1
  Audio loops the data between loop_begin and loop_end, playing forward only.

- LOOP_PINGPONG = 2
  Audio loops the data between loop_begin and loop_end, playing back and forth.

- LOOP_BACKWARD = 3
  Audio loops the data between loop_begin and loop_end, playing backward only.
