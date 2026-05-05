# PhysicsServer3D

## Meta

- Name: PhysicsServer3D
- Source: PhysicsServer3D.xml
- Inherits: Object
- Inheritance Chain: PhysicsServer3D -> Object

## Brief Description

A server interface for low-level 3D physics access.

## Description

PhysicsServer3D is the server responsible for all 3D physics. It can directly create and manipulate all physics objects: - A *space* is a self-contained world for a physics simulation. It contains bodies, areas, and joints. Its state can be queried for collision and intersection information, and several parameters of the simulation can be modified. - A *shape* is a geometric shape such as a sphere, a box, a cylinder, or a polygon. It can be used for collision detection by adding it to a body/area, possibly with an extra transformation relative to the body/area's origin. Bodies/areas can have multiple (transformed) shapes added to them, and a single shape can be added to bodies/areas multiple times with different local transformations. - A *body* is a physical object which can be in static, kinematic, or rigid mode. Its state (such as position and velocity) can be queried and updated. A force integration callback can be set to customize the body's physics. - An *area* is a region in space which can be used to detect bodies and areas entering and exiting it. A body monitoring callback can be set to report entering/exiting body shapes, and similarly an area monitoring callback can be set. Gravity and damping can be overridden within the area by setting area parameters. - A *joint* is a constraint, either between two bodies or on one body relative to a point. Parameters such as the joint bias and the rest length of a spring joint can be adjusted. Physics objects in PhysicsServer3D may be created and manipulated independently; they do not have to be tied to nodes in the scene tree. **Note:** All the 3D physics nodes use the physics server internally. Adding a physics node to the scene tree will cause a corresponding physics object to be created in the physics server. A rigid body node registers a callback that updates the node's transform with the transform of the respective body object in the physics server (every physics update). An area node registers a callback to inform the area node about overlaps with the respective area object in the physics server. The raycast node queries the direct state of the relevant space in the physics server.

## Quick Reference

