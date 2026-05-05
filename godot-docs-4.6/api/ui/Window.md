# Window

## Meta

- Name: Window
- Source: Window.xml
- Inherits: Viewport
- Inheritance Chain: Window -> Viewport -> Node -> Object

## Brief Description

Base class for all windows, dialogs, and popups.

## Description

A node that creates a window. The window can either be a native system window or embedded inside another Window (see Viewport.gui_embed_subwindows). At runtime, Windows will not close automatically when requested. You need to handle it manually using the close_requested signal (this applies both to pressing the close button and clicking outside of a popup).

## Quick Reference

```
[methods]
_get_contents_minimum_size() -> Vector2 [virtual const]
add_theme_color_override(name: StringName, color: Color) -> void
add_theme_constant_override(name: StringName, constant: int) -> void
add_theme_font_override(name: StringName, font: Font) -> void
add_theme_font_size_override(name: StringName, font_size: int) -> void
add_theme_icon_override(name: StringName, texture: Texture2D) -> void
add_theme_stylebox_override(name: StringName, stylebox: StyleBox) -> void
begin_bulk_theme_override() -> void
can_draw() -> bool [const]
child_controls_changed() -> void
end_bulk_theme_override() -> void
get_contents_minimum_size() -> Vector2 [const]
get_flag(flag: int (Window.Flags)) -> bool [const]
get_focused_window() -> Window [static]
get_layout_direction() -> int (Window.LayoutDirection) [const]
get_position_with_decorations() -> Vector2i [const]
get_size_with_decorations() -> Vector2i [const]
get_theme_color(name: StringName, theme_type: StringName = &"") -> Color [const]
get_theme_constant(name: StringName, theme_type: StringName = &"") -> int [const]
get_theme_default_base_scale() -> float [const]
get_theme_default_font() -> Font [const]
get_theme_default_font_size() -> int [const]
get_theme_font(name: StringName, theme_type: StringName = &"") -> Font [const]
get_theme_font_size(name: StringName, theme_type: StringName = &"") -> int [const]
get_theme_icon(name: StringName, theme_type: StringName = &"") -> Texture2D [const]
get_theme_stylebox(name: StringName, theme_type: StringName = &"") -> StyleBox [const]
get_window_id() -> int [const]
grab_focus() -> void
has_focus() -> bool [const]
has_theme_color(name: StringName, theme_type: StringName = &"") -> bool [const]
has_theme_color_override(name: StringName) -> bool [const]
has_theme_constant(name: StringName, theme_type: StringName = &"") -> bool [const]
has_theme_constant_override(name: StringName) -> bool [const]
has_theme_font(name: StringName, theme_type: StringName = &"") -> bool [const]
has_theme_font_override(name: StringName) -> bool [const]
has_theme_font_size(name: StringName, theme_type: StringName = &"") -> bool [const]
has_theme_font_size_override(name: StringName) -> bool [const]
has_theme_icon(name: StringName, theme_type: StringName = &"") -> bool [const]
has_theme_icon_override(name: StringName) -> bool [const]
has_theme_stylebox(name: StringName, theme_type: StringName = &"") -> bool [const]
has_theme_stylebox_override(name: StringName) -> bool [const]
hide() -> void
is_embedded() -> bool [const]
is_layout_rtl() -> bool [const]
is_maximize_allowed() -> bool [const]
is_using_font_oversampling() -> bool [const]
move_to_center() -> void
move_to_foreground() -> void
popup(rect: Rect2i = Rect2i(0, 0, 0, 0)) -> void
popup_centered(minsize: Vector2i = Vector2i(0, 0)) -> void
popup_centered_clamped(minsize: Vector2i = Vector2i(0, 0), fallback_ratio: float = 0.75) -> void
popup_centered_ratio(ratio: float = 0.8) -> void
popup_exclusive(from_node: Node, rect: Rect2i = Rect2i(0, 0, 0, 0)) -> void
popup_exclusive_centered(from_node: Node, minsize: Vector2i = Vector2i(0, 0)) -> void
popup_exclusive_centered_clamped(from_node: Node, minsize: Vector2i = Vector2i(0, 0), fallback_ratio: float = 0.75) -> void
popup_exclusive_centered_ratio(from_node: Node, ratio: float = 0.8) -> void
popup_exclusive_on_parent(from_node: Node, parent_rect: Rect2i) -> void
popup_on_parent(parent_rect: Rect2i) -> void
remove_theme_color_override(name: StringName) -> void
remove_theme_constant_override(name: StringName) -> void
remove_theme_font_override(name: StringName) -> void
remove_theme_font_size_override(name: StringName) -> void
remove_theme_icon_override(name: StringName) -> void
remove_theme_stylebox_override(name: StringName) -> void
request_attention() -> void
reset_size() -> void
set_flag(flag: int (Window.Flags), enabled: bool) -> void
set_ime_active(active: bool) -> void
set_ime_position(position: Vector2i) -> void
set_layout_direction(direction: int (Window.LayoutDirection)) -> void
set_unparent_when_invisible(unparent: bool) -> void
set_use_font_oversampling(enable: bool) -> void
show() -> void
start_drag() -> void
start_resize(edge: int (DisplayServer.WindowResizeEdge)) -> void

[properties]
accessibility_description: String = ""
accessibility_name: String = ""
always_on_top: bool = false
auto_translate: bool
borderless: bool = false
content_scale_aspect: int (Window.ContentScaleAspect) = 0
content_scale_factor: float = 1.0
content_scale_mode: int (Window.ContentScaleMode) = 0
content_scale_size: Vector2i = Vector2i(0, 0)
content_scale_stretch: int (Window.ContentScaleStretch) = 0
current_screen: int
exclude_from_capture: bool = false
exclusive: bool = false
extend_to_title: bool = false
force_native: bool = false
initial_position: int (Window.WindowInitialPosition) = 0
keep_title_visible: bool = false
max_size: Vector2i = Vector2i(0, 0)
maximize_disabled: bool = false
min_size: Vector2i = Vector2i(0, 0)
minimize_disabled: bool = false
mode: int (Window.Mode) = 0
mouse_passthrough: bool = false
mouse_passthrough_polygon: PackedVector2Array = PackedVector2Array()
nonclient_area: Rect2i = Rect2i(0, 0, 0, 0)
popup_window: bool = false
popup_wm_hint: bool = false
position: Vector2i = Vector2i(0, 0)
sharp_corners: bool = false
size: Vector2i = Vector2i(100, 100)
theme: Theme
theme_type_variation: StringName = &""
title: String = ""
transient: bool = false
transient_to_focused: bool = false
transparent: bool = false
unfocusable: bool = false
unresizable: bool = false
visible: bool = true
wrap_controls: bool = false
```

