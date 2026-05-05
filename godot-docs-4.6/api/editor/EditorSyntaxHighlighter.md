# EditorSyntaxHighlighter

## Meta

- Name: EditorSyntaxHighlighter
- Source: EditorSyntaxHighlighter.xml
- Inherits: SyntaxHighlighter
- Inheritance Chain: EditorSyntaxHighlighter -> SyntaxHighlighter -> Resource -> RefCounted -> Object

## Brief Description

Base class for SyntaxHighlighter used by the ScriptEditor.

## Description

Base class that all SyntaxHighlighters used by the ScriptEditor extend from. Add a syntax highlighter to an individual script by calling ScriptEditorBase.add_syntax_highlighter(). To apply to all scripts on open, call ScriptEditor.register_syntax_highlighter().

## Quick Reference

```
[methods]
_create() -> EditorSyntaxHighlighter [virtual const]
_get_name() -> String [virtual const]
_get_supported_languages() -> PackedStringArray [virtual const]
```

## Methods

- _create() -> EditorSyntaxHighlighter [virtual const]
  Virtual method which creates a new instance of the syntax highlighter.

- _get_name() -> String [virtual const]
  Virtual method which can be overridden to return the syntax highlighter name.

- _get_supported_languages() -> PackedStringArray [virtual const]
  Virtual method which can be overridden to return the supported language names.
