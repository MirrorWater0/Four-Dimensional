# PhysicsShapeQueryParameters2D

## Meta

- Name: PhysicsShapeQueryParameters2D
- Source: PhysicsShapeQueryParameters2D.xml
- Inherits: RefCounted
- Inheritance Chain: PhysicsShapeQueryParameters2D -> RefCounted -> Object

## Brief Description

Provides parameters for PhysicsDirectSpaceState2D's methods.

## Description

By changing various properties of this object, such as the shape, you can configure the parameters for PhysicsDirectSpaceState2D's methods.

## Quick Reference

```
[properties]
collide_with_areas: bool = false
collide_with_bodies: bool = true
collision_mask: int = 4294967295
exclude: RID[] = []
margin: float = 0.0
motion: Vector2 = Vector2(0, 0)
shape: Resource
shape_rid: RID = RID()
transform: Transform2D = Transform2D(1, 0, 0, 1, 0, 0)
```

## Properties

- collide_with_areas: bool = false [set set_collide_with_areas; get is_collide_with_areas_enabled]
  If true, the query will take Area2Ds into account.

- collide_with_bodies: bool = true [set set_collide_with_bodies; get is_collide_with_bodies_enabled]
  If true, the query will take PhysicsBody2Ds into account.

- collision_mask: int = 4294967295 [set set_collision_mask; get get_collision_mask]
  The physics layers the query will detect (as a bitmask). By default, all collision layers are detected. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

- exclude: RID[] = [] [set set_exclude; get get_exclude]
  The list of object RIDs that will be excluded from collisions. Use CollisionObject2D.get_rid() to get the RID associated with a CollisionObject2D-derived node. **Note:** The returned array is copied and any changes to it will not update the original property value. To update the value you need to modify the returned array, and then assign it to the property again.

- margin: float = 0.0 [set set_margin; get get_margin]
  The collision margin for the shape.

- motion: Vector2 = Vector2(0, 0) [set set_motion; get get_motion]
  The motion of the shape being queried for.

- shape: Resource [set set_shape; get get_shape]
  The Shape2D that will be used for collision/intersection queries. This stores the actual reference which avoids the shape to be released while being used for queries, so always prefer using this over shape_rid.

- shape_rid: RID = RID() [set set_shape_rid; get get_shape_rid]
  The queried shape's RID that will be used for collision/intersection queries. Use this over shape if you want to optimize for performance using the Servers API:

```
var shape_rid = PhysicsServer2D.circle_shape_create()
var radius = 64
PhysicsServer2D.shape_set_data(shape_rid, radius)

var params = PhysicsShapeQueryParameters2D.new()
params.shape_rid = shape_rid

# Execute physics queries here...

# Release the shape when done with physics queries.
PhysicsServer2D.free_rid(shape_rid)
```

```
RID shapeRid = PhysicsServer2D.CircleShapeCreate();
int radius = 64;
PhysicsServer2D.ShapeSetData(shapeRid, radius);

var params = new PhysicsShapeQueryParameters2D();
params.ShapeRid = shapeRid;

// Execute physics queries here...

// Release the shape when done with physics queries.
PhysicsServer2D.FreeRid(shapeRid);
```

- transform: Transform2D = Transform2D(1, 0, 0, 1, 0, 0) [set set_transform; get get_transform]
  The queried shape's transform matrix.
