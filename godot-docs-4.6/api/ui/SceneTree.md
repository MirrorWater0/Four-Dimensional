# SceneTree

## Meta

- Name: SceneTree
- Source: SceneTree.xml
- Inherits: MainLoop
- Inheritance Chain: SceneTree -> MainLoop -> Object

## Brief Description

Manages the game loop via a hierarchy of nodes.

## Description

As one of the most important classes, the SceneTree manages the hierarchy of nodes in a scene, as well as scenes themselves. Nodes can be added, fetched and removed. The whole scene tree (and thus the current scene) can be paused. Scenes can be loaded, switched and reloaded. You can also use the SceneTree to organize your nodes into **groups**: every node can be added to as many groups as you want to create, e.g. an "enemy" group. You can then iterate these groups or even call methods and set properties on all the nodes belonging to any given group. SceneTree is the default MainLoop implementation used by the engine, and is thus in charge of the game loop.

## Quick Reference

```
[methods]
call_group(group: StringName, method: StringName) -> void [vararg]
call_group_flags(flags: int, group: StringName, method: StringName) -> void [vararg]
change_scene_to_file(path: String) -> int (Error)
change_scene_to_node(node: Node) -> int (Error)
change_scene_to_packed(packed_scene: PackedScene) -> int (Error)
create_timer(time_sec: float, process_always: bool = true, process_in_physics: bool = false, ignore_time_scale: bool = false) -> SceneTreeTimer
create_tween() -> Tween
get_first_node_in_group(group: StringName) -> Node
get_frame() -> int [const]
get_multiplayer(for_path: NodePath = NodePath("")) -> MultiplayerAPI [const]
get_node_count() -> int [const]
get_node_count_in_group(group: StringName) -> int [const]
get_nodes_in_group(group: StringName) -> Node[]
get_processed_tweens() -> Tween[]
has_group(name: StringName) -> bool [const]
is_accessibility_enabled() -> bool [const]
is_accessibility_supported() -> bool [const]
notify_group(group: StringName, notification: int) -> void
notify_group_flags(call_flags: int, group: StringName, notification: int) -> void
queue_delete(obj: Object) -> void
quit(exit_code: int = 0) -> void
reload_current_scene() -> int (Error)
set_group(group: StringName, property: String, value: Variant) -> void
set_group_flags(call_flags: int, group: StringName, property: String, value: Variant) -> void
set_multiplayer(multiplayer: MultiplayerAPI, root_path: NodePath = NodePath("")) -> void
unload_current_scene() -> void

[properties]
auto_accept_quit: bool = true
current_scene: Node
debug_collisions_hint: bool = false
debug_navigation_hint: bool = false
debug_paths_hint: bool = false
edited_scene_root: Node
multiplayer_poll: bool = true
paused: bool = false
physics_interpolation: bool = false
quit_on_go_back: bool = true
root: Window
```

## Tutorials

- [SceneTree]($DOCS_URL/tutorials/scripting/scene_tree.html)
- [Multiple resolutions]($DOCS_URL/tutorials/rendering/multiple_resolutions.html)

## Methods

- call_group(group: StringName, method: StringName) -> void [vararg]
  Calls method on each node inside this tree added to the given group. You can pass arguments to method by specifying them at the end of this method call. Nodes that cannot call method (either because the method doesn't exist or the arguments do not match) are ignored. See also set_group() and notify_group(). **Note:** This method acts immediately on all selected nodes at once, which may cause stuttering in some performance-intensive situations. **Note:** In C#, method must be in snake_case when referring to built-in Godot methods. Prefer using the names exposed in the MethodName class to avoid allocating a new StringName on each call.

- call_group_flags(flags: int, group: StringName, method: StringName) -> void [vararg]
  Calls the given method on each node inside this tree added to the given group. Use flags to customize this method's behavior (see GroupCallFlags). Additional arguments for method can be passed at the end of this method. Nodes that cannot call method (either because the method doesn't exist or the arguments do not match) are ignored.


```
  # Calls "hide" to all nodes of the "enemies" group, at the end of the frame and in reverse tree order.
  get_tree().call_group_flags(
          SceneTree.GROUP_CALL_DEFERRED | SceneTree.GROUP_CALL_REVERSE,
          "enemies", "hide")

```
  **Note:** In C#, method must be in snake_case when referring to built-in Godot methods. Prefer using the names exposed in the MethodName class to avoid allocating a new StringName on each call.