```
[methods]
area_add_shape(area: RID, shape: RID, transform: Transform3D = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0), disabled: bool = false) -> void
area_attach_object_instance_id(area: RID, id: int) -> void
area_clear_shapes(area: RID) -> void
area_create() -> RID
area_get_collision_layer(area: RID) -> int [const]
area_get_collision_mask(area: RID) -> int [const]
area_get_object_instance_id(area: RID) -> int [const]
area_get_param(area: RID, param: int (PhysicsServer3D.AreaParameter)) -> Variant [const]
area_get_shape(area: RID, shape_idx: int) -> RID [const]
area_get_shape_count(area: RID) -> int [const]
area_get_shape_transform(area: RID, shape_idx: int) -> Transform3D [const]
area_get_space(area: RID) -> RID [const]
area_get_transform(area: RID) -> Transform3D [const]
area_remove_shape(area: RID, shape_idx: int) -> void
area_set_area_monitor_callback(area: RID, callback: Callable) -> void
area_set_collision_layer(area: RID, layer: int) -> void
area_set_collision_mask(area: RID, mask: int) -> void
area_set_monitor_callback(area: RID, callback: Callable) -> void
area_set_monitorable(area: RID, monitorable: bool) -> void
area_set_param(area: RID, param: int (PhysicsServer3D.AreaParameter), value: Variant) -> void
area_set_ray_pickable(area: RID, enable: bool) -> void
area_set_shape(area: RID, shape_idx: int, shape: RID) -> void
area_set_shape_disabled(area: RID, shape_idx: int, disabled: bool) -> void
area_set_shape_transform(area: RID, shape_idx: int, transform: Transform3D) -> void
area_set_space(area: RID, space: RID) -> void
area_set_transform(area: RID, transform: Transform3D) -> void
body_add_collision_exception(body: RID, excepted_body: RID) -> void
body_add_constant_central_force(body: RID, force: Vector3) -> void
body_add_constant_force(body: RID, force: Vector3, position: Vector3 = Vector3(0, 0, 0)) -> void
body_add_constant_torque(body: RID, torque: Vector3) -> void
body_add_shape(body: RID, shape: RID, transform: Transform3D = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0), disabled: bool = false) -> void
body_apply_central_force(body: RID, force: Vector3) -> void
body_apply_central_impulse(body: RID, impulse: Vector3) -> void
body_apply_force(body: RID, force: Vector3, position: Vector3 = Vector3(0, 0, 0)) -> void
body_apply_impulse(body: RID, impulse: Vector3, position: Vector3 = Vector3(0, 0, 0)) -> void
body_apply_torque(body: RID, torque: Vector3) -> void
body_apply_torque_impulse(body: RID, impulse: Vector3) -> void
body_attach_object_instance_id(body: RID, id: int) -> void
body_clear_shapes(body: RID) -> void
body_create() -> RID
body_get_collision_layer(body: RID) -> int [const]
body_get_collision_mask(body: RID) -> int [const]
body_get_collision_priority(body: RID) -> float [const]
body_get_constant_force(body: RID) -> Vector3 [const]
body_get_constant_torque(body: RID) -> Vector3 [const]
body_get_direct_state(body: RID) -> PhysicsDirectBodyState3D
body_get_max_contacts_reported(body: RID) -> int [const]
body_get_mode(body: RID) -> int (PhysicsServer3D.BodyMode) [const]
body_get_object_instance_id(body: RID) -> int [const]
body_get_param(body: RID, param: int (PhysicsServer3D.BodyParameter)) -> Variant [const]
body_get_shape(body: RID, shape_idx: int) -> RID [const]
body_get_shape_count(body: RID) -> int [const]
body_get_shape_transform(body: RID, shape_idx: int) -> Transform3D [const]
body_get_space(body: RID) -> RID [const]
body_get_state(body: RID, state: int (PhysicsServer3D.BodyState)) -> Variant [const]
body_is_axis_locked(body: RID, axis: int (PhysicsServer3D.BodyAxis)) -> bool [const]
body_is_continuous_collision_detection_enabled(body: RID) -> bool [const]
body_is_omitting_force_integration(body: RID) -> bool [const]
body_remove_collision_exception(body: RID, excepted_body: RID) -> void
body_remove_shape(body: RID, shape_idx: int) -> void
body_reset_mass_properties(body: RID) -> void
body_set_axis_lock(body: RID, axis: int (PhysicsServer3D.BodyAxis), lock: bool) -> void
body_set_axis_velocity(body: RID, axis_velocity: Vector3) -> void
body_set_collision_layer(body: RID, layer: int) -> void
body_set_collision_mask(body: RID, mask: int) -> void
body_set_collision_priority(body: RID, priority: float) -> void
body_set_constant_force(body: RID, force: Vector3) -> void
body_set_constant_torque(body: RID, torque: Vector3) -> void
body_set_enable_continuous_collision_detection(body: RID, enable: bool) -> void
body_set_force_integration_callback(body: RID, callable: Callable, userdata: Variant = null) -> void
body_set_max_contacts_reported(body: RID, amount: int) -> void
body_set_mode(body: RID, mode: int (PhysicsServer3D.BodyMode)) -> void
body_set_omit_force_integration(body: RID, enable: bool) -> void
body_set_param(body: RID, param: int (PhysicsServer3D.BodyParameter), value: Variant) -> void
body_set_ray_pickable(body: RID, enable: bool) -> void
body_set_shape(body: RID, shape_idx: int, shape: RID) -> void
body_set_shape_disabled(body: RID, shape_idx: int, disabled: bool) -> void
body_set_shape_transform(body: RID, shape_idx: int, transform: Transform3D) -> void
body_set_space(body: RID, space: RID) -> void
body_set_state(body: RID, state: int (PhysicsServer3D.BodyState), value: Variant) -> void
body_set_state_sync_callback(body: RID, callable: Callable) -> void
body_test_motion(body: RID, parameters: PhysicsTestMotionParameters3D, result: PhysicsTestMotionResult3D = null) -> bool
box_shape_create() -> RID
capsule_shape_create() -> RID
concave_polygon_shape_create() -> RID
cone_twist_joint_get_param(joint: RID, param: int (PhysicsServer3D.ConeTwistJointParam)) -> float [const]
cone_twist_joint_set_param(joint: RID, param: int (PhysicsServer3D.ConeTwistJointParam), value: float) -> void
convex_polygon_shape_create() -> RID
custom_shape_create() -> RID
cylinder_shape_create() -> RID
free_rid(rid: RID) -> void
generic_6dof_joint_get_flag(joint: RID, axis: int (Vector3.Axis), flag: int (PhysicsServer3D.G6DOFJointAxisFlag)) -> bool [const]
generic_6dof_joint_get_param(joint: RID, axis: int (Vector3.Axis), param: int (PhysicsServer3D.G6DOFJointAxisParam)) -> float [const]
generic_6dof_joint_set_flag(joint: RID, axis: int (Vector3.Axis), flag: int (PhysicsServer3D.G6DOFJointAxisFlag), enable: bool) -> void
generic_6dof_joint_set_param(joint: RID, axis: int (Vector3.Axis), param: int (PhysicsServer3D.G6DOFJointAxisParam), value: float) -> void
get_process_info(process_info: int (PhysicsServer3D.ProcessInfo)) -> int
heightmap_shape_create() -> RID
hinge_joint_get_flag(joint: RID, flag: int (PhysicsServer3D.HingeJointFlag)) -> bool [const]
hinge_joint_get_param(joint: RID, param: int (PhysicsServer3D.HingeJointParam)) -> float [const]
hinge_joint_set_flag(joint: RID, flag: int (PhysicsServer3D.HingeJointFlag), enabled: bool) -> void
hinge_joint_set_param(joint: RID, param: int (PhysicsServer3D.HingeJointParam), value: float) -> void
joint_clear(joint: RID) -> void
joint_create() -> RID
joint_disable_collisions_between_bodies(joint: RID, disable: bool) -> void
joint_get_solver_priority(joint: RID) -> int [const]
joint_get_type(joint: RID) -> int (PhysicsServer3D.JointType) [const]
joint_is_disabled_collisions_between_bodies(joint: RID) -> bool [const]
joint_make_cone_twist(joint: RID, body_A: RID, local_ref_A: Transform3D, body_B: RID, local_ref_B: Transform3D) -> void
joint_make_generic_6dof(joint: RID, body_A: RID, local_ref_A: Transform3D, body_B: RID, local_ref_B: Transform3D) -> void
joint_make_hinge(joint: RID, body_A: RID, hinge_A: Transform3D, body_B: RID, hinge_B: Transform3D) -> void
joint_make_pin(joint: RID, body_A: RID, local_A: Vector3, body_B: RID, local_B: Vector3) -> void
joint_make_slider(joint: RID, body_A: RID, local_ref_A: Transform3D, body_B: RID, local_ref_B: Transform3D) -> void
joint_set_solver_priority(joint: RID, priority: int) -> void
pin_joint_get_local_a(joint: RID) -> Vector3 [const]
pin_joint_get_local_b(joint: RID) -> Vector3 [const]
pin_joint_get_param(joint: RID, param: int (PhysicsServer3D.PinJointParam)) -> float [const]
pin_joint_set_local_a(joint: RID, local_A: Vector3) -> void
pin_joint_set_local_b(joint: RID, local_B: Vector3) -> void
pin_joint_set_param(joint: RID, param: int (PhysicsServer3D.PinJointParam), value: float) -> void
separation_ray_shape_create() -> RID
set_active(active: bool) -> void
shape_get_data(shape: RID) -> Variant [const]
shape_get_margin(shape: RID) -> float [const]
shape_get_type(shape: RID) -> int (PhysicsServer3D.ShapeType) [const]
shape_set_data(shape: RID, data: Variant) -> void
shape_set_margin(shape: RID, margin: float) -> void
slider_joint_get_param(joint: RID, param: int (PhysicsServer3D.SliderJointParam)) -> float [const]
slider_joint_set_param(joint: RID, param: int (PhysicsServer3D.SliderJointParam), value: float) -> void
soft_body_add_collision_exception(body: RID, body_b: RID) -> void
soft_body_apply_central_force(body: RID, force: Vector3) -> void
soft_body_apply_central_impulse(body: RID, impulse: Vector3) -> void
soft_body_apply_point_force(body: RID, point_index: int, force: Vector3) -> void
soft_body_apply_point_impulse(body: RID, point_index: int, impulse: Vector3) -> void
soft_body_create() -> RID
soft_body_get_bounds(body: RID) -> AABB [const]
soft_body_get_collision_layer(body: RID) -> int [const]
soft_body_get_collision_mask(body: RID) -> int [const]
soft_body_get_damping_coefficient(body: RID) -> float [const]
soft_body_get_drag_coefficient(body: RID) -> float [const]
soft_body_get_linear_stiffness(body: RID) -> float [const]
soft_body_get_point_global_position(body: RID, point_index: int) -> Vector3 [const]
soft_body_get_pressure_coefficient(body: RID) -> float [const]
soft_body_get_shrinking_factor(body: RID) -> float [const]
soft_body_get_simulation_precision(body: RID) -> int [const]
soft_body_get_space(body: RID) -> RID [const]
soft_body_get_state(body: RID, state: int (PhysicsServer3D.BodyState)) -> Variant [const]
soft_body_get_total_mass(body: RID) -> float [const]
soft_body_is_point_pinned(body: RID, point_index: int) -> bool [const]
soft_body_move_point(body: RID, point_index: int, global_position: Vector3) -> void
soft_body_pin_point(body: RID, point_index: int, pin: bool) -> void
soft_body_remove_all_pinned_points(body: RID) -> void
soft_body_remove_collision_exception(body: RID, body_b: RID) -> void
soft_body_set_collision_layer(body: RID, layer: int) -> void
soft_body_set_collision_mask(body: RID, mask: int) -> void
soft_body_set_damping_coefficient(body: RID, damping_coefficient: float) -> void
soft_body_set_drag_coefficient(body: RID, drag_coefficient: float) -> void
soft_body_set_linear_stiffness(body: RID, stiffness: float) -> void
soft_body_set_mesh(body: RID, mesh: RID) -> void
soft_body_set_pressure_coefficient(body: RID, pressure_coefficient: float) -> void
soft_body_set_ray_pickable(body: RID, enable: bool) -> void
soft_body_set_shrinking_factor(body: RID, shrinking_factor: float) -> void
soft_body_set_simulation_precision(body: RID, simulation_precision: int) -> void
soft_body_set_space(body: RID, space: RID) -> void
soft_body_set_state(body: RID, state: int (PhysicsServer3D.BodyState), variant: Variant) -> void
soft_body_set_total_mass(body: RID, total_mass: float) -> void
soft_body_set_transform(body: RID, transform: Transform3D) -> void
soft_body_update_rendering_server(body: RID, rendering_server_handler: PhysicsServer3DRenderingServerHandler) -> void
space_create() -> RID
space_get_direct_state(space: RID) -> PhysicsDirectSpaceState3D
space_get_param(space: RID, param: int (PhysicsServer3D.SpaceParameter)) -> float [const]
space_is_active(space: RID) -> bool [const]
space_set_active(space: RID, active: bool) -> void
space_set_param(space: RID, param: int (PhysicsServer3D.SpaceParameter), value: float) -> void
sphere_shape_create() -> RID
world_boundary_shape_create() -> RID
```

## Methods

- area_add_shape(area: RID, shape: RID, transform: Transform3D = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0), disabled: bool = false) -> void
  Adds a shape to the area, along with a transform matrix. Shapes are usually referenced by their index, so you should track which shape has a given index.

- area_attach_object_instance_id(area: RID, id: int) -> void
  Assigns the area to a descendant of Object, so it can exist in the node tree.

- area_clear_shapes(area: RID) -> void
  Removes all shapes from an area. It does not delete the shapes, so they can be reassigned later.

- area_create() -> RID
  Creates a 3D area object in the physics server, and returns the RID that identifies it. The default settings for the created area include a collision layer and mask set to 1, and monitorable set to false. Use area_add_shape() to add shapes to it, use area_set_transform() to set its transform, and use area_set_space() to add the area to a space. If you want the area to be detectable use area_set_monitorable().

- area_get_collision_layer(area: RID) -> int [const]
  Returns the physics layer or layers an area belongs to.

- area_get_collision_mask(area: RID) -> int [const]
  Returns the physics layer or layers an area can contact with.

- area_get_object_instance_id(area: RID) -> int [const]
  Gets the instance ID of the object the area is assigned to.

- area_get_param(area: RID, param: int (PhysicsServer3D.AreaParameter)) -> Variant [const]
  Returns an area parameter value. A list of available parameters is on the AreaParameter constants.

