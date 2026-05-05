# Quaternion

## Meta

- Name: Quaternion
- Source: Quaternion.xml
- Inherits: none

## Brief Description

A unit quaternion used for representing 3D rotations.

## Description

The Quaternion built-in Variant type is a 4D data structure that represents rotation in the form of a [Hamilton convention quaternion](https://en.wikipedia.org/wiki/Quaternions_and_spatial_rotation). Compared to the Basis type which can store both rotation and scale, quaternions can *only* store rotation. A Quaternion is composed by 4 floating-point components: w, x, y, and z. These components are very compact in memory, and because of this some operations are more efficient and less likely to cause floating-point errors. Methods such as get_angle(), get_axis(), and slerp() are faster than their Basis counterparts. For a great introduction to quaternions, see [this video by 3Blue1Brown](https://www.youtube.com/watch?v=d4EgbgTm0Bg). You do not need to know the math behind quaternions, as Godot provides several helper methods that handle it for you. These include slerp() and spherical_cubic_interpolate(), as well as the * operator. **Note:** Quaternions must be normalized before being used for rotation (see normalized()). **Note:** Similarly to Vector2 and Vector3, the components of a quaternion use 32-bit precision by default, unlike float which is always 64-bit. If double precision is needed, compile the engine with the option precision=double.

## Quick Reference

```
[methods]
angle_to(to: Quaternion) -> float [const]
dot(with: Quaternion) -> float [const]
exp() -> Quaternion [const]
from_euler(euler: Vector3) -> Quaternion [static]
get_angle() -> float [const]
get_axis() -> Vector3 [const]
get_euler(order: int = 2) -> Vector3 [const]
inverse() -> Quaternion [const]
is_equal_approx(to: Quaternion) -> bool [const]
is_finite() -> bool [const]
is_normalized() -> bool [const]
length() -> float [const]
length_squared() -> float [const]
log() -> Quaternion [const]
normalized() -> Quaternion [const]
slerp(to: Quaternion, weight: float) -> Quaternion [const]
slerpni(to: Quaternion, weight: float) -> Quaternion [const]
spherical_cubic_interpolate(b: Quaternion, pre_a: Quaternion, post_b: Quaternion, weight: float) -> Quaternion [const]
spherical_cubic_interpolate_in_time(b: Quaternion, pre_a: Quaternion, post_b: Quaternion, weight: float, b_t: float, pre_a_t: float, post_b_t: float) -> Quaternion [const]

[properties]
w: float = 1.0
x: float = 0.0
y: float = 0.0
z: float = 0.0
```

## Tutorials

- [3Blue1Brown's video on Quaternions](https://www.youtube.com/watch?v=d4EgbgTm0Bg)
- [Online Quaternion Visualization](https://quaternions.online/)
- [Using 3D transforms]($DOCS_URL/tutorials/3d/using_transforms.html#interpolating-with-quaternions)
- [Third Person Shooter (TPS) Demo](https://godotengine.org/asset-library/asset/2710)
- [Advanced Quaternion Visualization](https://iwatake2222.github.io/rotation_master/rotation_master.html)

## Constructors

- Quaternion() -> Quaternion
  Constructs a Quaternion identical to IDENTITY. **Note:** In C#, this constructs a Quaternion with all of its components set to 0.0.

- Quaternion(from: Quaternion) -> Quaternion
  Constructs a Quaternion as a copy of the given Quaternion.

- Quaternion(arc_from: Vector3, arc_to: Vector3) -> Quaternion
  Constructs a Quaternion representing the shortest arc between arc_from and arc_to. These can be imagined as two points intersecting a sphere's surface, with a radius of 1.0.

- Quaternion(axis: Vector3, angle: float) -> Quaternion
  Constructs a Quaternion representing rotation around the axis by the given angle, in radians. The axis must be a normalized vector.

- Quaternion(from: Basis) -> Quaternion
  Constructs a Quaternion from the given rotation Basis. This constructor is faster than Basis.get_rotation_quaternion(), but the given basis must be *orthonormalized* (see Basis.orthonormalized()). Otherwise, the constructor fails and returns IDENTITY.

- Quaternion(x: float, y: float, z: float, w: float) -> Quaternion
  Constructs a Quaternion defined by the given values. **Note:** Only normalized quaternions represent rotation; if these values are not normalized, the new Quaternion will not be a valid rotation.

## Methods

- angle_to(to: Quaternion) -> float [const]
  Returns the angle between this quaternion and to. This is the magnitude of the angle you would need to rotate by to get from one to the other. **Note:** The magnitude of the floating-point error for this method is abnormally high, so methods such as is_zero_approx will not work reliably.

- dot(with: Quaternion) -> float [const]
  Returns the dot product between this quaternion and with. This is equivalent to (quat.x * with.x) + (quat.y * with.y) + (quat.z * with.z) + (quat.w * with.w).

- exp() -> Quaternion [const]
  Returns the exponential of this quaternion. The rotation axis of the result is the normalized rotation axis of this quaternion, the angle of the result is the length of the vector part of this quaternion.

- from_euler(euler: Vector3) -> Quaternion [static]
  Constructs a new Quaternion from the given Vector3 of [Euler angles](https://en.wikipedia.org/wiki/Euler_angles), in radians. This method always uses the YXZ convention (EULER_ORDER_YXZ).

- get_angle() -> float [const]
  Returns the angle of the rotation represented by this quaternion. **Note:** The quaternion must be normalized.

- get_axis() -> Vector3 [const]
  Returns the rotation axis of the rotation represented by this quaternion.

- get_euler(order: int = 2) -> Vector3 [const]
  Returns this quaternion's rotation as a Vector3 of [Euler angles](https://en.wikipedia.org/wiki/Euler_angles), in radians. The order of each consecutive rotation can be changed with order (see EulerOrder constants). By default, the YXZ convention is used (EULER_ORDER_YXZ): Z (roll) is calculated first, then X (pitch), and lastly Y (yaw). When using the opposite method from_euler(), this order is reversed.

- inverse() -> Quaternion [const]
  Returns the inverse version of this quaternion, inverting the sign of every component except w.

- is_equal_approx(to: Quaternion) -> bool [const]
  Returns true if this quaternion and to are approximately equal, by calling @GlobalScope.is_equal_approx() on each component.

- is_finite() -> bool [const]
  Returns true if this quaternion is finite, by calling @GlobalScope.is_finite() on each component.

- is_normalized() -> bool [const]
  Returns true if this quaternion is normalized. See also normalized().

- length() -> float [const]
  Returns this quaternion's length, also called magnitude.

- length_squared() -> float [const]
  Returns this quaternion's length, squared. **Note:** This method is faster than length(), so prefer it if you only need to compare quaternion lengths.

- log() -> Quaternion [const]
  Returns the logarithm of this quaternion. Multiplies this quaternion's rotation axis by its rotation angle, and stores the result in the returned quaternion's vector part (x, y, and z). The returned quaternion's real part (w) is always 0.0.

- normalized() -> Quaternion [const]
  Returns a copy of this quaternion, normalized so that its length is 1.0. See also is_normalized().

- slerp(to: Quaternion, weight: float) -> Quaternion [const]
  Performs a spherical-linear interpolation with the to quaternion, given a weight and returns the result. Both this quaternion and to must be normalized.

- slerpni(to: Quaternion, weight: float) -> Quaternion [const]
  Performs a spherical-linear interpolation with the to quaternion, given a weight and returns the result. Unlike slerp(), this method does not check if the rotation path is smaller than 90 degrees. Both this quaternion and to must be normalized.

- spherical_cubic_interpolate(b: Quaternion, pre_a: Quaternion, post_b: Quaternion, weight: float) -> Quaternion [const]
  Performs a spherical cubic interpolation between quaternions pre_a, this vector, b, and post_b, by the given amount weight.

- spherical_cubic_interpolate_in_time(b: Quaternion, pre_a: Quaternion, post_b: Quaternion, weight: float, b_t: float, pre_a_t: float, post_b_t: float) -> Quaternion [const]
  Performs a spherical cubic interpolation between quaternions pre_a, this vector, b, and post_b, by the given amount weight. It can perform smoother interpolation than spherical_cubic_interpolate() by the time values.

## Properties

- w: float = 1.0
  W component of the quaternion. This is the "real" part. **Note:** Quaternion components should usually not be manipulated directly.

- x: float = 0.0
  X component of the quaternion. This is the value along the "imaginary" i axis. **Note:** Quaternion components should usually not be manipulated directly.

- y: float = 0.0
  Y component of the quaternion. This is the value along the "imaginary" j axis. **Note:** Quaternion components should usually not be manipulated directly.

- z: float = 0.0
  Z component of the quaternion. This is the value along the "imaginary" k axis. **Note:** Quaternion components should usually not be manipulated directly.

## Constants

- IDENTITY = Quaternion(0, 0, 0, 1)
  The identity quaternion, representing no rotation. This has the same rotation as Basis.IDENTITY. If a Vector3 is rotated (multiplied) by this quaternion, it does not change. **Note:** In GDScript, this constant is equivalent to creating a [constructor Quaternion] without any arguments. It can be used to make your code clearer, and for consistency with C#.

## Operators

- operator !=(right: Quaternion) -> bool
  Returns true if the components of both quaternions are not exactly equal. **Note:** Due to floating-point precision errors, consider using is_equal_approx() instead, which is more reliable.

- operator *(right: Quaternion) -> Quaternion
  Composes (multiplies) two quaternions. This rotates the right quaternion (the child) by this quaternion (the parent).

- operator *(right: Vector3) -> Vector3
  Rotates (multiplies) the right vector by this quaternion, returning a Vector3.

- operator *(right: float) -> Quaternion
  Multiplies each component of the Quaternion by the right float value. This operation is not meaningful on its own, but it can be used as a part of a larger expression.

- operator *(right: int) -> Quaternion
  Multiplies each component of the Quaternion by the right int value. This operation is not meaningful on its own, but it can be used as a part of a larger expression.

- operator +(right: Quaternion) -> Quaternion
  Adds each component of the left Quaternion to the right Quaternion. This operation is not meaningful on its own, but it can be used as a part of a larger expression, such as approximating an intermediate rotation between two nearby rotations.

- operator -(right: Quaternion) -> Quaternion
  Subtracts each component of the left Quaternion by the right Quaternion. This operation is not meaningful on its own, but it can be used as a part of a larger expression.

- operator /(right: float) -> Quaternion
  Divides each component of the Quaternion by the right float value. This operation is not meaningful on its own, but it can be used as a part of a larger expression.

- operator /(right: int) -> Quaternion
  Divides each component of the Quaternion by the right int value. This operation is not meaningful on its own, but it can be used as a part of a larger expression.

- operator ==(right: Quaternion) -> bool
  Returns true if the components of both quaternions are exactly equal. **Note:** Due to floating-point precision errors, consider using is_equal_approx() instead, which is more reliable.

- operator [](index: int) -> float
  Accesses each component of this quaternion by their index. Index 0 is the same as x, index 1 is the same as y, index 2 is the same as z, and index 3 is the same as w.

- operator unary+() -> Quaternion
  Returns the same value as if the + was not there. Unary + does nothing, but sometimes it can make your code more readable.

- operator unary-() -> Quaternion
  Returns the negative value of the Quaternion. This is the same as multiplying all components by -1. This operation results in a quaternion that represents the same rotation.
