# AnimationNodeAdd2

## Meta

- Name: AnimationNodeAdd2
- Source: AnimationNodeAdd2.xml
- Inherits: AnimationNodeSync
- Inheritance Chain: AnimationNodeAdd2 -> AnimationNodeSync -> AnimationNode -> Resource -> RefCounted -> Object

## Brief Description

Blends two animations additively inside of an AnimationNodeBlendTree.

## Description

A resource to add to an AnimationNodeBlendTree. Blends two animations additively based on the amount value. If the amount is greater than 1.0, the animation connected to "in" port is blended with the amplified animation connected to "add" port. If the amount is less than 0.0, the animation connected to "in" port is blended with the inverted animation connected to "add" port.

## Tutorials

- [Using AnimationTree]($DOCS_URL/tutorials/animation/animation_tree.html)