- area_get_shape(area: RID, shape_idx: int) -> RID [const]
  Returns the RID of the nth shape of an area.

- area_get_shape_count(area: RID) -> int [const]
  Returns the number of shapes assigned to an area.

- area_get_shape_transform(area: RID, shape_idx: int) -> Transform3D [const]
  Returns the transform matrix of a shape within an area.

- area_get_space(area: RID) -> RID [const]
  Returns the space assigned to the area.

- area_get_transform(area: RID) -> Transform3D [const]
  Returns the transform matrix for an area.

- area_remove_shape(area: RID, shape_idx: int) -> void
  Removes a shape from an area. It does not delete the shape, so it can be reassigned later.

- area_set_area_monitor_callback(area: RID, callback: Callable) -> void
  Sets the area's area monitor callback. This callback will be called when any other (shape of an) area enters or exits (a shape of) the given area, and must take the following five parameters: 1. an integer status: either AREA_BODY_ADDED or AREA_BODY_REMOVED depending on whether the other area's shape entered or exited the area, 2. an RID area_rid: the RID of the other area that entered or exited the area, 3. an integer instance_id: the ObjectID attached to the other area, 4. an integer area_shape_idx: the index of the shape of the other area that entered or exited the area, 5. an integer self_shape_idx: the index of the shape of the area where the other area entered or exited. By counting (or keeping track of) the shapes that enter and exit, it can be determined if an area (with all its shapes) is entering for the first time or exiting for the last time.

- area_set_collision_layer(area: RID, layer: int) -> void
  Assigns the area to one or many physics layers.

- area_set_collision_mask(area: RID, mask: int) -> void
  Sets which physics layers the area will monitor.

- area_set_monitor_callback(area: RID, callback: Callable) -> void
  Sets the area's body monitor callback. This callback will be called when any other (shape of a) body enters or exits (a shape of) the given area, and must take the following five parameters: 1. an integer status: either AREA_BODY_ADDED or AREA_BODY_REMOVED depending on whether the other body shape entered or exited the area, 2. an RID body_rid: the RID of the body that entered or exited the area, 3. an integer instance_id: the ObjectID attached to the body, 4. an integer body_shape_idx: the index of the shape of the body that entered or exited the area, 5. an integer self_shape_idx: the index of the shape of the area where the body entered or exited. By counting (or keeping track of) the shapes that enter and exit, it can be determined if a body (with all its shapes) is entering for the first time or exiting for the last time.

- area_set_monitorable(area: RID, monitorable: bool) -> void

- area_set_param(area: RID, param: int (PhysicsServer3D.AreaParameter), value: Variant) -> void
  Sets the value for an area parameter. A list of available parameters is on the AreaParameter constants.

- area_set_ray_pickable(area: RID, enable: bool) -> void
  Sets object pickable with rays.

- area_set_shape(area: RID, shape_idx: int, shape: RID) -> void
  Substitutes a given area shape by another. The old shape is selected by its index, the new one by its RID.

- area_set_shape_disabled(area: RID, shape_idx: int, disabled: bool) -> void

- area_set_shape_transform(area: RID, shape_idx: int, transform: Transform3D) -> void
  Sets the transform matrix for an area shape.

- area_set_space(area: RID, space: RID) -> void
  Assigns a space to the area.

- area_set_transform(area: RID, transform: Transform3D) -> void
  Sets the transform matrix for an area.

- body_add_collision_exception(body: RID, excepted_body: RID) -> void
  Adds a body to the list of bodies exempt from collisions.

- body_add_constant_central_force(body: RID, force: Vector3) -> void
  Adds a constant directional force without affecting rotation that keeps being applied over time until cleared with body_set_constant_force(body, Vector3(0, 0, 0)). This is equivalent to using body_add_constant_force() at the body's center of mass.

- body_add_constant_force(body: RID, force: Vector3, position: Vector3 = Vector3(0, 0, 0)) -> void
  Adds a constant positioned force to the body that keeps being applied over time until cleared with body_set_constant_force(body, Vector3(0, 0, 0)). position is the offset from the body origin in global coordinates.

- body_add_constant_torque(body: RID, torque: Vector3) -> void
  Adds a constant rotational force without affecting position that keeps being applied over time until cleared with body_set_constant_torque(body, Vector3(0, 0, 0)).

- body_add_shape(body: RID, shape: RID, transform: Transform3D = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0), disabled: bool = false) -> void
  Adds a shape to the body, along with a transform matrix. Shapes are usually referenced by their index, so you should track which shape has a given index.

- body_apply_central_force(body: RID, force: Vector3) -> void
  Applies a directional force without affecting rotation. A force is time dependent and meant to be applied every physics update. This is equivalent to using body_apply_force() at the body's center of mass.

- body_apply_central_impulse(body: RID, impulse: Vector3) -> void
  Applies a directional impulse without affecting rotation. An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise). This is equivalent to using body_apply_impulse() at the body's center of mass.

- body_apply_force(body: RID, force: Vector3, position: Vector3 = Vector3(0, 0, 0)) -> void
  Applies a positioned force to the body. A force is time dependent and meant to be applied every physics update. position is the offset from the body origin in global coordinates.

- body_apply_impulse(body: RID, impulse: Vector3, position: Vector3 = Vector3(0, 0, 0)) -> void
  Applies a positioned impulse to the body. An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise). position is the offset from the body origin in global coordinates.

- body_apply_torque(body: RID, torque: Vector3) -> void
  Applies a rotational force without affecting position. A force is time dependent and meant to be applied every physics update.

- body_apply_torque_impulse(body: RID, impulse: Vector3) -> void
  Applies a rotational impulse to the body without affecting the position. An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

- body_attach_object_instance_id(body: RID, id: int) -> void
  Assigns the area to a descendant of Object, so it can exist in the node tree.

- body_clear_shapes(body: RID) -> void
  Removes all shapes from a body.

- body_create() -> RID
  Creates a 3D body object in the physics server, and returns the RID that identifies it. The default settings for the created area include a collision layer and mask set to 1, and body mode set to BODY_MODE_RIGID. Use body_add_shape() to add shapes to it, use body_set_state() to set its transform, and use body_set_space() to add the body to a space.

- body_get_collision_layer(body: RID) -> int [const]
  Returns the physics layer or layers a body belongs to.

- body_get_collision_mask(body: RID) -> int [const]
  Returns the physics layer or layers a body can collide with.

- body_get_collision_priority(body: RID) -> float [const]
  Returns the body's collision priority.

- body_get_constant_force(body: RID) -> Vector3 [const]
  Returns the body's total constant positional forces applied during each physics update. See body_add_constant_force() and body_add_constant_central_force().

- body_get_constant_torque(body: RID) -> Vector3 [const]
  Returns the body's total constant rotational forces applied during each physics update. See body_add_constant_torque().

- body_get_direct_state(body: RID) -> PhysicsDirectBodyState3D
  Returns the PhysicsDirectBodyState3D of the body. Returns null if the body is destroyed or removed from the physics space.

- body_get_max_contacts_reported(body: RID) -> int [const]
  Returns the maximum contacts that can be reported. See body_set_max_contacts_reported().

- body_get_mode(body: RID) -> int (PhysicsServer3D.BodyMode) [const]
  Returns the body mode.

- body_get_object_instance_id(body: RID) -> int [const]
  Gets the instance ID of the object the area is assigned to.

- body_get_param(body: RID, param: int (PhysicsServer3D.BodyParameter)) -> Variant [const]
  Returns the value of a body parameter. A list of available parameters is on the BodyParameter constants.

- body_get_shape(body: RID, shape_idx: int) -> RID [const]
  Returns the RID of the nth shape of a body.

- body_get_shape_count(body: RID) -> int [const]
  Returns the number of shapes assigned to a body.

- body_get_shape_transform(body: RID, shape_idx: int) -> Transform3D [const]
  Returns the transform matrix of a body shape.

- body_get_space(body: RID) -> RID [const]
  Returns the RID of the space assigned to a body.

- body_get_state(body: RID, state: int (PhysicsServer3D.BodyState)) -> Variant [const]
  Returns a body state.

- body_is_axis_locked(body: RID, axis: int (PhysicsServer3D.BodyAxis)) -> bool [const]

- body_is_continuous_collision_detection_enabled(body: RID) -> bool [const]
  If true, the continuous collision detection mode is enabled.

