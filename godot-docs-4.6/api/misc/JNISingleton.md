# JNISingleton

## Meta

- Name: JNISingleton
- Source: JNISingleton.xml
- Inherits: Object
- Inheritance Chain: JNISingleton -> Object

## Brief Description

Singleton that connects the engine with Android plugins to interface with native Android code.

## Description

The JNISingleton is implemented only in the Android export. It's used to call methods and connect signals from an Android plugin written in Java or Kotlin. Methods and signals can be called and connected to the JNISingleton as if it is a Node. See [Java Native Interface - Wikipedia](https://en.wikipedia.org/wiki/Java_Native_Interface) for more information.

## Quick Reference

```
[methods]
has_java_method(method: StringName) -> bool [const]
```

## Tutorials

- [Creating Android plugins]($DOCS_URL/tutorials/platform/android/android_plugin.html#doc-android-plugin)

## Methods

- has_java_method(method: StringName) -> bool [const]
  Returns true if the given method name exists in the JNISingleton's Java methods.
