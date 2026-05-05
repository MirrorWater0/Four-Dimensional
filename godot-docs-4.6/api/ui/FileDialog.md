# FileDialog

## Meta

- Name: FileDialog
- Source: FileDialog.xml
- Inherits: ConfirmationDialog
- Inheritance Chain: FileDialog -> ConfirmationDialog -> AcceptDialog -> Window -> Viewport -> Node -> Object

## Brief Description

A dialog for selecting files or directories in the filesystem.

## Description

FileDialog is a preset dialog used to choose files and directories in the filesystem. It supports filter masks. FileDialog automatically sets its window title according to the file_mode. If you want to use a custom title, disable this by setting mode_overrides_title to false. **Note:** FileDialog is invisible by default. To make it visible, call one of the popup_* methods from Window on the node, such as Window.popup_centered_clamped().

## Quick Reference

```
[methods]
add_filter(filter: String, description: String = "", mime_type: String = "") -> void
add_option(name: String, values: PackedStringArray, default_value_index: int) -> void
clear_filename_filter() -> void
clear_filters() -> void
deselect_all() -> void
get_favorite_list() -> PackedStringArray [static]
get_line_edit() -> LineEdit
get_option_default(option: int) -> int [const]
get_option_name(option: int) -> String [const]
get_option_values(option: int) -> PackedStringArray [const]
get_recent_list() -> PackedStringArray [static]
get_selected_options() -> Dictionary [const]
get_vbox() -> VBoxContainer
invalidate() -> void
is_customization_flag_enabled(flag: int (FileDialog.Customization)) -> bool [const]
popup_file_dialog() -> void
set_customization_flag_enabled(flag: int (FileDialog.Customization), enabled: bool) -> void
set_favorite_list(favorites: PackedStringArray) -> void [static]
set_get_icon_callback(callback: Callable) -> void [static]
set_get_thumbnail_callback(callback: Callable) -> void [static]
set_option_default(option: int, default_value_index: int) -> void
set_option_name(option: int, name: String) -> void
set_option_values(option: int, values: PackedStringArray) -> void
set_recent_list(recents: PackedStringArray) -> void [static]

[properties]
access: int (FileDialog.Access) = 0
current_dir: String
current_file: String
current_path: String
deleting_enabled: bool = true
dialog_hide_on_ok: bool = false
display_mode: int (FileDialog.DisplayMode) = 0
favorites_enabled: bool = true
file_filter_toggle_enabled: bool = true
file_mode: int (FileDialog.FileMode) = 4
file_sort_options_enabled: bool = true
filename_filter: String = ""
filters: PackedStringArray = PackedStringArray()
folder_creation_enabled: bool = true
hidden_files_toggle_enabled: bool = true
layout_toggle_enabled: bool = true
mode_overrides_title: bool = true
option_count: int = 0
overwrite_warning_enabled: bool = true
recent_list_enabled: bool = true
root_subfolder: String = ""
show_hidden_files: bool = false
size: Vector2i = Vector2i(640, 360)
title: String = "Save a File"
use_native_dialog: bool = false
```

## Methods

- add_filter(filter: String, description: String = "", mime_type: String = "") -> void
  Adds a comma-separated file extension filter and comma-separated MIME type mime_type option to the FileDialog with an optional description, which restricts what files can be picked. A filter should be of the form "filename.extension", where filename and extension can be * to match any string. Filters starting with . (i.e. empty filenames) are not allowed. For example, a filter of "*.png, *.jpg", a mime_type of image/png, image/jpeg, and a description of "Images" results in filter text "Images (*.png, *.jpg)". **Note:** Embedded file dialogs and Windows file dialogs support only file extensions, while Android, Linux, and macOS file dialogs also support MIME types.

- add_option(name: String, values: PackedStringArray, default_value_index: int) -> void
  Adds an additional OptionButton to the file dialog. If values is empty, a CheckBox is added instead. default_value_index should be an index of the value in the values. If values is empty it should be either 1 (checked), or 0 (unchecked).

- clear_filename_filter() -> void
  Clear the filter for file names.

- clear_filters() -> void
  Clear all the added filters in the dialog.

- deselect_all() -> void
  Clear all currently selected items in the dialog.

- get_favorite_list() -> PackedStringArray [static]
  Returns the list of favorite directories, which is shared by all FileDialog nodes. Useful to store the list of favorites between project sessions. This method can be called only from the main thread.

- get_line_edit() -> LineEdit
  Returns the LineEdit for the selected file. **Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their CanvasItem.visible property.

- get_option_default(option: int) -> int [const]
  Returns the default value index of the OptionButton or CheckBox with index option.

