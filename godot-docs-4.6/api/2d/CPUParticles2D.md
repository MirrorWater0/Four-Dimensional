# CPUParticles2D

## Meta

- Name: CPUParticles2D
- Source: CPUParticles2D.xml
- Inherits: Node2D
- Inheritance Chain: CPUParticles2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

A CPU-based 2D particle emitter.

## Description

CPU-based 2D particle node used to create a variety of particle systems and effects. See also GPUParticles2D, which provides the same functionality with hardware acceleration, but may not run on older devices.

## Quick Reference

```
[methods]
convert_from_particles(particles: Node) -> void
get_param_curve(param: int (CPUParticles2D.Parameter)) -> Curve [const]
get_param_max(param: int (CPUParticles2D.Parameter)) -> float [const]
get_param_min(param: int (CPUParticles2D.Parameter)) -> float [const]
get_particle_flag(particle_flag: int (CPUParticles2D.ParticleFlags)) -> bool [const]
request_particles_process(process_time: float) -> void
restart(keep_seed: bool = false) -> void
set_param_curve(param: int (CPUParticles2D.Parameter), curve: Curve) -> void
set_param_max(param: int (CPUParticles2D.Parameter), value: float) -> void
set_param_min(param: int (CPUParticles2D.Parameter), value: float) -> void
set_particle_flag(particle_flag: int (CPUParticles2D.ParticleFlags), enable: bool) -> void

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
direction: Vector2 = Vector2(1, 0)
draw_order: int (CPUParticles2D.DrawOrder) = 0
emission_colors: PackedColorArray
emission_normals: PackedVector2Array
emission_points: PackedVector2Array
emission_rect_extents: Vector2
emission_ring_inner_radius: float
emission_ring_radius: float
emission_shape: int (CPUParticles2D.EmissionShape) = 0
emission_sphere_radius: float
emitting: bool = true
explosiveness: float = 0.0
fixed_fps: int = 0
fract_delta: bool = true
gravity: Vector2 = Vector2(0, 980)
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
one_shot: bool = false
orbit_velocity_curve: Curve
orbit_velocity_max: float = 0.0
orbit_velocity_min: float = 0.0
particle_flag_align_y: bool = false
physics_interpolation_mode: int (Node.PhysicsInterpolationMode) = 2
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
seed: int = 0
speed_scale: float = 1.0
split_scale: bool = false
spread: float = 45.0
tangential_accel_curve: Curve
tangential_accel_max: float = 0.0
tangential_accel_min: float = 0.0
texture: Texture2D
use_fixed_seed: bool = false
```

## Tutorials

- [Particle systems (2D)]($DOCS_URL/tutorials/2d/particle_systems_2d.html)

## Methods

- convert_from_particles(particles: Node) -> void
  Sets this node's properties to match a given GPUParticles2D node with an assigned ParticleProcessMaterial.

- get_param_curve(param: int (CPUParticles2D.Parameter)) -> Curve [const]
  Returns the Curve of the parameter specified by Parameter.

- get_param_max(param: int (CPUParticles2D.Parameter)) -> float [const]
  Returns the maximum value range for the given parameter.

- get_param_min(param: int (CPUParticles2D.Parameter)) -> float [const]
  Returns the minimum value range for the given parameter.

- get_particle_flag(particle_flag: int (CPUParticles2D.ParticleFlags)) -> bool [const]
  Returns the enabled state of the given particle flag.

- request_particles_process(process_time: float) -> void
  Requests the particles to process for extra process time during a single frame. Useful for particle playback, if used in combination with use_fixed_seed or by calling restart() with parameter keep_seed set to true.

- restart(keep_seed: bool = false) -> void
  Restarts the particle emitter. If keep_seed is true, the current random seed will be preserved. Useful for seeking and playback.

- set_param_curve(param: int (CPUParticles2D.Parameter), curve: Curve) -> void
  Sets the Curve of the parameter specified by Parameter. Should be a unit Curve.

