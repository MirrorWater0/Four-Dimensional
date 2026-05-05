# Compositor

## Meta

- Name: Compositor
- Source: Compositor.xml
- Inherits: Resource
- Inheritance Chain: Compositor -> Resource -> RefCounted -> Object

## Brief Description

Stores attributes used to customize how a Viewport is rendered.

## Description

The compositor resource stores attributes used to customize how a Viewport is rendered.

## Quick Reference

```
[properties]
compositor_effects: CompositorEffect[] = []
```

## Tutorials

- [The Compositor]($DOCS_URL/tutorials/rendering/compositor.html)

## Properties

- compositor_effects: CompositorEffect[] = [] [set set_compositor_effects; get get_compositor_effects]
  The custom CompositorEffects that are applied during rendering of viewports using this compositor.
