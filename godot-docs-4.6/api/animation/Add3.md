# AnimationNodeAdd3

## Meta

- Name: AnimationNodeAdd3
- Source: AnimationNodeAdd3.xml
- Inherits: AnimationNodeSync
- Inheritance Chain: AnimationNodeAdd3 -> AnimationNodeSync -> AnimationNode -> Resource -> RefCounted -> Object

## Brief Description

Blends two of three animations additively inside of an AnimationNodeBlendTree.

## Description

A resource to add to an AnimationNodeBlendTree. Blends two animations out of three additively out of three based on the amount value. This animation node has three inputs: - The base animation to add to - A "-add" animation to blend with when the blend amount is negative - A "+add" animation to blend with when the blend amount is positive If the absolute value of the amount is greater than 1.0, the animation connected to "in" port is blended with the amplified animation connected to "-add"/"+add" port.

## Tutorials

- [Using AnimationTree]($DOCS_URL/tutorials/animation/animation_tree.html)
- [Third Person Shooter (TPS) Demo](https://godotengine.org/asset-library/asset/2710)