- get_option_name(option: int) -> String [const]
  Returns the name of the OptionButton or CheckBox with index option.

- get_option_values(option: int) -> PackedStringArray [const]
  Returns an array of values of the OptionButton with index option.

- get_recent_list() -> PackedStringArray [static]
  Returns the list of recent directories, which is shared by all FileDialog nodes. Useful to store the list of recents between project sessions. This method can be called only from the main thread.

- get_selected_options() -> Dictionary [const]
  Returns a Dictionary with the selected values of the additional OptionButtons and/or CheckBoxes. Dictionary keys are names and values are selected value indices.

- get_vbox() -> VBoxContainer
  Returns the vertical box container of the dialog, custom controls can be added to it. **Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their CanvasItem.visible property. **Note:** Changes to this node are ignored by native file dialogs, use add_option() to add custom elements to the dialog instead.

- invalidate() -> void
  Invalidates and updates this dialog's content list. **Note:** This method does nothing on native file dialogs.

- is_customization_flag_enabled(flag: int (FileDialog.Customization)) -> bool [const]
  Returns true if the provided flag is enabled.

- popup_file_dialog() -> void
  Shows the FileDialog using the default size and position for file dialogs, and selects the file name if there is a current file.

- set_customization_flag_enabled(flag: int (FileDialog.Customization), enabled: bool) -> void
  Sets the specified customization flag, allowing to customize the features available in this FileDialog.

- set_favorite_list(favorites: PackedStringArray) -> void [static]
  Sets the list of favorite directories, which is shared by all FileDialog nodes. Useful to restore the list of favorites saved with get_favorite_list(). This method can be called only from the main thread. **Note:** FileDialog will update its internal ItemList of favorites when its visibility changes. Be sure to call this method earlier if you want your changes to have effect.

- set_get_icon_callback(callback: Callable) -> void [static]
  Sets the callback used by the FileDialog nodes to get a file icon, when DISPLAY_LIST mode is used. The callback should take a single String argument (file path), and return a Texture2D. If an invalid texture is returned, the [theme_item file] icon will be used instead.

- set_get_thumbnail_callback(callback: Callable) -> void [static]
  Sets the callback used by the FileDialog nodes to get a file icon, when DISPLAY_THUMBNAILS mode is used. The callback should take a single String argument (file path), and return a Texture2D. If an invalid texture is returned, the [theme_item file_thumbnail] icon will be used instead. Thumbnails are usually more complex and may take a while to load. To avoid stalling the application, you can use ImageTexture to asynchronously create the thumbnail.


```
  func _ready():
      FileDialog.set_get_thumbnail_callback(thumbnail_method)

  func thumbnail_method(path):
      var image_texture = ImageTexture.new()
      make_thumbnail_async(path, image_texture)
      return image_texture

  func make_thumbnail_async(path, image_texture):
      var thumbnail_texture = await generate_thumbnail(path) # Some method that generates a thumbnail.
      image_texture.set_image(thumbnail_texture.get_image())

```

- set_option_default(option: int, default_value_index: int) -> void
  Sets the default value index of the OptionButton or CheckBox with index option.

- set_option_name(option: int, name: String) -> void
  Sets the name of the OptionButton or CheckBox with index option.

- set_option_values(option: int, values: PackedStringArray) -> void
  Sets the option values of the OptionButton with index option.

- set_recent_list(recents: PackedStringArray) -> void [static]
  Sets the list of recent directories, which is shared by all FileDialog nodes. Useful to restore the list of recents saved with set_recent_list(). This method can be called only from the main thread. **Note:** FileDialog will update its internal ItemList of recent directories when its visibility changes. Be sure to call this method earlier if you want your changes to have effect.

## Properties

- access: int (FileDialog.Access) = 0 [set set_access; get get_access]
  The file system access scope. **Warning:** In Web builds, FileDialog cannot access the host file system. In sandboxed Linux and macOS environments, use_native_dialog is automatically used to allow limited access to host file system.

- current_dir: String [set set_current_dir; get get_current_dir]
  The current working directory of the file dialog. **Note:** For native file dialogs, this property is only treated as a hint and may not be respected by specific OS implementations.

- current_file: String [set set_current_file; get get_current_file]
  The currently selected file of the file dialog.

- current_path: String [set set_current_path; get get_current_path]
  The currently selected file path of the file dialog.

- deleting_enabled: bool = true [set set_customization_flag_enabled; get is_customization_flag_enabled]
  If true, the context menu will show the "Delete" option, which allows moving files and folders to trash.

- dialog_hide_on_ok: bool = false [set set_hide_on_ok; get get_hide_on_ok; override AcceptDialog]

