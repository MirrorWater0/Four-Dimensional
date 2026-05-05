# SkeletonProfileHumanoid

## Meta

- Name: SkeletonProfileHumanoid
- Source: SkeletonProfileHumanoid.xml
- Inherits: SkeletonProfile
- Inheritance Chain: SkeletonProfileHumanoid -> SkeletonProfile -> Resource -> RefCounted -> Object

## Brief Description

A humanoid SkeletonProfile preset.

## Description

A SkeletonProfile as a preset that is optimized for the human form. This exists for standardization, so all parameters are read-only. A humanoid skeleton profile contains 56 bones divided into 4 groups: "Body", "Face", "LeftHand", and "RightHand". It is structured as follows: [codeblock lang=text] Root └─ Hips ├─ LeftUpperLeg │  └─ LeftLowerLeg │     └─ LeftFoot │        └─ LeftToes ├─ RightUpperLeg │  └─ RightLowerLeg │     └─ RightFoot │        └─ RightToes └─ Spine └─ Chest └─ UpperChest ├─ Neck │   └─ Head │       ├─ Jaw │       ├─ LeftEye │       └─ RightEye ├─ LeftShoulder │  └─ LeftUpperArm │     └─ LeftLowerArm │        └─ LeftHand │           ├─ LeftThumbMetacarpal │           │  └─ LeftThumbProximal │           │    └─ LeftThumbDistal │           ├─ LeftIndexProximal │           │  └─ LeftIndexIntermediate │           │    └─ LeftIndexDistal │           ├─ LeftMiddleProximal │           │  └─ LeftMiddleIntermediate │           │    └─ LeftMiddleDistal │           ├─ LeftRingProximal │           │  └─ LeftRingIntermediate │           │    └─ LeftRingDistal │           └─ LeftLittleProximal │              └─ LeftLittleIntermediate │                └─ LeftLittleDistal └─ RightShoulder └─ RightUpperArm └─ RightLowerArm └─ RightHand ├─ RightThumbMetacarpal │  └─ RightThumbProximal │     └─ RightThumbDistal ├─ RightIndexProximal │  └─ RightIndexIntermediate │     └─ RightIndexDistal ├─ RightMiddleProximal │  └─ RightMiddleIntermediate │     └─ RightMiddleDistal ├─ RightRingProximal │  └─ RightRingIntermediate │     └─ RightRingDistal └─ RightLittleProximal └─ RightLittleIntermediate └─ RightLittleDistal

```

## Quick Reference

```
[properties]
bone_size: int = 56
group_size: int = 4
root_bone: StringName = &"Root"
scale_base_bone: StringName = &"Hips"
```

## Tutorials

- [Retargeting 3D Skeletons]($DOCS_URL/tutorials/assets_pipeline/retargeting_3d_skeletons.html)

## Properties

- bone_size: int = 56 [set set_bone_size; get get_bone_size; override SkeletonProfile]

- group_size: int = 4 [set set_group_size; get get_group_size; override SkeletonProfile]

- root_bone: StringName = &"Root" [set set_root_bone; get get_root_bone; override SkeletonProfile]

- scale_base_bone: StringName = &"Hips" [set set_scale_base_bone; get get_scale_base_bone; override SkeletonProfile]