- body_is_omitting_force_integration(body: RID) -> bool [const]
  Returns true if the body is omitting the standard force integration. See body_set_omit_force_integration().

- body_remove_collision_exception(body: RID, excepted_body: RID) -> void
  Removes a body from the list of bodies exempt from collisions. Continuous collision detection tries to predict where a moving body will collide, instead of moving it and correcting its movement if it collided.

- body_remove_shape(body: RID, shape_idx: int) -> void
  Removes a shape from a body. The shape is not deleted, so it can be reused afterwards.

- body_reset_mass_properties(body: RID) -> void
  Restores the default inertia and center of mass based on shapes to cancel any custom values previously set using body_set_param().

- body_set_axis_lock(body: RID, axis: int (PhysicsServer3D.BodyAxis), lock: bool) -> void

- body_set_axis_velocity(body: RID, axis_velocity: Vector3) -> void
  Sets an axis velocity. The velocity in the given vector axis will be set as the given vector length. This is useful for jumping behavior.

- body_set_collision_layer(body: RID, layer: int) -> void
  Sets the physics layer or layers a body belongs to.

- body_set_collision_mask(body: RID, mask: int) -> void
  Sets the physics layer or layers a body can collide with.

- body_set_collision_priority(body: RID, priority: float) -> void
  Sets the body's collision priority.

- body_set_constant_force(body: RID, force: Vector3) -> void
  Sets the body's total constant positional forces applied during each physics update. See body_add_constant_force() and body_add_constant_central_force().

- body_set_constant_torque(body: RID, torque: Vector3) -> void
  Sets the body's total constant rotational forces applied during each physics update. See body_add_constant_torque().

- body_set_enable_continuous_collision_detection(body: RID, enable: bool) -> void
  If true, the continuous collision detection mode is enabled. Continuous collision detection tries to predict where a moving body will collide, instead of moving it and correcting its movement if it collided.

- body_set_force_integration_callback(body: RID, callable: Callable, userdata: Variant = null) -> void
  Sets the body's custom force integration callback function to callable. Use an empty Callable ([code skip-lint]Callable()[/code]) to clear the custom callback. The function callable will be called every physics tick, before the standard force integration (see body_set_omit_force_integration()). It can be used for example to update the body's linear and angular velocity based on contact with other bodies. If userdata is not null, the function callable must take the following two parameters: 1. state: a PhysicsDirectBodyState3D, used to retrieve and modify the body's state, 2. [code skip-lint]userdata[/code]: a Variant; its value will be the userdata passed into this method. If userdata is null, then callable must take only the state parameter.

- body_set_max_contacts_reported(body: RID, amount: int) -> void
  Sets the maximum contacts to report. Bodies can keep a log of the contacts with other bodies. This is enabled by setting the maximum number of contacts reported to a number greater than 0.

- body_set_mode(body: RID, mode: int (PhysicsServer3D.BodyMode)) -> void
  Sets the body mode.

- body_set_omit_force_integration(body: RID, enable: bool) -> void
  Sets whether the body omits the standard force integration. If enable is true, the body will not automatically use applied forces, torques, and damping to update the body's linear and angular velocity. In this case, body_set_force_integration_callback() can be used to manually update the linear and angular velocity instead. This method is called when the property RigidBody3D.custom_integrator is set.

- body_set_param(body: RID, param: int (PhysicsServer3D.BodyParameter), value: Variant) -> void
  Sets a body parameter. A list of available parameters is on the BodyParameter constants.

- body_set_ray_pickable(body: RID, enable: bool) -> void
  Sets the body pickable with rays if enable is set.

- body_set_shape(body: RID, shape_idx: int, shape: RID) -> void
  Substitutes a given body shape by another. The old shape is selected by its index, the new one by its RID.

- body_set_shape_disabled(body: RID, shape_idx: int, disabled: bool) -> void

- body_set_shape_transform(body: RID, shape_idx: int, transform: Transform3D) -> void
  Sets the transform matrix for a body shape.

- body_set_space(body: RID, space: RID) -> void
  Assigns a space to the body (see space_create()).

- body_set_state(body: RID, state: int (PhysicsServer3D.BodyState), value: Variant) -> void
  Sets a body state.

- body_set_state_sync_callback(body: RID, callable: Callable) -> void
  Sets the body's state synchronization callback function to callable. Use an empty Callable ([code skip-lint]Callable()[/code]) to clear the callback. The function callable will be called every physics frame, assuming that the body was active during the previous physics tick, and can be used to fetch the latest state from the physics server. The function callable must take the following parameters: 1. state: a PhysicsDirectBodyState3D, used to retrieve the body's state.

- body_test_motion(body: RID, parameters: PhysicsTestMotionParameters3D, result: PhysicsTestMotionResult3D = null) -> bool
  Returns true if a collision would result from moving along a motion vector from a given point in space. PhysicsTestMotionParameters3D is passed to set motion parameters. PhysicsTestMotionResult3D can be passed to return additional information.

- box_shape_create() -> RID
  Creates a 3D box shape in the physics server, and returns the RID that identifies it. Use shape_set_data() to set the box's half-extents.

- capsule_shape_create() -> RID
  Creates a 3D capsule shape in the physics server, and returns the RID that identifies it. Use shape_set_data() to set the capsule's height and radius.

- concave_polygon_shape_create() -> RID
  Creates a 3D concave polygon shape in the physics server, and returns the RID that identifies it. Use shape_set_data() to set the concave polygon's triangles.

- cone_twist_joint_get_param(joint: RID, param: int (PhysicsServer3D.ConeTwistJointParam)) -> float [const]
  Gets a cone twist joint parameter.

- cone_twist_joint_set_param(joint: RID, param: int (PhysicsServer3D.ConeTwistJointParam), value: float) -> void
  Sets a cone twist joint parameter.

- convex_polygon_shape_create() -> RID
  Creates a 3D convex polygon shape in the physics server, and returns the RID that identifies it. Use shape_set_data() to set the convex polygon's points.

- custom_shape_create() -> RID
  Creates a custom shape in the physics server, and returns the RID that identifies it. Use shape_set_data() to set the shape's data. **Note:** Custom shapes are not supported by the built-in physics servers, so calling this method always produces an error when using Godot Physics or Jolt Physics. Custom physics servers implemented as GDExtensions may support a custom shape.

- cylinder_shape_create() -> RID
  Creates a 3D cylinder shape in the physics server, and returns the RID that identifies it. Use shape_set_data() to set the cylinder's height and radius.

- free_rid(rid: RID) -> void
  Destroys any of the objects created by PhysicsServer3D. If the RID passed is not one of the objects that can be created by PhysicsServer3D, an error will be sent to the console.

- generic_6dof_joint_get_flag(joint: RID, axis: int (Vector3.Axis), flag: int (PhysicsServer3D.G6DOFJointAxisFlag)) -> bool [const]
  Returns the value of a generic 6DOF joint flag.

- generic_6dof_joint_get_param(joint: RID, axis: int (Vector3.Axis), param: int (PhysicsServer3D.G6DOFJointAxisParam)) -> float [const]
  Returns the value of a generic 6DOF joint parameter.

- generic_6dof_joint_set_flag(joint: RID, axis: int (Vector3.Axis), flag: int (PhysicsServer3D.G6DOFJointAxisFlag), enable: bool) -> void
  Sets the value of a given generic 6DOF joint flag.

- generic_6dof_joint_set_param(joint: RID, axis: int (Vector3.Axis), param: int (PhysicsServer3D.G6DOFJointAxisParam), value: float) -> void
  Sets the value of a given generic 6DOF joint parameter.

- get_process_info(process_info: int (PhysicsServer3D.ProcessInfo)) -> int
  Returns the value of a physics engine state specified by process_info.

- heightmap_shape_create() -> RID
  Creates a 3D heightmap shape in the physics server, and returns the RID that identifies it. Use shape_set_data() to set the heightmap's data.

- hinge_joint_get_flag(joint: RID, flag: int (PhysicsServer3D.HingeJointFlag)) -> bool [const]
  Gets a hinge joint flag.

- hinge_joint_get_param(joint: RID, param: int (PhysicsServer3D.HingeJointParam)) -> float [const]
  Gets a hinge joint parameter.

- hinge_joint_set_flag(joint: RID, flag: int (PhysicsServer3D.HingeJointFlag), enabled: bool) -> void
  Sets a hinge joint flag.

