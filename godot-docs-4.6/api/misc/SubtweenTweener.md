# SubtweenTweener

## Meta

- Name: SubtweenTweener
- Source: SubtweenTweener.xml
- Inherits: Tweener
- Inheritance Chain: SubtweenTweener -> Tweener -> RefCounted -> Object

## Brief Description

Runs a Tween nested within another Tween.

## Description

SubtweenTweener is used to execute a Tween as one step in a sequence defined by another Tween. See Tween.tween_subtween() for more usage information. **Note:** Tween.tween_subtween() is the only correct way to create SubtweenTweener. Any SubtweenTweener created manually will not function correctly.

## Quick Reference

```
[methods]
set_delay(delay: float) -> SubtweenTweener
```

## Methods

- set_delay(delay: float) -> SubtweenTweener
  Sets the time in seconds after which the SubtweenTweener will start running the subtween. By default there's no delay.