## Methods

- _get_contents_minimum_size() -> Vector2 [virtual const]
  Virtual method to be implemented by the user. Overrides the value returned by get_contents_minimum_size().

- add_theme_color_override(name: StringName, color: Color) -> void
  Creates a local override for a theme Color with the specified name. Local overrides always take precedence when fetching theme items for the control. An override can be removed with remove_theme_color_override(). See also get_theme_color() and Control.add_theme_color_override() for more details.

- add_theme_constant_override(name: StringName, constant: int) -> void
  Creates a local override for a theme constant with the specified name. Local overrides always take precedence when fetching theme items for the control. An override can be removed with remove_theme_constant_override(). See also get_theme_constant().

- add_theme_font_override(name: StringName, font: Font) -> void
  Creates a local override for a theme Font with the specified name. Local overrides always take precedence when fetching theme items for the control. An override can be removed with remove_theme_font_override(). See also get_theme_font().

- add_theme_font_size_override(name: StringName, font_size: int) -> void
  Creates a local override for a theme font size with the specified name. Local overrides always take precedence when fetching theme items for the control. An override can be removed with remove_theme_font_size_override(). See also get_theme_font_size().

- add_theme_icon_override(name: StringName, texture: Texture2D) -> void
  Creates a local override for a theme icon with the specified name. Local overrides always take precedence when fetching theme items for the control. An override can be removed with remove_theme_icon_override(). See also get_theme_icon().

- add_theme_stylebox_override(name: StringName, stylebox: StyleBox) -> void
  Creates a local override for a theme StyleBox with the specified name. Local overrides always take precedence when fetching theme items for the control. An override can be removed with remove_theme_stylebox_override(). See also get_theme_stylebox() and Control.add_theme_stylebox_override() for more details.

- begin_bulk_theme_override() -> void
  Prevents *_theme_*_override methods from emitting NOTIFICATION_THEME_CHANGED until end_bulk_theme_override() is called.

- can_draw() -> bool [const]
  Returns whether the window is being drawn to the screen.

- child_controls_changed() -> void
  Requests an update of the Window size to fit underlying Control nodes.

- end_bulk_theme_override() -> void
  Ends a bulk theme override update. See begin_bulk_theme_override().

- get_contents_minimum_size() -> Vector2 [const]
  Returns the combined minimum size from the child Control nodes of the window. Use child_controls_changed() to update it when child nodes have changed. The value returned by this method can be overridden with _get_contents_minimum_size().

- get_flag(flag: int (Window.Flags)) -> bool [const]
  Returns true if the flag is set.

- get_focused_window() -> Window [static]
  Returns the focused window.

- get_layout_direction() -> int (Window.LayoutDirection) [const]
  Returns layout direction and text writing direction.

- get_position_with_decorations() -> Vector2i [const]
  Returns the window's position including its border. **Note:** If visible is false, this method returns the same value as position.

- get_size_with_decorations() -> Vector2i [const]
  Returns the window's size including its border. **Note:** If visible is false, this method returns the same value as size.

- get_theme_color(name: StringName, theme_type: StringName = &"") -> Color [const]
  Returns a Color from the first matching Theme in the tree if that Theme has a color item with the specified name and theme_type. See Control.get_theme_color() for more details.

- get_theme_constant(name: StringName, theme_type: StringName = &"") -> int [const]
  Returns a constant from the first matching Theme in the tree if that Theme has a constant item with the specified name and theme_type. See Control.get_theme_color() for more details.

- get_theme_default_base_scale() -> float [const]
  Returns the default base scale value from the first matching Theme in the tree if that Theme has a valid Theme.default_base_scale value. See Control.get_theme_color() for details.

- get_theme_default_font() -> Font [const]
  Returns the default font from the first matching Theme in the tree if that Theme has a valid Theme.default_font value. See Control.get_theme_color() for details.

- get_theme_default_font_size() -> int [const]
  Returns the default font size value from the first matching Theme in the tree if that Theme has a valid Theme.default_font_size value. See Control.get_theme_color() for details.

- get_theme_font(name: StringName, theme_type: StringName = &"") -> Font [const]
  Returns a Font from the first matching Theme in the tree if that Theme has a font item with the specified name and theme_type. See Control.get_theme_color() for details.

- get_theme_font_size(name: StringName, theme_type: StringName = &"") -> int [const]
  Returns a font size from the first matching Theme in the tree if that Theme has a font size item with the specified name and theme_type. See Control.get_theme_color() for details.