- hinge_joint_set_param(joint: RID, param: int (PhysicsServer3D.HingeJointParam), value: float) -> void
  Sets a hinge joint parameter.

- joint_clear(joint: RID) -> void

- joint_create() -> RID

- joint_disable_collisions_between_bodies(joint: RID, disable: bool) -> void
  Sets whether the bodies attached to the Joint3D will collide with each other.

- joint_get_solver_priority(joint: RID) -> int [const]
  Gets the priority value of the Joint3D.

- joint_get_type(joint: RID) -> int (PhysicsServer3D.JointType) [const]
  Returns the type of the Joint3D.

- joint_is_disabled_collisions_between_bodies(joint: RID) -> bool [const]
  Returns whether the bodies attached to the Joint3D will collide with each other.

- joint_make_cone_twist(joint: RID, body_A: RID, local_ref_A: Transform3D, body_B: RID, local_ref_B: Transform3D) -> void

- joint_make_generic_6dof(joint: RID, body_A: RID, local_ref_A: Transform3D, body_B: RID, local_ref_B: Transform3D) -> void
  Make the joint a generic six degrees of freedom (6DOF) joint. Use generic_6dof_joint_set_flag() and generic_6dof_joint_set_param() to set the joint's flags and parameters respectively.

- joint_make_hinge(joint: RID, body_A: RID, hinge_A: Transform3D, body_B: RID, hinge_B: Transform3D) -> void

- joint_make_pin(joint: RID, body_A: RID, local_A: Vector3, body_B: RID, local_B: Vector3) -> void

- joint_make_slider(joint: RID, body_A: RID, local_ref_A: Transform3D, body_B: RID, local_ref_B: Transform3D) -> void

- joint_set_solver_priority(joint: RID, priority: int) -> void
  Sets the priority value of the Joint3D.

- pin_joint_get_local_a(joint: RID) -> Vector3 [const]
  Returns position of the joint in the local space of body a of the joint.

- pin_joint_get_local_b(joint: RID) -> Vector3 [const]
  Returns position of the joint in the local space of body b of the joint.

- pin_joint_get_param(joint: RID, param: int (PhysicsServer3D.PinJointParam)) -> float [const]
  Gets a pin joint parameter.

- pin_joint_set_local_a(joint: RID, local_A: Vector3) -> void
  Sets position of the joint in the local space of body a of the joint.

- pin_joint_set_local_b(joint: RID, local_B: Vector3) -> void
  Sets position of the joint in the local space of body b of the joint.

- pin_joint_set_param(joint: RID, param: int (PhysicsServer3D.PinJointParam), value: float) -> void
  Sets a pin joint parameter.

- separation_ray_shape_create() -> RID
  Creates a 3D separation ray shape in the physics server, and returns the RID that identifies it. Use shape_set_data() to set the shape's length and slide_on_slope properties.

- set_active(active: bool) -> void
  Activates or deactivates the 3D physics engine.

- shape_get_data(shape: RID) -> Variant [const]
  Returns the shape data that configures the shape, such as the half-extents of a box or the triangles of a concave (trimesh) shape. See shape_set_data() for the precise format of this data in each case.

- shape_get_margin(shape: RID) -> float [const]
  Returns the collision margin for the shape. **Note:** This is not used in Godot Physics, so will always return 0.

- shape_get_type(shape: RID) -> int (PhysicsServer3D.ShapeType) [const]
  Returns the shape's type.

- shape_set_data(shape: RID, data: Variant) -> void
  Sets the shape data that configures the shape. The data to be passed depends on the shape's type (see shape_get_type()): - SHAPE_WORLD_BOUNDARY: a Plane, - SHAPE_SEPARATION_RAY: a dictionary containing the key "length" with a float value and the key "slide_on_slope" with a bool value, - SHAPE_SPHERE: a float that is the radius of the sphere, - SHAPE_BOX: a Vector3 containing the half-extents of the box, - SHAPE_CAPSULE: a dictionary containing the keys "height" and "radius" with float values, - SHAPE_CYLINDER: a dictionary containing the keys "height" and "radius" with float values, - SHAPE_CONVEX_POLYGON: a PackedVector3Array of points defining a convex polygon (the shape will be the convex hull of the points), - SHAPE_CONCAVE_POLYGON: a dictionary containing the key "faces" with a PackedVector3Array value (with a length divisible by 3, so that each 3-tuple of points forms a face) and the key "backface_collision" with a bool value, - SHAPE_HEIGHTMAP: a dictionary containing the keys "width" and "depth" with int values, and the key "heights" with a value that is a packed array of floats of length width * depth (that is a PackedFloat32Array, or a PackedFloat64Array if Godot was compiled with the precision=double option), and optionally the keys "min_height" and "max_height" with float values, - SHAPE_SOFT_BODY: the input data is ignored and this method has no effect, - SHAPE_CUSTOM: the input data is interpreted by a custom physics server, if it supports custom shapes.

- shape_set_margin(shape: RID, margin: float) -> void
  Sets the collision margin for the shape. **Note:** This is not used in Godot Physics.

- slider_joint_get_param(joint: RID, param: int (PhysicsServer3D.SliderJointParam)) -> float [const]
  Gets a slider joint parameter.

- slider_joint_set_param(joint: RID, param: int (PhysicsServer3D.SliderJointParam), value: float) -> void
  Gets a slider joint parameter.

- soft_body_add_collision_exception(body: RID, body_b: RID) -> void
  Adds the given body to the list of bodies exempt from collisions.

- soft_body_apply_central_force(body: RID, force: Vector3) -> void
  Distributes and applies a force to all points. A force is time dependent and meant to be applied every physics update.

- soft_body_apply_central_impulse(body: RID, impulse: Vector3) -> void
  Distributes and applies an impulse to all points. An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

- soft_body_apply_point_force(body: RID, point_index: int, force: Vector3) -> void
  Applies a force to a point. A force is time dependent and meant to be applied every physics update.

- soft_body_apply_point_impulse(body: RID, point_index: int, impulse: Vector3) -> void
  Applies an impulse to a point. An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

- soft_body_create() -> RID
  Creates a new soft body and returns its internal RID.

- soft_body_get_bounds(body: RID) -> AABB [const]
  Returns the bounds of the given soft body in global coordinates.

- soft_body_get_collision_layer(body: RID) -> int [const]
  Returns the physics layer or layers that the given soft body belongs to.

- soft_body_get_collision_mask(body: RID) -> int [const]
  Returns the physics layer or layers that the given soft body can collide with.

- soft_body_get_damping_coefficient(body: RID) -> float [const]
  Returns the damping coefficient of the given soft body.

- soft_body_get_drag_coefficient(body: RID) -> float [const]
  Returns the drag coefficient of the given soft body.

- soft_body_get_linear_stiffness(body: RID) -> float [const]
  Returns the linear stiffness of the given soft body.

- soft_body_get_point_global_position(body: RID, point_index: int) -> Vector3 [const]
  Returns the current position of the given soft body point in global coordinates.

- soft_body_get_pressure_coefficient(body: RID) -> float [const]
  Returns the pressure coefficient of the given soft body.

- soft_body_get_shrinking_factor(body: RID) -> float [const]
  Returns the shrinking factor of the given soft body.

- soft_body_get_simulation_precision(body: RID) -> int [const]
  Returns the simulation precision of the given soft body.

- soft_body_get_space(body: RID) -> RID [const]
  Returns the RID of the space assigned to the given soft body.

- soft_body_get_state(body: RID, state: int (PhysicsServer3D.BodyState)) -> Variant [const]
  Returns the given soft body state. **Note:** Godot's default physics implementation does not support BODY_STATE_LINEAR_VELOCITY, BODY_STATE_ANGULAR_VELOCITY, BODY_STATE_SLEEPING, or BODY_STATE_CAN_SLEEP.

- soft_body_get_total_mass(body: RID) -> float [const]
  Returns the total mass assigned to the given soft body.

- soft_body_is_point_pinned(body: RID, point_index: int) -> bool [const]
  Returns whether the given soft body point is pinned.

- soft_body_move_point(body: RID, point_index: int, global_position: Vector3) -> void
  Moves the given soft body point to a position in global coordinates.

- soft_body_pin_point(body: RID, point_index: int, pin: bool) -> void
  Pins or unpins the given soft body point based on the value of pin. **Note:** Pinning a point effectively makes it kinematic, preventing it from being affected by forces, but you can still move it using soft_body_move_point().

