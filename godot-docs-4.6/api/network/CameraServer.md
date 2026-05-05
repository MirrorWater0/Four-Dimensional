# CameraServer

## Meta

- Name: CameraServer
- Source: CameraServer.xml
- Inherits: Object
- Inheritance Chain: CameraServer -> Object

## Brief Description

Server keeping track of different cameras accessible in Godot.

## Description

The CameraServer keeps track of different cameras accessible in Godot. These are external cameras such as webcams or the cameras on your phone. It is notably used to provide AR modules with a video feed from the camera. **Note:** This class is currently only implemented on Linux, Android, macOS, and iOS. On other platforms no CameraFeeds will be available. To get a CameraFeed on iOS, the camera plugin from [godot-ios-plugins](https://github.com/godotengine/godot-ios-plugins) is required.

## Quick Reference

```
[methods]
add_feed(feed: CameraFeed) -> void
feeds() -> CameraFeed[]
get_feed(index: int) -> CameraFeed
get_feed_count() -> int
remove_feed(feed: CameraFeed) -> void

[properties]
monitoring_feeds: bool = false
```

## Methods

- add_feed(feed: CameraFeed) -> void
  Adds the camera feed to the camera server.

- feeds() -> CameraFeed[]
  Returns an array of CameraFeeds.

- get_feed(index: int) -> CameraFeed
  Returns the CameraFeed corresponding to the camera with the given index.

- get_feed_count() -> int
  Returns the number of CameraFeeds registered.

- remove_feed(feed: CameraFeed) -> void
  Removes the specified camera feed.

## Properties

- monitoring_feeds: bool = false [set set_monitoring_feeds; get is_monitoring_feeds]
  If true, the server is actively monitoring available camera feeds. This has a performance cost, so only set it to true when you're actively accessing the camera. **Note:** After setting it to true, you can receive updated camera feeds through the camera_feeds_updated signal.

```
func _ready():
    CameraServer.camera_feeds_updated.connect(_on_camera_feeds_updated)
    CameraServer.monitoring_feeds = true

func _on_camera_feeds_updated():
    var feeds = CameraServer.feeds()
```

```
public override void _Ready()
{
    CameraServer.CameraFeedsUpdated += OnCameraFeedsUpdated;
    CameraServer.MonitoringFeeds = true;
}

void OnCameraFeedsUpdated()
{
    var feeds = CameraServer.Feeds();
}
```

## Signals

- camera_feed_added(id: int)
  Emitted when a CameraFeed is added (e.g. a webcam is plugged in).

- camera_feed_removed(id: int)
  Emitted when a CameraFeed is removed (e.g. a webcam is unplugged).

- camera_feeds_updated()
  Emitted when camera feeds are updated.

## Constants

### Enum FeedImage

- FEED_RGBA_IMAGE = 0
  The RGBA camera image.

- FEED_YCBCR_IMAGE = 0
  The YCbCr(https://en.wikipedia.org/wiki/YCbCr) camera image.

- FEED_Y_IMAGE = 0
  The Y component camera image.

- FEED_CBCR_IMAGE = 1
  The CbCr component camera image.
