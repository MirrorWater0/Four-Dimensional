# Camera3D

## Meta

- Name: Camera3D
- Source: Camera3D.xml
- Inherits: Node3D
- Inheritance Chain: Camera3D -> Node3D -> Node -> Object

## Brief Description

Camera node, displays from a point of view.

## Description

Camera3D is a special node that displays what is visible from its current location. Cameras register themselves in the nearest Viewport node (when ascending the tree). Only one camera can be active per viewport. If no viewport is available ascending the tree, the camera will register in the global viewport. In other words, a camera just provides 3D display capabilities to a Viewport, and, without one, a scene registered in that Viewport (or higher viewports) can't be displayed.

## Quick Reference

```
[methods]
clear_current(enable_next: bool = true) -> void
get_camera_projection() -> Projection [const]
get_camera_rid() -> RID [const]
get_camera_transform() -> Transform3D [const]
get_cull_mask_value(layer_number: int) -> bool [const]
get_frustum() -> Plane[] [const]
get_pyramid_shape_rid() -> RID
is_position_behind(world_point: Vector3) -> bool [const]
is_position_in_frustum(world_point: Vector3) -> bool [const]
make_current() -> void
project_local_ray_normal(screen_point: Vector2) -> Vector3 [const]
project_position(screen_point: Vector2, z_depth: float) -> Vector3 [const]
project_ray_normal(screen_point: Vector2) -> Vector3 [const]
project_ray_origin(screen_point: Vector2) -> Vector3 [const]
set_cull_mask_value(layer_number: int, value: bool) -> void
set_frustum(size: float, offset: Vector2, z_near: float, z_far: float) -> void
set_orthogonal(size: float, z_near: float, z_far: float) -> void
set_perspective(fov: float, z_near: float, z_far: float) -> void
unproject_position(world_point: Vector3) -> Vector2 [const]

[properties]
attributes: CameraAttributes
compositor: Compositor
cull_mask: int = 1048575
current: bool = false
doppler_tracking: int (Camera3D.DopplerTracking) = 0
environment: Environment
far: float = 4000.0
fov: float = 75.0
frustum_offset: Vector2 = Vector2(0, 0)
h_offset: float = 0.0
keep_aspect: int (Camera3D.KeepAspect) = 1
near: float = 0.05
projection: int (Camera3D.ProjectionType) = 0
size: float = 1.0
v_offset: float = 0.0
```

## Tutorials

