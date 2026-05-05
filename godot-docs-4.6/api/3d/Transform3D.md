# Transform3D

## Meta

- Name: Transform3D
- Source: Transform3D.xml
- Inherits: none

## Brief Description

A 3×4 matrix representing a 3D transformation.

## Description

The Transform3D built-in Variant type is a 3×4 matrix representing a transformation in 3D space. It contains a Basis, which on its own can represent rotation, scale, and shear. Additionally, combined with its own origin, the transform can also represent a translation. For a general introduction, see the [Matrices and transforms]($DOCS_URL/tutorials/math/matrices_and_transforms.html) tutorial. **Note:** Godot uses a [right-handed coordinate system](https://en.wikipedia.org/wiki/Right-hand_rule), which is a common standard. For directions, the convention for built-in types like Camera3D is for -Z to point forward (+X is right, +Y is up, and +Z is back). Other objects may use different direction conventions. For more information, see the [3D asset direction conventions]($DOCS_URL/tutorials/assets_pipeline/importing_3d_scenes/model_export_considerations.html#d-asset-direction-conventions) tutorial.

## Quick Reference

```
[methods]
affine_inverse() -> Transform3D [const]
interpolate_with(xform: Transform3D, weight: float) -> Transform3D [const]
inverse() -> Transform3D [const]
is_equal_approx(xform: Transform3D) -> bool [const]
is_finite() -> bool [const]
looking_at(target: Vector3, up: Vector3 = Vector3(0, 1, 0), use_model_front: bool = false) -> Transform3D [const]
orthonormalized() -> Transform3D [const]
rotated(axis: Vector3, angle: float) -> Transform3D [const]
rotated_local(axis: Vector3, angle: float) -> Transform3D [const]
scaled(scale: Vector3) -> Transform3D [const]
scaled_local(scale: Vector3) -> Transform3D [const]
translated(offset: Vector3) -> Transform3D [const]
translated_local(offset: Vector3) -> Transform3D [const]

[properties]
basis: Basis = Basis(1, 0, 0, 0, 1, 0, 0, 0, 1)
origin: Vector3 = Vector3(0, 0, 0)
```

## Tutorials

- [Math documentation index]($DOCS_URL/tutorials/math/index.html)
- [Matrices and transforms]($DOCS_URL/tutorials/math/matrices_and_transforms.html)
- [Using 3D transforms]($DOCS_URL/tutorials/3d/using_transforms.html)
- [Matrix Transform Demo](https://godotengine.org/asset-library/asset/2787)
- [3D Platformer Demo](https://godotengine.org/asset-library/asset/2748)
- [2.5D Game Demo](https://godotengine.org/asset-library/asset/2783)

## Constructors

- Transform3D() -> Transform3D
  Constructs a Transform3D identical to IDENTITY. **Note:** In C#, this constructs a Transform3D with its origin and the components of its basis set to Vector3.ZERO.

- Transform3D(from: Transform3D) -> Transform3D
  Constructs a Transform3D as a copy of the given Transform3D.

- Transform3D(basis: Basis, origin: Vector3) -> Transform3D
  Constructs a Transform3D from a Basis and Vector3.

- Transform3D(from: Projection) -> Transform3D
  Constructs a Transform3D from a Projection. Because Transform3D is a 3×4 matrix and Projection is a 4×4 matrix, this operation trims the last row of the projection matrix (from.x.w, from.y.w, from.z.w, and from.w.w are not included in the new transform).

- Transform3D(x_axis: Vector3, y_axis: Vector3, z_axis: Vector3, origin: Vector3) -> Transform3D
  Constructs a Transform3D from four Vector3 values (also called matrix columns). The first three arguments are the basis's axes (Basis.x, Basis.y, and Basis.z).

## Methods

- affine_inverse() -> Transform3D [const]
  Returns the inverted version of this transform. Unlike inverse(), this method works with almost any basis, including non-uniform ones, but is slower. See also Basis.inverse(). **Note:** For this method to return correctly, the transform's basis needs to have a determinant that is not exactly 0.0 (see Basis.determinant()).

- interpolate_with(xform: Transform3D, weight: float) -> Transform3D [const]
  Returns the result of the linear interpolation between this transform and xform by the given weight. The weight should be between 0.0 and 1.0 (inclusive). Values outside this range are allowed and can be used to perform *extrapolation* instead.

- inverse() -> Transform3D [const]
  Returns the [inverted version of this transform](https://en.wikipedia.org/wiki/Invertible_matrix). See also Basis.inverse(). **Note:** For this method to return correctly, the transform's basis needs to be *orthonormal* (see orthonormalized()). That means the basis should only represent a rotation. If it does not, use affine_inverse() instead.

- is_equal_approx(xform: Transform3D) -> bool [const]
  Returns true if this transform and xform are approximately equal, by running @GlobalScope.is_equal_approx() on each component.

- is_finite() -> bool [const]
  Returns true if this transform is finite, by calling @GlobalScope.is_finite() on each component.

- looking_at(target: Vector3, up: Vector3 = Vector3(0, 1, 0), use_model_front: bool = false) -> Transform3D [const]
  Returns a copy of this transform rotated so that the forward axis (-Z) points towards the target position. The up axis (+Y) points as close to the up vector as possible while staying perpendicular to the forward axis. The resulting transform is orthonormalized. The existing rotation, scale, and skew information from the original transform is discarded. The target and up vectors cannot be zero, cannot be parallel to each other, and are defined in global/parent space. If use_model_front is true, the +Z axis (asset front) is treated as forward (implies +X is left) and points toward the target position. By default, the -Z axis (camera forward) is treated as forward (implies +X is right).

- orthonormalized() -> Transform3D [const]
  Returns a copy of this transform with its basis orthonormalized. An orthonormal basis is both *orthogonal* (the axes are perpendicular to each other) and *normalized* (the axes have a length of 1.0), which also means it can only represent a rotation. See also Basis.orthonormalized().

- rotated(axis: Vector3, angle: float) -> Transform3D [const]
  Returns a copy of this transform rotated around the given axis by the given angle (in radians). The axis must be a normalized vector (see Vector3.normalized()). If angle is positive, the basis is rotated counter-clockwise around the axis. This method is an optimized version of multiplying the given transform X with a corresponding rotation transform R from the left, i.e., R * X. This can be seen as transforming with respect to the global/parent frame.

- rotated_local(axis: Vector3, angle: float) -> Transform3D [const]
  Returns a copy of this transform rotated around the given axis by the given angle (in radians). The axis must be a normalized vector in the transform's local coordinate system. For example, to rotate around the local X-axis, use Vector3.RIGHT. This method is an optimized version of multiplying the given transform X with a corresponding rotation transform R from the right, i.e., X * R. This can be seen as transforming with respect to the local frame.

- scaled(scale: Vector3) -> Transform3D [const]
  Returns a copy of this transform scaled by the given scale factor. This method is an optimized version of multiplying the given transform X with a corresponding scaling transform S from the left, i.e., S * X. This can be seen as transforming with respect to the global/parent frame.

- scaled_local(scale: Vector3) -> Transform3D [const]
  Returns a copy of this transform scaled by the given scale factor. This method is an optimized version of multiplying the given transform X with a corresponding scaling transform S from the right, i.e., X * S. This can be seen as transforming with respect to the local frame.

- translated(offset: Vector3) -> Transform3D [const]
  Returns a copy of this transform translated by the given offset. This method is an optimized version of multiplying the given transform X with a corresponding translation transform T from the left, i.e., T * X. This can be seen as transforming with respect to the global/parent frame.

- translated_local(offset: Vector3) -> Transform3D [const]
  Returns a copy of this transform translated by the given offset. This method is an optimized version of multiplying the given transform X with a corresponding translation transform T from the right, i.e., X * T. This can be seen as transforming with respect to the local frame.

## Properties

- basis: Basis = Basis(1, 0, 0, 0, 1, 0, 0, 0, 1)
  The Basis of this transform. It is composed by 3 axes (Basis.x, Basis.y, and Basis.z). Together, these represent the transform's rotation, scale, and shear.

- origin: Vector3 = Vector3(0, 0, 0)
  The translation offset of this transform. In 3D space, this can be seen as the position.

## Constants

- IDENTITY = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0)
  The identity Transform3D. This is a transform with no translation, no rotation, and a scale of Vector3.ONE. Its basis is equal to Basis.IDENTITY. This also means that: - Its Basis.x points right (Vector3.RIGHT); - Its Basis.y points up (Vector3.UP); - Its Basis.z points back (Vector3.BACK).

```
var transform = Transform3D.IDENTITY
var basis = transform.basis
print("| X | Y | Z | Origin")
print("| %.f | %.f | %.f | %.f" % [basis.x.x, basis.y.x, basis.z.x, transform.origin.x])
print("| %.f | %.f | %.f | %.f" % [basis.x.y, basis.y.y, basis.z.y, transform.origin.y])
print("| %.f | %.f | %.f | %.f" % [basis.x.z, basis.y.z, basis.z.z, transform.origin.z])
# Prints:
# | X | Y | Z | Origin
# | 1 | 0 | 0 | 0
# | 0 | 1 | 0 | 0
# | 0 | 0 | 1 | 0
```

If a Vector3, an AABB, a Plane, a PackedVector3Array, or another Transform3D is transformed (multiplied) by this constant, no transformation occurs. **Note:** In GDScript, this constant is equivalent to creating a [constructor Transform3D] without any arguments. It can be used to make your code clearer, and for consistency with C#.

- FLIP_X = Transform3D(-1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0)
  Transform3D with mirroring applied perpendicular to the YZ plane. Its basis is equal to Basis.FLIP_X.

- FLIP_Y = Transform3D(1, 0, 0, 0, -1, 0, 0, 0, 1, 0, 0, 0)
  Transform3D with mirroring applied perpendicular to the XZ plane. Its basis is equal to Basis.FLIP_Y.

- FLIP_Z = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, -1, 0, 0, 0)
  Transform3D with mirroring applied perpendicular to the XY plane. Its basis is equal to Basis.FLIP_Z.

## Operators

- operator !=(right: Transform3D) -> bool
  Returns true if the components of both transforms are not equal. **Note:** Due to floating-point precision errors, consider using is_equal_approx() instead, which is more reliable.

- operator *(right: AABB) -> AABB
  Transforms (multiplies) the AABB by this transformation matrix.

- operator *(right: PackedVector3Array) -> PackedVector3Array
  Transforms (multiplies) every Vector3 element of the given PackedVector3Array by this transformation matrix. On larger arrays, this operation is much faster than transforming each Vector3 individually.

- operator *(right: Plane) -> Plane
  Transforms (multiplies) the Plane by this transformation matrix.

- operator *(right: Transform3D) -> Transform3D
  Transforms (multiplies) this transform by the right transform. This is the operation performed between parent and child Node3Ds. **Note:** If you need to only modify one attribute of this transform, consider using one of the following methods, instead: - For translation, see translated() or translated_local(). - For rotation, see rotated() or rotated_local(). - For scale, see scaled() or scaled_local().

- operator *(right: Vector3) -> Vector3
  Transforms (multiplies) the Vector3 by this transformation matrix.

- operator *(right: float) -> Transform3D
  Multiplies all components of the Transform3D by the given float, including the origin. This affects the transform's scale uniformly, scaling the basis.

- operator *(right: int) -> Transform3D
  Multiplies all components of the Transform3D by the given int, including the origin. This affects the transform's scale uniformly, scaling the basis.

- operator /(right: float) -> Transform3D
  Divides all components of the Transform3D by the given float, including the origin. This affects the transform's scale uniformly, scaling the basis.

- operator /(right: int) -> Transform3D
  Divides all components of the Transform3D by the given int, including the origin. This affects the transform's scale uniformly, scaling the basis.

- operator ==(right: Transform3D) -> bool
  Returns true if the components of both transforms are exactly equal. **Note:** Due to floating-point precision errors, consider using is_equal_approx() instead, which is more reliable.
