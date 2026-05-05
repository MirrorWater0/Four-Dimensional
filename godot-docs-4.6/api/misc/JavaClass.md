# JavaClass

## Meta

- Name: JavaClass
- Source: JavaClass.xml
- Inherits: RefCounted
- Inheritance Chain: JavaClass -> RefCounted -> Object

## Brief Description

Represents a class from the Java Native Interface.

## Description

Represents a class from the Java Native Interface. It is returned from JavaClassWrapper.wrap(). **Note:** This class only works on Android. On any other platform, this class does nothing. **Note:** This class is not to be confused with JavaScriptObject.

## Quick Reference

```
[methods]
get_java_class_name() -> String [const]
get_java_method_list() -> Dictionary[] [const]
get_java_parent_class() -> JavaClass [const]
has_java_method(method: StringName) -> bool [const]
```

## Methods

- get_java_class_name() -> String [const]
  Returns the Java class name.

- get_java_method_list() -> Dictionary[] [const]
  Returns the object's Java methods and their signatures as an Array of dictionaries, in the same format as Object.get_method_list().

- get_java_parent_class() -> JavaClass [const]
  Returns a JavaClass representing the Java parent class of this class.

- has_java_method(method: StringName) -> bool [const]
  Returns true if the given method name exists in the object's Java methods.
