# Plane

## Meta

- Name: Plane
- Source: Plane.xml
- Inherits: none

## Brief Description

A plane in Hessian normal form.

## Description

Represents a normalized plane equation. normal is the normal of the plane (a, b, c normalized), and d is the distance from the origin to the plane (in the direction of "normal"). "Over" or "Above" the plane is considered the side of the plane towards where the normal is pointing.

## Quick Reference

```
[methods]
distance_to(point: Vector3) -> float [const]
get_center() -> Vector3 [const]
has_point(point: Vector3, tolerance: float = 1e-05) -> bool [const]
intersect_3(b: Plane, c: Plane) -> Variant [const]
intersects_ray(from: Vector3, dir: Vector3) -> Variant [const]
intersects_segment(from: Vector3, to: Vector3) -> Variant [const]
is_equal_approx(to_plane: Plane) -> bool [const]
is_finite() -> bool [const]
is_point_over(point: Vector3) -> bool [const]
normalized() -> Plane [const]
project(point: Vector3) -> Vector3 [const]

[properties]
d: float = 0.0
normal: Vector3 = Vector3(0, 0, 0)
x: float = 0.0
y: float = 0.0
z: float = 0.0
```

## Tutorials

- [Math documentation index]($DOCS_URL/tutorials/math/index.html)

## Constructors

- Plane() -> Plane
  Constructs a default-initialized Plane with all components set to 0.

- Plane(from: Plane) -> Plane
  Constructs a Plane as a copy of the given Plane.

- Plane(a: float, b: float, c: float, d: float) -> Plane
  Creates a plane from the four parameters. The three components of the resulting plane's normal are a, b and c, and the plane has a distance of d from the origin.

- Plane(normal: Vector3) -> Plane
  Creates a plane from the normal vector. The plane will intersect the origin. The normal of the plane must be a unit vector.

- Plane(normal: Vector3, d: float) -> Plane
  Creates a plane from the normal vector and the plane's distance from the origin. The normal of the plane must be a unit vector.

- Plane(normal: Vector3, point: Vector3) -> Plane
  Creates a plane from the normal vector and a point on the plane. The normal of the plane must be a unit vector.

- Plane(point1: Vector3, point2: Vector3, point3: Vector3) -> Plane
  Creates a plane from the three points, given in clockwise order.

## Methods

- distance_to(point: Vector3) -> float [const]
  Returns the shortest distance from the plane to the position point. If the point is above the plane, the distance will be positive. If below, the distance will be negative.

- get_center() -> Vector3 [const]
  Returns the center of the plane.

- has_point(point: Vector3, tolerance: float = 1e-05) -> bool [const]
  Returns true if point is inside the plane. Comparison uses a custom minimum tolerance threshold.

- intersect_3(b: Plane, c: Plane) -> Variant [const]
  Returns the intersection point of the three planes b, c and this plane. If no intersection is found, null is returned.

- intersects_ray(from: Vector3, dir: Vector3) -> Variant [const]
  Returns the intersection point of a ray consisting of the position from and the direction normal dir with this plane. If no intersection is found, null is returned.

- intersects_segment(from: Vector3, to: Vector3) -> Variant [const]
  Returns the intersection point of a segment from position from to position to with this plane. If no intersection is found, null is returned.

- is_equal_approx(to_plane: Plane) -> bool [const]
  Returns true if this plane and to_plane are approximately equal, by running @GlobalScope.is_equal_approx() on each component.

- is_finite() -> bool [const]
  Returns true if this plane is finite, by calling @GlobalScope.is_finite() on each component.

- is_point_over(point: Vector3) -> bool [const]
  Returns true if point is located above the plane.

- normalized() -> Plane [const]
  Returns a copy of the plane, with normalized normal (so it's a unit vector). Returns Plane(0, 0, 0, 0) if normal can't be normalized (it has zero length).

- project(point: Vector3) -> Vector3 [const]
  Returns the orthogonal projection of point into a point in the plane.

## Properties

- d: float = 0.0
  The distance from the origin to the plane, expressed in terms of normal (according to its direction and magnitude). Actual absolute distance from the origin to the plane can be calculated as abs(d) / normal.length() (if normal has zero length then this Plane does not represent a valid plane). In the scalar equation of the plane ax + by + cz = d, this is [code skip-lint]d[/code], while the (a, b, c) coordinates are represented by the normal property.

- normal: Vector3 = Vector3(0, 0, 0)
  The normal of the plane, typically a unit vector. Shouldn't be a zero vector as Plane with such normal does not represent a valid plane. In the scalar equation of the plane ax + by + cz = d, this is the vector (a, b, c), where [code skip-lint]d[/code] is the d property.

- x: float = 0.0
  The X component of the plane's normal vector.

- y: float = 0.0
  The Y component of the plane's normal vector.

- z: float = 0.0
  The Z component of the plane's normal vector.

## Constants

- PLANE_YZ = Plane(1, 0, 0, 0)
  A plane that extends in the Y and Z axes (normal vector points +X).

- PLANE_XZ = Plane(0, 1, 0, 0)
  A plane that extends in the X and Z axes (normal vector points +Y).

- PLANE_XY = Plane(0, 0, 1, 0)
  A plane that extends in the X and Y axes (normal vector points +Z).

## Operators

- operator !=(right: Plane) -> bool
  Returns true if the planes are not equal. **Note:** Due to floating-point precision errors, consider using is_equal_approx() instead, which is more reliable.

- operator *(right: Transform3D) -> Plane
  Inversely transforms (multiplies) the Plane by the given Transform3D transformation matrix. plane * transform is equivalent to transform.affine_inverse() * plane. See Transform3D.affine_inverse().

- operator ==(right: Plane) -> bool
  Returns true if the planes are exactly equal. **Note:** Due to floating-point precision errors, consider using is_equal_approx() instead, which is more reliable.

- operator unary+() -> Plane
  Returns the same value as if the + was not there. Unary + does nothing, but sometimes it can make your code more readable.

- operator unary-() -> Plane
  Returns the negative value of the Plane. This is the same as writing Plane(-p.normal, -p.d). This operation flips the direction of the normal vector and also flips the distance value, resulting in a Plane that is in the same place, but facing the opposite direction.
