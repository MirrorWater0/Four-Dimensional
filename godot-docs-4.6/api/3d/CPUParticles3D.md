# CPUParticles3D

## Meta

- Name: CPUParticles3D
- Source: CPUParticles3D.xml
- Inherits: GeometryInstance3D
- Inheritance Chain: CPUParticles3D -> GeometryInstance3D -> VisualInstance3D -> Node3D -> Node -> Object

## Brief Description

A CPU-based 3D particle emitter.

## Description

CPU-based 3D particle node used to create a variety of particle systems and effects. See also GPUParticles3D, which provides the same functionality with hardware acceleration, but may not run on older devices.

## Quick Reference

```
[methods]
capture_aabb() -> AABB [const]
convert_from_particles(particles: Node) -> void
get_param_curve(param: int (CPUParticles3D.Parameter)) -> Curve [const]
get_param_max(param: int (CPUParticles3D.Parameter)) -> float [const]
get_param_min(param: int (CPUParticles3D.Parameter)) -> float [const]
get_particle_flag(particle_flag: int (CPUParticles3D.ParticleFlags)) -> bool [const]
request_particles_process(process_time: float) -> void
restart(keep_seed: bool = false) -> void
set_param_curve(param: int (CPUParticles3D.Parameter), curve: Curve) -> void
set_param_max(param: int (CPUParticles3D.Parameter), value: float) -> void
set_param_min(param: int (CPUParticles3D.Parameter), value: float) -> void
set_particle_flag(particle_flag: int (CPUParticles3D.ParticleFlags), enable: bool) -> void

[properties]
amount: int = 8
angle_curve: Curve
angle_max: float = 0.0
angle_min: float = 0.0
angular_velocity_curve: Curve
angular_velocity_max: float = 0.0
angular_velocity_min: float = 0.0
anim_offset_curve: Curve
anim_offset_max: float = 0.0
anim_offset_min: float = 0.0
anim_speed_curve: Curve
anim_speed_max: float = 0.0
anim_speed_min: float = 0.0
color: Color = Color(1, 1, 1, 1)
color_initial_ramp: Gradient
color_ramp: Gradient
damping_curve: Curve
damping_max: float = 0.0
damping_min: float = 0.0
direction: Vector3 = Vector3(1, 0, 0)
draw_order: int (CPUParticles3D.DrawOrder) = 0
emission_box_extents: Vector3
emission_colors: PackedColorArray = PackedColorArray()
emission_normals: PackedVector3Array
emission_points: PackedVector3Array
emission_ring_axis: Vector3
emission_ring_cone_angle: float
emission_ring_height: float
emission_ring_inner_radius: float
emission_ring_radius: float
emission_shape: int (CPUParticles3D.EmissionShape) = 0
emission_sphere_radius: float
emitting: bool = true
explosiveness: float = 0.0
fixed_fps: int = 0
flatness: float = 0.0
fract_delta: bool = true
gravity: Vector3 = Vector3(0, -9.8, 0)
hue_variation_curve: Curve
hue_variation_max: float = 0.0
hue_variation_min: float = 0.0
initial_velocity_max: float = 0.0
initial_velocity_min: float = 0.0
lifetime: float = 1.0
lifetime_randomness: float = 0.0
linear_accel_curve: Curve
linear_accel_max: float = 0.0
linear_accel_min: float = 0.0
local_coords: bool = false
mesh: Mesh
one_shot: bool = false
orbit_velocity_curve: Curve
orbit_velocity_max: float
orbit_velocity_min: float
particle_flag_align_y: bool = false
particle_flag_disable_z: bool = false
particle_flag_rotate_y: bool = false
preprocess: float = 0.0
radial_accel_curve: Curve
radial_accel_max: float = 0.0
radial_accel_min: float = 0.0
randomness: float = 0.0
scale_amount_curve: Curve
scale_amount_max: float = 1.0
scale_amount_min: float = 1.0
scale_curve_x: Curve
scale_curve_y: Curve
scale_curve_z: Curve
seed: int = 0
speed_scale: float = 1.0
split_scale: bool = false
spread: float = 45.0
tangential_accel_curve: Curve
tangential_accel_max: float = 0.0
tangential_accel_min: float = 0.0
use_fixed_seed: bool = false
visibility_aabb: AABB = AABB(0, 0, 0, 0, 0, 0)
```