- soft_body_remove_all_pinned_points(body: RID) -> void
  Unpins all points of the given soft body.

- soft_body_remove_collision_exception(body: RID, body_b: RID) -> void
  Removes the given body from the list of bodies exempt from collisions.

- soft_body_set_collision_layer(body: RID, layer: int) -> void
  Sets the physics layer or layers the given soft body belongs to.

- soft_body_set_collision_mask(body: RID, mask: int) -> void
  Sets the physics layer or layers the given soft body can collide with.

- soft_body_set_damping_coefficient(body: RID, damping_coefficient: float) -> void
  Sets the damping coefficient of the given soft body. Higher values will slow down the body more noticeably when forces are applied.

- soft_body_set_drag_coefficient(body: RID, drag_coefficient: float) -> void
  Sets the drag coefficient of the given soft body. Higher values increase this body's air resistance. **Note:** This value is currently unused by Godot's default physics implementation.

- soft_body_set_linear_stiffness(body: RID, stiffness: float) -> void
  Sets the linear stiffness of the given soft body. Higher values will result in a stiffer body, while lower values will increase the body's ability to bend. The value can be between 0.0 and 1.0 (inclusive).

- soft_body_set_mesh(body: RID, mesh: RID) -> void
  Sets the mesh of the given soft body.

- soft_body_set_pressure_coefficient(body: RID, pressure_coefficient: float) -> void
  Sets the pressure coefficient of the given soft body. Simulates pressure build-up from inside this body. Higher values increase the strength of this effect.

- soft_body_set_ray_pickable(body: RID, enable: bool) -> void
  Sets whether the given soft body will be pickable when using object picking.

- soft_body_set_shrinking_factor(body: RID, shrinking_factor: float) -> void
  Sets the shrinking factor of the given soft body.

- soft_body_set_simulation_precision(body: RID, simulation_precision: int) -> void
  Sets the simulation precision of the given soft body. Increasing this value will improve the resulting simulation, but can affect performance. Use with care.

- soft_body_set_space(body: RID, space: RID) -> void
  Assigns a space to the given soft body (see space_create()).

- soft_body_set_state(body: RID, state: int (PhysicsServer3D.BodyState), variant: Variant) -> void
  Sets the given body state for the given body. **Note:** Godot's default physics implementation does not support BODY_STATE_LINEAR_VELOCITY, BODY_STATE_ANGULAR_VELOCITY, BODY_STATE_SLEEPING, or BODY_STATE_CAN_SLEEP.

- soft_body_set_total_mass(body: RID, total_mass: float) -> void
  Sets the total mass for the given soft body.

- soft_body_set_transform(body: RID, transform: Transform3D) -> void
  Sets the global transform of the given soft body.

- soft_body_update_rendering_server(body: RID, rendering_server_handler: PhysicsServer3DRenderingServerHandler) -> void
  Requests that the physics server updates the rendering server with the latest positions of the given soft body's points through the rendering_server_handler interface.

- space_create() -> RID
  Creates a space. A space is a collection of parameters for the physics engine that can be assigned to an area or a body. It can be assigned to an area with area_set_space(), or to a body with body_set_space().

- space_get_direct_state(space: RID) -> PhysicsDirectSpaceState3D
  Returns the state of a space, a PhysicsDirectSpaceState3D. This object can be used to make collision/intersection queries.

- space_get_param(space: RID, param: int (PhysicsServer3D.SpaceParameter)) -> float [const]
  Returns the value of a space parameter.

- space_is_active(space: RID) -> bool [const]
  Returns whether the space is active.

- space_set_active(space: RID, active: bool) -> void
  Marks a space as active. It will not have an effect, unless it is assigned to an area or body.

- space_set_param(space: RID, param: int (PhysicsServer3D.SpaceParameter), value: float) -> void
  Sets the value for a space parameter. A list of available parameters is on the SpaceParameter constants.

- sphere_shape_create() -> RID
  Creates a 3D sphere shape in the physics server, and returns the RID that identifies it. Use shape_set_data() to set the sphere's radius.

- world_boundary_shape_create() -> RID
  Creates a 3D world boundary shape in the physics server, and returns the RID that identifies it. Use shape_set_data() to set the shape's normal direction and distance properties.

## Constants

### Enum JointType

- JOINT_TYPE_PIN = 0
  The Joint3D is a PinJoint3D.

- JOINT_TYPE_HINGE = 1
  The Joint3D is a HingeJoint3D.

- JOINT_TYPE_SLIDER = 2
  The Joint3D is a SliderJoint3D.

- JOINT_TYPE_CONE_TWIST = 3
  The Joint3D is a ConeTwistJoint3D.

- JOINT_TYPE_6DOF = 4
  The Joint3D is a Generic6DOFJoint3D.

- JOINT_TYPE_MAX = 5
  Represents the size of the JointType enum.

### Enum PinJointParam

- PIN_JOINT_BIAS = 0
  The strength with which the pinned objects try to stay in positional relation to each other. The higher, the stronger.

- PIN_JOINT_DAMPING = 1
  The strength with which the pinned objects try to stay in velocity relation to each other. The higher, the stronger.

- PIN_JOINT_IMPULSE_CLAMP = 2
  If above 0, this value is the maximum value for an impulse that this Joint3D puts on its ends.

### Enum HingeJointParam

- HINGE_JOINT_BIAS = 0
  The speed with which the two bodies get pulled together when they move in different directions.

- HINGE_JOINT_LIMIT_UPPER = 1
  The maximum rotation across the Hinge.

- HINGE_JOINT_LIMIT_LOWER = 2
  The minimum rotation across the Hinge.

- HINGE_JOINT_LIMIT_BIAS = 3
  The speed with which the rotation across the axis perpendicular to the hinge gets corrected.

- HINGE_JOINT_LIMIT_SOFTNESS = 4

- HINGE_JOINT_LIMIT_RELAXATION = 5
  The lower this value, the more the rotation gets slowed down.

- HINGE_JOINT_MOTOR_TARGET_VELOCITY = 6
  Target speed for the motor.

- HINGE_JOINT_MOTOR_MAX_IMPULSE = 7
  Maximum acceleration for the motor.

### Enum HingeJointFlag

- HINGE_JOINT_FLAG_USE_LIMIT = 0
  If true, the Hinge has a maximum and a minimum rotation.

- HINGE_JOINT_FLAG_ENABLE_MOTOR = 1
  If true, a motor turns the Hinge.

### Enum SliderJointParam

- SLIDER_JOINT_LINEAR_LIMIT_UPPER = 0
  The maximum difference between the pivot points on their X axis before damping happens.

- SLIDER_JOINT_LINEAR_LIMIT_LOWER = 1
  The minimum difference between the pivot points on their X axis before damping happens.

- SLIDER_JOINT_LINEAR_LIMIT_SOFTNESS = 2
  A factor applied to the movement across the slider axis once the limits get surpassed. The lower, the slower the movement.

- SLIDER_JOINT_LINEAR_LIMIT_RESTITUTION = 3
  The amount of restitution once the limits are surpassed. The lower, the more velocity-energy gets lost.

- SLIDER_JOINT_LINEAR_LIMIT_DAMPING = 4
  The amount of damping once the slider limits are surpassed.

- SLIDER_JOINT_LINEAR_MOTION_SOFTNESS = 5
  A factor applied to the movement across the slider axis as long as the slider is in the limits. The lower, the slower the movement.

- SLIDER_JOINT_LINEAR_MOTION_RESTITUTION = 6
  The amount of restitution inside the slider limits.

- SLIDER_JOINT_LINEAR_MOTION_DAMPING = 7
  The amount of damping inside the slider limits.

- SLIDER_JOINT_LINEAR_ORTHOGONAL_SOFTNESS = 8
  A factor applied to the movement across axes orthogonal to the slider.

- SLIDER_JOINT_LINEAR_ORTHOGONAL_RESTITUTION = 9
  The amount of restitution when movement is across axes orthogonal to the slider.

- SLIDER_JOINT_LINEAR_ORTHOGONAL_DAMPING = 10
  The amount of damping when movement is across axes orthogonal to the slider.

- SLIDER_JOINT_ANGULAR_LIMIT_UPPER = 11
  The upper limit of rotation in the slider.

- SLIDER_JOINT_ANGULAR_LIMIT_LOWER = 12
  The lower limit of rotation in the slider.