- get_theme_icon(name: StringName, theme_type: StringName = &"") -> Texture2D [const]
  Returns an icon from the first matching Theme in the tree if that Theme has an icon item with the specified name and theme_type. See Control.get_theme_color() for details.

- get_theme_stylebox(name: StringName, theme_type: StringName = &"") -> StyleBox [const]
  Returns a StyleBox from the first matching Theme in the tree if that Theme has a stylebox item with the specified name and theme_type. See Control.get_theme_color() for details.

- get_window_id() -> int [const]
  Returns the ID of the window.

- grab_focus() -> void
  Causes the window to grab focus, allowing it to receive user input.

- has_focus() -> bool [const]
  Returns true if the window is focused.

- has_theme_color(name: StringName, theme_type: StringName = &"") -> bool [const]
  Returns true if there is a matching Theme in the tree that has a color item with the specified name and theme_type. See Control.get_theme_color() for details.

- has_theme_color_override(name: StringName) -> bool [const]
  Returns true if there is a local override for a theme Color with the specified name in this Control node. See add_theme_color_override().

- has_theme_constant(name: StringName, theme_type: StringName = &"") -> bool [const]
  Returns true if there is a matching Theme in the tree that has a constant item with the specified name and theme_type. See Control.get_theme_color() for details.

- has_theme_constant_override(name: StringName) -> bool [const]
  Returns true if there is a local override for a theme constant with the specified name in this Control node. See add_theme_constant_override().

- has_theme_font(name: StringName, theme_type: StringName = &"") -> bool [const]
  Returns true if there is a matching Theme in the tree that has a font item with the specified name and theme_type. See Control.get_theme_color() for details.

- has_theme_font_override(name: StringName) -> bool [const]
  Returns true if there is a local override for a theme Font with the specified name in this Control node. See add_theme_font_override().

- has_theme_font_size(name: StringName, theme_type: StringName = &"") -> bool [const]
  Returns true if there is a matching Theme in the tree that has a font size item with the specified name and theme_type. See Control.get_theme_color() for details.

- has_theme_font_size_override(name: StringName) -> bool [const]
  Returns true if there is a local override for a theme font size with the specified name in this Control node. See add_theme_font_size_override().

- has_theme_icon(name: StringName, theme_type: StringName = &"") -> bool [const]
  Returns true if there is a matching Theme in the tree that has an icon item with the specified name and theme_type. See Control.get_theme_color() for details.

- has_theme_icon_override(name: StringName) -> bool [const]
  Returns true if there is a local override for a theme icon with the specified name in this Control node. See add_theme_icon_override().

- has_theme_stylebox(name: StringName, theme_type: StringName = &"") -> bool [const]
  Returns true if there is a matching Theme in the tree that has a stylebox item with the specified name and theme_type. See Control.get_theme_color() for details.

- has_theme_stylebox_override(name: StringName) -> bool [const]
  Returns true if there is a local override for a theme StyleBox with the specified name in this Control node. See add_theme_stylebox_override().

- hide() -> void
  Hides the window. This is not the same as minimized state. Hidden window can't be interacted with and needs to be made visible with show().

- is_embedded() -> bool [const]
  Returns true if the window is currently embedded in another window.

- is_layout_rtl() -> bool [const]
  Returns true if the layout is right-to-left.

- is_maximize_allowed() -> bool [const]
  Returns true if the window can be maximized (the maximize button is enabled).

- is_using_font_oversampling() -> bool [const]
  Returns true if font oversampling is enabled. See set_use_font_oversampling().

- move_to_center() -> void
  Centers the window in the current screen. If the window is embedded, it is centered in the embedder Viewport instead.

- move_to_foreground() -> void
  Causes the window to grab focus, allowing it to receive user input.

- popup(rect: Rect2i = Rect2i(0, 0, 0, 0)) -> void
  Shows the Window and makes it transient (see transient). If rect is provided, it will be set as the Window's size. Fails if called on the main window. If ProjectSettings.display/window/subwindows/embed_subwindows is true (single-window mode), rect's coordinates are global and relative to the main window's top-left corner (excluding window decorations). If rect's position coordinates are negative, the window will be located outside the main window and may not be visible as a result. If ProjectSettings.display/window/subwindows/embed_subwindows is false (multi-window mode), rect's coordinates are global and relative to the top-left corner of the leftmost screen. If rect's position coordinates are negative, the window will be placed at the top-left corner of the screen. **Note:** rect must be in global coordinates if specified.

- popup_centered(minsize: Vector2i = Vector2i(0, 0)) -> void
  Popups the Window at the center of the current screen, with optionally given minimum size. If the Window is embedded, it will be centered in the parent Viewport instead. **Note:** Calling it with the default value of minsize is equivalent to calling it with size.

- popup_centered_clamped(minsize: Vector2i = Vector2i(0, 0), fallback_ratio: float = 0.75) -> void
  Popups the Window centered inside its parent Window. fallback_ratio determines the maximum size of the Window, in relation to its parent. **Note:** Calling it with the default value of minsize is equivalent to calling it with size.

- popup_centered_ratio(ratio: float = 0.8) -> void
  If Window is embedded, popups the Window centered inside its embedder and sets its size as a ratio of embedder's size. If Window is a native window, popups the Window centered inside the screen of its parent Window and sets its size as a ratio of the screen size.

- popup_exclusive(from_node: Node, rect: Rect2i = Rect2i(0, 0, 0, 0)) -> void
  Attempts to parent this dialog to the last exclusive window relative to from_node, and then calls Window.popup() on it. The dialog must have no current parent, otherwise the method fails. See also set_unparent_when_invisible() and Node.get_last_exclusive_window().