- set_param_max(param: int (CPUParticles2D.Parameter), value: float) -> void
  Sets the maximum value for the given parameter.

- set_param_min(param: int (CPUParticles2D.Parameter), value: float) -> void
  Sets the minimum value for the given parameter.

- set_particle_flag(particle_flag: int (CPUParticles2D.ParticleFlags), enable: bool) -> void
  Enables or disables the given particle flag.

## Properties

- amount: int = 8 [set set_amount; get get_amount]
  Number of particles emitted in one emission cycle.

- angle_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's rotation will be animated along this Curve. Should be a unit Curve.

- angle_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum initial rotation applied to each particle, in degrees.

- angle_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum equivalent of angle_max.

- angular_velocity_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's angular velocity will vary along this Curve. Should be a unit Curve.

- angular_velocity_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum initial angular velocity (rotation speed) applied to each particle in *degrees* per second.

- angular_velocity_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum equivalent of angular_velocity_max.

- anim_offset_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's animation offset will vary along this Curve. Should be a unit Curve.

- anim_offset_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum animation offset that corresponds to frame index in the texture. 0 is the first frame, 1 is the last one. See CanvasItemMaterial.particles_animation.

- anim_offset_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum equivalent of anim_offset_max.

- anim_speed_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's animation speed will vary along this Curve. Should be a unit Curve.

- anim_speed_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum particle animation speed. Animation speed of 1 means that the particles will make full 0 to 1 offset cycle during lifetime, 2 means 2 cycles etc. With animation speed greater than 1, remember to enable CanvasItemMaterial.particles_anim_loop property if you want the animation to repeat.

- anim_speed_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum equivalent of anim_speed_max.

- color: Color = Color(1, 1, 1, 1) [set set_color; get get_color]
  Each particle's initial color. If texture is defined, it will be multiplied by this color.

- color_initial_ramp: Gradient [set set_color_initial_ramp; get get_color_initial_ramp]
  Each particle's initial color will vary along this Gradient (multiplied with color).

- color_ramp: Gradient [set set_color_ramp; get get_color_ramp]
  Each particle's color will vary along this Gradient over its lifetime (multiplied with color).

- damping_curve: Curve [set set_param_curve; get get_param_curve]
  Damping will vary along this Curve. Should be a unit Curve.

- damping_max: float = 0.0 [set set_param_max; get get_param_max]
  The maximum rate at which particles lose velocity. For example value of 100 means that the particle will go from 100 velocity to 0 in 1 second.

- damping_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum equivalent of damping_max.

- direction: Vector2 = Vector2(1, 0) [set set_direction; get get_direction]
  Unit vector specifying the particles' emission direction.

- draw_order: int (CPUParticles2D.DrawOrder) = 0 [set set_draw_order; get get_draw_order]
  Particle draw order.

- emission_colors: PackedColorArray [set set_emission_colors; get get_emission_colors]
  Sets the Colors to modulate particles by when using EMISSION_SHAPE_POINTS or EMISSION_SHAPE_DIRECTED_POINTS.

- emission_normals: PackedVector2Array [set set_emission_normals; get get_emission_normals]
  Sets the direction the particles will be emitted in when using EMISSION_SHAPE_DIRECTED_POINTS.

- emission_points: PackedVector2Array [set set_emission_points; get get_emission_points]
  Sets the initial positions to spawn particles when using EMISSION_SHAPE_POINTS or EMISSION_SHAPE_DIRECTED_POINTS.

- emission_rect_extents: Vector2 [set set_emission_rect_extents; get get_emission_rect_extents]
  The rectangle's extents if emission_shape is set to EMISSION_SHAPE_RECTANGLE.

- emission_ring_inner_radius: float [set set_emission_ring_inner_radius; get get_emission_ring_inner_radius]
  The ring's inner radius if emission_shape is set to EMISSION_SHAPE_RING.

- emission_ring_radius: float [set set_emission_ring_radius; get get_emission_ring_radius]
  The ring's outer radius if emission_shape is set to EMISSION_SHAPE_RING.

