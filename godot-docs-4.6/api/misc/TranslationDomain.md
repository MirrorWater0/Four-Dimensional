# TranslationDomain

## Meta

- Name: TranslationDomain
- Source: TranslationDomain.xml
- Inherits: RefCounted
- Inheritance Chain: TranslationDomain -> RefCounted -> Object

## Brief Description

A self-contained collection of Translation resources.

## Description

TranslationDomain is a self-contained collection of Translation resources. Translations can be added to or removed from it. If you're working with the main translation domain, it is more convenient to use the wrap methods on TranslationServer.

## Quick Reference

```
[methods]
add_translation(translation: Translation) -> void
clear() -> void
find_translations(locale: String, exact: bool) -> Translation[] [const]
get_locale_override() -> String [const]
get_translation_object(locale: String) -> Translation [const]
get_translations() -> Translation[] [const]
has_translation(translation: Translation) -> bool [const]
has_translation_for_locale(locale: String, exact: bool) -> bool [const]
pseudolocalize(message: StringName) -> StringName [const]
remove_translation(translation: Translation) -> void
set_locale_override(locale: String) -> void
translate(message: StringName, context: StringName = &"") -> StringName [const]
translate_plural(message: StringName, message_plural: StringName, n: int, context: StringName = &"") -> StringName [const]

[properties]
enabled: bool = true
pseudolocalization_accents_enabled: bool = true
pseudolocalization_double_vowels_enabled: bool = false
pseudolocalization_enabled: bool = false
pseudolocalization_expansion_ratio: float = 0.0
pseudolocalization_fake_bidi_enabled: bool = false
pseudolocalization_override_enabled: bool = false
pseudolocalization_prefix: String = "["
pseudolocalization_skip_placeholders_enabled: bool = true
pseudolocalization_suffix: String = "]"
```

## Methods

- add_translation(translation: Translation) -> void
  Adds a translation.

- clear() -> void
  Removes all translations.

- find_translations(locale: String, exact: bool) -> Translation[] [const]
  Returns the Translation instances that match locale (see TranslationServer.compare_locales()). If exact is true, only instances whose locale exactly equals locale will be returned.

- get_locale_override() -> String [const]
  Returns the locale override of the domain. Returns an empty string if locale override is disabled.

- get_translation_object(locale: String) -> Translation [const]
  Returns the Translation instance that best matches locale. Returns null if there are no matches.

- get_translations() -> Translation[] [const]
  Returns all available Translation instances as added by add_translation().

- has_translation(translation: Translation) -> bool [const]
  Returns true if this translation domain contains the given translation.

- has_translation_for_locale(locale: String, exact: bool) -> bool [const]
  Returns true if there are any Translation instances that match locale (see TranslationServer.compare_locales()). If exact is true, only instances whose locale exactly equals locale are considered.

- pseudolocalize(message: StringName) -> StringName [const]
  Returns the pseudolocalized string based on the message passed in.

- remove_translation(translation: Translation) -> void
  Removes the given translation.

- set_locale_override(locale: String) -> void
  Sets the locale override of the domain. If locale is an empty string, locale override is disabled. Otherwise, locale will be standardized to match known locales (e.g. en-US would be matched to en_US). **Note:** Calling this method does not automatically update texts in the scene tree. Please propagate the MainLoop.NOTIFICATION_TRANSLATION_CHANGED signal manually.

- translate(message: StringName, context: StringName = &"") -> StringName [const]
  Returns the current locale's translation for the given message and context.

- translate_plural(message: StringName, message_plural: StringName, n: int, context: StringName = &"") -> StringName [const]
  Returns the current locale's translation for the given message, plural message and context. The number n is the number or quantity of the plural object. It will be used to guide the translation system to fetch the correct plural form for the selected language.

## Properties

- enabled: bool = true [set set_enabled; get is_enabled]
  If true, translation is enabled. Otherwise, translate() and translate_plural() will return the input message unchanged regardless of the current locale.

- pseudolocalization_accents_enabled: bool = true [set set_pseudolocalization_accents_enabled; get is_pseudolocalization_accents_enabled]
  Replace all characters with their accented variants during pseudolocalization. **Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the MainLoop.NOTIFICATION_TRANSLATION_CHANGED notification manually after you have finished modifying pseudolocalization related options.

- pseudolocalization_double_vowels_enabled: bool = false [set set_pseudolocalization_double_vowels_enabled; get is_pseudolocalization_double_vowels_enabled]
  Double vowels in strings during pseudolocalization to simulate the lengthening of text due to localization. **Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the MainLoop.NOTIFICATION_TRANSLATION_CHANGED notification manually after you have finished modifying pseudolocalization related options.

- pseudolocalization_enabled: bool = false [set set_pseudolocalization_enabled; get is_pseudolocalization_enabled]
  If true, enables pseudolocalization for the project. This can be used to spot untranslatable strings or layout issues that may occur once the project is localized to languages that have longer strings than the source language. **Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the MainLoop.NOTIFICATION_TRANSLATION_CHANGED notification manually after you have finished modifying pseudolocalization related options.

- pseudolocalization_expansion_ratio: float = 0.0 [set set_pseudolocalization_expansion_ratio; get get_pseudolocalization_expansion_ratio]
  The expansion ratio to use during pseudolocalization. A value of 0.3 is sufficient for most practical purposes, and will increase the length of each string by 30%. **Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the MainLoop.NOTIFICATION_TRANSLATION_CHANGED notification manually after you have finished modifying pseudolocalization related options.

- pseudolocalization_fake_bidi_enabled: bool = false [set set_pseudolocalization_fake_bidi_enabled; get is_pseudolocalization_fake_bidi_enabled]
  If true, emulate bidirectional (right-to-left) text when pseudolocalization is enabled. This can be used to spot issues with RTL layout and UI mirroring that will crop up if the project is localized to RTL languages such as Arabic or Hebrew. **Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the MainLoop.NOTIFICATION_TRANSLATION_CHANGED notification manually after you have finished modifying pseudolocalization related options.

- pseudolocalization_override_enabled: bool = false [set set_pseudolocalization_override_enabled; get is_pseudolocalization_override_enabled]
  Replace all characters in the string with *. Useful for finding non-localizable strings. **Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the MainLoop.NOTIFICATION_TRANSLATION_CHANGED notification manually after you have finished modifying pseudolocalization related options.

- pseudolocalization_prefix: String = "[" [set set_pseudolocalization_prefix; get get_pseudolocalization_prefix]
  Prefix that will be prepended to the pseudolocalized string. **Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the MainLoop.NOTIFICATION_TRANSLATION_CHANGED notification manually after you have finished modifying pseudolocalization related options.

- pseudolocalization_skip_placeholders_enabled: bool = true [set set_pseudolocalization_skip_placeholders_enabled; get is_pseudolocalization_skip_placeholders_enabled]
  Skip placeholders for string formatting like %s or %f during pseudolocalization. Useful to identify strings which need additional control characters to display correctly. **Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the MainLoop.NOTIFICATION_TRANSLATION_CHANGED notification manually after you have finished modifying pseudolocalization related options.

- pseudolocalization_suffix: String = "]" [set set_pseudolocalization_suffix; get get_pseudolocalization_suffix]
  Suffix that will be appended to the pseudolocalized string. **Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the MainLoop.NOTIFICATION_TRANSLATION_CHANGED notification manually after you have finished modifying pseudolocalization related options.
