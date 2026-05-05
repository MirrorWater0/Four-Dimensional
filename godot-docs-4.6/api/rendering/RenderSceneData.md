# RenderSceneData

## Meta

- Name: RenderSceneData
- Source: RenderSceneData.xml
- Inherits: Object
- Inheritance Chain: RenderSceneData -> Object

## Brief Description

Abstract render data object, holds scene data related to rendering a single frame of a viewport.

## Description

Abstract scene data object, exists for the duration of rendering a single viewport. See also RenderSceneDataRD, RenderData, and RenderDataRD. **Note:** This is an internal rendering server object. Do not instantiate this class from a script.

## Quick Reference

```
[methods]
get_cam_projection() -> Projection [const]
get_cam_transform() -> Transform3D [const]
get_uniform_buffer() -> RID [const]
get_view_count() -> int [const]
get_view_eye_offset(view: int) -> Vector3 [const]
get_view_projection(view: int) -> Projection [const]
```

## Methods

- get_cam_projection() -> Projection [const]
  Returns the camera projection used to render this frame. **Note:** If more than one view is rendered, this will return a combined projection.

- get_cam_transform() -> Transform3D [const]
  Returns the camera transform used to render this frame. **Note:** If more than one view is rendered, this will return a centered transform.

- get_uniform_buffer() -> RID [const]
  Return the RID of the uniform buffer containing the scene data as a UBO.

- get_view_count() -> int [const]
  Returns the number of views being rendered.

- get_view_eye_offset(view: int) -> Vector3 [const]
  Returns the eye offset per view used to render this frame. This is the offset between our camera transform and the eye transform.

- get_view_projection(view: int) -> Projection [const]
  Returns the view projection per view used to render this frame. **Note:** If a single view is rendered, this returns the camera projection. If more than one view is rendered, this will return a projection for the given view including the eye offset.