- popup_exclusive_centered(from_node: Node, minsize: Vector2i = Vector2i(0, 0)) -> void
  Attempts to parent this dialog to the last exclusive window relative to from_node, and then calls Window.popup_centered() on it. The dialog must have no current parent, otherwise the method fails. See also set_unparent_when_invisible() and Node.get_last_exclusive_window().

- popup_exclusive_centered_clamped(from_node: Node, minsize: Vector2i = Vector2i(0, 0), fallback_ratio: float = 0.75) -> void
  Attempts to parent this dialog to the last exclusive window relative to from_node, and then calls Window.popup_centered_clamped() on it. The dialog must have no current parent, otherwise the method fails. See also set_unparent_when_invisible() and Node.get_last_exclusive_window().

- popup_exclusive_centered_ratio(from_node: Node, ratio: float = 0.8) -> void
  Attempts to parent this dialog to the last exclusive window relative to from_node, and then calls Window.popup_centered_ratio() on it. The dialog must have no current parent, otherwise the method fails. See also set_unparent_when_invisible() and Node.get_last_exclusive_window().

- popup_exclusive_on_parent(from_node: Node, parent_rect: Rect2i) -> void
  Attempts to parent this dialog to the last exclusive window relative to from_node, and then calls Window.popup_on_parent() on it. The dialog must have no current parent, otherwise the method fails. See also set_unparent_when_invisible() and Node.get_last_exclusive_window().

- popup_on_parent(parent_rect: Rect2i) -> void
  Popups the Window with a position shifted by parent Window's position. If the Window is embedded, has the same effect as popup().

- remove_theme_color_override(name: StringName) -> void
  Removes a local override for a theme Color with the specified name previously added by add_theme_color_override() or via the Inspector dock.

- remove_theme_constant_override(name: StringName) -> void
  Removes a local override for a theme constant with the specified name previously added by add_theme_constant_override() or via the Inspector dock.

- remove_theme_font_override(name: StringName) -> void
  Removes a local override for a theme Font with the specified name previously added by add_theme_font_override() or via the Inspector dock.

- remove_theme_font_size_override(name: StringName) -> void
  Removes a local override for a theme font size with the specified name previously added by add_theme_font_size_override() or via the Inspector dock.

- remove_theme_icon_override(name: StringName) -> void
  Removes a local override for a theme icon with the specified name previously added by add_theme_icon_override() or via the Inspector dock.

- remove_theme_stylebox_override(name: StringName) -> void
  Removes a local override for a theme StyleBox with the specified name previously added by add_theme_stylebox_override() or via the Inspector dock.

- request_attention() -> void
  Tells the OS that the Window needs an attention. This makes the window stand out in some way depending on the system, e.g. it might blink on the task bar.

- reset_size() -> void
  Resets the size to the minimum size, which is the max of min_size and (if wrap_controls is enabled) get_contents_minimum_size(). This is equivalent to calling set_size(Vector2i()) (or any size below the minimum).

- set_flag(flag: int (Window.Flags), enabled: bool) -> void
  Sets a specified window flag.

- set_ime_active(active: bool) -> void
  If active is true, enables system's native IME (Input Method Editor).

- set_ime_position(position: Vector2i) -> void
  Moves IME to the given position.

- set_layout_direction(direction: int (Window.LayoutDirection)) -> void
  Sets layout direction and text writing direction. Right-to-left layouts are necessary for certain languages (e.g. Arabic and Hebrew).

- set_unparent_when_invisible(unparent: bool) -> void
  If unparent is true, the window is automatically unparented when going invisible. **Note:** Make sure to keep a reference to the node, otherwise it will be orphaned. You also need to manually call Node.queue_free() to free the window if it's not parented.

- set_use_font_oversampling(enable: bool) -> void
  Enables font oversampling. This makes fonts look better when they are scaled up.

- show() -> void
  Makes the Window appear. This enables interactions with the Window and doesn't change any of its property other than visibility (unlike e.g. popup()).

- start_drag() -> void
  Starts an interactive drag operation on the window, using the current mouse position. Call this method when handling a mouse button being pressed to simulate a pressed event on the window's title bar. Using this method allows the window to participate in space switching, tiling, and other system features.

- start_resize(edge: int (DisplayServer.WindowResizeEdge)) -> void
  Starts an interactive resize operation on the window, using the current mouse position. Call this method when handling a mouse button being pressed to simulate a pressed event on the window's edge.

## Properties

- accessibility_description: String = "" [set set_accessibility_description; get get_accessibility_description]
  The human-readable node description that is reported to assistive apps.

- accessibility_name: String = "" [set set_accessibility_name; get get_accessibility_name]
  The human-readable node name that is reported to assistive apps.

- always_on_top: bool = false [set set_flag; get get_flag]
  If true, the window will be on top of all other windows. Does not work if transient is enabled.

- auto_translate: bool [set set_auto_translate; get is_auto_translating]
  Toggles if any text should automatically change to its translated version depending on the current locale.

- borderless: bool = false [set set_flag; get get_flag]
  If true, the window will have no borders.

- content_scale_aspect: int (Window.ContentScaleAspect) = 0 [set set_content_scale_aspect; get get_content_scale_aspect]
  Specifies how the content's aspect behaves when the Window is resized. The base aspect is determined by content_scale_size.

- content_scale_factor: float = 1.0 [set set_content_scale_factor; get get_content_scale_factor]
  Specifies the base scale of Window's content when its size is equal to content_scale_size. See also Viewport.get_stretch_transform().

- content_scale_mode: int (Window.ContentScaleMode) = 0 [set set_content_scale_mode; get get_content_scale_mode]
  Specifies how the content is scaled when the Window is resized.