## Tutorials

- [Particle systems (3D)]($DOCS_URL/tutorials/3d/particles/index.html)

## Methods

- capture_aabb() -> AABB [const]
  Returns the axis-aligned bounding box that contains all the particles that are active in the current frame.

- convert_from_particles(particles: Node) -> void
  Sets this node's properties to match a given GPUParticles3D node with an assigned ParticleProcessMaterial.

- get_param_curve(param: int (CPUParticles3D.Parameter)) -> Curve [const]
  Returns the Curve of the parameter specified by Parameter.

- get_param_max(param: int (CPUParticles3D.Parameter)) -> float [const]
  Returns the maximum value range for the given parameter.

- get_param_min(param: int (CPUParticles3D.Parameter)) -> float [const]
  Returns the minimum value range for the given parameter.

- get_particle_flag(particle_flag: int (CPUParticles3D.ParticleFlags)) -> bool [const]
  Returns the enabled state of the given particle flag.

- request_particles_process(process_time: float) -> void
  Requests the particles to process for extra process time during a single frame. Useful for particle playback, if used in combination with use_fixed_seed or by calling restart() with parameter keep_seed set to true.

- restart(keep_seed: bool = false) -> void
  Restarts the particle emitter. If keep_seed is true, the current random seed will be preserved. Useful for seeking and playback.

- set_param_curve(param: int (CPUParticles3D.Parameter), curve: Curve) -> void
  Sets the Curve of the parameter specified by Parameter. Should be a unit Curve.

- set_param_max(param: int (CPUParticles3D.Parameter), value: float) -> void
  Sets the maximum value for the given parameter.

- set_param_min(param: int (CPUParticles3D.Parameter), value: float) -> void
  Sets the minimum value for the given parameter.

- set_particle_flag(particle_flag: int (CPUParticles3D.ParticleFlags), enable: bool) -> void
  Enables or disables the given particle flag.

## Properties

- amount: int = 8 [set set_amount; get get_amount]
  Number of particles emitted in one emission cycle.

- angle_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's rotation will be animated along this Curve. Should be a unit Curve.

- angle_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum angle.

- angle_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum angle.

- angular_velocity_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's angular velocity (rotation speed) will vary along this Curve over its lifetime. Should be a unit Curve.

- angular_velocity_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum initial angular velocity (rotation speed) applied to each particle in *degrees* per second.

- angular_velocity_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum initial angular velocity (rotation speed) applied to each particle in *degrees* per second.

- anim_offset_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's animation offset will vary along this Curve. Should be a unit Curve.

- anim_offset_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum animation offset.

- anim_offset_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum animation offset.

- anim_speed_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's animation speed will vary along this Curve. Should be a unit Curve.

- anim_speed_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum particle animation speed.

- anim_speed_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum particle animation speed.

- color: Color = Color(1, 1, 1, 1) [set set_color; get get_color]
  Each particle's initial color. **Note:** color multiplies the particle mesh's vertex colors. To have a visible effect on a BaseMaterial3D, BaseMaterial3D.vertex_color_use_as_albedo *must* be true. For a ShaderMaterial, ALBEDO *= COLOR.rgb; must be inserted in the shader's fragment() function. Otherwise, color will have no visible effect.

- color_initial_ramp: Gradient [set set_color_initial_ramp; get get_color_initial_ramp]
  Each particle's initial color will vary along this Gradient (multiplied with color). **Note:** color_initial_ramp multiplies the particle mesh's vertex colors. To have a visible effect on a BaseMaterial3D, BaseMaterial3D.vertex_color_use_as_albedo *must* be true. For a ShaderMaterial, ALBEDO *= COLOR.rgb; must be inserted in the shader's fragment() function. Otherwise, color_initial_ramp will have no visible effect.

- color_ramp: Gradient [set set_color_ramp; get get_color_ramp]
  Each particle's color will vary along this Gradient over its lifetime (multiplied with color). **Note:** color_ramp multiplies the particle mesh's vertex colors. To have a visible effect on a BaseMaterial3D, BaseMaterial3D.vertex_color_use_as_albedo *must* be true. For a ShaderMaterial, ALBEDO *= COLOR.rgb; must be inserted in the shader's fragment() function. Otherwise, color_ramp will have no visible effect.

