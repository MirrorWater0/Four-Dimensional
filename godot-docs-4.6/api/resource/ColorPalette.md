# ColorPalette

## Meta

- Name: ColorPalette
- Source: ColorPalette.xml
- Inherits: Resource
- Inheritance Chain: ColorPalette -> Resource -> RefCounted -> Object

## Brief Description

A resource class for managing a palette of colors, which can be loaded and saved using ColorPicker.

## Description

The ColorPalette resource is designed to store and manage a collection of colors. This resource is useful in scenarios where a predefined set of colors is required, such as for creating themes, designing user interfaces, or managing game assets. The built-in ColorPicker control can also make use of ColorPalette without additional code.

## Quick Reference

```
[properties]
colors: PackedColorArray = PackedColorArray()
```

## Properties

- colors: PackedColorArray = PackedColorArray() [set set_colors; get get_colors]
  A PackedColorArray containing the colors in the palette.
