# Separator

## Meta

- Name: Separator
- Source: Separator.xml
- Inherits: Control
- Inheritance Chain: Separator -> Control -> CanvasItem -> Node -> Object

## Brief Description

Abstract base class for separators.

## Description

Abstract base class for separators, used for separating other controls. Separators are purely visual and normally drawn as a StyleBoxLine.

## Theme Items

- separation: int [constant] = 0
  The size of the area covered by the separator. Effectively works like a minimum width/height.

- separator: StyleBox [style]
  The style for the separator line. Works best with StyleBoxLine (remember to enable StyleBoxLine.vertical for VSeparator).
