# PackedVector4Array

## Meta

- Name: PackedVector4Array
- Source: PackedVector4Array.xml
- Inherits: none

## Brief Description

A packed array of Vector4s.

## Description

An array specifically designed to hold Vector4. Packs data tightly, so it saves memory for large array sizes. **Differences between packed arrays, typed arrays, and untyped arrays:** Packed arrays are generally faster to iterate on and modify compared to a typed array of the same type (e.g. PackedVector4Array versus ArrayVector4). Also, packed arrays consume less memory. As a downside, packed arrays are less flexible as they don't offer as many convenience methods such as Array.map(). Typed arrays are in turn faster to iterate on and modify than untyped arrays. **Note:** Packed arrays are always passed by reference. To get a copy of an array that can be modified independently of the original array, use duplicate(). This is *not* the case for built-in properties and methods. In these cases the returned packed array is a copy, and changing it will *not* affect the original value. To update a built-in property of this type, modify the returned array and then assign it to the property again.

## Quick Reference

```
[methods]
append(value: Vector4) -> bool
append_array(array: PackedVector4Array) -> void
bsearch(value: Vector4, before: bool = true) -> int [const]
clear() -> void
count(value: Vector4) -> int [const]
duplicate() -> PackedVector4Array [const]
erase(value: Vector4) -> bool
fill(value: Vector4) -> void
find(value: Vector4, from: int = 0) -> int [const]
get(index: int) -> Vector4 [const]
has(value: Vector4) -> bool [const]
insert(at_index: int, value: Vector4) -> int
is_empty() -> bool [const]
push_back(value: Vector4) -> bool
remove_at(index: int) -> void
resize(new_size: int) -> int
reverse() -> void
rfind(value: Vector4, from: int = -1) -> int [const]
set(index: int, value: Vector4) -> void
size() -> int [const]
slice(begin: int, end: int = 2147483647) -> PackedVector4Array [const]
sort() -> void
to_byte_array() -> PackedByteArray [const]
```

## Constructors

- PackedVector4Array() -> PackedVector4Array
  Constructs an empty PackedVector4Array.

- PackedVector4Array(from: PackedVector4Array) -> PackedVector4Array
  Constructs a PackedVector4Array as a copy of the given PackedVector4Array.

- PackedVector4Array(from: Array) -> PackedVector4Array
  Constructs a new PackedVector4Array. Optionally, you can pass in a generic Array that will be converted. **Note:** When initializing a PackedVector4Array with elements, it must be initialized with an Array of Vector4 values:


```
  var array = PackedVector4Array([Vector4(12, 34, 56, 78), Vector4(90, 12, 34, 56)])

```

## Methods

- append(value: Vector4) -> bool
  Appends an element at the end of the array (alias of push_back()).

- append_array(array: PackedVector4Array) -> void
  Appends a PackedVector4Array at the end of this array.

- bsearch(value: Vector4, before: bool = true) -> int [const]
  Finds the index of an existing value (or the insertion index that maintains sorting order, if the value is not yet present in the array) using binary search. Optionally, a before specifier can be passed. If false, the returned index comes after all existing entries of the value in the array. **Note:** Calling bsearch() on an unsorted array results in unexpected behavior. **Note:** Vectors with @GDScript.NAN elements don't behave the same as other vectors. Therefore, the results from this method may not be accurate if NaNs are included.

- clear() -> void
  Clears the array. This is equivalent to using resize() with a size of 0.

- count(value: Vector4) -> int [const]
  Returns the number of times an element is in the array. **Note:** Vectors with @GDScript.NAN elements don't behave the same as other vectors. Therefore, the results from this method may not be accurate if NaNs are included.

- duplicate() -> PackedVector4Array [const]
  Creates a copy of the array, and returns it.

- erase(value: Vector4) -> bool
  Removes the first occurrence of a value from the array and returns true. If the value does not exist in the array, nothing happens and false is returned. To remove an element by index, use remove_at() instead. **Note:** Vectors with @GDScript.NAN elements don't behave the same as other vectors. Therefore, the results from this method may not be accurate if NaNs are included.

