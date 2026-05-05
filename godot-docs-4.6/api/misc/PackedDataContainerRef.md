# PackedDataContainerRef

## Meta

- Name: PackedDataContainerRef
- Source: PackedDataContainerRef.xml
- Inherits: RefCounted
- Inheritance Chain: PackedDataContainerRef -> RefCounted -> Object

## Brief Description

An internal class used by PackedDataContainer to pack nested arrays and dictionaries.

## Description

When packing nested containers using PackedDataContainer, they are recursively packed into PackedDataContainerRef (only applies to Array and Dictionary). Their data can be retrieved the same way as from PackedDataContainer.

```
var packed = PackedDataContainer.new()
packed.pack([1, 2, 3, ["nested1", "nested2"], 4, 5, 6])

for element in packed:
    if element is PackedDataContainerRef:
        for subelement in element:
            print("::", subelement)
    else:
        print(element)
```

Prints: [codeblock lang=text] 1 2 3 ::nested1 ::nested2 4 5 6

```

## Quick Reference

```
[methods]
size() -> int [const]
```

## Methods

- size() -> int [const]
  Returns the size of the packed container (see Array.size() and Dictionary.size()).
