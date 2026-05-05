# CanvasLayer

## Meta

- Name: CanvasLayer
- Source: CanvasLayer.xml
- Inherits: Node
- Inheritance Chain: CanvasLayer -> Node -> Object

## Brief Description

A node used for independent rendering of objects within a 2D scene.

## Description

CanvasItem-derived nodes that are direct or indirect children of a CanvasLayer will be drawn in that layer. The layer is a numeric index that defines the draw order. The default 2D scene renders with index 0, so a CanvasLayer with index -1 will be drawn below, and a CanvasLayer with index 1 will be drawn above. This order will hold regardless of the CanvasItem.z_index of the nodes within each layer. CanvasLayers can be hidden and they can also optionally follow the viewport. This makes them useful for HUDs like health bar overlays (on layers 1 and higher) or backgrounds (on layers -1 and lower). **Note:** Embedded Windows are placed on layer 1024. CanvasItems on layers 1025 and higher appear in front of embedded windows. **Note:** Each CanvasLayer is drawn on one specific Viewport and cannot be shared between multiple Viewports, see custom_viewport. When using multiple Viewports, for example in a split-screen game, you need to create an individual CanvasLayer for each Viewport you want it to be drawn on.

## Quick Reference

```
[methods]
get_canvas() -> RID [const]
get_final_transform() -> Transform2D [const]
hide() -> void
show() -> void

[properties]
custom_viewport: Node
follow_viewport_enabled: bool = false
follow_viewport_scale: float = 1.0
layer: int = 1
offset: Vector2 = Vector2(0, 0)
rotation: float = 0.0
scale: Vector2 = Vector2(1, 1)
transform: Transform2D = Transform2D(1, 0, 0, 1, 0, 0)
visible: bool = true
```

## Tutorials

- [Viewport and canvas transforms]($DOCS_URL/tutorials/2d/2d_transforms.html)
- [Canvas layers]($DOCS_URL/tutorials/2d/canvas_layers.html)
- [2D Dodge The Creeps Demo](https://godotengine.org/asset-library/asset/2712)

## Methods

- get_canvas() -> RID [const]
  Returns the RID of the canvas used by this layer.

- get_final_transform() -> Transform2D [const]
  Returns the transform from the CanvasLayers coordinate system to the Viewports coordinate system.

- hide() -> void
  Hides any CanvasItem under this CanvasLayer. This is equivalent to setting visible to false.

- show() -> void
  Shows any CanvasItem under this CanvasLayer. This is equivalent to setting visible to true.

## Properties

- custom_viewport: Node [set set_custom_viewport; get get_custom_viewport]
  The custom Viewport node assigned to the CanvasLayer. If null, uses the default viewport instead.

- follow_viewport_enabled: bool = false [set set_follow_viewport; get is_following_viewport]
  If enabled, the CanvasLayer maintains its position in world space. If disabled, the CanvasLayer stays in a fixed position on the screen. Together with follow_viewport_scale, this can be used for a pseudo-3D effect.

- follow_viewport_scale: float = 1.0 [set set_follow_viewport_scale; get get_follow_viewport_scale]
  Scales the layer when using follow_viewport_enabled. Layers moving into the foreground should have increasing scales, while layers moving into the background should have decreasing scales.

- layer: int = 1 [set set_layer; get get_layer]
  Layer index for draw order. Lower values are drawn behind higher values. **Note:** If multiple CanvasLayers have the same layer index, CanvasItem children of one CanvasLayer are drawn behind the CanvasItem children of the other CanvasLayer. Which CanvasLayer is drawn in front is non-deterministic. **Note:** The layer index should be between RenderingServer.CANVAS_LAYER_MIN and RenderingServer.CANVAS_LAYER_MAX (inclusive). Any other value will wrap around.

- offset: Vector2 = Vector2(0, 0) [set set_offset; get get_offset]
  The layer's base offset.

- rotation: float = 0.0 [set set_rotation; get get_rotation]
  The layer's rotation in radians.

- scale: Vector2 = Vector2(1, 1) [set set_scale; get get_scale]
  The layer's scale.

- transform: Transform2D = Transform2D(1, 0, 0, 1, 0, 0) [set set_transform; get get_transform]
  The layer's transform.

- visible: bool = true [set set_visible; get is_visible]
  If false, any CanvasItem under this CanvasLayer will be hidden. Unlike CanvasItem.visible, visibility of a CanvasLayer isn't propagated to underlying layers.

## Signals

- visibility_changed()
  Emitted when visibility of the layer is changed. See visible.
