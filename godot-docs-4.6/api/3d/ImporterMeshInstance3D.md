# ImporterMeshInstance3D

## Meta

- Name: ImporterMeshInstance3D
- Source: ImporterMeshInstance3D.xml
- Inherits: Node3D
- Inheritance Chain: ImporterMeshInstance3D -> Node3D -> Node -> Object

## Quick Reference

```
[properties]
cast_shadow: int (GeometryInstance3D.ShadowCastingSetting) = 1
layer_mask: int = 1
mesh: ImporterMesh
skeleton_path: NodePath = NodePath("")
skin: Skin
visibility_range_begin: float = 0.0
visibility_range_begin_margin: float = 0.0
visibility_range_end: float = 0.0
visibility_range_end_margin: float = 0.0
visibility_range_fade_mode: int (GeometryInstance3D.VisibilityRangeFadeMode) = 0
```

## Properties

- cast_shadow: int (GeometryInstance3D.ShadowCastingSetting) = 1 [set set_cast_shadows_setting; get get_cast_shadows_setting]

- layer_mask: int = 1 [set set_layer_mask; get get_layer_mask]

- mesh: ImporterMesh [set set_mesh; get get_mesh]

- skeleton_path: NodePath = NodePath("") [set set_skeleton_path; get get_skeleton_path]

- skin: Skin [set set_skin; get get_skin]

- visibility_range_begin: float = 0.0 [set set_visibility_range_begin; get get_visibility_range_begin]

- visibility_range_begin_margin: float = 0.0 [set set_visibility_range_begin_margin; get get_visibility_range_begin_margin]

- visibility_range_end: float = 0.0 [set set_visibility_range_end; get get_visibility_range_end]

- visibility_range_end_margin: float = 0.0 [set set_visibility_range_end_margin; get get_visibility_range_end_margin]

- visibility_range_fade_mode: int (GeometryInstance3D.VisibilityRangeFadeMode) = 0 [set set_visibility_range_fade_mode; get get_visibility_range_fade_mode]
