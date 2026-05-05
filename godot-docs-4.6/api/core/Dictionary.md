# Dictionary

## Meta

- Name: Dictionary
- Source: Dictionary.xml
- Inherits: none

## Brief Description

A built-in data structure that holds key-value pairs.

## Description

Dictionaries are associative containers that contain values referenced by unique keys. Dictionaries will preserve the insertion order when adding new entries. In other programming languages, this data structure is often referred to as a hash map or an associative array. You can define a dictionary by placing a comma-separated list of key: value pairs inside curly braces {}. Creating a dictionary:

```
var my_dict = {} # Creates an empty dictionary.

var dict_variable_key = "Another key name"
var dict_variable_value = "value2"
var another_dict = {
    "Some key name": "value1",
    dict_variable_key: dict_variable_value,
}

var points_dict = { "White": 50, "Yellow": 75, "Orange": 100 }

# Alternative Lua-style syntax.
# Doesn't require quotes around keys, but only string constants can be used as key names.
# Additionally, key names must start with a letter or an underscore.
# Here, `some_key` is a string literal, not a variable!
another_dict = {
    some_key = 42,
}
```

```
var myDict = new Godot.Collections.Dictionary(); // Creates an empty dictionary.
var pointsDict = new Godot.Collections.Dictionary
{
    { "White", 50 },
    { "Yellow", 75 },
    { "Orange", 100 },
};
```

You can access a dictionary's value by referencing its corresponding key. In the above example, points_dict["White"] will return 50. You can also write points_dict.White, which is equivalent. However, you'll have to use the bracket syntax if the key you're accessing the dictionary with isn't a fixed string (such as a number or variable).

```
@export_enum("White", "Yellow", "Orange") var my_color: String
var points_dict = { "White": 50, "Yellow": 75, "Orange": 100 }
func _ready():
    # We can't use dot syntax here as `my_color` is a variable.
    var points = points_dictmy_color
```

```
[Export(PropertyHint.Enum, "White,Yellow,Orange")]
public string MyColor { get; set; }
private Godot.Collections.Dictionary _pointsDict = new Godot.Collections.Dictionary
{
    { "White", 50 },
    { "Yellow", 75 },
    { "Orange", 100 },
};

public override void _Ready()
{
    int points = (int)_pointsDictMyColor;
}
```

In the above code, points will be assigned the value that is paired with the appropriate color selected in my_color. Dictionaries can contain more complex data:

```
var my_dict = {
    "First Array": [1, 2, 3, 4] # Assigns an Array to a String key.
}
```

```
var myDict = new Godot.Collections.Dictionary
{
    { "First Array", new Godot.Collections.Array { 1, 2, 3, 4 } }
};
```

To add a key to an existing dictionary, access it like an existing key and assign to it:

```
var points_dict = { "White": 50, "Yellow": 75, "Orange": 100 }
points_dict["Blue"] = 150 # Add "Blue" as a key and assign 150 as its value.
```

```
var pointsDict = new Godot.Collections.Dictionary
{
    { "White", 50 },
    { "Yellow", 75 },
    { "Orange", 100 },
};
pointsDict["Blue"] = 150; // Add "Blue" as a key and assign 150 as its value.
```

Finally, untyped dictionaries can contain different types of keys and values in the same dictionary:

```
# This is a valid dictionary.
# To access the string "Nested value" below, use `my_dict.sub_dict.sub_key` or `my_dict["sub_dict"]["sub_key"]`.
# Indexing styles can be mixed and matched depending on your needs.
var my_dict = {
    "String Key": 5,
    4: [1, 2, 3],
    7: "Hello",
    "sub_dict": { "sub_key": "Nested value" },
}
```

```
// This is a valid dictionary.
// To access the string "Nested value" below, use `((Godot.Collections.Dictionary)myDict["sub_dict"])["sub_key"]`.
var myDict = new Godot.Collections.Dictionary {
    { "String Key", 5 },
    { 4, new Godot.Collections.Array { 1, 2, 3 } },
    { 7, "Hello" },
    { "sub_dict", new Godot.Collections.Dictionary { { "sub_key", "Nested value" } } },
};
```

The keys of a dictionary can be iterated with the for keyword:

```
var groceries = { "Orange": 20, "Apple": 2, "Banana": 4 }
for fruit in groceries:
    var amount = groceriesfruit
```

```
var groceries = new Godot.Collections.Dictionary { { "Orange", 20 }, { "Apple", 2 }, { "Banana", 4 } };
foreach (var (fruit, amount) in groceries)
{
    // `fruit` is the key, `amount` is the value.
}
```