- content_scale_size: Vector2i = Vector2i(0, 0) [set set_content_scale_size; get get_content_scale_size]
  The content's base size in "virtual" pixels. Not to be confused with size, which sets the actual window's physical size in pixels. If set to a value greater than 0 and content_scale_mode is set to a value other than CONTENT_SCALE_MODE_DISABLED, the Window's content will be scaled when the window is resized to a different size. Higher values will make the content appear *smaller*, as it will be able to fit more of the project in view. On the root Window, this is set to match ProjectSettings.display/window/size/viewport_width and ProjectSettings.display/window/size/viewport_height by default. For example, when using CONTENT_SCALE_MODE_CANVAS_ITEMS and content_scale_size set to Vector2i(1280, 720), using a window size of 2560×1440 will make 2D elements appear at double their original size, as the content is scaled by a factor of 2.0 (2560.0 / 1280.0 = 2.0, 1440.0 / 720.0 = 2.0). See [the Base size section of the Multiple resolutions documentation]($DOCS_URL/tutorials/rendering/multiple_resolutions.html#base-size) for details.

- content_scale_stretch: int (Window.ContentScaleStretch) = 0 [set set_content_scale_stretch; get get_content_scale_stretch]
  The policy to use to determine the final scale factor for 2D elements. This affects how content_scale_factor is applied, in addition to the automatic scale factor determined by content_scale_size.

- current_screen: int [set set_current_screen; get get_current_screen]
  The screen the window is currently on.

- exclude_from_capture: bool = false [set set_flag; get get_flag]
  If true, the Window is excluded from screenshots taken by DisplayServer.screen_get_image(), DisplayServer.screen_get_image_rect(), and DisplayServer.screen_get_pixel(). **Note:** This property is implemented on macOS and Windows. **Note:** Enabling this setting will prevent standard screenshot methods from capturing a window image, but does **NOT** guarantee that other apps won't be able to capture an image. It should not be used as a DRM or security measure.

- exclusive: bool = false [set set_exclusive; get is_exclusive]
  If true, the Window will be in exclusive mode. Exclusive windows are always on top of their parent and will block all input going to the parent Window. Needs transient enabled to work.

- extend_to_title: bool = false [set set_flag; get get_flag]
  If true, the Window contents is expanded to the full size of the window, window title bar is transparent. **Note:** This property is implemented only on macOS. **Note:** This property only works with native windows.

- force_native: bool = false [set set_force_native; get get_force_native]
  If true, native window will be used regardless of parent viewport and project settings.

- initial_position: int (Window.WindowInitialPosition) = 0 [set set_initial_position; get get_initial_position]
  Specifies the initial type of position for the Window.

- keep_title_visible: bool = false [set set_keep_title_visible; get get_keep_title_visible]
  If true, the Window width is expanded to keep the title bar text fully visible.

- max_size: Vector2i = Vector2i(0, 0) [set set_max_size; get get_max_size]
  If non-zero, the Window can't be resized to be bigger than this size. **Note:** This property will be ignored if the value is lower than min_size.

- maximize_disabled: bool = false [set set_flag; get get_flag]
  If true, the Window's maximize button is disabled. **Note:** If both minimize and maximize buttons are disabled, buttons are fully hidden, and only close button is visible. **Note:** This property is implemented only on macOS and Windows.

- min_size: Vector2i = Vector2i(0, 0) [set set_min_size; get get_min_size]
  If non-zero, the Window can't be resized to be smaller than this size. **Note:** This property will be ignored in favor of get_contents_minimum_size() if wrap_controls is enabled and if its size is bigger.

- minimize_disabled: bool = false [set set_flag; get get_flag]
  If true, the Window's minimize button is disabled. **Note:** If both minimize and maximize buttons are disabled, buttons are fully hidden, and only close button is visible. **Note:** This property is implemented only on macOS and Windows.

- mode: int (Window.Mode) = 0 [set set_mode; get get_mode]
  Set's the window's current mode. **Note:** Fullscreen mode is not exclusive full screen on Windows and Linux. **Note:** This method only works with native windows, i.e. the main window and Window-derived nodes when Viewport.gui_embed_subwindows is disabled in the main viewport.

- mouse_passthrough: bool = false [set set_flag; get get_flag]
  If true, all mouse events will be passed to the underlying window of the same application. See also mouse_passthrough_polygon. **Note:** This property is implemented on Linux (X11), macOS and Windows. **Note:** This property only works with native windows.

- mouse_passthrough_polygon: PackedVector2Array = PackedVector2Array() [set set_mouse_passthrough_polygon; get get_mouse_passthrough_polygon]
  Sets a polygonal region of the window which accepts mouse events. Mouse events outside the region will be passed through. Passing an empty array will disable passthrough support (all mouse events will be intercepted by the window, which is the default behavior).

```
# Set region, using Path2D node.
$Window.mouse_passthrough_polygon = $Path2D.curve.get_baked_points()

# Set region, using Polygon2D node.
$Window.mouse_passthrough_polygon = $Polygon2D.polygon

# Reset region to default.
$Window.mouse_passthrough_polygon = []
```

```
// Set region, using Path2D node.
GetNode<Window>("Window").MousePassthroughPolygon = GetNode<Path2D>("Path2D").Curve.GetBakedPoints();

// Set region, using Polygon2D node.
GetNode<Window>("Window").MousePassthroughPolygon = GetNode<Polygon2D>("Polygon2D").Polygon;

// Reset region to default.
GetNode<Window>("Window").MousePassthroughPolygon = [];
```

