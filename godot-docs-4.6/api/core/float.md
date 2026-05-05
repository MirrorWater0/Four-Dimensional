# float

## Meta

- Name: float
- Source: float.xml
- Inherits: none

## Brief Description

A built-in type for floating-point numbers.

## Description

The float built-in type is a 64-bit double-precision floating-point number, equivalent to double in C++. This type has 14 reliable decimal digits of precision. The maximum value of float is approximately 1.79769e308, and the minimum is approximately -1.79769e308. Many methods and properties in the engine use 32-bit single-precision floating-point numbers instead, equivalent to [code skip-lint]float[/code] in C++, which have 6 reliable decimal digits of precision. For data structures such as Vector2 and Vector3, Godot uses 32-bit floating-point numbers by default, but it can be changed to use 64-bit doubles if Godot is compiled with the precision=double option. Math done using the float type is not guaranteed to be exact and will often result in small errors. You should usually use the @GlobalScope.is_equal_approx() and @GlobalScope.is_zero_approx() methods instead of == to compare float values for equality.

## Tutorials

- [Wikipedia: Double-precision floating-point format](https://en.wikipedia.org/wiki/Double-precision_floating-point_format)
- [Wikipedia: Single-precision floating-point format](https://en.wikipedia.org/wiki/Single-precision_floating-point_format)

## Constructors

- float() -> float
  Constructs a default-initialized float set to 0.0.

- float(from: float) -> float
  Constructs a float as a copy of the given float.

- float(from: String) -> float
  Converts a String to a float, following the same rules as String.to_float().

- float(from: bool) -> float
  Cast a bool value to a floating-point value, float(true) will be equal to 1.0 and float(false) will be equal to 0.0.

- float(from: int) -> float
  Cast an int value to a floating-point value, float(1) will be equal to 1.0.

## Operators

- operator !=(right: float) -> bool
  Returns true if two floats are different from each other. **Note:** @GDScript.NAN doesn't behave the same as other numbers. Therefore, the results from this operator may not be accurate if NaNs are included.

- operator !=(right: int) -> bool
  Returns true if the integer has different value than the float.

- operator *(right: Color) -> Color
  Multiplies each component of the Color, including the alpha, by the given float.

```
print(1.5 * Color(0.5, 0.5, 0.5)) # Prints (0.75, 0.75, 0.75, 1.5)
```

- operator *(right: Quaternion) -> Quaternion
  Multiplies each component of the Quaternion by the given float. This operation is not meaningful on its own, but it can be used as a part of a larger expression.

- operator *(right: Vector2) -> Vector2
  Multiplies each component of the Vector2 by the given float.

```
print(2.5 * Vector2(1, 3)) # Prints (2.5, 7.5)
```

- operator *(right: Vector2i) -> Vector2
  Multiplies each component of the Vector2i by the given float. Returns a Vector2.

```
print(0.9 * Vector2i(10, 15)) # Prints (9.0, 13.5)
```

- operator *(right: Vector3) -> Vector3
  Multiplies each component of the Vector3 by the given float.

- operator *(right: Vector3i) -> Vector3
  Multiplies each component of the Vector3i by the given float. Returns a Vector3.

```
print(0.9 * Vector3i(10, 15, 20)) # Prints (9.0, 13.5, 18.0)
```

- operator *(right: Vector4) -> Vector4
  Multiplies each component of the Vector4 by the given float.

- operator *(right: Vector4i) -> Vector4
  Multiplies each component of the Vector4i by the given float. Returns a Vector4.

```
print(0.9 * Vector4i(10, 15, 20, -10)) # Prints (9.0, 13.5, 18.0, -9.0)
```

- operator *(right: float) -> float
  Multiplies two floats.

- operator *(right: int) -> float
  Multiplies a float and an int. The result is a float.

- operator **(right: float) -> float
  Raises a float to a power of a float.

```
print(39.0625**0.25) # 2.5
```

- operator **(right: int) -> float
  Raises a float to a power of an int. The result is a float.

```
print(0.9**3) # 0.729
```

- operator +(right: float) -> float
  Adds two floats.

- operator +(right: int) -> float
  Adds a float and an int. The result is a float.

- operator -(right: float) -> float
  Subtracts a float from a float.

- operator -(right: int) -> float
  Subtracts an int from a float. The result is a float.

- operator /(right: float) -> float
  Divides two floats.

- operator /(right: int) -> float
  Divides a float by an int. The result is a float.

- operator <(right: float) -> bool
  Returns true if the left float is less than the right one. **Note:** @GDScript.NAN doesn't behave the same as other numbers. Therefore, the results from this operator may not be accurate if NaNs are included.

- operator <(right: int) -> bool
  Returns true if this float is less than the given int.

- operator <=(right: float) -> bool
  Returns true if the left float is less than or equal to the right one. **Note:** @GDScript.NAN doesn't behave the same as other numbers. Therefore, the results from this operator may not be accurate if NaNs are included.

- operator <=(right: int) -> bool
  Returns true if this float is less than or equal to the given int.

- operator ==(right: float) -> bool
  Returns true if both floats are exactly equal. **Note:** Due to floating-point precision errors, consider using @GlobalScope.is_equal_approx() or @GlobalScope.is_zero_approx() instead, which are more reliable. **Note:** @GDScript.NAN doesn't behave the same as other numbers. Therefore, the results from this operator may not be accurate if NaNs are included.

- operator ==(right: int) -> bool
  Returns true if the float and the given int are equal.

- operator >(right: float) -> bool
  Returns true if the left float is greater than the right one. **Note:** @GDScript.NAN doesn't behave the same as other numbers. Therefore, the results from this operator may not be accurate if NaNs are included.

- operator >(right: int) -> bool
  Returns true if this float is greater than the given int.

- operator >=(right: float) -> bool
  Returns true if the left float is greater than or equal to the right one. **Note:** @GDScript.NAN doesn't behave the same as other numbers. Therefore, the results from this operator may not be accurate if NaNs are included.

- operator >=(right: int) -> bool
  Returns true if this float is greater than or equal to the given int.

- operator unary+() -> float
  Returns the same value as if the + was not there. Unary + does nothing, but sometimes it can make your code more readable.

- operator unary-() -> float
  Returns the negative value of the float. If positive, turns the number negative. If negative, turns the number positive. With floats, the number zero can be either positive or negative.