- damping_curve: Curve [set set_param_curve; get get_param_curve]
  Damping will vary along this Curve. Should be a unit Curve.

- damping_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum damping.

- damping_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum damping.

- direction: Vector3 = Vector3(1, 0, 0) [set set_direction; get get_direction]
  Unit vector specifying the particles' emission direction.

- draw_order: int (CPUParticles3D.DrawOrder) = 0 [set set_draw_order; get get_draw_order]
  Particle draw order.

- emission_box_extents: Vector3 [set set_emission_box_extents; get get_emission_box_extents]
  The rectangle's extents if emission_shape is set to EMISSION_SHAPE_BOX.

- emission_colors: PackedColorArray = PackedColorArray() [set set_emission_colors; get get_emission_colors]
  Sets the Colors to modulate particles by when using EMISSION_SHAPE_POINTS or EMISSION_SHAPE_DIRECTED_POINTS. **Note:** emission_colors multiplies the particle mesh's vertex colors. To have a visible effect on a BaseMaterial3D, BaseMaterial3D.vertex_color_use_as_albedo *must* be true. For a ShaderMaterial, ALBEDO *= COLOR.rgb; must be inserted in the shader's fragment() function. Otherwise, emission_colors will have no visible effect.

- emission_normals: PackedVector3Array [set set_emission_normals; get get_emission_normals]
  Sets the direction the particles will be emitted in when using EMISSION_SHAPE_DIRECTED_POINTS.

- emission_points: PackedVector3Array [set set_emission_points; get get_emission_points]
  Sets the initial positions to spawn particles when using EMISSION_SHAPE_POINTS or EMISSION_SHAPE_DIRECTED_POINTS.

- emission_ring_axis: Vector3 [set set_emission_ring_axis; get get_emission_ring_axis]
  The axis of the ring when using the emitter EMISSION_SHAPE_RING.

- emission_ring_cone_angle: float [set set_emission_ring_cone_angle; get get_emission_ring_cone_angle]
  The angle of the cone when using the emitter EMISSION_SHAPE_RING. The default angle of 90 degrees results in a ring, while an angle of 0 degrees results in a cone. Intermediate values will result in a ring where one end is larger than the other. **Note:** Depending on emission_ring_height, the angle may be clamped if the ring's end is reached to form a perfect cone.

- emission_ring_height: float [set set_emission_ring_height; get get_emission_ring_height]
  The height of the ring when using the emitter EMISSION_SHAPE_RING.

- emission_ring_inner_radius: float [set set_emission_ring_inner_radius; get get_emission_ring_inner_radius]
  The inner radius of the ring when using the emitter EMISSION_SHAPE_RING.

- emission_ring_radius: float [set set_emission_ring_radius; get get_emission_ring_radius]
  The radius of the ring when using the emitter EMISSION_SHAPE_RING.

- emission_shape: int (CPUParticles3D.EmissionShape) = 0 [set set_emission_shape; get get_emission_shape]
  Particles will be emitted inside this region.

- emission_sphere_radius: float [set set_emission_sphere_radius; get get_emission_sphere_radius]
  The sphere's radius if EmissionShape is set to EMISSION_SHAPE_SPHERE.

- emitting: bool = true [set set_emitting; get is_emitting]
  If true, particles are being emitted. emitting can be used to start and stop particles from emitting. However, if one_shot is true setting emitting to true will not restart the emission cycle until after all active particles finish processing. You can use the finished signal to be notified once all active particles finish processing.

- explosiveness: float = 0.0 [set set_explosiveness_ratio; get get_explosiveness_ratio]
  How rapidly particles in an emission cycle are emitted. If greater than 0, there will be a gap in emissions before the next cycle begins.

- fixed_fps: int = 0 [set set_fixed_fps; get get_fixed_fps]
  The particle system's frame rate is fixed to a value. For example, changing the value to 2 will make the particles render at 2 frames per second. Note this does not slow down the particle system itself.

- flatness: float = 0.0 [set set_flatness; get get_flatness]
  Amount of spread in Y/Z plane. A value of 1 restricts particles to X/Z plane.

- fract_delta: bool = true [set set_fractional_delta; get get_fractional_delta]
  If true, results in fractional delta calculation which has a smoother particles display effect.

- gravity: Vector3 = Vector3(0, -9.8, 0) [set set_gravity; get get_gravity]
  Gravity applied to every particle.