- SLIDER_JOINT_ANGULAR_LIMIT_SOFTNESS = 13
  A factor applied to the all rotation once the limit is surpassed.

- SLIDER_JOINT_ANGULAR_LIMIT_RESTITUTION = 14
  The amount of restitution of the rotation when the limit is surpassed.

- SLIDER_JOINT_ANGULAR_LIMIT_DAMPING = 15
  The amount of damping of the rotation when the limit is surpassed.

- SLIDER_JOINT_ANGULAR_MOTION_SOFTNESS = 16
  A factor that gets applied to the all rotation in the limits.

- SLIDER_JOINT_ANGULAR_MOTION_RESTITUTION = 17
  The amount of restitution of the rotation in the limits.

- SLIDER_JOINT_ANGULAR_MOTION_DAMPING = 18
  The amount of damping of the rotation in the limits.

- SLIDER_JOINT_ANGULAR_ORTHOGONAL_SOFTNESS = 19
  A factor that gets applied to the all rotation across axes orthogonal to the slider.

- SLIDER_JOINT_ANGULAR_ORTHOGONAL_RESTITUTION = 20
  The amount of restitution of the rotation across axes orthogonal to the slider.

- SLIDER_JOINT_ANGULAR_ORTHOGONAL_DAMPING = 21
  The amount of damping of the rotation across axes orthogonal to the slider.

- SLIDER_JOINT_MAX = 22
  Represents the size of the SliderJointParam enum.

### Enum ConeTwistJointParam

- CONE_TWIST_JOINT_SWING_SPAN = 0
  Swing is rotation from side to side, around the axis perpendicular to the twist axis. The swing span defines, how much rotation will not get corrected along the swing axis. Could be defined as looseness in the ConeTwistJoint3D. If below 0.05, this behavior is locked.

- CONE_TWIST_JOINT_TWIST_SPAN = 1
  Twist is the rotation around the twist axis, this value defined how far the joint can twist. Twist is locked if below 0.05.

- CONE_TWIST_JOINT_BIAS = 2
  The speed with which the swing or twist will take place. The higher, the faster.

- CONE_TWIST_JOINT_SOFTNESS = 3
  The ease with which the Joint3D twists, if it's too low, it takes more force to twist the joint.

- CONE_TWIST_JOINT_RELAXATION = 4
  Defines, how fast the swing- and twist-speed-difference on both sides gets synced.

### Enum G6DOFJointAxisParam

- G6DOF_JOINT_LINEAR_LOWER_LIMIT = 0
  The minimum difference between the pivot points' axes.

- G6DOF_JOINT_LINEAR_UPPER_LIMIT = 1
  The maximum difference between the pivot points' axes.

- G6DOF_JOINT_LINEAR_LIMIT_SOFTNESS = 2
  A factor that gets applied to the movement across the axes. The lower, the slower the movement.

- G6DOF_JOINT_LINEAR_RESTITUTION = 3
  The amount of restitution on the axes movement. The lower, the more velocity-energy gets lost.

- G6DOF_JOINT_LINEAR_DAMPING = 4
  The amount of damping that happens at the linear motion across the axes.

- G6DOF_JOINT_LINEAR_MOTOR_TARGET_VELOCITY = 5
  The velocity that the joint's linear motor will attempt to reach.

- G6DOF_JOINT_LINEAR_MOTOR_FORCE_LIMIT = 6
  The maximum force that the linear motor can apply while trying to reach the target velocity.

- G6DOF_JOINT_LINEAR_SPRING_STIFFNESS = 7

- G6DOF_JOINT_LINEAR_SPRING_DAMPING = 8

- G6DOF_JOINT_LINEAR_SPRING_EQUILIBRIUM_POINT = 9

- G6DOF_JOINT_ANGULAR_LOWER_LIMIT = 10
  The minimum rotation in negative direction to break loose and rotate around the axes.

- G6DOF_JOINT_ANGULAR_UPPER_LIMIT = 11
  The minimum rotation in positive direction to break loose and rotate around the axes.

- G6DOF_JOINT_ANGULAR_LIMIT_SOFTNESS = 12
  A factor that gets multiplied onto all rotations across the axes.

- G6DOF_JOINT_ANGULAR_DAMPING = 13
  The amount of rotational damping across the axes. The lower, the more damping occurs.

- G6DOF_JOINT_ANGULAR_RESTITUTION = 14
  The amount of rotational restitution across the axes. The lower, the more restitution occurs.

- G6DOF_JOINT_ANGULAR_FORCE_LIMIT = 15
  The maximum amount of force that can occur, when rotating around the axes.

- G6DOF_JOINT_ANGULAR_ERP = 16
  When correcting the crossing of limits in rotation across the axes, this error tolerance factor defines how much the correction gets slowed down. The lower, the slower.

- G6DOF_JOINT_ANGULAR_MOTOR_TARGET_VELOCITY = 17
  Target speed for the motor at the axes.

- G6DOF_JOINT_ANGULAR_MOTOR_FORCE_LIMIT = 18
  Maximum acceleration for the motor at the axes.

- G6DOF_JOINT_ANGULAR_SPRING_STIFFNESS = 19

- G6DOF_JOINT_ANGULAR_SPRING_DAMPING = 20

- G6DOF_JOINT_ANGULAR_SPRING_EQUILIBRIUM_POINT = 21

- G6DOF_JOINT_MAX = 22
  Represents the size of the G6DOFJointAxisParam enum.

### Enum G6DOFJointAxisFlag

- G6DOF_JOINT_FLAG_ENABLE_LINEAR_LIMIT = 0
  If set, linear motion is possible within the given limits.

- G6DOF_JOINT_FLAG_ENABLE_ANGULAR_LIMIT = 1
  If set, rotational motion is possible.

- G6DOF_JOINT_FLAG_ENABLE_ANGULAR_SPRING = 2

- G6DOF_JOINT_FLAG_ENABLE_LINEAR_SPRING = 3

- G6DOF_JOINT_FLAG_ENABLE_MOTOR = 4
  If set, there is a rotational motor across these axes.

- G6DOF_JOINT_FLAG_ENABLE_LINEAR_MOTOR = 5
  If set, there is a linear motor on this axis that targets a specific velocity.

- G6DOF_JOINT_FLAG_MAX = 6
  Represents the size of the G6DOFJointAxisFlag enum.

### Enum ShapeType

- SHAPE_WORLD_BOUNDARY = 0
  Constant for creating a world boundary shape (used by the WorldBoundaryShape3D resource).

- SHAPE_SEPARATION_RAY = 1
  Constant for creating a separation ray shape (used by the SeparationRayShape3D resource).

- SHAPE_SPHERE = 2
  Constant for creating a sphere shape (used by the SphereShape3D resource).

- SHAPE_BOX = 3
  Constant for creating a box shape (used by the BoxShape3D resource).

- SHAPE_CAPSULE = 4
  Constant for creating a capsule shape (used by the CapsuleShape3D resource).

- SHAPE_CYLINDER = 5
  Constant for creating a cylinder shape (used by the CylinderShape3D resource).

- SHAPE_CONVEX_POLYGON = 6
  Constant for creating a convex polygon shape (used by the ConvexPolygonShape3D resource).

- SHAPE_CONCAVE_POLYGON = 7
  Constant for creating a concave polygon (trimesh) shape (used by the ConcavePolygonShape3D resource).

- SHAPE_HEIGHTMAP = 8
  Constant for creating a heightmap shape (used by the HeightMapShape3D resource).

- SHAPE_SOFT_BODY = 9
  Constant used internally for a soft body shape. Any attempt to create this kind of shape results in an error.

- SHAPE_CUSTOM = 10
  Constant used internally for a custom shape. Any attempt to create this kind of shape results in an error when using Godot Physics or Jolt Physics.

### Enum AreaParameter

- AREA_PARAM_GRAVITY_OVERRIDE_MODE = 0
  Constant to set/get gravity override mode in an area. See AreaSpaceOverrideMode for possible values.

- AREA_PARAM_GRAVITY = 1
  Constant to set/get gravity strength in an area.

- AREA_PARAM_GRAVITY_VECTOR = 2
  Constant to set/get gravity vector/center in an area.

- AREA_PARAM_GRAVITY_IS_POINT = 3
  Constant to set/get whether the gravity vector of an area is a direction, or a center point.