- display_mode: int (FileDialog.DisplayMode) = 0 [set set_display_mode; get get_display_mode]
  Display mode of the dialog's file list.

- favorites_enabled: bool = true [set set_customization_flag_enabled; get is_customization_flag_enabled]
  If true, shows the toggle favorite button and favorite list on the left side of the dialog.

- file_filter_toggle_enabled: bool = true [set set_customization_flag_enabled; get is_customization_flag_enabled]
  If true, shows the toggle file filter button.

- file_mode: int (FileDialog.FileMode) = 4 [set set_file_mode; get get_file_mode]
  The dialog's open or save mode, which affects the selection behavior.

- file_sort_options_enabled: bool = true [set set_customization_flag_enabled; get is_customization_flag_enabled]
  If true, shows the file sorting options button.

- filename_filter: String = "" [set set_filename_filter; get get_filename_filter]
  The filter for file names (case-insensitive). When set to a non-empty string, only files that contains the substring will be shown. filename_filter can be edited by the user with the filter button at the top of the file dialog. See also filters, which should be used to restrict the file types that can be selected instead of filename_filter which is meant to be set by the user.

- filters: PackedStringArray = PackedStringArray() [set set_filters; get get_filters]
  The available file type filters. Each filter string in the array should be formatted like this: *.png,*.jpg,*.jpeg;Image Files;image/png,image/jpeg. The description text of the filter is optional and can be omitted. Both file extensions and MIME type should be always set. **Note:** Embedded file dialogs and Windows file dialogs support only file extensions, while Android, Linux, and macOS file dialogs also support MIME types.

- folder_creation_enabled: bool = true [set set_customization_flag_enabled; get is_customization_flag_enabled]
  If true, shows the button for creating new directories (when using FILE_MODE_OPEN_DIR, FILE_MODE_OPEN_ANY, or FILE_MODE_SAVE_FILE), and the context menu will have the "New Folder..." option.

- hidden_files_toggle_enabled: bool = true [set set_customization_flag_enabled; get is_customization_flag_enabled]
  If true, shows the toggle hidden files button.

- layout_toggle_enabled: bool = true [set set_customization_flag_enabled; get is_customization_flag_enabled]
  If true, shows the layout switch buttons (list/thumbnails).

- mode_overrides_title: bool = true [set set_mode_overrides_title; get is_mode_overriding_title]
  If true, changing the file_mode property will set the window title accordingly (e.g. setting file_mode to FILE_MODE_OPEN_FILE will change the window title to "Open a File").

- option_count: int = 0 [set set_option_count; get get_option_count]
  The number of additional OptionButtons and CheckBoxes in the dialog.

- overwrite_warning_enabled: bool = true [set set_customization_flag_enabled; get is_customization_flag_enabled]
  If true, the FileDialog will warn the user before overwriting files in save mode.

- recent_list_enabled: bool = true [set set_customization_flag_enabled; get is_customization_flag_enabled]
  If true, shows the recent directories list on the left side of the dialog.

- root_subfolder: String = "" [set set_root_subfolder; get get_root_subfolder]
  If non-empty, the given sub-folder will be "root" of this FileDialog, i.e. user won't be able to go to its parent directory. **Note:** This property is ignored by native file dialogs.

- show_hidden_files: bool = false [set set_show_hidden_files; get is_showing_hidden_files]
  If true, the dialog will show hidden files. **Note:** This property is ignored by native file dialogs on Android and Linux.

- size: Vector2i = Vector2i(640, 360) [set set_size; get get_size; override Window]

- title: String = "Save a File" [set set_title; get get_title; override Window]

- use_native_dialog: bool = false [set set_use_native_dialog; get get_use_native_dialog]
  If true, and if supported by the current DisplayServer, OS native dialog will be used instead of custom one. **Note:** On Android, it is only supported when using ACCESS_FILESYSTEM. For access mode ACCESS_RESOURCES and ACCESS_USERDATA, the system will fall back to custom FileDialog. **Note:** On Linux and macOS, sandboxed apps always use native dialogs to access the host file system. **Note:** On macOS, sandboxed apps will save security-scoped bookmarks to retain access to the opened folders across multiple sessions. Use OS.get_granted_permissions() to get a list of saved bookmarks. **Note:** Native dialogs are isolated from the base process, file dialog properties can't be modified once the dialog is shown. **Note:** This property is ignored in EditorFileDialog.

## Signals

- dir_selected(dir: String)
  Emitted when the user selects a directory.

- file_selected(path: String)
  Emitted when the user selects a file by double-clicking it or pressing the **OK** button.

- filename_filter_changed(filter: String)
  Emitted when the filter for file names changes.

- files_selected(paths: PackedStringArray)
  Emitted when the user selects multiple files.

