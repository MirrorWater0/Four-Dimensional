# OptimizedTranslation

## Meta

- Name: OptimizedTranslation
- Source: OptimizedTranslation.xml
- Inherits: Translation
- Inheritance Chain: OptimizedTranslation -> Translation -> Resource -> RefCounted -> Object

## Brief Description

An optimized translation.

## Description

An optimized translation. Uses real-time compressed translations, which results in very small dictionaries. This class does not store the untranslated strings for optimization purposes. Therefore, Translation.get_message_list() always returns an empty array, and Translation.get_message_count() always returns 0.

## Quick Reference

```
[methods]
generate(from: Translation) -> void
```

## Methods

- generate(from: Translation) -> void
  Generates and sets an optimized translation from the given Translation resource. **Note:** Messages in from should not use context or plural forms. **Note:** This method is intended to be used in the editor. It does nothing when called from an exported project.
