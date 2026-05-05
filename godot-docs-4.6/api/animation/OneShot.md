# AnimationNodeOneShot

## Meta

- Name: AnimationNodeOneShot
- Source: AnimationNodeOneShot.xml
- Inherits: AnimationNodeSync
- Inheritance Chain: AnimationNodeOneShot -> AnimationNodeSync -> AnimationNode -> Resource -> RefCounted -> Object

## Brief Description

Plays an animation once in an AnimationNodeBlendTree.

## Description

A resource to add to an AnimationNodeBlendTree. This animation node will execute a sub-animation and return once it finishes. Blend times for fading in and out can be customized, as well as filters. After setting the request and changing the animation playback, the one-shot node automatically clears the request on the next process frame by setting its request value to ONE_SHOT_REQUEST_NONE.

```
# Play child animation connected to "shot" port.
animation_tree.set("parameters/OneShot/request", AnimationNodeOneShot.ONE_SHOT_REQUEST_FIRE)
# Alternative syntax (same result as above).
animation_tree["parameters/OneShot/request"] = AnimationNodeOneShot.ONE_SHOT_REQUEST_FIRE

# Abort child animation connected to "shot" port.
animation_tree.set("parameters/OneShot/request", AnimationNodeOneShot.ONE_SHOT_REQUEST_ABORT)
# Alternative syntax (same result as above).
animation_tree["parameters/OneShot/request"] = AnimationNodeOneShot.ONE_SHOT_REQUEST_ABORT

# Abort child animation with fading out connected to "shot" port.
animation_tree.set("parameters/OneShot/request", AnimationNodeOneShot.ONE_SHOT_REQUEST_FADE_OUT)
# Alternative syntax (same result as above).
animation_tree["parameters/OneShot/request"] = AnimationNodeOneShot.ONE_SHOT_REQUEST_FADE_OUT

# Get current state (read-only).
animation_tree.get("parameters/OneShot/active")
# Alternative syntax (same result as above).
animation_tree["parameters/OneShot/active"]

# Get current internal state (read-only).
animation_tree.get("parameters/OneShot/internal_active")
# Alternative syntax (same result as above).
animation_tree["parameters/OneShot/internal_active"]
```

```
// Play child animation connected to "shot" port.
animationTree.Set("parameters/OneShot/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);

// Abort child animation connected to "shot" port.
animationTree.Set("parameters/OneShot/request", (int)AnimationNodeOneShot.OneShotRequest.Abort);

// Abort child animation with fading out connected to "shot" port.
animationTree.Set("parameters/OneShot/request", (int)AnimationNodeOneShot.OneShotRequest.FadeOut);

// Get current state (read-only).
animationTree.Get("parameters/OneShot/active");

// Get current internal state (read-only).
animationTree.Get("parameters/OneShot/internal_active");
```

## Quick Reference

```
[properties]
abort_on_reset: bool = false
autorestart: bool = false
autorestart_delay: float = 1.0
autorestart_random_delay: float = 0.0
break_loop_at_end: bool = false
fadein_curve: Curve
fadein_time: float = 0.0
fadeout_curve: Curve
fadeout_time: float = 0.0
mix_mode: int (AnimationNodeOneShot.MixMode) = 0
```

## Tutorials

- [Using AnimationTree]($DOCS_URL/tutorials/animation/animation_tree.html)
- [Third Person Shooter (TPS) Demo](https://godotengine.org/asset-library/asset/2710)

## Properties

- abort_on_reset: bool = false [set set_abort_on_reset; get is_aborted_on_reset]
  If true, the sub-animation will abort if resumed with a reset after a prior interruption.

- autorestart: bool = false [set set_autorestart; get has_autorestart]
  If true, the sub-animation will restart automatically after finishing. In other words, to start auto restarting, the animation must be played once with the ONE_SHOT_REQUEST_FIRE request. The ONE_SHOT_REQUEST_ABORT request stops the auto restarting, but it does not disable the autorestart itself. So, the ONE_SHOT_REQUEST_FIRE request will start auto restarting again.

- autorestart_delay: float = 1.0 [set set_autorestart_delay; get get_autorestart_delay]
  The delay after which the automatic restart is triggered, in seconds.

- autorestart_random_delay: float = 0.0 [set set_autorestart_random_delay; get get_autorestart_random_delay]
  If autorestart is true, a random additional delay (in seconds) between 0 and this value will be added to autorestart_delay.

- break_loop_at_end: bool = false [set set_break_loop_at_end; get is_loop_broken_at_end]
  If true, breaks the loop at the end of the loop cycle for transition, even if the animation is looping.

- fadein_curve: Curve [set set_fadein_curve; get get_fadein_curve]
  Determines how cross-fading between animations is eased. If empty, the transition will be linear. Should be a unit Curve.

- fadein_time: float = 0.0 [set set_fadein_time; get get_fadein_time]
  The fade-in duration. For example, setting this to 1.0 for a 5 second length animation will produce a cross-fade that starts at 0 second and ends at 1 second during the animation. **Note:** AnimationNodeOneShot transitions the current state after the fading has finished.

- fadeout_curve: Curve [set set_fadeout_curve; get get_fadeout_curve]
  Determines how cross-fading between animations is eased. If empty, the transition will be linear. Should be a unit Curve.

- fadeout_time: float = 0.0 [set set_fadeout_time; get get_fadeout_time]
  The fade-out duration. For example, setting this to 1.0 for a 5 second length animation will produce a cross-fade that starts at 4 second and ends at 5 second during the animation. **Note:** AnimationNodeOneShot transitions the current state after the fading has finished.

- mix_mode: int (AnimationNodeOneShot.MixMode) = 0 [set set_mix_mode; get get_mix_mode]
  The blend type.

## Constants

### Enum OneShotRequest

- ONE_SHOT_REQUEST_NONE = 0
  The default state of the request. Nothing is done.

- ONE_SHOT_REQUEST_FIRE = 1
  The request to play the animation connected to "shot" port.

- ONE_SHOT_REQUEST_ABORT = 2
  The request to stop the animation connected to "shot" port.

- ONE_SHOT_REQUEST_FADE_OUT = 3
  The request to fade out the animation connected to "shot" port.

### Enum MixMode

- MIX_MODE_BLEND = 0
  Blends two animations. See also AnimationNodeBlend2.

- MIX_MODE_ADD = 1
  Blends two animations additively. See also AnimationNodeAdd2.
