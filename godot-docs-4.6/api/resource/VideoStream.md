# VideoStream

## Meta

- Name: VideoStream
- Source: VideoStream.xml
- Inherits: Resource
- Inheritance Chain: VideoStream -> Resource -> RefCounted -> Object

## Brief Description

Base resource for video streams.

## Description

Base resource type for all video streams. Classes that derive from VideoStream can all be used as resource types to play back videos in VideoStreamPlayer.

## Quick Reference

```
[methods]
_instantiate_playback() -> VideoStreamPlayback [virtual required]

[properties]
file: String = ""
```

## Tutorials

- [Playing videos]($DOCS_URL/tutorials/animation/playing_videos.html)
- [Runtime file loading and saving]($DOCS_URL/tutorials/io/runtime_file_loading_and_saving.html)

## Methods

- _instantiate_playback() -> VideoStreamPlayback [virtual required]
  Called when the video starts playing, to initialize and return a subclass of VideoStreamPlayback.

## Properties

- file: String = "" [set set_file; get get_file]
  The video file path or URI that this VideoStream resource handles. For VideoStreamTheora, this filename should be an Ogg Theora video file with the .ogv extension.
