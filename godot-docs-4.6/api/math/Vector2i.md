# Vector2i

## Meta

- Name: Vector2i
- Source: Vector2i.xml
- Inherits: none

## Brief Description

A 2D vector using integer coordinates.

## Description

A 2-element structure that can be used to represent 2D grid coordinates or any other pair of integers. It uses integer coordinates and is therefore preferable to Vector2 when exact precision is required. Note that the values are limited to 32 bits, and unlike Vector2 this cannot be configured with an engine build option. Use int or PackedInt64Array if 64-bit values are needed. **Note:** In a boolean context, a Vector2i will evaluate to false if it's equal to Vector2i(0, 0). Otherwise, a Vector2i will always evaluate to true.

## Quick Reference

```
[methods]
abs() -> Vector2i [const]
aspect() -> float [const]
clamp(min: Vector2i, max: Vector2i) -> Vector2i [const]
clampi(min: int, max: int) -> Vector2i [const]
distance_squared_to(to: Vector2i) -> int [const]
distance_to(to: Vector2i) -> float [const]
length() -> float [const]
length_squared() -> int [const]
max(with: Vector2i) -> Vector2i [const]
max_axis_index() -> int [const]
maxi(with: int) -> Vector2i [const]
min(with: Vector2i) -> Vector2i [const]
min_axis_index() -> int [const]
mini(with: int) -> Vector2i [const]
sign() -> Vector2i [const]
snapped(step: Vector2i) -> Vector2i [const]
snappedi(step: int) -> Vector2i [const]

[properties]
x: int = 0
y: int = 0
```

## Tutorials

