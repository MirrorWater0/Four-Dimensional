# JavaObject

## Meta

- Name: JavaObject
- Source: JavaObject.xml
- Inherits: RefCounted
- Inheritance Chain: JavaObject -> RefCounted -> Object

## Brief Description

Represents an object from the Java Native Interface.

## Description

Represents an object from the Java Native Interface. It can be returned from Java methods called on JavaClass or other JavaObjects. See JavaClassWrapper for an example. **Note:** This class only works on Android. On any other platform, this class does nothing. **Note:** This class is not to be confused with JavaScriptObject.

## Quick Reference

```
[methods]
get_java_class() -> JavaClass [const]
has_java_method(method: StringName) -> bool [const]
```

## Methods

- get_java_class() -> JavaClass [const]
  Returns the JavaClass that this object is an instance of.

- has_java_method(method: StringName) -> bool [const]
  Returns true if the given method name exists in the object's Java methods.
