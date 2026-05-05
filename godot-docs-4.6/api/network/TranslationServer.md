# TranslationServer

## Meta

- Name: TranslationServer
- Source: TranslationServer.xml
- Inherits: Object
- Inheritance Chain: TranslationServer -> Object

## Brief Description

The server responsible for language translations.

## Description

The translation server is the API backend that manages all language translations. Translations are stored in TranslationDomains, which can be accessed by name. The most commonly used translation domain is the main translation domain. It always exists and can be accessed using an empty StringName. The translation server provides wrapper methods for accessing the main translation domain directly, without having to fetch the translation domain first. Custom translation domains are mainly for advanced usages like editor plugins. Names starting with godot. are reserved for engine internals.

## Quick Reference

```
[methods]
add_translation(translation: Translation) -> void
clear() -> void
compare_locales(locale_a: String, locale_b: String) -> int [const]
find_translations(locale: String, exact: bool) -> Translation[] [const]
format_number(number: String, locale: String) -> String [const]
get_all_countries() -> PackedStringArray [const]
get_all_languages() -> PackedStringArray [const]
get_all_scripts() -> PackedStringArray [const]
get_country_name(country: String) -> String [const]
get_language_name(language: String) -> String [const]
get_loaded_locales() -> PackedStringArray [const]
get_locale() -> String [const]
get_locale_name(locale: String) -> String [const]
get_or_add_domain(domain: StringName) -> TranslationDomain
get_percent_sign(locale: String) -> String [const]
get_plural_rules(locale: String) -> String [const]
get_script_name(script: String) -> String [const]
get_tool_locale() -> String
get_translation_object(locale: String) -> Translation
get_translations() -> Translation[] [const]
has_domain(domain: StringName) -> bool [const]
has_translation(translation: Translation) -> bool [const]
has_translation_for_locale(locale: String, exact: bool) -> bool [const]
parse_number(number: String, locale: String) -> String [const]
pseudolocalize(message: StringName) -> StringName [const]
reload_pseudolocalization() -> void
remove_domain(domain: StringName) -> void
remove_translation(translation: Translation) -> void
set_locale(locale: String) -> void
standardize_locale(locale: String, add_defaults: bool = false) -> String [const]
translate(message: StringName, context: StringName = &"") -> StringName [const]
translate_plural(message: StringName, plural_message: StringName, n: int, context: StringName = &"") -> StringName [const]

[properties]
pseudolocalization_enabled: bool = false
```

## Tutorials

- [Internationalizing games]($DOCS_URL/tutorials/i18n/internationalizing_games.html)
- [Locales]($DOCS_URL/tutorials/i18n/locales.html)

## Methods

- add_translation(translation: Translation) -> void
  Adds a translation to the main translation domain.

- clear() -> void
  Removes all translations from the main translation domain.

- compare_locales(locale_a: String, locale_b: String) -> int [const]
  Compares two locales and returns a similarity score between 0 (no match) and 10 (full match).

- find_translations(locale: String, exact: bool) -> Translation[] [const]
  Returns the Translation instances in the main translation domain that match locale (see compare_locales()). If exact is true, only instances whose locale exactly equals locale will be returned.

- format_number(number: String, locale: String) -> String [const]
  Converts a number from Western Arabic (0..9) to the numeral system used in the given locale.

- get_all_countries() -> PackedStringArray [const]
  Returns an array of known country codes.

- get_all_languages() -> PackedStringArray [const]
  Returns array of known language codes.

- get_all_scripts() -> PackedStringArray [const]
  Returns an array of known script codes.

- get_country_name(country: String) -> String [const]
  Returns a readable country name for the country code.

- get_language_name(language: String) -> String [const]
  Returns a readable language name for the language code.

- get_loaded_locales() -> PackedStringArray [const]
  Returns an array of all loaded locales of the project.

- get_locale() -> String [const]
  Returns the current locale of the project. See also OS.get_locale() and OS.get_locale_language() to query the locale of the user system.

- get_locale_name(locale: String) -> String [const]
  Returns a locale's language and its variant (e.g. "en_US" would return "English (United States)").

- get_or_add_domain(domain: StringName) -> TranslationDomain
  Returns the translation domain with the specified name. An empty translation domain will be created and added if it does not exist.

- get_percent_sign(locale: String) -> String [const]
  Returns the percent sign used in the given locale.

- get_plural_rules(locale: String) -> String [const]
  Returns the default plural rules for the locale.

- get_script_name(script: String) -> String [const]
  Returns a readable script name for the script code.

- get_tool_locale() -> String
  Returns the current locale of the editor. **Note:** When called from an exported project returns the same value as get_locale().

- get_translation_object(locale: String) -> Translation
  Returns the Translation instance that best matches locale in the main translation domain. Returns null if there are no matches.

- get_translations() -> Translation[] [const]
  Returns all available Translation instances in the main translation domain as added by add_translation().

- has_domain(domain: StringName) -> bool [const]
  Returns true if a translation domain with the specified name exists.

- has_translation(translation: Translation) -> bool [const]
  Returns true if the main translation domain contains the given translation.

- has_translation_for_locale(locale: String, exact: bool) -> bool [const]
  Returns true if there are any Translation instances in the main translation domain that match locale (see compare_locales()). If exact is true, only instances whose locale exactly equals locale are considered.

- parse_number(number: String, locale: String) -> String [const]
  Converts number from the numeral system used in the given locale to Western Arabic (0..9).

- pseudolocalize(message: StringName) -> StringName [const]
  Returns the pseudolocalized string based on the message passed in. **Note:** This method always uses the main translation domain.

- reload_pseudolocalization() -> void
  Reparses the pseudolocalization options and reloads the translation for the main translation domain.

- remove_domain(domain: StringName) -> void
  Removes the translation domain with the specified name. **Note:** Trying to remove the main translation domain is an error.

- remove_translation(translation: Translation) -> void
  Removes the given translation from the main translation domain.

- set_locale(locale: String) -> void
  Sets the locale of the project. The locale string will be standardized to match known locales (e.g. en-US would be matched to en_US). If translations have been loaded beforehand for the new locale, they will be applied.

- standardize_locale(locale: String, add_defaults: bool = false) -> String [const]
  Returns a locale string standardized to match known locales (e.g. en-US would be matched to en_US). If add_defaults is true, the locale may have a default script or country added.

- translate(message: StringName, context: StringName = &"") -> StringName [const]
  Returns the current locale's translation for the given message and context. **Note:** This method always uses the main translation domain.

- translate_plural(message: StringName, plural_message: StringName, n: int, context: StringName = &"") -> StringName [const]
  Returns the current locale's translation for the given message, plural message and context. The number n is the number or quantity of the plural object. It will be used to guide the translation system to fetch the correct plural form for the selected language. **Note:** This method always uses the main translation domain.

## Properties

- pseudolocalization_enabled: bool = false [set set_pseudolocalization_enabled; get is_pseudolocalization_enabled]
  If true, enables the use of pseudolocalization on the main translation domain. See ProjectSettings.internationalization/pseudolocalization/use_pseudolocalization for details.
