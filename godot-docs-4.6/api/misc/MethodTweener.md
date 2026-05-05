# MethodTweener

## Meta

- Name: MethodTweener
- Source: MethodTweener.xml
- Inherits: Tweener
- Inheritance Chain: MethodTweener -> Tweener -> RefCounted -> Object

## Brief Description

Interpolates an abstract value and supplies it to a method called over time.

## Description

MethodTweener is similar to a combination of CallbackTweener and PropertyTweener. It calls a method providing an interpolated value as a parameter. See Tween.tween_method() for more usage information. The tweener will finish automatically if the callback's target object is freed. **Note:** Tween.tween_method() is the only correct way to create MethodTweener. Any MethodTweener created manually will not function correctly.

## Quick Reference

```
[methods]
set_delay(delay: float) -> MethodTweener
set_ease(ease: int (Tween.EaseType)) -> MethodTweener
set_trans(trans: int (Tween.TransitionType)) -> MethodTweener
```

## Methods

- set_delay(delay: float) -> MethodTweener
  Sets the time in seconds after which the MethodTweener will start interpolating. By default there's no delay.

- set_ease(ease: int (Tween.EaseType)) -> MethodTweener
  Sets the type of used easing from Tween.EaseType. If not set, the default easing is used from the Tween that contains this Tweener.

- set_trans(trans: int (Tween.TransitionType)) -> MethodTweener
  Sets the type of used transition from Tween.TransitionType. If not set, the default transition is used from the Tween that contains this Tweener.