- [Third Person Shooter (TPS) Demo](https://godotengine.org/asset-library/asset/2710)

## Methods

- clear_current(enable_next: bool = true) -> void
  If this is the current camera, remove it from being current. If enable_next is true, request to make the next camera current, if any.

- get_camera_projection() -> Projection [const]
  Returns the projection matrix that this camera uses to render to its associated viewport. The camera must be part of the scene tree to function.

- get_camera_rid() -> RID [const]
  Returns the camera's RID from the RenderingServer.

- get_camera_transform() -> Transform3D [const]
  Returns the transform of the camera plus the vertical (v_offset) and horizontal (h_offset) offsets; and any other adjustments made to the position and orientation of the camera by subclassed cameras such as XRCamera3D.

- get_cull_mask_value(layer_number: int) -> bool [const]
  Returns whether or not the specified layer of the cull_mask is enabled, given a layer_number between 1 and 20.

- get_frustum() -> Plane[] [const]
  Returns the camera's frustum planes in world space units as an array of Planes in the following order: near, far, left, top, right, bottom. Not to be confused with frustum_offset.

- get_pyramid_shape_rid() -> RID
  Returns the RID of a pyramid shape encompassing the camera's view frustum, ignoring the camera's near plane. The tip of the pyramid represents the position of the camera.

- is_position_behind(world_point: Vector3) -> bool [const]
  Returns true if the given position is behind the camera (the blue part of the linked diagram). [See this diagram](https://raw.githubusercontent.com/godotengine/godot-docs/master/img/camera3d_position_frustum.png) for an overview of position query methods. **Note:** A position which returns false may still be outside the camera's field of view.

- is_position_in_frustum(world_point: Vector3) -> bool [const]
  Returns true if the given position is inside the camera's frustum (the green part of the linked diagram). [See this diagram](https://raw.githubusercontent.com/godotengine/godot-docs/master/img/camera3d_position_frustum.png) for an overview of position query methods.

- make_current() -> void
  Makes this camera the current camera for the Viewport (see class description). If the camera node is outside the scene tree, it will attempt to become current once it's added.

- project_local_ray_normal(screen_point: Vector2) -> Vector3 [const]
  Returns a normal vector from the screen point location directed along the camera. Orthogonal cameras are normalized. Perspective cameras account for perspective, screen width/height, etc.

- project_position(screen_point: Vector2, z_depth: float) -> Vector3 [const]
  Returns the 3D point in world space that maps to the given 2D coordinate in the Viewport rectangle on a plane that is the given z_depth distance into the scene away from the camera.

- project_ray_normal(screen_point: Vector2) -> Vector3 [const]
  Returns a normal vector in world space, that is the result of projecting a point on the Viewport rectangle by the inverse camera projection. This is useful for casting rays in the form of (origin, normal) for object intersection or picking.

- project_ray_origin(screen_point: Vector2) -> Vector3 [const]
  Returns a 3D position in world space, that is the result of projecting a point on the Viewport rectangle by the inverse camera projection. This is useful for casting rays in the form of (origin, normal) for object intersection or picking.

- set_cull_mask_value(layer_number: int, value: bool) -> void
  Based on value, enables or disables the specified layer in the cull_mask, given a layer_number between 1 and 20.

- set_frustum(size: float, offset: Vector2, z_near: float, z_far: float) -> void
  Sets the camera projection to frustum mode (see PROJECTION_FRUSTUM), by specifying a size, an offset, and the z_near and z_far clip planes in world space units. See also frustum_offset.

- set_orthogonal(size: float, z_near: float, z_far: float) -> void
  Sets the camera projection to orthogonal mode (see PROJECTION_ORTHOGONAL), by specifying a size, and the z_near and z_far clip planes in world space units. As a hint, 3D games that look 2D often use this projection, with size specified in pixels.

- set_perspective(fov: float, z_near: float, z_far: float) -> void
  Sets the camera projection to perspective mode (see PROJECTION_PERSPECTIVE), by specifying a fov (field of view) angle in degrees, and the z_near and z_far clip planes in world space units.

- unproject_position(world_point: Vector3) -> Vector2 [const]
  Returns the 2D coordinate in the Viewport rectangle that maps to the given 3D point in world space. **Note:** When using this to position GUI elements over a 3D viewport, use is_position_behind() to prevent them from appearing if the 3D point is behind the camera:


```
  # This code block is part of a script that inherits from Node3D.
  # `control` is a reference to a node inheriting from Control.
  control.visible = not get_viewport().get_camera_3d().is_position_behind(global_transform.origin)
  control.position = get_viewport().get_camera_3d().unproject_position(global_transform.origin)

```

## Properties

- attributes: CameraAttributes [set set_attributes; get get_attributes]
  The CameraAttributes to use for this camera.

- compositor: Compositor [set set_compositor; get get_compositor]
  The Compositor to use for this camera.

- cull_mask: int = 1048575 [set set_cull_mask; get get_cull_mask]
  The culling mask that describes which VisualInstance3D.layers are rendered by this camera. By default, all 20 user-visible layers are rendered. **Note:** Since the cull_mask allows for 32 layers to be stored in total, there are an additional 12 layers that are only used internally by the engine and aren't exposed in the editor. Setting cull_mask using a script allows you to toggle those reserved layers, which can be useful for editor plugins. To adjust cull_mask more easily using a script, use get_cull_mask_value() and set_cull_mask_value(). **Note:** VoxelGI, SDFGI and LightmapGI will always take all layers into account to determine what contributes to global illumination. If this is an issue, set GeometryInstance3D.gi_mode to GeometryInstance3D.GI_MODE_DISABLED for meshes and Light3D.light_bake_mode to Light3D.BAKE_DISABLED for lights to exclude them from global illumination.

- current: bool = false [set set_current; get is_current]
  If true, the ancestor Viewport is currently using this camera. If multiple cameras are in the scene, one will always be made current. For example, if two Camera3D nodes are present in the scene and only one is current, setting one camera's current to false will cause the other camera to be made current.

- doppler_tracking: int (Camera3D.DopplerTracking) = 0 [set set_doppler_tracking; get get_doppler_tracking]
  If not DOPPLER_TRACKING_DISABLED, this camera will simulate the [Doppler effect](https://en.wikipedia.org/wiki/Doppler_effect) for objects changed in particular _process methods. **Note:** The Doppler effect will only be heard on AudioStreamPlayer3Ds if AudioStreamPlayer3D.doppler_tracking is not set to AudioStreamPlayer3D.DOPPLER_TRACKING_DISABLED.

- environment: Environment [set set_environment; get get_environment]
  The Environment to use for this camera.

- far: float = 4000.0 [set set_far; get get_far]
  The distance to the far culling boundary for this camera relative to its local Z axis. Higher values allow the camera to see further away, while decreasing far can improve performance if it results in objects being partially or fully culled.

- fov: float = 75.0 [set set_fov; get get_fov]
  The camera's field of view angle (in degrees). Only applicable in perspective mode. Since keep_aspect locks one axis, fov sets the other axis' field of view angle. For reference, the default vertical field of view value (75.0) is equivalent to a horizontal FOV of: - ~91.31 degrees in a 4:3 viewport - ~101.67 degrees in a 16:10 viewport - ~107.51 degrees in a 16:9 viewport - ~121.63 degrees in a 21:9 viewport

- frustum_offset: Vector2 = Vector2(0, 0) [set set_frustum_offset; get get_frustum_offset]
  The camera's frustum offset. This can be changed from the default to create "tilted frustum" effects such as [Y-shearing](https://zdoom.org/wiki/Y-shearing). **Note:** Only effective if projection is PROJECTION_FRUSTUM.

- h_offset: float = 0.0 [set set_h_offset; get get_h_offset]
  The horizontal (X) offset of the camera viewport.

- keep_aspect: int (Camera3D.KeepAspect) = 1 [set set_keep_aspect_mode; get get_keep_aspect_mode]
  The axis to lock during fov/size adjustments. Can be either KEEP_WIDTH or KEEP_HEIGHT.

- near: float = 0.05 [set set_near; get get_near]
  The distance to the near culling boundary for this camera relative to its local Z axis. Lower values allow the camera to see objects more up close to its origin, at the cost of lower precision across the *entire* range. Values lower than the default can lead to increased Z-fighting.

- projection: int (Camera3D.ProjectionType) = 0 [set set_projection; get get_projection]
  The camera's projection mode. In PROJECTION_PERSPECTIVE mode, objects' Z distance from the camera's local space scales their perceived size.

- size: float = 1.0 [set set_size; get get_size]
  The camera's size in meters measured as the diameter of the width or height, depending on keep_aspect. Only applicable in orthogonal and frustum modes.

- v_offset: float = 0.0 [set set_v_offset; get get_v_offset]
  The vertical (Y) offset of the camera viewport.

## Constants

### Enum ProjectionType

- PROJECTION_PERSPECTIVE = 0
  Perspective projection. Objects on the screen becomes smaller when they are far away.

- PROJECTION_ORTHOGONAL = 1
  Orthogonal projection, also known as orthographic projection. Objects remain the same size on the screen no matter how far away they are.

- PROJECTION_FRUSTUM = 2
  Frustum projection. This mode allows adjusting frustum_offset to create "tilted frustum" effects.

### Enum KeepAspect

- KEEP_WIDTH = 0
  Preserves the horizontal aspect ratio; also known as Vert- scaling. This is usually the best option for projects running in portrait mode, as taller aspect ratios will benefit from a wider vertical FOV.

- KEEP_HEIGHT = 1
  Preserves the vertical aspect ratio; also known as Hor+ scaling. This is usually the best option for projects running in landscape mode, as wider aspect ratios will automatically benefit from a wider horizontal FOV.

### Enum DopplerTracking

- DOPPLER_TRACKING_DISABLED = 0
  Disables [Doppler effect](https://en.wikipedia.org/wiki/Doppler_effect) simulation (default).

- DOPPLER_TRACKING_IDLE_STEP = 1
  Simulate [Doppler effect](https://en.wikipedia.org/wiki/Doppler_effect) by tracking positions of objects that are changed in _process. Changes in the relative velocity of this camera compared to those objects affect how audio is perceived (changing the audio's AudioStreamPlayer3D.pitch_scale).

- DOPPLER_TRACKING_PHYSICS_STEP = 2
  Simulate [Doppler effect](https://en.wikipedia.org/wiki/Doppler_effect) by tracking positions of objects that are changed in _physics_process. Changes in the relative velocity of this camera compared to those objects affect how audio is perceived (changing the audio's AudioStreamPlayer3D.pitch_scale).