**Note:** This property is ignored if mouse_passthrough is set to true. **Note:** On Windows, the portion of a window that lies outside the region is not drawn, while on Linux (X11) and macOS it is. **Note:** This property is implemented on Linux (X11), macOS and Windows.

- nonclient_area: Rect2i = Rect2i(0, 0, 0, 0) [set set_nonclient_area; get get_nonclient_area]
  If set, defines the window's custom decoration area which will receive mouse input, even if normal input to the window is blocked (such as when it has an exclusive child opened). See also nonclient_window_input.

- popup_window: bool = false [set set_flag; get get_flag]
  If true, the Window will be considered a popup. Popups are sub-windows that don't show as separate windows in system's window manager's window list and will send close request when anything is clicked outside of them (unless exclusive is enabled).

- popup_wm_hint: bool = false [set set_flag; get get_flag]
  If true, the Window will signal to the window manager that it is supposed to be an implementation-defined "popup" (usually a floating, borderless, untileable and immovable child window).

- position: Vector2i = Vector2i(0, 0) [set set_position; get get_position]
  The window's position in pixels. If ProjectSettings.display/window/subwindows/embed_subwindows is false, the position is in absolute screen coordinates. This typically applies to editor plugins. If the setting is true, the window's position is in the coordinates of its parent Viewport. **Note:** This property only works if initial_position is set to WINDOW_INITIAL_POSITION_ABSOLUTE.

- sharp_corners: bool = false [set set_flag; get get_flag]
  If true, the Window will override the OS window style to display sharp corners. **Note:** This property is implemented only on Windows (11). **Note:** This property only works with native windows.

- size: Vector2i = Vector2i(100, 100) [set set_size; get get_size]
  The window's size in pixels. See also content_scale_size, which doesn't set the window's physical size but affects how scaling works relative to the current content_scale_mode.

- theme: Theme [set set_theme; get get_theme]
  The Theme resource this node and all its Control and Window children use. If a child node has its own Theme resource set, theme items are merged with child's definitions having higher priority. **Note:** Window styles will have no effect unless the window is embedded.

- theme_type_variation: StringName = &"" [set set_theme_type_variation; get get_theme_type_variation]
  The name of a theme type variation used by this Window to look up its own theme items. See Control.theme_type_variation for more details.

- title: String = "" [set set_title; get get_title]
  The window's title. If the Window is native, title styles set in Theme will have no effect.

- transient: bool = false [set set_transient; get is_transient]
  If true, the Window is transient, i.e. it's considered a child of another Window. The transient window will be destroyed with its transient parent and will return focus to their parent when closed. The transient window is displayed on top of a non-exclusive full-screen parent window. Transient windows can't enter full-screen mode. Note that behavior might be different depending on the platform.

- transient_to_focused: bool = false [set set_transient_to_focused; get is_transient_to_focused]
  If true, and the Window is transient, this window will (at the time of becoming visible) become transient to the currently focused window instead of the immediate parent window in the hierarchy. Note that the transient parent is assigned at the time this window becomes visible, so changing it afterwards has no effect until re-shown.

- transparent: bool = false [set set_flag; get get_flag]
  If true, the Window's background can be transparent. This is best used with embedded windows. **Note:** Transparency support is implemented on Linux, macOS and Windows, but availability might vary depending on GPU driver, display manager, and compositor capabilities. **Note:** This property has no effect if ProjectSettings.display/window/per_pixel_transparency/allowed is set to false.

- unfocusable: bool = false [set set_flag; get get_flag]
  If true, the Window can't be focused nor interacted with. It can still be visible.

- unresizable: bool = false [set set_flag; get get_flag]
  If true, the window can't be resized.

- visible: bool = true [set set_visible; get is_visible]
  If true, the window is visible.

- wrap_controls: bool = false [set set_wrap_controls; get is_wrapping_controls]
  If true, the window's size will automatically update when a child node is added or removed, ignoring min_size if the new size is bigger. If false, you need to call child_controls_changed() manually.

## Signals

- about_to_popup()
  Emitted right after popup() call, before the Window appears or does anything.

- close_requested()
  Emitted when the Window's close button is pressed or when popup_window is enabled and user clicks outside the window. This signal can be used to handle window closing, e.g. by connecting it to hide().

- dpi_changed()
  Emitted when the Window's DPI changes as a result of OS-level changes (e.g. moving the window from a Retina display to a lower resolution one). **Note:** Only implemented on macOS and Linux (Wayland).

- files_dropped(files: PackedStringArray)
  Emitted when files are dragged from the OS file manager and dropped in the game window. The argument is a list of file paths.

```
func _ready():
    get_window().files_dropped.connect(on_files_dropped)

func on_files_dropped(files):
    print(files)
```

**Note:** This signal only works with native windows, i.e. the main window and Window-derived nodes when Viewport.gui_embed_subwindows is disabled in the main viewport.

- focus_entered()
  Emitted when the Window gains focus.

- focus_exited()
  Emitted when the Window loses its focus.

- go_back_requested()
  Emitted when a go back request is sent (e.g. pressing the "Back" button on Android), right after Node.NOTIFICATION_WM_GO_BACK_REQUEST.

- mouse_entered()
  Emitted when the mouse cursor enters the Window's visible area, that is not occluded behind other Controls or windows, provided its Viewport.gui_disable_input is false and regardless if it's currently focused or not.

- mouse_exited()
  Emitted when the mouse cursor leaves the Window's visible area, that is not occluded behind other Controls or windows, provided its Viewport.gui_disable_input is false and regardless if it's currently focused or not.

- nonclient_window_input(event: InputEvent)
  Emitted when the mouse event is received by the custom decoration area defined by nonclient_area, and normal input to the window is blocked (such as when it has an exclusive child opened). event's position is in the embedder's coordinate system.

