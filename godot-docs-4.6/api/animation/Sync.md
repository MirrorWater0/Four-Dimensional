# AnimationNodeSync

## Meta

- Name: AnimationNodeSync
- Source: AnimationNodeSync.xml
- Inherits: AnimationNode
- Inheritance Chain: AnimationNodeSync -> AnimationNode -> Resource -> RefCounted -> Object

## Brief Description

Base class for AnimationNodes with multiple input ports that must be synchronized.

## Description

An animation node used to combine, mix, or blend two or more animations together while keeping them synchronized within an AnimationTree.

## Quick Reference

```
[properties]
sync: bool = false
```

## Tutorials

- [Using AnimationTree]($DOCS_URL/tutorials/animation/animation_tree.html)

## Properties

- sync: bool = false [set set_use_sync; get is_using_sync]
  If false, the blended animations' frame are stopped when the blend value is 0. If true, forcing the blended animations to advance frame.