- fill(value: Vector4) -> void
  Assigns the given value to all elements in the array. This can typically be used together with resize() to create an array with a given size and initialized elements.

- find(value: Vector4, from: int = 0) -> int [const]
  Searches the array for a value and returns its index or -1 if not found. Optionally, the initial search index can be passed. **Note:** Vectors with @GDScript.NAN elements don't behave the same as other vectors. Therefore, the results from this method may not be accurate if NaNs are included.

- get(index: int) -> Vector4 [const]
  Returns the Vector4 at the given index in the array. If index is out-of-bounds or negative, this method fails and returns Vector4(0, 0, 0, 0). This method is similar (but not identical) to the [] operator. Most notably, when this method fails, it doesn't pause project execution if run from the editor.

- has(value: Vector4) -> bool [const]
  Returns true if the array contains value. **Note:** Vectors with @GDScript.NAN elements don't behave the same as other vectors. Therefore, the results from this method may not be accurate if NaNs are included.

- insert(at_index: int, value: Vector4) -> int
  Inserts a new element at a given position in the array. The position must be valid, or at the end of the array (idx == size()).

- is_empty() -> bool [const]
  Returns true if the array is empty.

- push_back(value: Vector4) -> bool
  Inserts a Vector4 at the end.

- remove_at(index: int) -> void
  Removes an element from the array by index.

- resize(new_size: int) -> int
  Sets the size of the array. If the array is grown, reserves elements at the end of the array. If the array is shrunk, truncates the array to the new size. Calling resize() once and assigning the new values is faster than adding new elements one by one. Returns OK on success, or one of the following Error constants if this method fails: ERR_INVALID_PARAMETER if the size is negative, or ERR_OUT_OF_MEMORY if allocations fail. Use size() to find the actual size of the array after resize.

- reverse() -> void
  Reverses the order of the elements in the array.

- rfind(value: Vector4, from: int = -1) -> int [const]
  Searches the array in reverse order. Optionally, a start search index can be passed. If negative, the start index is considered relative to the end of the array. **Note:** Vectors with @GDScript.NAN elements don't behave the same as other vectors. Therefore, the results from this method may not be accurate if NaNs are included.

- set(index: int, value: Vector4) -> void
  Changes the Vector4 at the given index.

- size() -> int [const]
  Returns the number of elements in the array.

- slice(begin: int, end: int = 2147483647) -> PackedVector4Array [const]
  Returns the slice of the PackedVector4Array, from begin (inclusive) to end (exclusive), as a new PackedVector4Array. The absolute value of begin and end will be clamped to the array size, so the default value for end makes it slice to the size of the array by default (i.e. arr.slice(1) is a shorthand for arr.slice(1, arr.size())). If either begin or end are negative, they will be relative to the end of the array (i.e. arr.slice(0, -2) is a shorthand for arr.slice(0, arr.size() - 2)).

- sort() -> void
  Sorts the elements of the array in ascending order. **Note:** Vectors with @GDScript.NAN elements don't behave the same as other vectors. Therefore, the results from this method may not be accurate if NaNs are included.

- to_byte_array() -> PackedByteArray [const]
  Returns a PackedByteArray with each vector encoded as bytes.

## Operators

- operator !=(right: PackedVector4Array) -> bool
  Returns true if contents of the arrays differ.

- operator +(right: PackedVector4Array) -> PackedVector4Array
  Returns a new PackedVector4Array with contents of right added at the end of this array. For better performance, consider using append_array() instead.

- operator ==(right: PackedVector4Array) -> bool
  Returns true if contents of both arrays are the same, i.e. they have all equal Vector4s at the corresponding indices.

- operator [](index: int) -> Vector4
  Returns the Vector4 at index index. Negative indices can be used to access the elements starting from the end. Using index out of array's bounds will result in an error.