- AREA_PARAM_GRAVITY_POINT_UNIT_DISTANCE = 4
  Constant to set/get the distance at which the gravity strength is equal to the gravity controlled by AREA_PARAM_GRAVITY. For example, on a planet 100 meters in radius with a surface gravity of 4.0 m/s², set the gravity to 4.0 and the unit distance to 100.0. The gravity will have falloff according to the inverse square law, so in the example, at 200 meters from the center the gravity will be 1.0 m/s² (twice the distance, 1/4th the gravity), at 50 meters it will be 16.0 m/s² (half the distance, 4x the gravity), and so on. The above is true only when the unit distance is a positive number. When this is set to 0.0, the gravity will be constant regardless of distance.

- AREA_PARAM_LINEAR_DAMP_OVERRIDE_MODE = 5
  Constant to set/get linear damping override mode in an area. See AreaSpaceOverrideMode for possible values.

- AREA_PARAM_LINEAR_DAMP = 6
  Constant to set/get the linear damping factor of an area.

- AREA_PARAM_ANGULAR_DAMP_OVERRIDE_MODE = 7
  Constant to set/get angular damping override mode in an area. See AreaSpaceOverrideMode for possible values.

- AREA_PARAM_ANGULAR_DAMP = 8
  Constant to set/get the angular damping factor of an area.

- AREA_PARAM_PRIORITY = 9
  Constant to set/get the priority (order of processing) of an area.

- AREA_PARAM_WIND_FORCE_MAGNITUDE = 10
  Constant to set/get the magnitude of area-specific wind force. This wind force only applies to SoftBody3D nodes. Other physics bodies are currently not affected by wind.

- AREA_PARAM_WIND_SOURCE = 11
  Constant to set/get the 3D vector that specifies the origin from which an area-specific wind blows.

- AREA_PARAM_WIND_DIRECTION = 12
  Constant to set/get the 3D vector that specifies the direction in which an area-specific wind blows.

- AREA_PARAM_WIND_ATTENUATION_FACTOR = 13
  Constant to set/get the exponential rate at which wind force decreases with distance from its origin.

### Enum AreaSpaceOverrideMode

- AREA_SPACE_OVERRIDE_DISABLED = 0
  This area does not affect gravity/damp. These are generally areas that exist only to detect collisions, and objects entering or exiting them.

- AREA_SPACE_OVERRIDE_COMBINE = 1
  This area adds its gravity/damp values to whatever has been calculated so far. This way, many overlapping areas can combine their physics to make interesting effects.

- AREA_SPACE_OVERRIDE_COMBINE_REPLACE = 2
  This area adds its gravity/damp values to whatever has been calculated so far. Then stops taking into account the rest of the areas, even the default one.

- AREA_SPACE_OVERRIDE_REPLACE = 3
  This area replaces any gravity/damp, even the default one, and stops taking into account the rest of the areas.

- AREA_SPACE_OVERRIDE_REPLACE_COMBINE = 4
  This area replaces any gravity/damp calculated so far, but keeps calculating the rest of the areas, down to the default one.

### Enum BodyMode

- BODY_MODE_STATIC = 0
  Constant for static bodies. In this mode, a body can be only moved by user code and doesn't collide with other bodies along its path when moved.

- BODY_MODE_KINEMATIC = 1
  Constant for kinematic bodies. In this mode, a body can be only moved by user code and collides with other bodies along its path.

- BODY_MODE_RIGID = 2
  Constant for rigid bodies. In this mode, a body can be pushed by other bodies and has forces applied.

- BODY_MODE_RIGID_LINEAR = 3
  Constant for linear rigid bodies. In this mode, a body can not rotate, and only its linear velocity is affected by external forces.

### Enum BodyParameter

- BODY_PARAM_BOUNCE = 0
  Constant to set/get a body's bounce factor.

- BODY_PARAM_FRICTION = 1
  Constant to set/get a body's friction.

- BODY_PARAM_MASS = 2
  Constant to set/get a body's mass.

- BODY_PARAM_INERTIA = 3
  Constant to set/get a body's inertia.

- BODY_PARAM_CENTER_OF_MASS = 4
  Constant to set/get a body's center of mass position in the body's local coordinate system.

- BODY_PARAM_GRAVITY_SCALE = 5
  Constant to set/get a body's gravity multiplier.

- BODY_PARAM_LINEAR_DAMP_MODE = 6
  Constant to set/get a body's linear damping mode. See BodyDampMode for possible values.

- BODY_PARAM_ANGULAR_DAMP_MODE = 7
  Constant to set/get a body's angular damping mode. See BodyDampMode for possible values.

- BODY_PARAM_LINEAR_DAMP = 8
  Constant to set/get a body's linear damping factor.

- BODY_PARAM_ANGULAR_DAMP = 9
  Constant to set/get a body's angular damping factor.

- BODY_PARAM_MAX = 10
  Represents the size of the BodyParameter enum.

### Enum BodyDampMode

- BODY_DAMP_MODE_COMBINE = 0
  The body's damping value is added to any value set in areas or the default value.

- BODY_DAMP_MODE_REPLACE = 1
  The body's damping value replaces any value set in areas or the default value.

### Enum BodyState

- BODY_STATE_TRANSFORM = 0
  Constant to set/get the current transform matrix of the body.

- BODY_STATE_LINEAR_VELOCITY = 1
  Constant to set/get the current linear velocity of the body.

- BODY_STATE_ANGULAR_VELOCITY = 2
  Constant to set/get the current angular velocity of the body.

- BODY_STATE_SLEEPING = 3
  Constant to sleep/wake up a body, or to get whether it is sleeping.

- BODY_STATE_CAN_SLEEP = 4
  Constant to set/get whether the body can sleep.

### Enum AreaBodyStatus

- AREA_BODY_ADDED = 0
  The value of the first parameter and area callback function receives, when an object enters one of its shapes.

- AREA_BODY_REMOVED = 1
  The value of the first parameter and area callback function receives, when an object exits one of its shapes.

### Enum ProcessInfo

- INFO_ACTIVE_OBJECTS = 0
  Constant to get the number of objects that are not sleeping.

- INFO_COLLISION_PAIRS = 1
  Constant to get the number of possible collisions.

- INFO_ISLAND_COUNT = 2
  Constant to get the number of space regions where a collision could occur.

### Enum SpaceParameter

- SPACE_PARAM_CONTACT_RECYCLE_RADIUS = 0
  Constant to set/get the maximum distance a pair of bodies has to move before their collision status has to be recalculated.

- SPACE_PARAM_CONTACT_MAX_SEPARATION = 1
  Constant to set/get the maximum distance a shape can be from another before they are considered separated and the contact is discarded.

- SPACE_PARAM_CONTACT_MAX_ALLOWED_PENETRATION = 2
  Constant to set/get the maximum distance a shape can penetrate another shape before it is considered a collision.

- SPACE_PARAM_CONTACT_DEFAULT_BIAS = 3
  Constant to set/get the default solver bias for all physics contacts. A solver bias is a factor controlling how much two objects "rebound", after overlapping, to avoid leaving them in that state because of numerical imprecision.

- SPACE_PARAM_BODY_LINEAR_VELOCITY_SLEEP_THRESHOLD = 4
  Constant to set/get the threshold linear velocity of activity. A body marked as potentially inactive for both linear and angular velocity will be put to sleep after the time given.

- SPACE_PARAM_BODY_ANGULAR_VELOCITY_SLEEP_THRESHOLD = 5
  Constant to set/get the threshold angular velocity of activity. A body marked as potentially inactive for both linear and angular velocity will be put to sleep after the time given.

- SPACE_PARAM_BODY_TIME_TO_SLEEP = 6
  Constant to set/get the maximum time of activity. A body marked as potentially inactive for both linear and angular velocity will be put to sleep after this time.

- SPACE_PARAM_SOLVER_ITERATIONS = 7
  Constant to set/get the number of solver iterations for contacts and constraints. The greater the number of iterations, the more accurate the collisions and constraints will be. However, a greater number of iterations requires more CPU power, which can decrease performance.

### Enum BodyAxis

- BODY_AXIS_LINEAR_X = 1

- BODY_AXIS_LINEAR_Y = 2

- BODY_AXIS_LINEAR_Z = 4

- BODY_AXIS_ANGULAR_X = 8

- BODY_AXIS_ANGULAR_Y = 16

- BODY_AXIS_ANGULAR_Z = 32