- theme_changed()
  Emitted when the NOTIFICATION_THEME_CHANGED notification is sent.

- title_changed()
  Emitted when window title bar text is changed.

- titlebar_changed()
  Emitted when window title bar decorations are changed, e.g. macOS window enter/exit full screen mode, or extend-to-title flag is changed.

- visibility_changed()
  Emitted when Window is made visible or disappears.

- window_input(event: InputEvent)
  Emitted when the Window is currently focused and receives any input, passing the received event as an argument. The event's position, if present, is in the embedder's coordinate system.

## Constants

- NOTIFICATION_VISIBILITY_CHANGED = 30
  Emitted when Window's visibility changes, right before visibility_changed.

- NOTIFICATION_THEME_CHANGED = 32
  Sent when the node needs to refresh its theme items. This happens in one of the following cases: - The theme property is changed on this node or any of its ancestors. - The theme_type_variation property is changed on this node. - The node enters the scene tree. **Note:** As an optimization, this notification won't be sent from changes that occur while this node is outside of the scene tree. Instead, all of the theme item updates can be applied at once when the node enters the scene tree.

### Enum Mode

- MODE_WINDOWED = 0
  Windowed mode, i.e. Window doesn't occupy the whole screen (unless set to the size of the screen).

- MODE_MINIMIZED = 1
  Minimized window mode, i.e. Window is not visible and available on window manager's window list. Normally happens when the minimize button is pressed.

- MODE_MAXIMIZED = 2
  Maximized window mode, i.e. Window will occupy whole screen area except task bar and still display its borders. Normally happens when the maximize button is pressed.

- MODE_FULLSCREEN = 3
  Full screen mode with full multi-window support. Full screen window covers the entire display area of a screen and has no decorations. The display's video mode is not changed. **On Android:** This enables immersive mode. **On macOS:** A new desktop is used to display the running project. **Note:** Regardless of the platform, enabling full screen will change the window size to match the monitor's size. Therefore, make sure your project supports [multiple resolutions]($DOCS_URL/tutorials/rendering/multiple_resolutions.html) when enabling full screen mode.

- MODE_EXCLUSIVE_FULLSCREEN = 4
  A single window full screen mode. This mode has less overhead, but only one window can be open on a given screen at a time (opening a child window or application switching will trigger a full screen transition). Full screen window covers the entire display area of a screen and has no border or decorations. The display's video mode is not changed. **Note:** This mode might not work with screen recording software. **On Android:** This enables immersive mode. **On Windows:** Depending on video driver, full screen transition might cause screens to go black for a moment. **On macOS:** A new desktop is used to display the running project. Exclusive full screen mode prevents Dock and Menu from showing up when the mouse pointer is hovering the edge of the screen. **On Linux (X11):** Exclusive full screen mode bypasses compositor. **On Linux (Wayland):** Equivalent to MODE_FULLSCREEN. **Note:** Regardless of the platform, enabling full screen will change the window size to match the monitor's size. Therefore, make sure your project supports [multiple resolutions]($DOCS_URL/tutorials/rendering/multiple_resolutions.html) when enabling full screen mode.

### Enum Flags

- FLAG_RESIZE_DISABLED = 0
  The window can't be resized by dragging its resize grip. It's still possible to resize the window using size. This flag is ignored for full screen windows. Set with unresizable.

- FLAG_BORDERLESS = 1
  The window do not have native title bar and other decorations. This flag is ignored for full-screen windows. Set with borderless.

- FLAG_ALWAYS_ON_TOP = 2
  The window is floating on top of all other windows. This flag is ignored for full-screen windows. Set with always_on_top.

- FLAG_TRANSPARENT = 3
  The window background can be transparent. Set with transparent. **Note:** This flag has no effect if either ProjectSettings.display/window/per_pixel_transparency/allowed, or the window's Viewport.transparent_bg is set to false.

- FLAG_NO_FOCUS = 4
  The window can't be focused. No-focus window will ignore all input, except mouse clicks. Set with unfocusable.

- FLAG_POPUP = 5
  Window is part of menu or OptionButton dropdown. This flag can't be changed when the window is visible. An active popup window will exclusively receive all input, without stealing focus from its parent. Popup windows are automatically closed when uses click outside it, or when an application is switched. Popup window must have transient parent set (see transient). **Note:** This flag has no effect in embedded windows (unless said window is a Popup).

- FLAG_EXTEND_TO_TITLE = 6
  Window content is expanded to the full size of the window. Unlike borderless window, the frame is left intact and can be used to resize the window, title bar is transparent, but have minimize/maximize/close buttons. Set with extend_to_title. **Note:** This flag is implemented only on macOS. **Note:** This flag has no effect in embedded windows.

- FLAG_MOUSE_PASSTHROUGH = 7
  All mouse events are passed to the underlying window of the same application. **Note:** This flag has no effect in embedded windows.

- FLAG_SHARP_CORNERS = 8
  Window style is overridden, forcing sharp corners. **Note:** This flag has no effect in embedded windows. **Note:** This flag is implemented only on Windows (11).

- FLAG_EXCLUDE_FROM_CAPTURE = 9
  Windows is excluded from screenshots taken by DisplayServer.screen_get_image(), DisplayServer.screen_get_image_rect(), and DisplayServer.screen_get_pixel(). **Note:** This flag has no effect in embedded windows. **Note:** This flag is implemented on macOS and Windows (10, 20H1). **Note:** Setting this flag will prevent standard screenshot methods from capturing a window image, but does **NOT** guarantee that other apps won't be able to capture an image. It should not be used as a DRM or security measure.

