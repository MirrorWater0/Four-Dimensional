# PropertyTweener

## Meta

- Name: PropertyTweener
- Source: PropertyTweener.xml
- Inherits: Tweener
- Inheritance Chain: PropertyTweener -> Tweener -> RefCounted -> Object

## Brief Description

Interpolates an Object's property over time.

## Description

PropertyTweener is used to interpolate a property in an object. See Tween.tween_property() for more usage information. The tweener will finish automatically if the target object is freed. **Note:** Tween.tween_property() is the only correct way to create PropertyTweener. Any PropertyTweener created manually will not function correctly.

## Quick Reference

```
[methods]
as_relative() -> PropertyTweener
from(value: Variant) -> PropertyTweener
from_current() -> PropertyTweener
set_custom_interpolator(interpolator_method: Callable) -> PropertyTweener
set_delay(delay: float) -> PropertyTweener
set_ease(ease: int (Tween.EaseType)) -> PropertyTweener
set_trans(trans: int (Tween.TransitionType)) -> PropertyTweener
```

## Methods

- as_relative() -> PropertyTweener
  When called, the final value will be used as a relative value instead. **Example:** Move the node by 100 pixels to the right.


```
  var tween = get_tree().create_tween()
  tween.tween_property(self, "position", Vector2.RIGHT * 100, 1).as_relative()

```

```
  Tween tween = GetTree().CreateTween();
  tween.TweenProperty(this, "position", Vector2.Right * 100.0f, 1.0f).AsRelative();

```

- from(value: Variant) -> PropertyTweener
  Sets a custom initial value to the PropertyTweener. **Example:** Move the node from position (100, 100) to (200, 100).


```
  var tween = get_tree().create_tween()
  tween.tween_property(self, "position", Vector2(200, 100), 1).from(Vector2(100, 100))

```

```
  Tween tween = GetTree().CreateTween();
  tween.TweenProperty(this, "position", new Vector2(200.0f, 100.0f), 1.0f).From(new Vector2(100.0f, 100.0f));

```

- from_current() -> PropertyTweener
  Makes the PropertyTweener use the current property value (i.e. at the time of creating this PropertyTweener) as a starting point. This is equivalent of using from() with the current value. These two calls will do the same:


```
  tween.tween_property(self, "position", Vector2(200, 100), 1).from(position)
  tween.tween_property(self, "position", Vector2(200, 100), 1).from_current()

```

```
  tween.TweenProperty(this, "position", new Vector2(200.0f, 100.0f), 1.0f).From(Position);
  tween.TweenProperty(this, "position", new Vector2(200.0f, 100.0f), 1.0f).FromCurrent();

```

- set_custom_interpolator(interpolator_method: Callable) -> PropertyTweener
  Allows interpolating the value with a custom easing function. The provided interpolator_method will be called with a value ranging from 0.0 to 1.0 and is expected to return a value within the same range (values outside the range can be used for overshoot). The return value of the method is then used for interpolation between initial and final value. Note that the parameter passed to the method is still subject to the tweener's own easing.


```
  @export var curve: Curve

  func _ready():
      var tween = create_tween()
      # Interpolate the value using a custom curve.
      tween.tween_property(self, "position:x", 300, 1).as_relative().set_custom_interpolator(tween_curve)

  func tween_curve(v):
      return curve.sample_baked(v)

```

```
  Export
  public Curve Curve { get; set; }

  public override void _Ready()
  {
      Tween tween = CreateTween();
      // Interpolate the value using a custom curve.
      Callable tweenCurveCallable = Callable.From<float, float>(TweenCurve);
      tween.TweenProperty(this, "position:x", 300.0f, 1.0f).AsRelative().SetCustomInterpolator(tweenCurveCallable);
  }

  private float TweenCurve(float value)
  {
      return Curve.SampleBaked(value);
  }

```

- set_delay(delay: float) -> PropertyTweener
  Sets the time in seconds after which the PropertyTweener will start interpolating. By default there's no delay.

- set_ease(ease: int (Tween.EaseType)) -> PropertyTweener
  Sets the type of used easing from Tween.EaseType. If not set, the default easing is used from the Tween that contains this Tweener.

- set_trans(trans: int (Tween.TransitionType)) -> PropertyTweener
  Sets the type of used transition from Tween.TransitionType. If not set, the default transition is used from the Tween that contains this Tweener.
