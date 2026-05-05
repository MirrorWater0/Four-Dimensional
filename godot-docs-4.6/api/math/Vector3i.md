# Vector3i

## Meta

- Name: Vector3i
- Source: Vector3i.xml
- Inherits: none

## Brief Description

A 3D vector using integer coordinates.

## Description

A 3-element structure that can be used to represent 3D grid coordinates or any other triplet of integers. It uses integer coordinates and is therefore preferable to Vector3 when exact precision is required. Note that the values are limited to 32 bits, and unlike Vector3 this cannot be configured with an engine build option. Use int or PackedInt64Array if 64-bit values are needed. **Note:** In a boolean context, a Vector3i will evaluate to false if it's equal to Vector3i(0, 0, 0). Otherwise, a Vector3i will always evaluate to true.

## Quick Reference

```
[methods]
abs() -> Vector3i [const]
clamp(min: Vector3i, max: Vector3i) -> Vector3i [const]
clampi(min: int, max: int) -> Vector3i [const]
distance_squared_to(to: Vector3i) -> int [const]
distance_to(to: Vector3i) -> float [const]
length() -> float [const]
length_squared() -> int [const]
max(with: Vector3i) -> Vector3i [const]
max_axis_index() -> int [const]
maxi(with: int) -> Vector3i [const]
min(with: Vector3i) -> Vector3i [const]
min_axis_index() -> int [const]
mini(with: int) -> Vector3i [const]
sign() -> Vector3i [const]
snapped(step: Vector3i) -> Vector3i [const]
snappedi(step: int) -> Vector3i [const]

[properties]
x: int = 0
y: int = 0
z: int = 0
```

## Tutorials

