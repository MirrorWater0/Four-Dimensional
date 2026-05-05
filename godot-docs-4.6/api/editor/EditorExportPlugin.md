# EditorExportPlugin

## Meta

- Name: EditorExportPlugin
- Source: EditorExportPlugin.xml
- Inherits: RefCounted
- Inheritance Chain: EditorExportPlugin -> RefCounted -> Object

## Brief Description

A script that is executed when exporting the project.

## Description

EditorExportPlugins are automatically invoked whenever the user exports the project. Their most common use is to determine what files are being included in the exported project. For each plugin, _export_begin() is called at the beginning of the export process and then _export_file() is called for each exported file. To use EditorExportPlugin, register it using the EditorPlugin.add_export_plugin() method first.

## Quick Reference

```
[methods]
_begin_customize_resources(platform: EditorExportPlatform, features: PackedStringArray) -> bool [virtual const]
_begin_customize_scenes(platform: EditorExportPlatform, features: PackedStringArray) -> bool [virtual const]
_customize_resource(resource: Resource, path: String) -> Resource [virtual required]
_customize_scene(scene: Node, path: String) -> Node [virtual required]
_end_customize_resources() -> void [virtual]
_end_customize_scenes() -> void [virtual]
_export_begin(features: PackedStringArray, is_debug: bool, path: String, flags: int) -> void [virtual]
_export_end() -> void [virtual]
_export_file(path: String, type: String, features: PackedStringArray) -> void [virtual]
_get_android_dependencies(platform: EditorExportPlatform, debug: bool) -> PackedStringArray [virtual const]
_get_android_dependencies_maven_repos(platform: EditorExportPlatform, debug: bool) -> PackedStringArray [virtual const]
_get_android_libraries(platform: EditorExportPlatform, debug: bool) -> PackedStringArray [virtual const]
_get_android_manifest_activity_element_contents(platform: EditorExportPlatform, debug: bool) -> String [virtual const]
_get_android_manifest_application_element_contents(platform: EditorExportPlatform, debug: bool) -> String [virtual const]
_get_android_manifest_element_contents(platform: EditorExportPlatform, debug: bool) -> String [virtual const]
_get_customization_configuration_hash() -> int [virtual required const]
_get_export_features(platform: EditorExportPlatform, debug: bool) -> PackedStringArray [virtual const]
_get_export_option_visibility(platform: EditorExportPlatform, option: String) -> bool [virtual const]
_get_export_option_warning(platform: EditorExportPlatform, option: String) -> String [virtual const]
_get_export_options(platform: EditorExportPlatform) -> Dictionary[] [virtual const]
_get_export_options_overrides(platform: EditorExportPlatform) -> Dictionary [virtual const]
_get_name() -> String [virtual required const]
_should_update_export_options(platform: EditorExportPlatform) -> bool [virtual const]
_supports_platform(platform: EditorExportPlatform) -> bool [virtual const]
_update_android_prebuilt_manifest(platform: EditorExportPlatform, manifest_data: PackedByteArray) -> PackedByteArray [virtual const]
add_apple_embedded_platform_bundle_file(path: String) -> void
add_apple_embedded_platform_cpp_code(code: String) -> void
add_apple_embedded_platform_embedded_framework(path: String) -> void
add_apple_embedded_platform_framework(path: String) -> void
add_apple_embedded_platform_linker_flags(flags: String) -> void
add_apple_embedded_platform_plist_content(plist_content: String) -> void
add_apple_embedded_platform_project_static_lib(path: String) -> void
add_file(path: String, file: PackedByteArray, remap: bool) -> void
add_ios_bundle_file(path: String) -> void
add_ios_cpp_code(code: String) -> void
add_ios_embedded_framework(path: String) -> void
add_ios_framework(path: String) -> void
add_ios_linker_flags(flags: String) -> void
add_ios_plist_content(plist_content: String) -> void
add_ios_project_static_lib(path: String) -> void
add_macos_plugin_file(path: String) -> void
add_shared_object(path: String, tags: PackedStringArray, target: String) -> void
get_export_platform() -> EditorExportPlatform [const]
get_export_preset() -> EditorExportPreset [const]
get_option(name: StringName) -> Variant [const]
skip() -> void
```

