# ExternalTexture

## Meta

- Name: ExternalTexture
- Source: ExternalTexture.xml
- Inherits: Texture2D
- Inheritance Chain: ExternalTexture -> Texture2D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

Texture which displays the content of an external buffer.

## Description

Displays the content of an external buffer provided by the platform. Requires the OES_EGL_image_external(https://registry.khronos.org/OpenGL/extensions/OES/OES_EGL_image_external.txt) extension (OpenGL) or VK_ANDROID_external_memory_android_hardware_buffer(https://registry.khronos.org/vulkan/specs/1.1-extensions/html/vkspec.html#VK_ANDROID_external_memory_android_hardware_buffer) extension (Vulkan). **Note:** This is currently only supported in Android builds.

## Quick Reference

```
[methods]
get_external_texture_id() -> int [const]
set_external_buffer_id(external_buffer_id: int) -> void

[properties]
resource_local_to_scene: bool = false
size: Vector2 = Vector2(256, 256)
```

## Methods

- get_external_texture_id() -> int [const]
  Returns the external texture ID. Depending on your use case, you may need to pass this to platform APIs, for example, when creating an android.graphics.SurfaceTexture on Android.

- set_external_buffer_id(external_buffer_id: int) -> void
  Sets the external buffer ID. Depending on your use case, you may need to call this with data received from a platform API, for example, SurfaceTexture.getHardwareBuffer() on Android.

## Properties

- resource_local_to_scene: bool = false [set set_local_to_scene; get is_local_to_scene; override Resource]

- size: Vector2 = Vector2(256, 256) [set set_size; get get_size]
  External texture size.