- emission_shape: int (CPUParticles2D.EmissionShape) = 0 [set set_emission_shape; get get_emission_shape]
  Particles will be emitted inside this region.

- emission_sphere_radius: float [set set_emission_sphere_radius; get get_emission_sphere_radius]
  The sphere's radius if emission_shape is set to EMISSION_SHAPE_SPHERE.

- emitting: bool = true [set set_emitting; get is_emitting]
  If true, particles are being emitted. emitting can be used to start and stop particles from emitting. However, if one_shot is true setting emitting to true will not restart the emission cycle until after all active particles finish processing. You can use the finished signal to be notified once all active particles finish processing.

- explosiveness: float = 0.0 [set set_explosiveness_ratio; get get_explosiveness_ratio]
  How rapidly particles in an emission cycle are emitted. If greater than 0, there will be a gap in emissions before the next cycle begins.

- fixed_fps: int = 0 [set set_fixed_fps; get get_fixed_fps]
  The particle system's frame rate is fixed to a value. For example, changing the value to 2 will make the particles render at 2 frames per second. Note this does not slow down the simulation of the particle system itself.

- fract_delta: bool = true [set set_fractional_delta; get get_fractional_delta]
  If true, results in fractional delta calculation which has a smoother particles display effect.

- gravity: Vector2 = Vector2(0, 980) [set set_gravity; get get_gravity]
  Gravity applied to every particle.

- hue_variation_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's hue will vary along this Curve. Should be a unit Curve.

- hue_variation_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum initial hue variation applied to each particle. It will shift the particle color's hue.

- hue_variation_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum equivalent of hue_variation_max.

- initial_velocity_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum initial velocity magnitude for each particle. Direction comes from direction and spread.

- initial_velocity_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum equivalent of initial_velocity_max.

- lifetime: float = 1.0 [set set_lifetime; get get_lifetime]
  Amount of time each particle will exist.

- lifetime_randomness: float = 0.0 [set set_lifetime_randomness; get get_lifetime_randomness]
  Particle lifetime randomness ratio.

- linear_accel_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's linear acceleration will vary along this Curve. Should be a unit Curve.

- linear_accel_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum linear acceleration applied to each particle in the direction of motion.

- linear_accel_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum equivalent of linear_accel_max.

- local_coords: bool = false [set set_use_local_coordinates; get get_use_local_coordinates]
  If true, particles use the parent node's coordinate space (known as local coordinates). This will cause particles to move and rotate along the CPUParticles2D node (and its parents) when it is moved or rotated. If false, particles use global coordinates; they will not move or rotate along the CPUParticles2D node (and its parents) when it is moved or rotated.

- one_shot: bool = false [set set_one_shot; get get_one_shot]
  If true, only one emission cycle occurs. If set true during a cycle, emission will stop at the cycle's end.

- orbit_velocity_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's orbital velocity will vary along this Curve. Should be a unit Curve.

- orbit_velocity_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum orbital velocity applied to each particle. Makes the particles circle around origin. Specified in number of full rotations around origin per second.

- orbit_velocity_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum equivalent of orbit_velocity_max.

- particle_flag_align_y: bool = false [set set_particle_flag; get get_particle_flag]
  Align Y axis of particle with the direction of its velocity.

- physics_interpolation_mode: int (Node.PhysicsInterpolationMode) = 2 [set set_physics_interpolation_mode; get get_physics_interpolation_mode; override Node]

- preprocess: float = 0.0 [set set_pre_process_time; get get_pre_process_time]
  Particle system starts as if it had already run for this many seconds.

- radial_accel_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's radial acceleration will vary along this Curve. Should be a unit Curve.

- radial_accel_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum radial acceleration applied to each particle. Makes particle accelerate away from the origin or towards it if negative.

- radial_accel_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum equivalent of radial_accel_max.

- randomness: float = 0.0 [set set_randomness_ratio; get get_randomness_ratio]
  Emission lifetime randomness ratio.

