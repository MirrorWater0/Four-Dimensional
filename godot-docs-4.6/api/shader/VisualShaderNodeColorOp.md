# VisualShaderNodeColorOp

## Meta

- Name: VisualShaderNodeColorOp
- Source: VisualShaderNodeColorOp.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeColorOp -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A Color operator to be used within the visual shader graph.

## Description

Applies operator to two color inputs.

## Quick Reference

```
[properties]
operator: int (VisualShaderNodeColorOp.Operator) = 0
```

## Properties

- operator: int (VisualShaderNodeColorOp.Operator) = 0 [set set_operator; get get_operator]
  An operator to be applied to the inputs.

## Constants

### Enum Operator

- OP_SCREEN = 0
  Produce a screen effect with the following formula:

```
result = vec3(1.0) - (vec3(1.0) - a) * (vec3(1.0) - b);
```

- OP_DIFFERENCE = 1
  Produce a difference effect with the following formula:

```
result = abs(a - b);
```

- OP_DARKEN = 2
  Produce a darken effect with the following formula:

```
result = min(a, b);
```

- OP_LIGHTEN = 3
  Produce a lighten effect with the following formula:

```
result = max(a, b);
```

- OP_OVERLAY = 4
  Produce an overlay effect with the following formula:

```
for (int i = 0; i < 3; i++) {
    float base = ai;
    float blend = bi;
    if (base < 0.5) {
        resulti = 2.0 * base * blend;
    } else {
        resulti = 1.0 - 2.0 * (1.0 - blend) * (1.0 - base);
    }
}
```

- OP_DODGE = 5
  Produce a dodge effect with the following formula:

```
result = a / (vec3(1.0) - b);
```

- OP_BURN = 6
  Produce a burn effect with the following formula:

```
result = vec3(1.0) - (vec3(1.0) - a) / b;
```

- OP_SOFT_LIGHT = 7
  Produce a soft light effect with the following formula:

```
for (int i = 0; i < 3; i++) {
    float base = ai;
    float blend = bi;
    if (base < 0.5) {
        resulti = base * (blend + 0.5);
    } else {
        resulti = 1.0 - (1.0 - base) * (1.0 - (blend - 0.5));
    }
}
```

- OP_HARD_LIGHT = 8
  Produce a hard light effect with the following formula:

```
for (int i = 0; i < 3; i++) {
    float base = ai;
    float blend = bi;
    if (base < 0.5) {
        resulti = base * (2.0 * blend);
    } else {
        resulti = 1.0 - (1.0 - base) * (1.0 - 2.0 * (blend - 0.5));
    }
}
```

- OP_MAX = 9
  Represents the size of the Operator enum.