- [Math documentation index]($DOCS_URL/tutorials/math/index.html)
- [Vector math]($DOCS_URL/tutorials/math/vector_math.html)
- [3Blue1Brown Essence of Linear Algebra](https://www.youtube.com/playlist?list=PLZHQObOWTQDPD3MizzM2xVFitgF8hE_ab)

## Constructors

- Vector3i() -> Vector3i
  Constructs a default-initialized Vector3i with all components set to 0.

- Vector3i(from: Vector3i) -> Vector3i
  Constructs a Vector3i as a copy of the given Vector3i.

- Vector3i(from: Vector3) -> Vector3i
  Constructs a new Vector3i from the given Vector3 by truncating components' fractional parts (rounding towards zero). For a different behavior consider passing the result of Vector3.ceil(), Vector3.floor() or Vector3.round() to this constructor instead.

- Vector3i(x: int, y: int, z: int) -> Vector3i
  Returns a Vector3i with the given components.

## Methods

- abs() -> Vector3i [const]
  Returns a new vector with all components in absolute values (i.e. positive).

- clamp(min: Vector3i, max: Vector3i) -> Vector3i [const]
  Returns a new vector with all components clamped between the components of min and max, by running @GlobalScope.clamp() on each component.

- clampi(min: int, max: int) -> Vector3i [const]
  Returns a new vector with all components clamped between min and max, by running @GlobalScope.clamp() on each component.

- distance_squared_to(to: Vector3i) -> int [const]
  Returns the squared distance between this vector and to. This method runs faster than distance_to(), so prefer it if you need to compare vectors or need the squared distance for some formula.

- distance_to(to: Vector3i) -> float [const]
  Returns the distance between this vector and to.

- length() -> float [const]
  Returns the length (magnitude) of this vector.

- length_squared() -> int [const]
  Returns the squared length (squared magnitude) of this vector. This method runs faster than length(), so prefer it if you need to compare vectors or need the squared distance for some formula.

- max(with: Vector3i) -> Vector3i [const]
  Returns the component-wise maximum of this and with, equivalent to Vector3i(maxi(x, with.x), maxi(y, with.y), maxi(z, with.z)).

- max_axis_index() -> int [const]
  Returns the axis of the vector's highest value. See AXIS_* constants. If all components are equal, this method returns AXIS_X.

- maxi(with: int) -> Vector3i [const]
  Returns the component-wise maximum of this and with, equivalent to Vector3i(maxi(x, with), maxi(y, with), maxi(z, with)).

- min(with: Vector3i) -> Vector3i [const]
  Returns the component-wise minimum of this and with, equivalent to Vector3i(mini(x, with.x), mini(y, with.y), mini(z, with.z)).

- min_axis_index() -> int [const]
  Returns the axis of the vector's lowest value. See AXIS_* constants. If all components are equal, this method returns AXIS_Z.

- mini(with: int) -> Vector3i [const]
  Returns the component-wise minimum of this and with, equivalent to Vector3i(mini(x, with), mini(y, with), mini(z, with)).

- sign() -> Vector3i [const]
  Returns a new vector with each component set to 1 if it's positive, -1 if it's negative, and 0 if it's zero. The result is identical to calling @GlobalScope.sign() on each component.

- snapped(step: Vector3i) -> Vector3i [const]
  Returns a new vector with each component snapped to the closest multiple of the corresponding component in step.

- snappedi(step: int) -> Vector3i [const]
  Returns a new vector with each component snapped to the closest multiple of step.

## Properties

- x: int = 0
  The vector's X component. Also accessible by using the index position 0.

- y: int = 0
  The vector's Y component. Also accessible by using the index position 1.

- z: int = 0
  The vector's Z component. Also accessible by using the index position 2.

## Constants

### Enum Axis

- AXIS_X = 0
  Enumerated value for the X axis. Returned by max_axis_index() and min_axis_index().

- AXIS_Y = 1
  Enumerated value for the Y axis. Returned by max_axis_index() and min_axis_index().

- AXIS_Z = 2
  Enumerated value for the Z axis. Returned by max_axis_index() and min_axis_index().

- ZERO = Vector3i(0, 0, 0)
  Zero vector, a vector with all components set to 0.

- ONE = Vector3i(1, 1, 1)
  One vector, a vector with all components set to 1.

- MIN = Vector3i(-2147483648, -2147483648, -2147483648)
  Min vector, a vector with all components equal to INT32_MIN. Can be used as a negative integer equivalent of Vector3.INF.

- MAX = Vector3i(2147483647, 2147483647, 2147483647)
  Max vector, a vector with all components equal to INT32_MAX. Can be used as an integer equivalent of Vector3.INF.

- LEFT = Vector3i(-1, 0, 0)
  Left unit vector. Represents the local direction of left, and the global direction of west.

- RIGHT = Vector3i(1, 0, 0)
  Right unit vector. Represents the local direction of right, and the global direction of east.

- UP = Vector3i(0, 1, 0)
  Up unit vector.

- DOWN = Vector3i(0, -1, 0)
  Down unit vector.

- FORWARD = Vector3i(0, 0, -1)
  Forward unit vector. Represents the local direction of forward, and the global direction of north.

- BACK = Vector3i(0, 0, 1)
  Back unit vector. Represents the local direction of back, and the global direction of south.

## Operators

- operator !=(right: Vector3i) -> bool
  Returns true if the vectors are not equal.

- operator %(right: Vector3i) -> Vector3i
  Gets the remainder of each component of the Vector3i with the components of the given Vector3i. This operation uses truncated division, which is often not desired as it does not work well with negative numbers. Consider using @GlobalScope.posmod() instead if you want to handle negative numbers.

```
print(Vector3i(10, -20, 30) % Vector3i(7, 8, 9)) # Prints (3, -4, 3)
```

- operator %(right: int) -> Vector3i
  Gets the remainder of each component of the Vector3i with the given int. This operation uses truncated division, which is often not desired as it does not work well with negative numbers. Consider using @GlobalScope.posmod() instead if you want to handle negative numbers.

```
print(Vector3i(10, -20, 30) % 7) # Prints (3, -6, 2)
```

- operator *(right: Vector3i) -> Vector3i
  Multiplies each component of the Vector3i by the components of the given Vector3i.

```
print(Vector3i(10, 20, 30) * Vector3i(3, 4, 5)) # Prints (30, 80, 150)
```

- operator *(right: float) -> Vector3
  Multiplies each component of the Vector3i by the given float. Returns a Vector3.

```
print(Vector3i(10, 15, 20) * 0.9) # Prints (9.0, 13.5, 18.0)
```

- operator *(right: int) -> Vector3i
  Multiplies each component of the Vector3i by the given int.

- operator +(right: Vector3i) -> Vector3i
  Adds each component of the Vector3i by the components of the given Vector3i.

```
print(Vector3i(10, 20, 30) + Vector3i(3, 4, 5)) # Prints (13, 24, 35)
```

- operator -(right: Vector3i) -> Vector3i
  Subtracts each component of the Vector3i by the components of the given Vector3i.

```
print(Vector3i(10, 20, 30) - Vector3i(3, 4, 5)) # Prints (7, 16, 25)
```

- operator /(right: Vector3i) -> Vector3i
  Divides each component of the Vector3i by the components of the given Vector3i.

```
print(Vector3i(10, 20, 30) / Vector3i(2, 5, 3)) # Prints (5, 4, 10)
```

- operator /(right: float) -> Vector3
  Divides each component of the Vector3i by the given float. Returns a Vector3.

```
print(Vector3i(1, 2, 3) / 2.5) # Prints (0.4, 0.8, 1.2)
```

- operator /(right: int) -> Vector3i
  Divides each component of the Vector3i by the given int.

- operator <(right: Vector3i) -> bool
  Compares two Vector3i vectors by first checking if the X value of the left vector is less than the X value of the right vector. If the X values are exactly equal, then it repeats this check with the Y values of the two vectors, and then with the Z values. This operator is useful for sorting vectors.

- operator <=(right: Vector3i) -> bool
  Compares two Vector3i vectors by first checking if the X value of the left vector is less than or equal to the X value of the right vector. If the X values are exactly equal, then it repeats this check with the Y values of the two vectors, and then with the Z values. This operator is useful for sorting vectors.

- operator ==(right: Vector3i) -> bool
  Returns true if the vectors are equal.

- operator >(right: Vector3i) -> bool
  Compares two Vector3i vectors by first checking if the X value of the left vector is greater than the X value of the right vector. If the X values are exactly equal, then it repeats this check with the Y values of the two vectors, and then with the Z values. This operator is useful for sorting vectors.

- operator >=(right: Vector3i) -> bool
  Compares two Vector3i vectors by first checking if the X value of the left vector is greater than or equal to the X value of the right vector. If the X values are exactly equal, then it repeats this check with the Y values of the two vectors, and then with the Z values. This operator is useful for sorting vectors.

- operator [](index: int) -> int
  Access vector components using their index. v0 is equivalent to v.x, v1 is equivalent to v.y, and v2 is equivalent to v.z.

- operator unary+() -> Vector3i
  Returns the same value as if the + was not there. Unary + does nothing, but sometimes it can make your code more readable.

- operator unary-() -> Vector3i
  Returns the negative value of the Vector3i. This is the same as writing Vector3i(-v.x, -v.y, -v.z). This operation flips the direction of the vector while keeping the same magnitude.
