# CameraFeed

## Meta

- Name: CameraFeed
- Source: CameraFeed.xml
- Inherits: RefCounted
- Inheritance Chain: CameraFeed -> RefCounted -> Object

## Brief Description

A camera feed gives you access to a single physical camera attached to your device.

## Description

A camera feed gives you access to a single physical camera attached to your device. When enabled, Godot will start capturing frames from the camera which can then be used. See also CameraServer. **Note:** Many cameras will return YCbCr images which are split into two textures and need to be combined in a shader. Godot does this automatically for you if you set the environment to show the camera image in the background. **Note:** This class is currently only implemented on Linux, Android, macOS, and iOS. On other platforms no CameraFeeds will be available. To get a CameraFeed on iOS, the camera plugin from [godot-ios-plugins](https://github.com/godotengine/godot-ios-plugins) is required.

## Quick Reference

```
[methods]
_activate_feed() -> bool [virtual]
_deactivate_feed() -> void [virtual]
get_datatype() -> int (CameraFeed.FeedDataType) [const]
get_id() -> int [const]
get_name() -> String [const]
get_position() -> int (CameraFeed.FeedPosition) [const]
get_texture_tex_id(feed_image_type: int (CameraServer.FeedImage)) -> int
set_external(width: int, height: int) -> void
set_format(index: int, parameters: Dictionary) -> bool
set_name(name: String) -> void
set_position(position: int (CameraFeed.FeedPosition)) -> void
set_rgb_image(rgb_image: Image) -> void
set_ycbcr_image(ycbcr_image: Image) -> void
set_ycbcr_images(y_image: Image, cbcr_image: Image) -> void

[properties]
feed_is_active: bool = false
feed_transform: Transform2D = Transform2D(1, 0, 0, -1, 0, 1)
formats: Array = []
```

## Methods

- _activate_feed() -> bool [virtual]
  Called when the camera feed is activated.

- _deactivate_feed() -> void [virtual]
  Called when the camera feed is deactivated.

- get_datatype() -> int (CameraFeed.FeedDataType) [const]
  Returns feed image data type.

- get_id() -> int [const]
  Returns the unique ID for this feed.

- get_name() -> String [const]
  Returns the camera's name.

- get_position() -> int (CameraFeed.FeedPosition) [const]
  Returns the position of camera on the device.

- get_texture_tex_id(feed_image_type: int (CameraServer.FeedImage)) -> int
  Returns the texture backend ID (usable by some external libraries that need a handle to a texture to write data).

- set_external(width: int, height: int) -> void
  Sets the feed as external feed provided by another library.

- set_format(index: int, parameters: Dictionary) -> bool
  Sets the feed format parameters for the given index in the formats array. Returns true on success. By default, the YUYV encoded stream is transformed to FEED_RGB. The YUYV encoded stream output format can be changed by setting parameters's output entry to one of the following: - "separate" will result in FEED_YCBCR_SEP; - "grayscale" will result in desaturated FEED_RGB; - "copy" will result in FEED_YCBCR.

- set_name(name: String) -> void
  Sets the camera's name.

- set_position(position: int (CameraFeed.FeedPosition)) -> void
  Sets the position of this camera.

- set_rgb_image(rgb_image: Image) -> void
  Sets RGB image for this feed.

- set_ycbcr_image(ycbcr_image: Image) -> void
  Sets YCbCr image for this feed.

- set_ycbcr_images(y_image: Image, cbcr_image: Image) -> void
  Sets Y and CbCr images for this feed.

## Properties

- feed_is_active: bool = false [set set_active; get is_active]
  If true, the feed is active.

- feed_transform: Transform2D = Transform2D(1, 0, 0, -1, 0, 1) [set set_transform; get get_transform]
  The transform applied to the camera's image.

- formats: Array = [] [get get_formats]
  Formats supported by the feed. Each entry is a Dictionary describing format parameters.

## Signals

- format_changed()
  Emitted when the format has changed.

- frame_changed()
  Emitted when a new frame is available.

## Constants

### Enum FeedDataType

- FEED_NOIMAGE = 0
  No image set for the feed.

- FEED_RGB = 1
  Feed supplies RGB images.

- FEED_YCBCR = 2
  Feed supplies YCbCr images that need to be converted to RGB.

- FEED_YCBCR_SEP = 3
  Feed supplies separate Y and CbCr images that need to be combined and converted to RGB.

- FEED_EXTERNAL = 4
  Feed supplies external image.

### Enum FeedPosition

- FEED_UNSPECIFIED = 0
  Unspecified position.

- FEED_FRONT = 1
  Camera is mounted at the front of the device.

- FEED_BACK = 2
  Camera is mounted at the back of the device.
