# SkeletonModificationStack2D

## Meta

- Name: SkeletonModificationStack2D
- Source: SkeletonModificationStack2D.xml
- Inherits: Resource
- Inheritance Chain: SkeletonModificationStack2D -> Resource -> RefCounted -> Object

## Brief Description

A resource that holds a stack of SkeletonModification2Ds.

## Description

This resource is used by the Skeleton and holds a stack of SkeletonModification2Ds. This controls the order of the modifications and how they are applied. Modification order is especially important for full-body IK setups, as you need to execute the modifications in the correct order to get the desired results. For example, you want to execute a modification on the spine *before* the arms on a humanoid skeleton. This resource also controls how strongly all of the modifications are applied to the Skeleton2D.

## Quick Reference

```
[methods]
add_modification(modification: SkeletonModification2D) -> void
delete_modification(mod_idx: int) -> void
enable_all_modifications(enabled: bool) -> void
execute(delta: float, execution_mode: int) -> void
get_is_setup() -> bool [const]
get_modification(mod_idx: int) -> SkeletonModification2D [const]
get_skeleton() -> Skeleton2D [const]
set_modification(mod_idx: int, modification: SkeletonModification2D) -> void
setup() -> void

[properties]
enabled: bool = false
modification_count: int = 0
strength: float = 1.0
```

## Methods

- add_modification(modification: SkeletonModification2D) -> void
  Adds the passed-in SkeletonModification2D to the stack.

- delete_modification(mod_idx: int) -> void
  Deletes the SkeletonModification2D at the index position mod_idx, if it exists.

- enable_all_modifications(enabled: bool) -> void
  Enables all SkeletonModification2Ds in the stack.

- execute(delta: float, execution_mode: int) -> void
  Executes all of the SkeletonModification2Ds in the stack that use the same execution mode as the passed-in execution_mode, starting from index 0 to modification_count. **Note:** The order of the modifications can matter depending on the modifications. For example, modifications on a spine should operate before modifications on the arms in order to get proper results.

- get_is_setup() -> bool [const]
  Returns a boolean that indicates whether the modification stack is setup and can execute.

- get_modification(mod_idx: int) -> SkeletonModification2D [const]
  Returns the SkeletonModification2D at the passed-in index, mod_idx.

- get_skeleton() -> Skeleton2D [const]
  Returns the Skeleton2D node that the SkeletonModificationStack2D is bound to.

- set_modification(mod_idx: int, modification: SkeletonModification2D) -> void
  Sets the modification at mod_idx to the passed-in modification, modification.

- setup() -> void
  Sets up the modification stack so it can execute. This function should be called by Skeleton2D and shouldn't be manually called unless you know what you are doing.

## Properties

- enabled: bool = false [set set_enabled; get get_enabled]
  If true, the modification's in the stack will be called. This is handled automatically through the Skeleton2D node.

- modification_count: int = 0 [set set_modification_count; get get_modification_count]
  The number of modifications in the stack.

- strength: float = 1.0 [set set_strength; get get_strength]
  The interpolation strength of the modifications in stack. A value of 0 will make it where the modifications are not applied, a strength of 0.5 will be half applied, and a strength of 1 will allow the modifications to be fully applied and override the Skeleton2D Bone2D poses.