- change_scene_to_file(path: String) -> int (Error)
  Changes the running scene to the one at the given path, after loading it into a PackedScene and creating a new instance. Returns OK on success, ERR_CANT_OPEN if the path cannot be loaded into a PackedScene, or ERR_CANT_CREATE if that scene cannot be instantiated. **Note:** See change_scene_to_node() for details on the order of operations.

- change_scene_to_node(node: Node) -> int (Error)
  Changes the running scene to the provided Node. Useful when you want to set up the new scene before changing. Returns OK on success, ERR_INVALID_PARAMETER if the node is null, or ERR_UNCONFIGURED if the node is already inside the scene tree. **Note:** Operations happen in the following order when change_scene_to_node() is called: 1. The current scene node is immediately removed from the tree. From that point, Node.get_tree() called on the current (outgoing) scene will return null. current_scene will be null too, because the new scene is not available yet. 2. At the end of the frame, the formerly current scene, already removed from the tree, will be deleted (freed from memory) and then the new scene node will be added to the tree. Node.get_tree() and current_scene will be back to working as usual. This ensures that both scenes aren't running at the same time, while still freeing the previous scene in a safe way similar to Node.queue_free(). If you want to reliably access the new scene, await the scene_changed signal. **Warning:** After using this method, the SceneTree will take ownership of the node and will free it automatically when changing scene again. Any references you had to that node will become invalid.

- change_scene_to_packed(packed_scene: PackedScene) -> int (Error)
  Changes the running scene to a new instance of the given PackedScene (which must be valid). Returns OK on success, ERR_CANT_CREATE if the scene cannot be instantiated, or ERR_INVALID_PARAMETER if the scene is invalid. **Note:** See change_scene_to_node() for details on the order of operations.

- create_timer(time_sec: float, process_always: bool = true, process_in_physics: bool = false, ignore_time_scale: bool = false) -> SceneTreeTimer
  Returns a new SceneTreeTimer. After time_sec in seconds have passed, the timer will emit SceneTreeTimer.timeout and will be automatically freed. If process_always is false, the timer will be paused when setting SceneTree.paused to true. If process_in_physics is true, the timer will update at the end of the physics frame, instead of the process frame. If ignore_time_scale is true, the timer will ignore Engine.time_scale and update with the real, elapsed time. This method is commonly used to create a one-shot delay timer, as in the following example:


```
  func some_function():
      print("start")
      await get_tree().create_timer(1.0).timeout
      print("end")

```

```
  public async Task SomeFunction()
  {
      GD.Print("start");
      await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
      GD.Print("end");
  }

```
  **Note:** The timer is always updated *after* all of the nodes in the tree. A node's Node._process() method would be called before the timer updates (or Node._physics_process() if process_in_physics is set to true).

- create_tween() -> Tween
  Creates and returns a new Tween processed in this tree. The Tween will start automatically on the next process frame or physics frame (depending on its Tween.TweenProcessMode). **Note:** A Tween created using this method is not bound to any Node. It may keep working until there is nothing left to animate. If you want the Tween to be automatically killed when the Node is freed, use Node.create_tween() or Tween.bind_node().

- get_first_node_in_group(group: StringName) -> Node
  Returns the first Node found inside the tree, that has been added to the given group, in scene hierarchy order. Returns null if no match is found. See also get_nodes_in_group().

- get_frame() -> int [const]
  Returns how many physics process steps have been processed, since the application started. This is *not* a measurement of elapsed time. See also physics_frame. For the number of frames rendered, see Engine.get_process_frames().

- get_multiplayer(for_path: NodePath = NodePath("")) -> MultiplayerAPI [const]
  Searches for the MultiplayerAPI configured for the given path, if one does not exist it searches the parent paths until one is found. If the path is empty, or none is found, the default one is returned. See set_multiplayer().

- get_node_count() -> int [const]
  Returns the number of nodes inside this tree.

- get_node_count_in_group(group: StringName) -> int [const]
  Returns the number of nodes assigned to the given group.

- get_nodes_in_group(group: StringName) -> Node[]
  Returns an Array containing all nodes inside this tree, that have been added to the given group, in scene hierarchy order.

- get_processed_tweens() -> Tween[]
  Returns an Array of currently existing Tweens in the tree, including paused tweens.

- has_group(name: StringName) -> bool [const]
  Returns true if a node added to the given group name exists in the tree.

