# AnimationNodeBlend2

## Meta

- Name: AnimationNodeBlend2
- Source: AnimationNodeBlend2.xml
- Inherits: AnimationNodeSync
- Inheritance Chain: AnimationNodeBlend2 -> AnimationNodeSync -> AnimationNode -> Resource -> RefCounted -> Object

## Brief Description

Blends two animations linearly inside of an AnimationNodeBlendTree.

## Description

A resource to add to an AnimationNodeBlendTree. Blends two animations linearly based on the amount value. In general, the blend value should be in the [0.0, 1.0] range. Values outside of this range can blend amplified or inverted animations, however, AnimationNodeAdd2 works better for this purpose.

## Tutorials

- [Using AnimationTree]($DOCS_URL/tutorials/animation/animation_tree.html)
- [3D Platformer Demo](https://godotengine.org/asset-library/asset/2748)
- [Third Person Shooter (TPS) Demo](https://godotengine.org/asset-library/asset/2710)
