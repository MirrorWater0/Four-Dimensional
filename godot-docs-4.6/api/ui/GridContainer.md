# GridContainer

## Meta

- Name: GridContainer
- Source: GridContainer.xml
- Inherits: Container
- Inheritance Chain: GridContainer -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

A container that arranges its child controls in a grid layout.

## Description

GridContainer arranges its child controls in a grid layout. The number of columns is specified by the columns property, whereas the number of rows depends on how many are needed for the child controls. The number of rows and columns is preserved for every size of the container. **Note:** GridContainer only works with child nodes inheriting from Control. It won't rearrange child nodes inheriting from Node2D.

## Quick Reference

```
[properties]
columns: int = 1
```

## Tutorials

- [Using Containers]($DOCS_URL/tutorials/ui/gui_containers.html)
- [Operating System Testing Demo](https://godotengine.org/asset-library/asset/2789)

## Properties

- columns: int = 1 [set set_columns; get get_columns]
  The number of columns in the GridContainer. If modified, GridContainer reorders its Control-derived children to accommodate the new layout.

## Theme Items

- h_separation: int [constant] = 4
  The horizontal separation of child nodes.

- v_separation: int [constant] = 4
  The vertical separation of child nodes.
