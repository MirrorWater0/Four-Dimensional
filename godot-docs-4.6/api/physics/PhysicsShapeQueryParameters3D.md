# PhysicsShapeQueryParameters3D

## Meta

- Name: PhysicsShapeQueryParameters3D
- Source: PhysicsShapeQueryParameters3D.xml
- Inherits: RefCounted
- Inheritance Chain: PhysicsShapeQueryParameters3D -> RefCounted -> Object

## Brief Description

Provides parameters for PhysicsDirectSpaceState3D's methods.

## Description

By changing various properties of this object, such as the shape, you can configure the parameters for PhysicsDirectSpaceState3D's methods.

## Quick Reference

```
[properties]
collide_with_areas: bool = false
collide_with_bodies: bool = true
collision_mask: int = 4294967295
exclude: RID[] = []
margin: float = 0.0
motion: Vector3 = Vector3(0, 0, 0)
shape: Resource
shape_rid: RID = RID()
transform: Transform3D = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0)
```

## Properties

- collide_with_areas: bool = false [set set_collide_with_areas; get is_collide_with_areas_enabled]
  If true, the query will take Area3Ds into account.

- collide_with_bodies: bool = true [set set_collide_with_bodies; get is_collide_with_bodies_enabled]
  If true, the query will take PhysicsBody3Ds into account.

- collision_mask: int = 4294967295 [set set_collision_mask; get get_collision_mask]
  The physics layers the query will detect (as a bitmask). By default, all collision layers are detected. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

- exclude: RID[] = [] [set set_exclude; get get_exclude]
  The list of object RIDs that will be excluded from collisions. Use CollisionObject3D.get_rid() to get the RID associated with a CollisionObject3D-derived node. **Note:** The returned array is copied and any changes to it will not update the original property value. To update the value you need to modify the returned array, and then assign it to the property again.

- margin: float = 0.0 [set set_margin; get get_margin]
  The collision margin for the shape.

- motion: Vector3 = Vector3(0, 0, 0) [set set_motion; get get_motion]
  The motion of the shape being queried for.

- shape: Resource [set set_shape; get get_shape]
  The Shape3D that will be used for collision/intersection queries. This stores the actual reference which avoids the shape to be released while being used for queries, so always prefer using this over shape_rid.

- shape_rid: RID = RID() [set set_shape_rid; get get_shape_rid]
  The queried shape's RID that will be used for collision/intersection queries. Use this over shape if you want to optimize for performance using the Servers API:

```
var shape_rid = PhysicsServer3D.sphere_shape_create()
var radius = 2.0
PhysicsServer3D.shape_set_data(shape_rid, radius)

var params = PhysicsShapeQueryParameters3D.new()
params.shape_rid = shape_rid

# Execute physics queries here...

# Release the shape when done with physics queries.
PhysicsServer3D.free_rid(shape_rid)
```

```
RID shapeRid = PhysicsServer3D.SphereShapeCreate();
float radius = 2.0f;
PhysicsServer3D.ShapeSetData(shapeRid, radius);

var params = new PhysicsShapeQueryParameters3D();
params.ShapeRid = shapeRid;

// Execute physics queries here...

// Release the shape when done with physics queries.
PhysicsServer3D.FreeRid(shapeRid);
```

- transform: Transform3D = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0) [set set_transform; get get_transform]
  The queried shape's transform matrix.
