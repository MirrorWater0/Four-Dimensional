# PopupPanel

## Meta

- Name: PopupPanel
- Source: PopupPanel.xml
- Inherits: Popup
- Inheritance Chain: PopupPanel -> Popup -> Window -> Viewport -> Node -> Object

## Brief Description

A popup with a panel background.

## Description

A popup with a configurable panel background. Any child controls added to this node will be stretched to fit the panel's size (similar to how PanelContainer works). If you are making windows, see Window.

## Quick Reference

```
[properties]
transparent: bool = true
transparent_bg: bool = true
```

## Properties

- transparent: bool = true [set set_flag; get get_flag; override Window]

- transparent_bg: bool = true [set set_transparent_background; get has_transparent_background; override Viewport]

## Theme Items

- panel: StyleBox [style]
  StyleBox for the background panel.