- hue_variation_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's hue will vary along this Curve. Should be a unit Curve.

- hue_variation_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum hue variation.

- hue_variation_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum hue variation.

- initial_velocity_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum value of the initial velocity.

- initial_velocity_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum value of the initial velocity.

- lifetime: float = 1.0 [set set_lifetime; get get_lifetime]
  Amount of time each particle will exist.

- lifetime_randomness: float = 0.0 [set set_lifetime_randomness; get get_lifetime_randomness]
  Particle lifetime randomness ratio.

- linear_accel_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's linear acceleration will vary along this Curve. Should be a unit Curve.

- linear_accel_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum linear acceleration.

- linear_accel_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum linear acceleration.

- local_coords: bool = false [set set_use_local_coordinates; get get_use_local_coordinates]
  If true, particles use the parent node's coordinate space (known as local coordinates). This will cause particles to move and rotate along the CPUParticles3D node (and its parents) when it is moved or rotated. If false, particles use global coordinates; they will not move or rotate along the CPUParticles3D node (and its parents) when it is moved or rotated.

- mesh: Mesh [set set_mesh; get get_mesh]
  The Mesh used for each particle. If null, particles will be spheres.

- one_shot: bool = false [set set_one_shot; get get_one_shot]
  If true, only one emission cycle occurs. If set true during a cycle, emission will stop at the cycle's end.

- orbit_velocity_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's orbital velocity will vary along this Curve. Should be a unit Curve.

- orbit_velocity_max: float [set set_param_max; get get_param_max]
  Maximum orbit velocity.

- orbit_velocity_min: float [set set_param_min; get get_param_min]
  Minimum orbit velocity.

- particle_flag_align_y: bool = false [set set_particle_flag; get get_particle_flag]
  Align Y axis of particle with the direction of its velocity.

- particle_flag_disable_z: bool = false [set set_particle_flag; get get_particle_flag]
  If true, particles will not move on the Z axis.

- particle_flag_rotate_y: bool = false [set set_particle_flag; get get_particle_flag]
  If true, particles rotate around Y axis by angle_min.

- preprocess: float = 0.0 [set set_pre_process_time; get get_pre_process_time]
  Particle system starts as if it had already run for this many seconds.

- radial_accel_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's radial acceleration will vary along this Curve. Should be a unit Curve.

- radial_accel_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum radial acceleration.

- radial_accel_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum radial acceleration.

- randomness: float = 0.0 [set set_randomness_ratio; get get_randomness_ratio]
  Emission lifetime randomness ratio.

- scale_amount_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's scale will vary along this Curve. Should be a unit Curve.

- scale_amount_max: float = 1.0 [set set_param_max; get get_param_max]
  Maximum scale.

- scale_amount_min: float = 1.0 [set set_param_min; get get_param_min]
  Minimum scale.

- scale_curve_x: Curve [set set_scale_curve_x; get get_scale_curve_x]
  Curve for the scale over life, along the x axis.

- scale_curve_y: Curve [set set_scale_curve_y; get get_scale_curve_y]
  Curve for the scale over life, along the y axis.

- scale_curve_z: Curve [set set_scale_curve_z; get get_scale_curve_z]
  Curve for the scale over life, along the z axis.

- seed: int = 0 [set set_seed; get get_seed]
  Sets the random seed used by the particle system. Only effective if use_fixed_seed is true.

- speed_scale: float = 1.0 [set set_speed_scale; get get_speed_scale]
  Particle system's running speed scaling ratio. A value of 0 can be used to pause the particles.

- split_scale: bool = false [set set_split_scale; get get_split_scale]
  If set to true, three different scale curves can be specified, one per scale axis.

- spread: float = 45.0 [set set_spread; get get_spread]
  Each particle's initial direction range from +spread to -spread degrees. Applied to X/Z plane and Y/Z planes.

- tangential_accel_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's tangential acceleration will vary along this Curve. Should be a unit Curve.

- tangential_accel_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum tangent acceleration.

- tangential_accel_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum tangent acceleration.

- use_fixed_seed: bool = false [set set_use_fixed_seed; get get_use_fixed_seed]
  If true, particles will use the same seed for every simulation using the seed defined in seed. This is useful for situations where the visual outcome should be consistent across replays, for example when using Movie Maker mode.