- FLAG_POPUP_WM_HINT = 10
  Signals the window manager that this window is supposed to be an implementation-defined "popup" (usually a floating, borderless, untileable and immovable child window).

- FLAG_MINIMIZE_DISABLED = 11
  Window minimize button is disabled. **Note:** This flag is implemented on macOS and Windows.

- FLAG_MAXIMIZE_DISABLED = 12
  Window maximize button is disabled. **Note:** This flag is implemented on macOS and Windows.

- FLAG_MAX = 13
  Max value of the Flags.

### Enum ContentScaleMode

- CONTENT_SCALE_MODE_DISABLED = 0
  The content will not be scaled to match the Window's size (content_scale_size is ignored).

- CONTENT_SCALE_MODE_CANVAS_ITEMS = 1
  The content will be rendered at the target size. This is more performance-expensive than CONTENT_SCALE_MODE_VIEWPORT, but provides better results.

- CONTENT_SCALE_MODE_VIEWPORT = 2
  The content will be rendered at the base size and then scaled to the target size. More performant than CONTENT_SCALE_MODE_CANVAS_ITEMS, but results in pixelated image.

### Enum ContentScaleAspect

- CONTENT_SCALE_ASPECT_IGNORE = 0
  The aspect will be ignored. Scaling will simply stretch the content to fit the target size.

- CONTENT_SCALE_ASPECT_KEEP = 1
  The content's aspect will be preserved. If the target size has different aspect from the base one, the image will be centered and black bars will appear on left and right sides.

- CONTENT_SCALE_ASPECT_KEEP_WIDTH = 2
  The content can be expanded vertically. Scaling horizontally will result in keeping the width ratio and then black bars on left and right sides.

- CONTENT_SCALE_ASPECT_KEEP_HEIGHT = 3
  The content can be expanded horizontally. Scaling vertically will result in keeping the height ratio and then black bars on top and bottom sides.

- CONTENT_SCALE_ASPECT_EXPAND = 4
  The content's aspect will be preserved. If the target size has different aspect from the base one, the content will stay in the top-left corner and add an extra visible area in the stretched space.

### Enum ContentScaleStretch

- CONTENT_SCALE_STRETCH_FRACTIONAL = 0
  The content will be stretched according to a fractional factor. This fills all the space available in the window, but allows "pixel wobble" to occur due to uneven pixel scaling.

- CONTENT_SCALE_STRETCH_INTEGER = 1
  The content will be stretched only according to an integer factor, preserving sharp pixels. This may leave a black background visible on the window's edges depending on the window size.

### Enum LayoutDirection

- LAYOUT_DIRECTION_INHERITED = 0
  Automatic layout direction, determined from the parent window layout direction.

- LAYOUT_DIRECTION_APPLICATION_LOCALE = 1
  Automatic layout direction, determined from the current locale.

- LAYOUT_DIRECTION_LTR = 2
  Left-to-right layout direction.

- LAYOUT_DIRECTION_RTL = 3
  Right-to-left layout direction.

- LAYOUT_DIRECTION_SYSTEM_LOCALE = 4
  Automatic layout direction, determined from the system locale.

- LAYOUT_DIRECTION_MAX = 5
  Represents the size of the LayoutDirection enum.

- LAYOUT_DIRECTION_LOCALE = 1

### Enum WindowInitialPosition

- WINDOW_INITIAL_POSITION_ABSOLUTE = 0
  Initial window position is determined by position.

- WINDOW_INITIAL_POSITION_CENTER_PRIMARY_SCREEN = 1
  Initial window position is the center of the primary screen.

- WINDOW_INITIAL_POSITION_CENTER_MAIN_WINDOW_SCREEN = 2
  Initial window position is the center of the main window screen.

- WINDOW_INITIAL_POSITION_CENTER_OTHER_SCREEN = 3
  Initial window position is the center of current_screen screen.

- WINDOW_INITIAL_POSITION_CENTER_SCREEN_WITH_MOUSE_FOCUS = 4
  Initial window position is the center of the screen containing the mouse pointer.

- WINDOW_INITIAL_POSITION_CENTER_SCREEN_WITH_KEYBOARD_FOCUS = 5
  Initial window position is the center of the screen containing the window with the keyboard focus.

## Theme Items

- title_color: Color [color] = Color(0.875, 0.875, 0.875, 1)
  The color of the title's text.

- title_outline_modulate: Color [color] = Color(0, 0, 0, 1)
  The color of the title's text outline.

- close_h_offset: int [constant] = 18
  Horizontal position offset of the close button, relative to the end of the title bar, towards the beginning of the title bar.

- close_v_offset: int [constant] = 24
  Vertical position offset of the close button, relative to the bottom of the title bar, towards the top of the title bar.

- resize_margin: int [constant] = 4
  Defines the outside margin at which the window border can be grabbed with mouse and resized.

- title_height: int [constant] = 36
  Height of the title bar.

- title_outline_size: int [constant] = 0
  The size of the title outline.

- title_font: Font [font]
  The font used to draw the title.

- title_font_size: int [font_size]
  The size of the title font.

- close: Texture2D [icon]
  The icon for the close button.

- close_pressed: Texture2D [icon]
  The icon for the close button when it's being pressed.

- embedded_border: StyleBox [style]
  The background style used when the Window is embedded. Note that this is drawn only under the window's content, excluding the title. For proper borders and title bar style, you can use expand_margin_* properties of StyleBoxFlat. **Note:** The content background will not be visible unless transparent is enabled.

- embedded_unfocused_border: StyleBox [style]
  The background style used when the Window is embedded and unfocused.
