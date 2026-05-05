# RandomNumberGenerator

## Meta

- Name: RandomNumberGenerator
- Source: RandomNumberGenerator.xml
- Inherits: RefCounted
- Inheritance Chain: RandomNumberGenerator -> RefCounted -> Object

## Brief Description

Provides methods for generating pseudo-random numbers.

## Description

RandomNumberGenerator is a class for generating pseudo-random numbers. It currently uses PCG32(https://www.pcg-random.org/). **Note:** The underlying algorithm is an implementation detail and should not be depended upon. To generate a random float number (within a given range) based on a time-dependent seed:

```
var rng = RandomNumberGenerator.new()
func _ready():
    var my_random_number = rng.randf_range(-10.0, 10.0)
```

## Quick Reference

```
[methods]
rand_weighted(weights: PackedFloat32Array) -> int
randf() -> float
randf_range(from: float, to: float) -> float
randfn(mean: float = 0.0, deviation: float = 1.0) -> float
randi() -> int
randi_range(from: int, to: int) -> int
randomize() -> void

[properties]
seed: int = 0
state: int = 0
```

## Tutorials

- [Random number generation]($DOCS_URL/tutorials/math/random_number_generation.html)

## Methods

- rand_weighted(weights: PackedFloat32Array) -> int
  Returns a random index with non-uniform weights. Prints an error and returns -1 if the array is empty.


```
  var rng = RandomNumberGenerator.new()

  var my_array = ["one", "two", "three", "four"]
  var weights = PackedFloat32Array([0.5, 1, 1, 2])

  # Prints one of the four elements in `my_array`.
  # It is more likely to print "four", and less likely to print "one".
  print(my_array[rng.rand_weighted(weights)])

```

- randf() -> float
  Returns a pseudo-random float between 0.0 and 1.0 (inclusive).

- randf_range(from: float, to: float) -> float
  Returns a pseudo-random float between from and to (inclusive).

- randfn(mean: float = 0.0, deviation: float = 1.0) -> float
  Returns a [normally-distributed](https://en.wikipedia.org/wiki/Normal_distribution), pseudo-random floating-point number from the specified mean and a standard deviation. This is also known as a Gaussian distribution. **Note:** This method uses the [Box-Muller transform](https://en.wikipedia.org/wiki/Box%E2%80%93Muller_transform) algorithm.

- randi() -> int
  Returns a pseudo-random 32-bit unsigned integer between 0 and 4294967295 (inclusive).

- randi_range(from: int, to: int) -> int
  Returns a pseudo-random 32-bit signed integer between from and to (inclusive).

- randomize() -> void
  Sets up a time-based seed for this RandomNumberGenerator instance. Unlike the @GlobalScope random number generation functions, different RandomNumberGenerator instances can use different seeds.

## Properties

- seed: int = 0 [set set_seed; get get_seed]
  Initializes the random number generator state based on the given seed value. A given seed will give a reproducible sequence of pseudo-random numbers. **Note:** The RNG does not have an avalanche effect, and can output similar random streams given similar seeds. Consider using a hash function to improve your seed quality if they're sourced externally. **Note:** Setting this property produces a side effect of changing the internal state, so make sure to initialize the seed *before* modifying the state: **Note:** The default value of this property is pseudo-random, and changes when calling randomize(). The 0 value documented here is a placeholder, and not the actual default seed.

```
var rng = RandomNumberGenerator.new()
rng.seed = hash("Godot")
rng.state = 100 # Restore to some previously saved state.
```

- state: int = 0 [set set_state; get get_state]
  The current state of the random number generator. Save and restore this property to restore the generator to a previous state:

```
var rng = RandomNumberGenerator.new()
print(rng.randf())
var saved_state = rng.state # Store current state.
print(rng.randf()) # Advance internal state.
rng.state = saved_state # Restore the state.
print(rng.randf()) # Prints the same value as previously.
```

**Note:** Do not set state to arbitrary values, since the random number generator requires the state to have certain qualities to behave properly. It should only be set to values that came from the state property itself. To initialize the random number generator with arbitrary input, use seed instead. **Note:** The default value of this property is pseudo-random, and changes when calling randomize(). The 0 value documented here is a placeholder, and not the actual default state.
