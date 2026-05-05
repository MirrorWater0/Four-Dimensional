# bool

## Meta

- Name: bool
- Source: bool.xml
- Inherits: none

## Brief Description

A built-in boolean type.

## Description

The bool is a built-in Variant type that may only store one of two values: true or false. You can imagine it as a switch that can be either turned on or off, or as a binary digit that can either be 1 or 0. Booleans can be directly used in if, and other conditional statements:

```
var can_shoot = true
if can_shoot:
    launch_bullet()
```

```
bool canShoot = true;
if (canShoot)
{
    LaunchBullet();
}
```

All comparison operators return booleans (==, >, <=, etc.). As such, it is not necessary to compare booleans themselves. You do not need to add == true or == false. Booleans can be combined with the logical operators and, or, not to create complex conditions:

```
if bullets > 0 and not is_reloading():
    launch_bullet()

if bullets == 0 or is_reloading():
    play_clack_sound()
```

```
if (bullets > 0 && !IsReloading())
{
    LaunchBullet();
}

if (bullets == 0 || IsReloading())
{
    PlayClackSound();
}
```

**Note:** In modern programming languages, logical operators are evaluated in order. All remaining conditions are skipped if their result would have no effect on the final value. This concept is known as [short-circuit evaluation](https://en.wikipedia.org/wiki/Short-circuit_evaluation) and can be useful to avoid evaluating expensive conditions in some performance-critical cases. **Note:** By convention, built-in methods and properties that return booleans are usually defined as yes-no questions, single adjectives, or similar (String.is_empty(), Node.can_process(), Camera2D.enabled, etc.).

## Constructors

- bool() -> bool
  Constructs a bool set to false.

- bool(from: bool) -> bool
  Constructs a bool as a copy of the given bool.

- bool(from: float) -> bool
  Cast a float value to a boolean value. Returns false if from is equal to 0.0 (including -0.0), and true for all other values (including @GDScript.INF and @GDScript.NAN).

- bool(from: int) -> bool
  Cast an int value to a boolean value. Returns false if from is equal to 0, and true for all other values.

## Operators

- operator !=(right: bool) -> bool
  Returns true if the two booleans are not equal. That is, one is true and the other is false. This operation can be seen as a logical XOR.

- operator <(right: bool) -> bool
  Returns true if the left operand is false and the right operand is true.

- operator ==(right: bool) -> bool
  Returns true if the two booleans are equal. That is, both are true or both are false. This operation can be seen as a logical EQ or XNOR.

- operator >(right: bool) -> bool
  Returns true if the left operand is true and the right operand is false.