- is_accessibility_enabled() -> bool [const]
  Returns true if accessibility features are enabled, and accessibility information updates are actively processed.

- is_accessibility_supported() -> bool [const]
  Returns true if accessibility features are supported by the OS and enabled in project settings.

- notify_group(group: StringName, notification: int) -> void
  Calls Object.notification() with the given notification to all nodes inside this tree added to the group. See also [Godot notifications]($DOCS_URL/tutorials/best_practices/godot_notifications.html) and call_group() and set_group(). **Note:** This method acts immediately on all selected nodes at once, which may cause stuttering in some performance-intensive situations.

- notify_group_flags(call_flags: int, group: StringName, notification: int) -> void
  Calls Object.notification() with the given notification to all nodes inside this tree added to the group. Use call_flags to customize this method's behavior (see GroupCallFlags).

- queue_delete(obj: Object) -> void
  Queues the given obj to be deleted, calling its Object.free() at the end of the current frame. This method is similar to Node.queue_free().

- quit(exit_code: int = 0) -> void
  Quits the application at the end of the current iteration, with the given exit_code. By convention, an exit code of 0 indicates success, whereas any other exit code indicates an error. For portability reasons, it should be between 0 and 125 (inclusive). **Note:** On iOS this method doesn't work. Instead, as recommended by the [iOS Human Interface Guidelines](https://developer.apple.com/library/archive/qa/qa1561/_index.html), the user is expected to close apps via the Home button.

- reload_current_scene() -> int (Error)
  Reloads the currently active scene, replacing current_scene with a new instance of its original PackedScene. Returns OK on success, ERR_UNCONFIGURED if no current_scene is defined, ERR_CANT_OPEN if current_scene cannot be loaded into a PackedScene, or ERR_CANT_CREATE if the scene cannot be instantiated.

- set_group(group: StringName, property: String, value: Variant) -> void
  Sets the given property to value on all nodes inside this tree added to the given group. Nodes that do not have the property are ignored. See also call_group() and notify_group(). **Note:** This method acts immediately on all selected nodes at once, which may cause stuttering in some performance-intensive situations. **Note:** In C#, property must be in snake_case when referring to built-in Godot properties. Prefer using the names exposed in the PropertyName class to avoid allocating a new StringName on each call.

- set_group_flags(call_flags: int, group: StringName, property: String, value: Variant) -> void
  Sets the given property to value on all nodes inside this tree added to the given group. Nodes that do not have the property are ignored. Use call_flags to customize this method's behavior (see GroupCallFlags). **Note:** In C#, property must be in snake_case when referring to built-in Godot properties. Prefer using the names exposed in the PropertyName class to avoid allocating a new StringName on each call.

- set_multiplayer(multiplayer: MultiplayerAPI, root_path: NodePath = NodePath("")) -> void
  Sets a custom MultiplayerAPI with the given root_path (controlling also the relative subpaths), or override the default one if root_path is empty. **Note:** No MultiplayerAPI must be configured for the subpath containing root_path, nested custom multiplayers are not allowed. I.e. if one is configured for "/root/Foo" setting one for "/root/Foo/Bar" will cause an error. **Note:** set_multiplayer() should be called *before* the child nodes are ready at the given root_path. If multiplayer nodes like MultiplayerSpawner or MultiplayerSynchronizer are added to the tree before the custom multiplayer API is set, they will not work.

- unload_current_scene() -> void
  If a current scene is loaded, calling this method will unload it.

## Properties

- auto_accept_quit: bool = true [set set_auto_accept_quit; get is_auto_accept_quit]
  If true, the application automatically accepts quitting requests. For mobile platforms, see quit_on_go_back.

- current_scene: Node [set set_current_scene; get get_current_scene]
  The root node of the currently loaded main scene, usually as a direct child of root. See also change_scene_to_file(), change_scene_to_packed(), and reload_current_scene(). **Warning:** Setting this property directly may not work as expected, as it does *not* add or remove any nodes from this tree.

- debug_collisions_hint: bool = false [set set_debug_collisions_hint; get is_debugging_collisions_hint]
  If true, collision shapes will be visible when running the game from the editor for debugging purposes. **Note:** This property is not designed to be changed at run-time. Changing the value of debug_collisions_hint while the project is running will not have the desired effect.

- debug_navigation_hint: bool = false [set set_debug_navigation_hint; get is_debugging_navigation_hint]
  If true, navigation polygons will be visible when running the game from the editor for debugging purposes. **Note:** This property is not designed to be changed at run-time. Changing the value of debug_navigation_hint while the project is running will not have the desired effect.