## Tutorials

- [Export Android plugins]($DOCS_URL/tutorials/platform/android/android_plugin.html)

## Methods

- _begin_customize_resources(platform: EditorExportPlatform, features: PackedStringArray) -> bool [virtual const]
  Return true if this plugin will customize resources based on the platform and features used. When enabled, _get_customization_configuration_hash() and _customize_resource() will be called and must be implemented.

- _begin_customize_scenes(platform: EditorExportPlatform, features: PackedStringArray) -> bool [virtual const]
  Return true if this plugin will customize scenes based on the platform and features used. When enabled, _get_customization_configuration_hash() and _customize_scene() will be called and must be implemented. **Note:** _customize_scene() will only be called for scenes that have been modified since the last export.

- _customize_resource(resource: Resource, path: String) -> Resource [virtual required]
  Customize a resource. If changes are made to it, return the same or a new resource. Otherwise, return null. When a new resource is returned, resource will be replaced by a copy of the new resource. The path argument is only used when customizing an actual file, otherwise this means that this resource is part of another one and it will be empty. Implementing this method is required if _begin_customize_resources() returns true. **Note:** When customizing any of the following types and returning another resource, the other resource should not be skipped using skip() in _export_file(): - AtlasTexture - CompressedCubemap - CompressedCubemapArray - CompressedTexture2D - CompressedTexture2DArray - CompressedTexture3D

- _customize_scene(scene: Node, path: String) -> Node [virtual required]
  Customize a scene. If changes are made to it, return the same or a new scene. Otherwise, return null. If a new scene is returned, it is up to you to dispose of the old one. Implementing this method is required if _begin_customize_scenes() returns true.

- _end_customize_resources() -> void [virtual]
  This is called when the customization process for resources ends.

- _end_customize_scenes() -> void [virtual]
  This is called when the customization process for scenes ends.

- _export_begin(features: PackedStringArray, is_debug: bool, path: String, flags: int) -> void [virtual]
  Virtual method to be overridden by the user. It is called when the export starts and provides all information about the export. features is the list of features for the export, is_debug is true for debug builds, path is the target path for the exported project. flags is only used when running a runnable profile, e.g. when using native run on Android.

- _export_end() -> void [virtual]
  Virtual method to be overridden by the user. Called when the export is finished.

- _export_file(path: String, type: String, features: PackedStringArray) -> void [virtual]
  Virtual method to be overridden by the user. Called for each exported file before _customize_resource() and _customize_scene(). The arguments can be used to identify the file. path is the path of the file, type is the Resource represented by the file (e.g. PackedScene), and features is the list of features for the export. Calling skip() inside this callback will make the file not included in the export.

