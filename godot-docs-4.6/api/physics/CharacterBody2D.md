# CharacterBody2D

## Meta

- Name: CharacterBody2D
- Source: CharacterBody2D.xml
- Inherits: PhysicsBody2D
- Inheritance Chain: CharacterBody2D -> PhysicsBody2D -> CollisionObject2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

A 2D physics body specialized for characters moved by script.

## Description

CharacterBody2D is a specialized class for physics bodies that are meant to be user-controlled. They are not affected by physics at all, but they affect other physics bodies in their path. They are mainly used to provide high-level API to move objects with wall and slope detection (move_and_slide() method) in addition to the general collision detection provided by PhysicsBody2D.move_and_collide(). This makes it useful for highly configurable physics bodies that must move in specific ways and collide with the world, as is often the case with user-controlled characters. For game objects that don't require complex movement or collision detection, such as moving platforms, AnimatableBody2D is simpler to configure.

## Quick Reference

```
[methods]
apply_floor_snap() -> void
get_floor_angle(up_direction: Vector2 = Vector2(0, -1)) -> float [const]
get_floor_normal() -> Vector2 [const]
get_last_motion() -> Vector2 [const]
get_last_slide_collision() -> KinematicCollision2D
get_platform_velocity() -> Vector2 [const]
get_position_delta() -> Vector2 [const]
get_real_velocity() -> Vector2 [const]
get_slide_collision(slide_idx: int) -> KinematicCollision2D
get_slide_collision_count() -> int [const]
get_wall_normal() -> Vector2 [const]
is_on_ceiling() -> bool [const]
is_on_ceiling_only() -> bool [const]
is_on_floor() -> bool [const]
is_on_floor_only() -> bool [const]
is_on_wall() -> bool [const]
is_on_wall_only() -> bool [const]
move_and_slide() -> bool

[properties]
floor_block_on_wall: bool = true
floor_constant_speed: bool = false
floor_max_angle: float = 0.7853982
floor_snap_length: float = 1.0
floor_stop_on_slope: bool = true
max_slides: int = 4
motion_mode: int (CharacterBody2D.MotionMode) = 0
platform_floor_layers: int = 4294967295
platform_on_leave: int (CharacterBody2D.PlatformOnLeave) = 0
platform_wall_layers: int = 0
safe_margin: float = 0.08
slide_on_ceiling: bool = true
up_direction: Vector2 = Vector2(0, -1)
velocity: Vector2 = Vector2(0, 0)
wall_min_slide_angle: float = 0.2617994
```

## Tutorials