## Constants

### Enum FileMode

- FILE_MODE_OPEN_FILE = 0
  The dialog allows selecting one, and only one file.

- FILE_MODE_OPEN_FILES = 1
  The dialog allows selecting multiple files.

- FILE_MODE_OPEN_DIR = 2
  The dialog only allows selecting a directory, disallowing the selection of any file.

- FILE_MODE_OPEN_ANY = 3
  The dialog allows selecting one file or directory.

- FILE_MODE_SAVE_FILE = 4
  The dialog will warn when a file exists.

### Enum Access

- ACCESS_RESOURCES = 0
  The dialog only allows accessing files under the Resource path (res://).

- ACCESS_USERDATA = 1
  The dialog only allows accessing files under user data path (user://).

- ACCESS_FILESYSTEM = 2
  The dialog allows accessing files on the whole file system.

### Enum DisplayMode

- DISPLAY_THUMBNAILS = 0
  The dialog displays files as a grid of thumbnails. Use [theme_item thumbnail_size] to adjust their size.

- DISPLAY_LIST = 1
  The dialog displays files as a list of filenames.

### Enum Customization

- CUSTOMIZATION_HIDDEN_FILES = 0
  Toggles visibility of the favorite button, and the favorite list on the left side of the dialog. Equivalent to hidden_files_toggle_enabled.

- CUSTOMIZATION_CREATE_FOLDER = 1
  If enabled, shows the button for creating new directories (when using FILE_MODE_OPEN_DIR, FILE_MODE_OPEN_ANY, or FILE_MODE_SAVE_FILE). Equivalent to folder_creation_enabled.

- CUSTOMIZATION_FILE_FILTER = 2
  If enabled, shows the toggle file filter button. Equivalent to file_filter_toggle_enabled.

- CUSTOMIZATION_FILE_SORT = 3
  If enabled, shows the file sorting options button. Equivalent to file_sort_options_enabled.

- CUSTOMIZATION_FAVORITES = 4
  If enabled, shows the toggle favorite button and favorite list on the left side of the dialog. Equivalent to favorites_enabled.

- CUSTOMIZATION_RECENT = 5
  If enabled, shows the recent directories list on the left side of the dialog. Equivalent to recent_list_enabled.

- CUSTOMIZATION_LAYOUT = 6
  If enabled, shows the layout switch buttons (list/thumbnails). Equivalent to layout_toggle_enabled.

- CUSTOMIZATION_OVERWRITE_WARNING = 7
  If enabled, the FileDialog will warn the user before overwriting files in save mode. Equivalent to overwrite_warning_enabled.

- CUSTOMIZATION_DELETE = 8
  If enabled, the context menu will show the "Delete" option, which allows moving files and folders to trash. Equivalent to deleting_enabled.

## Theme Items

- file_disabled_color: Color [color] = Color(1, 1, 1, 0.25)
  The color tint for disabled files (when the FileDialog is used in open folder mode).

- file_icon_color: Color [color] = Color(1, 1, 1, 1)
  The color modulation applied to the file icon.

- folder_icon_color: Color [color] = Color(1, 1, 1, 1)
  The color modulation applied to the folder icon.

- thumbnail_size: int [constant] = 64
  The size of thumbnail icons when DISPLAY_THUMBNAILS is enabled.

- back_folder: Texture2D [icon]
  Custom icon for the back arrow.

- create_folder: Texture2D [icon]
  Custom icon for the create folder button.

- favorite: Texture2D [icon]
  Custom icon for favorite folder button.

- favorite_down: Texture2D [icon]
  Custom icon for button to move down a favorite entry.

- favorite_up: Texture2D [icon]
  Custom icon for button to move up a favorite entry.

- file: Texture2D [icon]
  Custom icon for files.

- file_thumbnail: Texture2D [icon]
  Icon for files when in thumbnail mode.

- folder: Texture2D [icon]
  Custom icon for folders.

- folder_thumbnail: Texture2D [icon]
  Icon for folders when in thumbnail mode.

- forward_folder: Texture2D [icon]
  Custom icon for the forward arrow.

- list_mode: Texture2D [icon]
  Icon for the button that enables list mode.

- parent_folder: Texture2D [icon]
  Custom icon for the parent folder arrow.

- reload: Texture2D [icon]
  Custom icon for the reload button.

- sort: Texture2D [icon]
  Custom icon for the sorting options menu.

- thumbnail_mode: Texture2D [icon]
  Icon for the button that enables thumbnail mode.

- toggle_filename_filter: Texture2D [icon]
  Custom icon for the toggle button for the filter for file names.

- toggle_hidden: Texture2D [icon]
  Custom icon for the toggle hidden button.