- scale_amount_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's scale will vary along this Curve. Should be a unit Curve.

- scale_amount_max: float = 1.0 [set set_param_max; get get_param_max]
  Maximum initial scale applied to each particle.

- scale_amount_min: float = 1.0 [set set_param_min; get get_param_min]
  Minimum equivalent of scale_amount_max.

- scale_curve_x: Curve [set set_scale_curve_x; get get_scale_curve_x]
  Each particle's horizontal scale will vary along this Curve. Should be a unit Curve. split_scale must be enabled.

- scale_curve_y: Curve [set set_scale_curve_y; get get_scale_curve_y]
  Each particle's vertical scale will vary along this Curve. Should be a unit Curve. split_scale must be enabled.

- seed: int = 0 [set set_seed; get get_seed]
  Sets the random seed used by the particle system. Only effective if use_fixed_seed is true.

- speed_scale: float = 1.0 [set set_speed_scale; get get_speed_scale]
  Particle system's running speed scaling ratio. A value of 0 can be used to pause the particles.

- split_scale: bool = false [set set_split_scale; get get_split_scale]
  If true, the scale curve will be split into x and y components. See scale_curve_x and scale_curve_y.

- spread: float = 45.0 [set set_spread; get get_spread]
  Each particle's initial direction range from +spread to -spread degrees.

- tangential_accel_curve: Curve [set set_param_curve; get get_param_curve]
  Each particle's tangential acceleration will vary along this Curve. Should be a unit Curve.

- tangential_accel_max: float = 0.0 [set set_param_max; get get_param_max]
  Maximum tangential acceleration applied to each particle. Tangential acceleration is perpendicular to the particle's velocity giving the particles a swirling motion.

- tangential_accel_min: float = 0.0 [set set_param_min; get get_param_min]
  Minimum equivalent of tangential_accel_max.

- texture: Texture2D [set set_texture; get get_texture]
  Particle texture. If null, particles will be squares.

- use_fixed_seed: bool = false [set set_use_fixed_seed; get get_use_fixed_seed]
  If true, particles will use the same seed for every simulation using the seed defined in seed. This is useful for situations where the visual outcome should be consistent across replays, for example when using Movie Maker mode.

## Signals

- finished()
  Emitted when all active particles have finished processing. When one_shot is disabled, particles will process continuously, so this is never emitted.

## Constants

### Enum DrawOrder

- DRAW_ORDER_INDEX = 0
  Particles are drawn in the order emitted.

- DRAW_ORDER_LIFETIME = 1
  Particles are drawn in order of remaining lifetime. In other words, the particle with the highest lifetime is drawn at the front.

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
  Present for consistency with 3D particle nodes, not used in 2D.

- PARTICLE_FLAG_DISABLE_Z = 2
  Present for consistency with 3D particle nodes, not used in 2D.

- PARTICLE_FLAG_MAX = 3
  Represents the size of the ParticleFlags enum.

### Enum EmissionShape

- EMISSION_SHAPE_POINT = 0
  All particles will be emitted from a single point.

- EMISSION_SHAPE_SPHERE = 1
  Particles will be emitted in the volume of a sphere flattened to two dimensions.

- EMISSION_SHAPE_SPHERE_SURFACE = 2
  Particles will be emitted on the surface of a sphere flattened to two dimensions.

- EMISSION_SHAPE_RECTANGLE = 3
  Particles will be emitted in the area of a rectangle.

- EMISSION_SHAPE_POINTS = 4
  Particles will be emitted at a position chosen randomly among emission_points. Particle color will be modulated by emission_colors.

- EMISSION_SHAPE_DIRECTED_POINTS = 5
  Particles will be emitted at a position chosen randomly among emission_points. Particle velocity and rotation will be set based on emission_normals. Particle color will be modulated by emission_colors.

- EMISSION_SHAPE_RING = 6
  Particles will be emitted in the area of a ring parameterized by its outer and inner radius.

- EMISSION_SHAPE_MAX = 7
  Represents the size of the EmissionShape enum.