- [Math documentation index]($DOCS_URL/tutorials/math/index.html)
- [Vector math]($DOCS_URL/tutorials/math/vector_math.html)
- [3Blue1Brown Essence of Linear Algebra](https://www.youtube.com/playlist?list=PLZHQObOWTQDPD3MizzM2xVFitgF8hE_ab)

## Constructors

- Vector2i() -> Vector2i
  Constructs a default-initialized Vector2i with all components set to 0.

- Vector2i(from: Vector2i) -> Vector2i
  Constructs a Vector2i as a copy of the given Vector2i.

- Vector2i(from: Vector2) -> Vector2i
  Constructs a new Vector2i from the given Vector2 by truncating components' fractional parts (rounding towards zero). For a different behavior consider passing the result of Vector2.ceil(), Vector2.floor() or Vector2.round() to this constructor instead.

- Vector2i(x: int, y: int) -> Vector2i
  Constructs a new Vector2i from the given x and y.

## Methods

- abs() -> Vector2i [const]
  Returns a new vector with all components in absolute values (i.e. positive).

- aspect() -> float [const]
  Returns the aspect ratio of this vector, the ratio of x to y.

- clamp(min: Vector2i, max: Vector2i) -> Vector2i [const]
  Returns a new vector with all components clamped between the components of min and max, by running @GlobalScope.clamp() on each component.

- clampi(min: int, max: int) -> Vector2i [const]
  Returns a new vector with all components clamped between min and max, by running @GlobalScope.clamp() on each component.

- distance_squared_to(to: Vector2i) -> int [const]
  Returns the squared distance between this vector and to. This method runs faster than distance_to(), so prefer it if you need to compare vectors or need the squared distance for some formula.

- distance_to(to: Vector2i) -> float [const]
  Returns the distance between this vector and to.

- length() -> float [const]
  Returns the length (magnitude) of this vector.

- length_squared() -> int [const]
  Returns the squared length (squared magnitude) of this vector. This method runs faster than length(), so prefer it if you need to compare vectors or need the squared distance for some formula.

- max(with: Vector2i) -> Vector2i [const]
  Returns the component-wise maximum of this and with, equivalent to Vector2i(maxi(x, with.x), maxi(y, with.y)).

- max_axis_index() -> int [const]
  Returns the axis of the vector's highest value. See AXIS_* constants. If all components are equal, this method returns AXIS_X.

- maxi(with: int) -> Vector2i [const]
  Returns the component-wise maximum of this and with, equivalent to Vector2i(maxi(x, with), maxi(y, with)).

- min(with: Vector2i) -> Vector2i [const]
  Returns the component-wise minimum of this and with, equivalent to Vector2i(mini(x, with.x), mini(y, with.y)).

- min_axis_index() -> int [const]
  Returns the axis of the vector's lowest value. See AXIS_* constants. If all components are equal, this method returns AXIS_Y.

- mini(with: int) -> Vector2i [const]
  Returns the component-wise minimum of this and with, equivalent to Vector2i(mini(x, with), mini(y, with)).

- sign() -> Vector2i [const]
  Returns a new vector with each component set to 1 if it's positive, -1 if it's negative, and 0 if it's zero. The result is identical to calling @GlobalScope.sign() on each component.

- snapped(step: Vector2i) -> Vector2i [const]
  Returns a new vector with each component snapped to the closest multiple of the corresponding component in step.

- snappedi(step: int) -> Vector2i [const]
  Returns a new vector with each component snapped to the closest multiple of step.

## Properties

- x: int = 0
  The vector's X component. Also accessible by using the index position 0.

- y: int = 0
  The vector's Y component. Also accessible by using the index position 1.

## Constants

### Enum Axis

- AXIS_X = 0
  Enumerated value for the X axis. Returned by max_axis_index() and min_axis_index().

- AXIS_Y = 1
  Enumerated value for the Y axis. Returned by max_axis_index() and min_axis_index().

- ZERO = Vector2i(0, 0)
  Zero vector, a vector with all components set to 0.

- ONE = Vector2i(1, 1)
  One vector, a vector with all components set to 1.

- MIN = Vector2i(-2147483648, -2147483648)
  Min vector, a vector with all components equal to INT32_MIN. Can be used as a negative integer equivalent of Vector2.INF.

- MAX = Vector2i(2147483647, 2147483647)
  Max vector, a vector with all components equal to INT32_MAX. Can be used as an integer equivalent of Vector2.INF.

- LEFT = Vector2i(-1, 0)
  Left unit vector. Represents the direction of left.

- RIGHT = Vector2i(1, 0)
  Right unit vector. Represents the direction of right.

- UP = Vector2i(0, -1)
  Up unit vector. Y is down in 2D, so this vector points -Y.

- DOWN = Vector2i(0, 1)
  Down unit vector. Y is down in 2D, so this vector points +Y.

## Operators

- operator !=(right: Vector2i) -> bool
  Returns true if the vectors are not equal.

- operator %(right: Vector2i) -> Vector2i
  Gets the remainder of each component of the Vector2i with the components of the given Vector2i. This operation uses truncated division, which is often not desired as it does not work well with negative numbers. Consider using @GlobalScope.posmod() instead if you want to handle negative numbers.

```
print(Vector2i(10, -20) % Vector2i(7, 8)) # Prints (3, -4)
```

- operator %(right: int) -> Vector2i
  Gets the remainder of each component of the Vector2i with the given int. This operation uses truncated division, which is often not desired as it does not work well with negative numbers. Consider using @GlobalScope.posmod() instead if you want to handle negative numbers.

```
print(Vector2i(10, -20) % 7) # Prints (3, -6)
```

- operator *(right: Vector2i) -> Vector2i
  Multiplies each component of the Vector2i by the components of the given Vector2i.

```
print(Vector2i(10, 20) * Vector2i(3, 4)) # Prints (30, 80)
```

- operator *(right: float) -> Vector2
  Multiplies each component of the Vector2i by the given float. Returns a Vector2.

```
print(Vector2i(10, 15) * 0.9) # Prints (9.0, 13.5)
```

- operator *(right: int) -> Vector2i
  Multiplies each component of the Vector2i by the given int.

- operator +(right: Vector2i) -> Vector2i
  Adds each component of the Vector2i by the components of the given Vector2i.

```
print(Vector2i(10, 20) + Vector2i(3, 4)) # Prints (13, 24)
```

- operator -(right: Vector2i) -> Vector2i
  Subtracts each component of the Vector2i by the components of the given Vector2i.

```
print(Vector2i(10, 20) - Vector2i(3, 4)) # Prints (7, 16)
```

- operator /(right: Vector2i) -> Vector2i
  Divides each component of the Vector2i by the components of the given Vector2i.

```
print(Vector2i(10, 20) / Vector2i(2, 5)) # Prints (5, 4)
```

- operator /(right: float) -> Vector2
  Divides each component of the Vector2i by the given float. Returns a Vector2.

```
print(Vector2i(1, 2) / 2.5) # Prints (0.4, 0.8)
```

- operator /(right: int) -> Vector2i
  Divides each component of the Vector2i by the given int.

- operator <(right: Vector2i) -> bool
  Compares two Vector2i vectors by first checking if the X value of the left vector is less than the X value of the right vector. If the X values are exactly equal, then it repeats this check with the Y values of the two vectors. This operator is useful for sorting vectors.

- operator <=(right: Vector2i) -> bool
  Compares two Vector2i vectors by first checking if the X value of the left vector is less than or equal to the X value of the right vector. If the X values are exactly equal, then it repeats this check with the Y values of the two vectors. This operator is useful for sorting vectors.

- operator ==(right: Vector2i) -> bool
  Returns true if the vectors are equal.

- operator >(right: Vector2i) -> bool
  Compares two Vector2i vectors by first checking if the X value of the left vector is greater than the X value of the right vector. If the X values are exactly equal, then it repeats this check with the Y values of the two vectors. This operator is useful for sorting vectors.

- operator >=(right: Vector2i) -> bool
  Compares two Vector2i vectors by first checking if the X value of the left vector is greater than or equal to the X value of the right vector. If the X values are exactly equal, then it repeats this check with the Y values of the two vectors. This operator is useful for sorting vectors.

- operator [](index: int) -> int
  Access vector components using their index. v0 is equivalent to v.x, and v1 is equivalent to v.y.

- operator unary+() -> Vector2i
  Returns the same value as if the + was not there. Unary + does nothing, but sometimes it can make your code more readable.

- operator unary-() -> Vector2i
  Returns the negative value of the Vector2i. This is the same as writing Vector2i(-v.x, -v.y). This operation flips the direction of the vector while keeping the same magnitude.