- [Physics introduction]($DOCS_URL/tutorials/physics/physics_introduction.html)
- [Troubleshooting physics issues]($DOCS_URL/tutorials/physics/troubleshooting_physics_issues.html)
- [Kinematic character (2D)]($DOCS_URL/tutorials/physics/kinematic_character_2d.html)
- [Using CharacterBody2D]($DOCS_URL/tutorials/physics/using_character_body_2d.html)
- [2D Kinematic Character Demo](https://godotengine.org/asset-library/asset/2719)
- [2D Platformer Demo](https://godotengine.org/asset-library/asset/2727)

## Methods

- apply_floor_snap() -> void
  Allows to manually apply a snap to the floor regardless of the body's velocity. This function does nothing when is_on_floor() returns true.

- get_floor_angle(up_direction: Vector2 = Vector2(0, -1)) -> float [const]
  Returns the floor's collision angle at the last collision point according to up_direction, which is Vector2.UP by default. This value is always positive and only valid after calling move_and_slide() and when is_on_floor() returns true.

- get_floor_normal() -> Vector2 [const]
  Returns the collision normal of the floor at the last collision point. Only valid after calling move_and_slide() and when is_on_floor() returns true. **Warning:** The collision normal is not always the same as the surface normal.

- get_last_motion() -> Vector2 [const]
  Returns the last motion applied to the CharacterBody2D during the last call to move_and_slide(). The movement can be split into multiple motions when sliding occurs, and this method return the last one, which is useful to retrieve the current direction of the movement.

- get_last_slide_collision() -> KinematicCollision2D
  Returns a KinematicCollision2D if a collision occurred. The returned value contains information about the latest collision that occurred during the last call to move_and_slide(). Returns null if no collision occurred. See also get_slide_collision().

- get_platform_velocity() -> Vector2 [const]
  Returns the linear velocity of the platform at the last collision point. Only valid after calling move_and_slide().

- get_position_delta() -> Vector2 [const]
  Returns the travel (position delta) that occurred during the last call to move_and_slide().

- get_real_velocity() -> Vector2 [const]
  Returns the current real velocity since the last call to move_and_slide(). For example, when you climb a slope, you will move diagonally even though the velocity is horizontal. This method returns the diagonal movement, as opposed to velocity which returns the requested velocity.

- get_slide_collision(slide_idx: int) -> KinematicCollision2D
  Returns a KinematicCollision2D, which contains information about a collision that occurred during the last call to move_and_slide(). Since the body can collide several times in a single call to move_and_slide(), you must specify the index of the collision in the range 0 to (get_slide_collision_count() - 1). See also get_last_slide_collision(). **Example:** Iterate through the collisions with a for loop:


```
  for i in get_slide_collision_count():
      var collision = get_slide_collision(i)
      print("Collided with: ", collision.get_collider().name)

```

```
  for (int i = 0; i < GetSlideCollisionCount(); i++)
  {
      KinematicCollision2D collision = GetSlideCollision(i);
      GD.Print("Collided with: ", (collision.GetCollider() as Node).Name);
  }

```

- get_slide_collision_count() -> int [const]
  Returns the number of times the body collided and changed direction during the last call to move_and_slide().

- get_wall_normal() -> Vector2 [const]
  Returns the collision normal of the wall at the last collision point. Only valid after calling move_and_slide() and when is_on_wall() returns true. **Warning:** The collision normal is not always the same as the surface normal.

- is_on_ceiling() -> bool [const]
  Returns true if the body collided with the ceiling on the last call of move_and_slide(). Otherwise, returns false. The up_direction and floor_max_angle are used to determine whether a surface is "ceiling" or not.

- is_on_ceiling_only() -> bool [const]
  Returns true if the body collided only with the ceiling on the last call of move_and_slide(). Otherwise, returns false. The up_direction and floor_max_angle are used to determine whether a surface is "ceiling" or not.

- is_on_floor() -> bool [const]
  Returns true if the body collided with the floor on the last call of move_and_slide(). Otherwise, returns false. The up_direction and floor_max_angle are used to determine whether a surface is "floor" or not.

- is_on_floor_only() -> bool [const]
  Returns true if the body collided only with the floor on the last call of move_and_slide(). Otherwise, returns false. The up_direction and floor_max_angle are used to determine whether a surface is "floor" or not.

- is_on_wall() -> bool [const]
  Returns true if the body collided with a wall on the last call of move_and_slide(). Otherwise, returns false. The up_direction and floor_max_angle are used to determine whether a surface is "wall" or not.

- is_on_wall_only() -> bool [const]
  Returns true if the body collided only with a wall on the last call of move_and_slide(). Otherwise, returns false. The up_direction and floor_max_angle are used to determine whether a surface is "wall" or not.

- move_and_slide() -> bool
  Moves the body based on velocity. If the body collides with another, it will slide along the other body (by default only on floor) rather than stop immediately. If the other body is a CharacterBody2D or RigidBody2D, it will also be affected by the motion of the other body. You can use this to make moving and rotating platforms, or to make nodes push other nodes. This method should be used in Node._physics_process() (or in a method called by Node._physics_process()), as it uses the physics step's delta value automatically in calculations. Otherwise, the simulation will run at an incorrect speed. Modifies velocity if a slide collision occurred. To get the latest collision call get_last_slide_collision(), for detailed information about collisions that occurred, use get_slide_collision(). When the body touches a moving platform, the platform's velocity is automatically added to the body motion. If a collision occurs due to the platform's motion, it will always be first in the slide collisions. The general behavior and available properties change according to the motion_mode. Returns true if the body collided, otherwise, returns false.

## Properties

- floor_block_on_wall: bool = true [set set_floor_block_on_wall_enabled; get is_floor_block_on_wall_enabled]
  If true, the body will be able to move on the floor only. This option avoids to be able to walk on walls, it will however allow to slide down along them.

- floor_constant_speed: bool = false [set set_floor_constant_speed_enabled; get is_floor_constant_speed_enabled]
  If false (by default), the body will move faster on downward slopes and slower on upward slopes. If true, the body will always move at the same speed on the ground no matter the slope. Note that you need to use floor_snap_length to stick along a downward slope at constant speed.

- floor_max_angle: float = 0.7853982 [set set_floor_max_angle; get get_floor_max_angle]
  Maximum angle (in radians) where a slope is still considered a floor (or a ceiling), rather than a wall, when calling move_and_slide(). The default value equals 45 degrees.

- floor_snap_length: float = 1.0 [set set_floor_snap_length; get get_floor_snap_length]
  Sets a snapping distance. When set to a value different from 0.0, the body is kept attached to slopes when calling move_and_slide(). The snapping vector is determined by the given distance along the opposite direction of the up_direction. As long as the snapping vector is in contact with the ground and the body moves against up_direction, the body will remain attached to the surface. Snapping is not applied if the body moves along up_direction, meaning it contains vertical rising velocity, so it will be able to detach from the ground when jumping or when the body is pushed up by something. If you want to apply a snap without taking into account the velocity, use apply_floor_snap().

- floor_stop_on_slope: bool = true [set set_floor_stop_on_slope_enabled; get is_floor_stop_on_slope_enabled]
  If true, the body will not slide on slopes when calling move_and_slide() when the body is standing still. If false, the body will slide on floor's slopes when velocity applies a downward force.

- max_slides: int = 4 [set set_max_slides; get get_max_slides]
  Maximum number of times the body can change direction before it stops when calling move_and_slide(). Must be greater than zero.

- motion_mode: int (CharacterBody2D.MotionMode) = 0 [set set_motion_mode; get get_motion_mode]
  Sets the motion mode which defines the behavior of move_and_slide().

- platform_floor_layers: int = 4294967295 [set set_platform_floor_layers; get get_platform_floor_layers]
  Collision layers that will be included for detecting floor bodies that will act as moving platforms to be followed by the CharacterBody2D. By default, all floor bodies are detected and propagate their velocity.

- platform_on_leave: int (CharacterBody2D.PlatformOnLeave) = 0 [set set_platform_on_leave; get get_platform_on_leave]
  Sets the behavior to apply when you leave a moving platform. By default, to be physically accurate, when you leave the last platform velocity is applied.

- platform_wall_layers: int = 0 [set set_platform_wall_layers; get get_platform_wall_layers]
  Collision layers that will be included for detecting wall bodies that will act as moving platforms to be followed by the CharacterBody2D. By default, all wall bodies are ignored.

- safe_margin: float = 0.08 [set set_safe_margin; get get_safe_margin]
  Extra margin used for collision recovery when calling move_and_slide(). If the body is at least this close to another body, it will consider them to be colliding and will be pushed away before performing the actual motion. A higher value means it's more flexible for detecting collision, which helps with consistently detecting walls and floors. A lower value forces the collision algorithm to use more exact detection, so it can be used in cases that specifically require precision, e.g at very low scale to avoid visible jittering, or for stability with a stack of character bodies.

- slide_on_ceiling: bool = true [set set_slide_on_ceiling_enabled; get is_slide_on_ceiling_enabled]
  If true, during a jump against the ceiling, the body will slide, if false it will be stopped and will fall vertically.

- up_direction: Vector2 = Vector2(0, -1) [set set_up_direction; get get_up_direction]
  Vector pointing upwards, used to determine what is a wall and what is a floor (or a ceiling) when calling move_and_slide(). Defaults to Vector2.UP. As the vector will be normalized it can't be equal to Vector2.ZERO, if you want all collisions to be reported as walls, consider using MOTION_MODE_FLOATING as motion_mode.

- velocity: Vector2 = Vector2(0, 0) [set set_velocity; get get_velocity]
  Current velocity vector in pixels per second, used and modified during calls to move_and_slide(). **Note:** A common mistake is setting this property to the desired velocity multiplied by delta, which produces a motion vector in pixels.

- wall_min_slide_angle: float = 0.2617994 [set set_wall_min_slide_angle; get get_wall_min_slide_angle]
  Minimum angle (in radians) where the body is allowed to slide when it encounters a wall. The default value equals 15 degrees. This property only affects movement when motion_mode is MOTION_MODE_FLOATING.

## Constants

### Enum MotionMode

- MOTION_MODE_GROUNDED = 0
  Apply when notions of walls, ceiling and floor are relevant. In this mode the body motion will react to slopes (acceleration/slowdown). This mode is suitable for sided games like platformers.

- MOTION_MODE_FLOATING = 1
  Apply when there is no notion of floor or ceiling. All collisions will be reported as on_wall. In this mode, when you slide, the speed will always be constant. This mode is suitable for top-down games.

### Enum PlatformOnLeave

- PLATFORM_ON_LEAVE_ADD_VELOCITY = 0
  Add the last platform velocity to the velocity when you leave a moving platform.

- PLATFORM_ON_LEAVE_ADD_UPWARD_VELOCITY = 1
  Add the last platform velocity to the velocity when you leave a moving platform, but any downward motion is ignored. It's useful to keep full jump height even when the platform is moving down.

- PLATFORM_ON_LEAVE_DO_NOTHING = 2
  Do nothing when leaving a platform.
