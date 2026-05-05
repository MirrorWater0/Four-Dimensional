# PackedInt32Array

## Meta

- Name: PackedInt32Array
- Source: PackedInt32Array.xml
- Inherits: none

## Brief Description

A packed array of 32-bit integers.

## Description

An array specifically designed to hold 32-bit integer values. Packs data tightly, so it saves memory for large array sizes. **Note:** This type stores signed 32-bit integers, which means it can take values in the interval [-2^31, 2^31 - 1], i.e. [-2147483648, 2147483647]. Exceeding those bounds will wrap around. In comparison, int uses signed 64-bit integers which can hold much larger values. If you need to pack 64-bit integers tightly, see PackedInt64Array. **Note:** Packed arrays are always passed by reference. To get a copy of an array that can be modified independently of the original array, use duplicate(). This is *not* the case for built-in properties and methods. In these cases the returned packed array is a copy, and changing it will *not* affect the original value. To update a built-in property of this type, modify the returned array and then assign it to the property again.

## Quick Reference

```
[methods]
append(value: int) -> bool
append_array(array: PackedInt32Array) -> void
bsearch(value: int, before: bool = true) -> int [const]
clear() -> void
count(value: int) -> int [const]
duplicate() -> PackedInt32Array [const]
erase(value: int) -> bool
fill(value: int) -> void
find(value: int, from: int = 0) -> int [const]
get(index: int) -> int [const]
has(value: int) -> bool [const]
insert(at_index: int, value: int) -> int
is_empty() -> bool [const]
push_back(value: int) -> bool
remove_at(index: int) -> void
resize(new_size: int) -> int
reverse() -> void
rfind(value: int, from: int = -1) -> int [const]
set(index: int, value: int) -> void
size() -> int [const]
slice(begin: int, end: int = 2147483647) -> PackedInt32Array [const]
sort() -> void
to_byte_array() -> PackedByteArray [const]
```

## Constructors

- PackedInt32Array() -> PackedInt32Array
  Constructs an empty PackedInt32Array.

- PackedInt32Array(from: PackedInt32Array) -> PackedInt32Array
  Constructs a PackedInt32Array as a copy of the given PackedInt32Array.

- PackedInt32Array(from: Array) -> PackedInt32Array
  Constructs a new PackedInt32Array. Optionally, you can pass in a generic Array that will be converted.

## Methods

- append(value: int) -> bool
  Appends an element at the end of the array (alias of push_back()).

- append_array(array: PackedInt32Array) -> void
  Appends a PackedInt32Array at the end of this array.

- bsearch(value: int, before: bool = true) -> int [const]
  Finds the index of an existing value (or the insertion index that maintains sorting order, if the value is not yet present in the array) using binary search. Optionally, a before specifier can be passed. If false, the returned index comes after all existing entries of the value in the array. **Note:** Calling bsearch() on an unsorted array results in unexpected behavior.

- clear() -> void
  Clears the array. This is equivalent to using resize() with a size of 0.

- count(value: int) -> int [const]
  Returns the number of times an element is in the array.

- duplicate() -> PackedInt32Array [const]
  Creates a copy of the array, and returns it.

- erase(value: int) -> bool
  Removes the first occurrence of a value from the array and returns true. If the value does not exist in the array, nothing happens and false is returned. To remove an element by index, use remove_at() instead.

- fill(value: int) -> void
  Assigns the given value to all elements in the array. This can typically be used together with resize() to create an array with a given size and initialized elements.

- find(value: int, from: int = 0) -> int [const]
  Searches the array for a value and returns its index or -1 if not found. Optionally, the initial search index can be passed.

- get(index: int) -> int [const]
  Returns the 32-bit integer at the given index in the array. If index is out-of-bounds or negative, this method fails and returns 0. This method is similar (but not identical) to the [] operator. Most notably, when this method fails, it doesn't pause project execution if run from the editor.

- has(value: int) -> bool [const]
  Returns true if the array contains value.

- insert(at_index: int, value: int) -> int
  Inserts a new integer at a given position in the array. The position must be valid, or at the end of the array (idx == size()).

- is_empty() -> bool [const]
  Returns true if the array is empty.

- push_back(value: int) -> bool
  Appends a value to the array.

- remove_at(index: int) -> void
  Removes an element from the array by index.

- resize(new_size: int) -> int
  Sets the size of the array. If the array is grown, reserves elements at the end of the array. If the array is shrunk, truncates the array to the new size. Calling resize() once and assigning the new values is faster than adding new elements one by one. Returns OK on success, or one of the following Error constants if this method fails: ERR_INVALID_PARAMETER if the size is negative, or ERR_OUT_OF_MEMORY if allocations fail. Use size() to find the actual size of the array after resize.

- reverse() -> void
  Reverses the order of the elements in the array.

- rfind(value: int, from: int = -1) -> int [const]
  Searches the array in reverse order. Optionally, a start search index can be passed. If negative, the start index is considered relative to the end of the array.

- set(index: int, value: int) -> void
  Changes the integer at the given index.

- size() -> int [const]
  Returns the number of elements in the array.

- slice(begin: int, end: int = 2147483647) -> PackedInt32Array [const]
  Returns the slice of the PackedInt32Array, from begin (inclusive) to end (exclusive), as a new PackedInt32Array. The absolute value of begin and end will be clamped to the array size, so the default value for end makes it slice to the size of the array by default (i.e. arr.slice(1) is a shorthand for arr.slice(1, arr.size())). If either begin or end are negative, they will be relative to the end of the array (i.e. arr.slice(0, -2) is a shorthand for arr.slice(0, arr.size() - 2)).

- sort() -> void
  Sorts the elements of the array in ascending order.

- to_byte_array() -> PackedByteArray [const]
  Returns a copy of the data converted to a PackedByteArray, where each element has been encoded as 4 bytes. The size of the new array will be int32_array.size() * 4.

## Operators

- operator !=(right: PackedInt32Array) -> bool
  Returns true if contents of the arrays differ.

- operator +(right: PackedInt32Array) -> PackedInt32Array
  Returns a new PackedInt32Array with contents of right added at the end of this array. For better performance, consider using append_array() instead.

- operator ==(right: PackedInt32Array) -> bool
  Returns true if contents of both arrays are the same, i.e. they have all equal ints at the corresponding indices.

- operator [](index: int) -> int
  Returns the int at index index. Negative indices can be used to access the elements starting from the end. Using index out of array's bounds will result in an error. Note that int type is 64-bit, unlike the values stored in the array.