To enforce a certain type for keys and values, you can create a *typed dictionary*. Typed dictionaries can only contain keys and values of the given types, or that inherit from the given classes:

```
# Creates a typed dictionary with String keys and int values.
# Attempting to use any other type for keys or values will result in an error.
var typed_dict: Dictionary[String, int] = {
    "some_key": 1,
    "some_other_key": 2,
}

# Creates a typed dictionary with String keys and values of any type.
# Attempting to use any other type for keys will result in an error.
var typed_dict_key_only: Dictionary[String, Variant] = {
    "some_key": 12.34,
    "some_other_key": "string",
}
```

```
// Creates a typed dictionary with String keys and int values.
// Attempting to use any other type for keys or values will result in an error.
var typedDict = new Godot.Collections.Dictionary<String, int> {
    {"some_key", 1},
    {"some_other_key", 2},
};

// Creates a typed dictionary with String keys and values of any type.
// Attempting to use any other type for keys will result in an error.
var typedDictKeyOnly = new Godot.Collections.Dictionary<String, Variant> {
    {"some_key", 12.34},
    {"some_other_key", "string"},
};
```

**Note:** Dictionaries are always passed by reference. To get a copy of a dictionary which can be modified independently of the original dictionary, use duplicate(). **Note:** Erasing elements while iterating over dictionaries is **not** supported and will result in unpredictable behavior.

## Quick Reference

```
[methods]
assign(dictionary: Dictionary) -> void
clear() -> void
duplicate(deep: bool = false) -> Dictionary [const]
duplicate_deep(deep_subresources_mode: int = 1) -> Dictionary [const]
erase(key: Variant) -> bool
find_key(value: Variant) -> Variant [const]
get(key: Variant, default: Variant = null) -> Variant [const]
get_or_add(key: Variant, default: Variant = null) -> Variant
get_typed_key_builtin() -> int [const]
get_typed_key_class_name() -> StringName [const]
get_typed_key_script() -> Variant [const]
get_typed_value_builtin() -> int [const]
get_typed_value_class_name() -> StringName [const]
get_typed_value_script() -> Variant [const]
has(key: Variant) -> bool [const]
has_all(keys: Array) -> bool [const]
hash() -> int [const]
is_empty() -> bool [const]
is_read_only() -> bool [const]
is_same_typed(dictionary: Dictionary) -> bool [const]
is_same_typed_key(dictionary: Dictionary) -> bool [const]
is_same_typed_value(dictionary: Dictionary) -> bool [const]
is_typed() -> bool [const]
is_typed_key() -> bool [const]
is_typed_value() -> bool [const]
keys() -> Array [const]
make_read_only() -> void
merge(dictionary: Dictionary, overwrite: bool = false) -> void
merged(dictionary: Dictionary, overwrite: bool = false) -> Dictionary [const]
recursive_equal(dictionary: Dictionary, recursion_count: int) -> bool [const]
set(key: Variant, value: Variant) -> bool
size() -> int [const]
sort() -> void
values() -> Array [const]
```

## Tutorials

