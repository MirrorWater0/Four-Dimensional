# CameraTexture

## Meta

- Name: CameraTexture
- Source: CameraTexture.xml
- Inherits: Texture2D
- Inheritance Chain: CameraTexture -> Texture2D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

Texture provided by a CameraFeed.

## Description

This texture gives access to the camera texture provided by a CameraFeed. **Note:** Many cameras supply YCbCr images which need to be converted in a shader.

## Quick Reference

```
[properties]
camera_feed_id: int = 0
camera_is_active: bool = false
resource_local_to_scene: bool = false
which_feed: int (CameraServer.FeedImage) = 0
```

## Properties

- camera_feed_id: int = 0 [set set_camera_feed_id; get get_camera_feed_id]
  The ID of the CameraFeed for which we want to display the image.

- camera_is_active: bool = false [set set_camera_active; get get_camera_active]
  Convenience property that gives access to the active property of the CameraFeed.

- resource_local_to_scene: bool = false [set set_local_to_scene; get is_local_to_scene; override Resource]

- which_feed: int (CameraServer.FeedImage) = 0 [set set_which_feed; get get_which_feed]
  Which image within the CameraFeed we want access to, important if the camera image is split in a Y and CbCr component.