- debug_paths_hint: bool = false [set set_debug_paths_hint; get is_debugging_paths_hint]
  If true, curves from Path2D and Path3D nodes will be visible when running the game from the editor for debugging purposes. **Note:** This property is not designed to be changed at run-time. Changing the value of debug_paths_hint while the project is running will not have the desired effect.

- edited_scene_root: Node [set set_edited_scene_root; get get_edited_scene_root]
  The root of the scene currently being edited in the editor. This is usually a direct child of root. **Note:** This property does nothing in release builds.

- multiplayer_poll: bool = true [set set_multiplayer_poll_enabled; get is_multiplayer_poll_enabled]
  If true (default value), enables automatic polling of the MultiplayerAPI for this SceneTree during process_frame. If false, you need to manually call MultiplayerAPI.poll() to process network packets and deliver RPCs. This allows running RPCs in a different loop (e.g. physics, thread, specific time step) and for manual Mutex protection when accessing the MultiplayerAPI from threads.

- paused: bool = false [set set_pause; get is_paused]
  If true, the scene tree is considered paused. This causes the following behavior: - 2D and 3D physics will be stopped, as well as collision detection and related signals. - Depending on each node's Node.process_mode, their Node._process(), Node._physics_process() and Node._input() callback methods may not called anymore.

- physics_interpolation: bool = false [set set_physics_interpolation_enabled; get is_physics_interpolation_enabled]
  If true, the renderer will interpolate the transforms of objects (both physics and non-physics) between the last two transforms, so that smooth motion is seen even when physics ticks do not coincide with rendered frames. The default value of this property is controlled by ProjectSettings.physics/common/physics_interpolation. **Note:** Although this is a global setting, finer control of individual branches of the SceneTree is possible using Node.physics_interpolation_mode.

- quit_on_go_back: bool = true [set set_quit_on_go_back; get is_quit_on_go_back]
  If true, the application quits automatically when navigating back (e.g. using the system "Back" button on Android). To handle 'Go Back' button when this option is disabled, use DisplayServer.WINDOW_EVENT_GO_BACK_REQUEST.

- root: Window [get get_root]
  The tree's root Window. This is top-most Node of the scene tree, and is always present. An absolute NodePath always starts from this node. Children of the root node may include the loaded current_scene, as well as any AutoLoad($DOCS_URL/tutorials/scripting/singletons_autoload.html) configured in the Project Settings. **Warning:** Do not delete this node. This will result in unstable behavior, followed by a crash.

## Signals

- node_added(node: Node)
  Emitted when the node enters this tree.

- node_configuration_warning_changed(node: Node)
  Emitted when the node's Node.update_configuration_warnings() is called. Only emitted in the editor.

- node_removed(node: Node)
  Emitted when the node exits this tree.

- node_renamed(node: Node)
  Emitted when the node's Node.name is changed.

- physics_frame()
  Emitted immediately before Node._physics_process() is called on every node in this tree.

- process_frame()
  Emitted immediately before Node._process() is called on every node in this tree.

- scene_changed()
  Emitted after the new scene is added to scene tree and initialized. Can be used to reliably access current_scene when changing scenes.

```
# This code should be inside an autoload.
get_tree().change_scene_to_file(other_scene_path)
await get_tree().scene_changed
print(get_tree().current_scene) # Prints the new scene.
```

- tree_changed()
  Emitted any time the tree's hierarchy changes (nodes being moved, renamed, etc.).

- tree_process_mode_changed()
  Emitted when the Node.process_mode of any node inside the tree is changed. Only emitted in the editor, to update the visibility of disabled nodes.

## Constants

### Enum GroupCallFlags

- GROUP_CALL_DEFAULT = 0
  Call nodes within a group with no special behavior (default).

- GROUP_CALL_REVERSE = 1
  Call nodes within a group in reverse tree hierarchy order (all nested children are called before their respective parent nodes).

- GROUP_CALL_DEFERRED = 2
  Call nodes within a group at the end of the current frame (can be either process or physics frame), similar to Object.call_deferred().

- GROUP_CALL_UNIQUE = 4
  Call nodes within a group only once, even if the call is executed many times in the same frame. Must be combined with GROUP_CALL_DEFERRED to work. **Note:** Different arguments are not taken into account. Therefore, when the same call is executed with different arguments, only the first call will be performed.
