# FoldableGroup

## Meta

- Name: FoldableGroup
- Source: FoldableGroup.xml
- Inherits: Resource
- Inheritance Chain: FoldableGroup -> Resource -> RefCounted -> Object

## Brief Description

A group of foldable containers that doesn't allow more than one container to be expanded at a time.

## Description

A group of FoldableContainer-derived nodes. Only one container can be expanded at a time.

## Quick Reference

```
[methods]
get_containers() -> FoldableContainer[] [const]
get_expanded_container() -> FoldableContainer [const]

[properties]
allow_folding_all: bool = false
resource_local_to_scene: bool = true
```

## Methods

- get_containers() -> FoldableContainer[] [const]
  Returns an Array of FoldableContainers that have this as their FoldableGroup (see FoldableContainer.foldable_group). This is equivalent to ButtonGroup but for FoldableContainers.

- get_expanded_container() -> FoldableContainer [const]
  Returns the current expanded container.

## Properties

- allow_folding_all: bool = false [set set_allow_folding_all; get is_allow_folding_all]
  If true, it is possible to fold all containers in this FoldableGroup.

- resource_local_to_scene: bool = true [set set_local_to_scene; get is_local_to_scene; override Resource]

## Signals

- expanded(container: FoldableContainer)
  Emitted when one of the containers of the group is expanded.
