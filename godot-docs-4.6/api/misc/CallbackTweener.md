# CallbackTweener

## Meta

- Name: CallbackTweener
- Source: CallbackTweener.xml
- Inherits: Tweener
- Inheritance Chain: CallbackTweener -> Tweener -> RefCounted -> Object

## Brief Description

Calls the specified method after optional delay.

## Description

CallbackTweener is used to call a method in a tweening sequence. See Tween.tween_callback() for more usage information. The tweener will finish automatically if the callback's target object is freed. **Note:** Tween.tween_callback() is the only correct way to create CallbackTweener. Any CallbackTweener created manually will not function correctly.

## Quick Reference

```
[methods]
set_delay(delay: float) -> CallbackTweener
```

## Methods

- set_delay(delay: float) -> CallbackTweener
  Makes the callback call delayed by given time in seconds. **Example:** Call Node.queue_free() after 2 seconds:


```
  var tween = get_tree().create_tween()
  tween.tween_callback(queue_free).set_delay(2)

```