- [GDScript basics: Dictionary]($DOCS_URL/tutorials/scripting/gdscript/gdscript_basics.html#dictionary)
- [3D Voxel Demo](https://godotengine.org/asset-library/asset/2755)
- [Operating System Testing Demo](https://godotengine.org/asset-library/asset/2789)

## Constructors

- Dictionary() -> Dictionary
  Constructs an empty Dictionary.

- Dictionary(base: Dictionary, key_type: int, key_class_name: StringName, key_script: Variant, value_type: int, value_class_name: StringName, value_script: Variant) -> Dictionary
  Creates a typed dictionary from the base dictionary. A typed dictionary can only contain keys and values of the given types, or that inherit from the given classes, as described by this constructor's parameters.

- Dictionary(from: Dictionary) -> Dictionary
  Returns the same dictionary as from. If you need a copy of the dictionary, use duplicate().

## Methods

- assign(dictionary: Dictionary) -> void
  Assigns elements of another dictionary into the dictionary. Resizes the dictionary to match dictionary. Performs type conversions if the dictionary is typed.

- clear() -> void
  Clears the dictionary, removing all entries from it.

- duplicate(deep: bool = false) -> Dictionary [const]
  Returns a new copy of the dictionary. By default, a **shallow** copy is returned: all nested Array, Dictionary, and Resource keys and values are shared with the original dictionary. Modifying any of those in one dictionary will also affect them in the other. If deep is true, a **deep** copy is returned: all nested arrays and dictionaries are also duplicated (recursively). Any Resource is still shared with the original dictionary, though.

- duplicate_deep(deep_subresources_mode: int = 1) -> Dictionary [const]
  Duplicates this dictionary, deeply, like duplicate() when passing true, with extra control over how subresources are handled. deep_subresources_mode must be one of the values from Resource.DeepDuplicateMode. By default, only internal resources will be duplicated (recursively).

- erase(key: Variant) -> bool
  Removes the dictionary entry by key, if it exists. Returns true if the given key existed in the dictionary, otherwise false. **Note:** Do not erase entries while iterating over the dictionary. You can iterate over the keys() array instead.

- find_key(value: Variant) -> Variant [const]
  Finds and returns the first key whose associated value is equal to value, or null if it is not found. **Note:** null is also a valid key. If inside the dictionary, find_key() may give misleading results.

- get(key: Variant, default: Variant = null) -> Variant [const]
  Returns the corresponding value for the given key in the dictionary. If the key does not exist, returns default, or null if the parameter is omitted.

- get_or_add(key: Variant, default: Variant = null) -> Variant
  Gets a value and ensures the key is set. If the key exists in the dictionary, this behaves like get(). Otherwise, the default value is inserted into the dictionary and returned.

- get_typed_key_builtin() -> int [const]
  Returns the built-in Variant type of the typed dictionary's keys as a Variant.Type constant. If the keys are not typed, returns TYPE_NIL. See also is_typed_key().

- get_typed_key_class_name() -> StringName [const]
  Returns the **built-in** class name of the typed dictionary's keys, if the built-in Variant type is TYPE_OBJECT. Otherwise, returns an empty StringName. See also is_typed_key() and Object.get_class().

- get_typed_key_script() -> Variant [const]
  Returns the Script instance associated with this typed dictionary's keys, or null if it does not exist. See also is_typed_key().

- get_typed_value_builtin() -> int [const]
  Returns the built-in Variant type of the typed dictionary's values as a Variant.Type constant. If the values are not typed, returns TYPE_NIL. See also is_typed_value().

- get_typed_value_class_name() -> StringName [const]
  Returns the **built-in** class name of the typed dictionary's values, if the built-in Variant type is TYPE_OBJECT. Otherwise, returns an empty StringName. See also is_typed_value() and Object.get_class().

- get_typed_value_script() -> Variant [const]
  Returns the Script instance associated with this typed dictionary's values, or null if it does not exist. See also is_typed_value().

- has(key: Variant) -> bool [const]
  Returns true if the dictionary contains an entry with the given key.


```
  var my_dict = {
      "Godot" : 4,
      210 : null,
  }

  print(my_dict.has("Godot")) # Prints true
  print(my_dict.has(210))     # Prints true
  print(my_dict.has(4))       # Prints false

```

```
  var myDict = new Godot.Collections.Dictionary
  {
      { "Godot", 4 },
      { 210, default },
  };

  GD.Print(myDict.ContainsKey("Godot")); // Prints True
  GD.Print(myDict.ContainsKey(210));     // Prints True
  GD.Print(myDict.ContainsKey(4));       // Prints False

```
  In GDScript, this is equivalent to the in operator:


```
  if "Godot" in { "Godot": 4 }:
      print("The key is here!") # Will be printed.

```
  **Note:** This method returns true as long as the key exists, even if its corresponding value is null.

- has_all(keys: Array) -> bool [const]
  Returns true if the dictionary contains all keys in the given keys array.


```
  var data = { "width": 10, "height": 20 }
  data.has_all(["height", "width"]) # Returns true

```

- hash() -> int [const]
  Returns a hashed 32-bit integer value representing the dictionary contents.


```
  var dict1 = { "A": 10, "B": 2 }
  var dict2 = { "A": 10, "B": 2 }

  print(dict1.hash() == dict2.hash()) # Prints true

```

```
  var dict1 = new Godot.Collections.Dictionary { { "A", 10 }, { "B", 2 } };
  var dict2 = new Godot.Collections.Dictionary { { "A", 10 }, { "B", 2 } };

  // Godot.Collections.Dictionary has no Hash() method. Use GD.Hash() instead.
  GD.Print(GD.Hash(dict1) == GD.Hash(dict2)); // Prints True

```
  **Note:** Dictionaries with the same entries but in a different order will not have the same hash. **Note:** Dictionaries with equal hash values are *not* guaranteed to be the same, because of hash collisions. On the contrary, dictionaries with different hash values are guaranteed to be different.

- is_empty() -> bool [const]
  Returns true if the dictionary is empty (its size is 0). See also size().

- is_read_only() -> bool [const]
  Returns true if the dictionary is read-only. See make_read_only(). Dictionaries are automatically read-only if declared with const keyword.

- is_same_typed(dictionary: Dictionary) -> bool [const]
  Returns true if the dictionary is typed the same as dictionary.

- is_same_typed_key(dictionary: Dictionary) -> bool [const]
  Returns true if the dictionary's keys are typed the same as dictionary's keys.

- is_same_typed_value(dictionary: Dictionary) -> bool [const]
  Returns true if the dictionary's values are typed the same as dictionary's values.

- is_typed() -> bool [const]
  Returns true if the dictionary is typed. Typed dictionaries can only store keys/values of their associated type and provide type safety for the [] operator. Methods of typed dictionary still return Variant.

- is_typed_key() -> bool [const]
  Returns true if the dictionary's keys are typed.

- is_typed_value() -> bool [const]
  Returns true if the dictionary's values are typed.

- keys() -> Array [const]
  Returns the list of keys in the dictionary.

- make_read_only() -> void
  Makes the dictionary read-only, i.e. disables modification of the dictionary's contents. Does not apply to nested content, e.g. content of nested dictionaries.

- merge(dictionary: Dictionary, overwrite: bool = false) -> void
  Adds entries from dictionary to this dictionary. By default, duplicate keys are not copied over, unless overwrite is true.


```
  var dict = { "item": "sword", "quantity": 2 }
  var other_dict = { "quantity": 15, "color": "silver" }

  # Overwriting of existing keys is disabled by default.
  dict.merge(other_dict)
  print(dict)  # { "item": "sword", "quantity": 2, "color": "silver" }

  # With overwriting of existing keys enabled.
  dict.merge(other_dict, true)
  print(dict)  # { "item": "sword", "quantity": 15, "color": "silver" }

```

```
  var dict = new Godot.Collections.Dictionary
  {
      ["item"] = "sword",
      ["quantity"] = 2,
  };

  var otherDict = new Godot.Collections.Dictionary
  {
      ["quantity"] = 15,
      ["color"] = "silver",
  };

  // Overwriting of existing keys is disabled by default.
  dict.Merge(otherDict);
  GD.Print(dict); // { "item": "sword", "quantity": 2, "color": "silver" }

  // With overwriting of existing keys enabled.
  dict.Merge(otherDict, true);
  GD.Print(dict); // { "item": "sword", "quantity": 15, "color": "silver" }

```
  **Note:** merge() is *not* recursive. Nested dictionaries are considered as keys that can be overwritten or not depending on the value of overwrite, but they will never be merged together.

- merged(dictionary: Dictionary, overwrite: bool = false) -> Dictionary [const]
  Returns a copy of this dictionary merged with the other dictionary. By default, duplicate keys are not copied over, unless overwrite is true. See also merge(). This method is useful for quickly making dictionaries with default values:


```
  var base = { "fruit": "apple", "vegetable": "potato" }
  var extra = { "fruit": "orange", "dressing": "vinegar" }
  # Prints { "fruit": "orange", "vegetable": "potato", "dressing": "vinegar" }
  print(extra.merged(base))
  # Prints { "fruit": "apple", "vegetable": "potato", "dressing": "vinegar" }
  print(extra.merged(base, true))

```

- recursive_equal(dictionary: Dictionary, recursion_count: int) -> bool [const]
  Returns true if the two dictionaries contain the same keys and values, inner Dictionary and Array keys and values are compared recursively.

- set(key: Variant, value: Variant) -> bool
  Sets the value of the element at the given key to the given value. Returns true if the value is set successfully. Fails and returns false if the dictionary is read-only, or if key and value don't match the dictionary's types. This is the same as using the [] operator (dictkey = value).

- size() -> int [const]
  Returns the number of entries in the dictionary. Empty dictionaries ({ }) always return 0. See also is_empty().

- sort() -> void
  Sorts the dictionary in ascending order, by key. The final order is dependent on the "less than" (<) comparison between keys.


```
  var numbers = { "c": 2, "a": 0, "b": 1 }
  numbers.sort()
  print(numbers) # Prints { "a": 0, "b": 1, "c": 2 }

```
  This method ensures that the dictionary's entries are ordered consistently when keys() or values() are called, or when the dictionary needs to be converted to a string through @GlobalScope.str() or JSON.stringify().

- values() -> Array [const]
  Returns the list of values in this dictionary.

## Operators

- operator !=(right: Dictionary) -> bool
  Returns true if the two dictionaries do not contain the same keys and values.

- operator ==(right: Dictionary) -> bool
  Returns true if the two dictionaries contain the same keys and values. The order of the entries does not matter. **Note:** In C#, by convention, this operator compares by **reference**. If you need to compare by value, iterate over both dictionaries.

- operator [](key: Variant) -> Variant
  Returns the corresponding value for the given key in the dictionary. If the entry does not exist, fails and returns null. For safe access, use get() or has().