- _get_android_dependencies(platform: EditorExportPlatform, debug: bool) -> PackedStringArray [virtual const]
  Virtual method to be overridden by the user. This is called to retrieve the set of Android dependencies provided by this plugin. Each returned Android dependency should have the format of an Android remote binary dependency: org.godot.example:my-plugin:0.0.0 For more information see [Android documentation on dependencies](https://developer.android.com/build/dependencies?agpversion=4.1#dependency-types). **Note:** Only supported on Android and requires EditorExportPlatformAndroid.gradle_build/use_gradle_build to be enabled.

- _get_android_dependencies_maven_repos(platform: EditorExportPlatform, debug: bool) -> PackedStringArray [virtual const]
  Virtual method to be overridden by the user. This is called to retrieve the URLs of Maven repositories for the set of Android dependencies provided by this plugin. For more information see [Gradle documentation on dependency management](https://docs.gradle.org/current/userguide/dependency_management.html#sec:maven_repo). **Note:** Google's Maven repo and the Maven Central repo are already included by default. **Note:** Only supported on Android and requires EditorExportPlatformAndroid.gradle_build/use_gradle_build to be enabled.

- _get_android_libraries(platform: EditorExportPlatform, debug: bool) -> PackedStringArray [virtual const]
  Virtual method to be overridden by the user. This is called to retrieve the local paths of the Android libraries archive (AAR) files provided by this plugin. **Note:** Relative paths **must** be relative to Godot's res://addons/ directory. For example, an AAR file located under res://addons/hello_world_plugin/HelloWorld.release.aar can be returned as an absolute path using res://addons/hello_world_plugin/HelloWorld.release.aar or a relative path using hello_world_plugin/HelloWorld.release.aar. **Note:** Only supported on Android and requires EditorExportPlatformAndroid.gradle_build/use_gradle_build to be enabled.

- _get_android_manifest_activity_element_contents(platform: EditorExportPlatform, debug: bool) -> String [virtual const]
  Virtual method to be overridden by the user. This is used at export time to update the contents of the activity element in the generated Android manifest. **Note:** Only supported on Android and requires EditorExportPlatformAndroid.gradle_build/use_gradle_build to be enabled.

- _get_android_manifest_application_element_contents(platform: EditorExportPlatform, debug: bool) -> String [virtual const]
  Virtual method to be overridden by the user. This is used at export time to update the contents of the application element in the generated Android manifest. **Note:** Only supported on Android and requires EditorExportPlatformAndroid.gradle_build/use_gradle_build to be enabled.

- _get_android_manifest_element_contents(platform: EditorExportPlatform, debug: bool) -> String [virtual const]
  Virtual method to be overridden by the user. This is used at export time to update the contents of the manifest element in the generated Android manifest. **Note:** Only supported on Android and requires EditorExportPlatformAndroid.gradle_build/use_gradle_build to be enabled.

- _get_customization_configuration_hash() -> int [virtual required const]
  Return a hash based on the configuration passed (for both scenes and resources). This helps keep separate caches for separate export configurations. Implementing this method is required if _begin_customize_resources() returns true.

- _get_export_features(platform: EditorExportPlatform, debug: bool) -> PackedStringArray [virtual const]
  Return a PackedStringArray of additional features this preset, for the given platform, should have.

- _get_export_option_visibility(platform: EditorExportPlatform, option: String) -> bool [virtual const]
  Validates option and returns the visibility for the specified platform. The default implementation returns true for all options.

- _get_export_option_warning(platform: EditorExportPlatform, option: String) -> String [virtual const]
  Check the requirements for the given option and return a non-empty warning string if they are not met. **Note:** Use get_option() to check the value of the export options.

- _get_export_options(platform: EditorExportPlatform) -> Dictionary[] [virtual const]
  Return a list of export options that can be configured for this export plugin. Each element in the return value is a Dictionary with the following keys: - option: A dictionary with the structure documented by Object.get_property_list(), but all keys are optional. - default_value: The default value for this option. - update_visibility: An optional boolean value. If set to true, the preset will emit Object.property_list_changed when the option is changed.

- _get_export_options_overrides(platform: EditorExportPlatform) -> Dictionary [virtual const]
  Return a Dictionary of override values for export options, that will be used instead of user-provided values. Overridden options will be hidden from the user interface.


```
  class MyExportPlugin extends EditorExportPlugin:
      func _get_name() -> String:
          return "MyExportPlugin"

      func _supports_platform(platform) -> bool:
          if platform is EditorExportPlatformPC:
              # Run on all desktop platforms including Windows, MacOS and Linux.
              return true
          return false

      func _get_export_options_overrides(platform) -> Dictionary:
          # Override "Embed PCK" to always be enabled.
          return {
              "binary_format/embed_pck": true,
          }

```

- _get_name() -> String [virtual required const]
  Return the name identifier of this plugin (for future identification by the exporter). The plugins are sorted by name before exporting. Implementing this method is required.

- _should_update_export_options(platform: EditorExportPlatform) -> bool [virtual const]
  Return true if the result of _get_export_options() has changed and the export options of the preset corresponding to platform should be updated.

- _supports_platform(platform: EditorExportPlatform) -> bool [virtual const]
  Return true if the plugin supports the given platform.

- _update_android_prebuilt_manifest(platform: EditorExportPlatform, manifest_data: PackedByteArray) -> PackedByteArray [virtual const]
  Provide access to the Android prebuilt manifest and allows the plugin to modify it if needed. Implementers of this virtual method should take the binary manifest data from manifest_data, copy it, modify it, and then return it with the modifications. If no modifications are needed, then an empty PackedByteArray should be returned.

- add_apple_embedded_platform_bundle_file(path: String) -> void
  Adds an Apple embedded platform bundle file from the given path to the exported project.

- add_apple_embedded_platform_cpp_code(code: String) -> void
  Adds C++ code to the Apple embedded platform export. The final code is created from the code appended by each active export plugin.

- add_apple_embedded_platform_embedded_framework(path: String) -> void
  Adds a dynamic library (*.dylib, *.framework) to the Linking Phase in the Apple embedded platform's Xcode project and embeds it into the resulting binary. **Note:** For static libraries (*.a), this works in the same way as add_apple_embedded_platform_framework(). **Note:** This method should not be used for System libraries as they are already present on the device.

- add_apple_embedded_platform_framework(path: String) -> void
  Adds a static library (*.a) or a dynamic library (*.dylib, *.framework) to the Linking Phase to the Apple embedded platform's Xcode project.

- add_apple_embedded_platform_linker_flags(flags: String) -> void
  Adds linker flags for the Apple embedded platform export.

- add_apple_embedded_platform_plist_content(plist_content: String) -> void
  Adds additional fields to the Apple embedded platform's project Info.plist file.

- add_apple_embedded_platform_project_static_lib(path: String) -> void
  Adds a static library from the given path to the Apple embedded platform project.

- add_file(path: String, file: PackedByteArray, remap: bool) -> void
  Adds a custom file to be exported. path is the virtual path that can be used to load the file, file is the binary data of the file. When called inside _export_file() and remap is true, the current file will not be exported, but instead remapped to this custom file. remap is ignored when called in other places. file will not be imported, so consider using _customize_resource() to remap imported resources.

- add_ios_bundle_file(path: String) -> void
  Adds an iOS bundle file from the given path to the exported project.

- add_ios_cpp_code(code: String) -> void
  Adds C++ code to the iOS export. The final code is created from the code appended by each active export plugin.

- add_ios_embedded_framework(path: String) -> void
  Adds a dynamic library (*.dylib, *.framework) to Linking Phase in iOS's Xcode project and embeds it into resulting binary. **Note:** For static libraries (*.a), this works the in same way as add_apple_embedded_platform_framework(). **Note:** This method should not be used for System libraries as they are already present on the device.

- add_ios_framework(path: String) -> void
  Adds a static library (*.a) or a dynamic library (*.dylib, *.framework) to the Linking Phase to the iOS Xcode project.

- add_ios_linker_flags(flags: String) -> void
  Adds linker flags for the iOS export.

- add_ios_plist_content(plist_content: String) -> void
  Adds additional fields to the iOS project Info.plist file.

- add_ios_project_static_lib(path: String) -> void
  Adds a static library from the given path to the iOS project.

- add_macos_plugin_file(path: String) -> void
  Adds file or directory matching path to PlugIns directory of macOS app bundle. **Note:** This is useful only for macOS exports.

- add_shared_object(path: String, tags: PackedStringArray, target: String) -> void
  Adds a shared object or a directory containing only shared objects with the given tags and destination path. **Note:** In case of macOS exports, those shared objects will be added to Frameworks directory of app bundle. In case of a directory code-sign will error if you place non code object in directory.

- get_export_platform() -> EditorExportPlatform [const]
  Returns currently used export platform.

- get_export_preset() -> EditorExportPreset [const]
  Returns currently used export preset.

- get_option(name: StringName) -> Variant [const]
  Returns the current value of an export option supplied by _get_export_options().

- skip() -> void
  To be called inside _export_file(). Skips the current file, so it's not included in the export.