- visibility_aabb: AABB = AABB(0, 0, 0, 0, 0, 0) [set set_visibility_aabb; get get_visibility_aabb]
  The AABB that determines the node's region which needs to be visible on screen for the particle system to be active. Grow the box if particles suddenly appear/disappear when the node enters/exits the screen. The AABB can be grown via code or with the **Particles → Generate AABB** editor tool.

## Signals

- finished()
  Emitted when all active particles have finished processing. When one_shot is disabled, particles will process continuously, so this is never emitted.

## Constants

### Enum DrawOrder

- DRAW_ORDER_INDEX = 0
  Particles are drawn in the order emitted.

- DRAW_ORDER_LIFETIME = 1
  Particles are drawn in order of remaining lifetime. In other words, the particle with the highest lifetime is drawn at the front.

- DRAW_ORDER_VIEW_DEPTH = 2
  Particles are drawn in order of depth.

### Enum Parameter

- PARAM_INITIAL_LINEAR_VELOCITY = 0
  Use with set_param_min(), set_param_max(), and set_param_curve() to set initial velocity properties.

- PARAM_ANGULAR_VELOCITY = 1
  Use with set_param_min(), set_param_max(), and set_param_curve() to set angular velocity properties.

- PARAM_ORBIT_VELOCITY = 2
  Use with set_param_min(), set_param_max(), and set_param_curve() to set orbital velocity properties.

- PARAM_LINEAR_ACCEL = 3
  Use with set_param_min(), set_param_max(), and set_param_curve() to set linear acceleration properties.

- PARAM_RADIAL_ACCEL = 4
  Use with set_param_min(), set_param_max(), and set_param_curve() to set radial acceleration properties.

- PARAM_TANGENTIAL_ACCEL = 5
  Use with set_param_min(), set_param_max(), and set_param_curve() to set tangential acceleration properties.

- PARAM_DAMPING = 6
  Use with set_param_min(), set_param_max(), and set_param_curve() to set damping properties.

- PARAM_ANGLE = 7
  Use with set_param_min(), set_param_max(), and set_param_curve() to set angle properties.

- PARAM_SCALE = 8
  Use with set_param_min(), set_param_max(), and set_param_curve() to set scale properties.

- PARAM_HUE_VARIATION = 9
  Use with set_param_min(), set_param_max(), and set_param_curve() to set hue variation properties.

- PARAM_ANIM_SPEED = 10
  Use with set_param_min(), set_param_max(), and set_param_curve() to set animation speed properties.

- PARAM_ANIM_OFFSET = 11
  Use with set_param_min(), set_param_max(), and set_param_curve() to set animation offset properties.

- PARAM_MAX = 12
  Represents the size of the Parameter enum.

### Enum ParticleFlags

- PARTICLE_FLAG_ALIGN_Y_TO_VELOCITY = 0
  Use with set_particle_flag() to set particle_flag_align_y.

- PARTICLE_FLAG_ROTATE_Y = 1
  Use with set_particle_flag() to set particle_flag_rotate_y.

- PARTICLE_FLAG_DISABLE_Z = 2
  Use with set_particle_flag() to set particle_flag_disable_z.

- PARTICLE_FLAG_MAX = 3
  Represents the size of the ParticleFlags enum.

### Enum EmissionShape

- EMISSION_SHAPE_POINT = 0
  All particles will be emitted from a single point.

- EMISSION_SHAPE_SPHERE = 1
  Particles will be emitted in the volume of a sphere.

- EMISSION_SHAPE_SPHERE_SURFACE = 2
  Particles will be emitted on the surface of a sphere.

- EMISSION_SHAPE_BOX = 3
  Particles will be emitted in the volume of a box.

- EMISSION_SHAPE_POINTS = 4
  Particles will be emitted at a position chosen randomly among emission_points. Particle color will be modulated by emission_colors.

- EMISSION_SHAPE_DIRECTED_POINTS = 5
  Particles will be emitted at a position chosen randomly among emission_points. Particle velocity and rotation will be set based on emission_normals. Particle color will be modulated by emission_colors.

- EMISSION_SHAPE_RING = 6
  Particles will be emitted in a ring or cylinder.

- EMISSION_SHAPE_MAX = 7
  Represents the size of the EmissionShape enum.
