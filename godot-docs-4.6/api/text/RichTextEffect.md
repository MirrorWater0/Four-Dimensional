# RichTextEffect

## Meta

- Name: RichTextEffect
- Source: RichTextEffect.xml
- Inherits: Resource
- Inheritance Chain: RichTextEffect -> Resource -> RefCounted -> Object

## Brief Description

A custom effect for a RichTextLabel.

## Description

A custom effect for a RichTextLabel, which can be loaded in the RichTextLabel inspector or using RichTextLabel.install_effect(). **Note:** For a RichTextEffect to be usable, a BBCode tag must be defined as a member variable called bbcode in the script.

[gdscript skip-lint] # The RichTextEffect will be usable like this: `exampleSome text[/example]` var bbcode = "example"

```
[csharp skip-lint]
// The RichTextEffect will be usable like this: `exampleSome text[/example]`
string bbcode = "example";
```

**Note:** As soon as a RichTextLabel contains at least one RichTextEffect, it will continuously process the effect unless the project is paused. This may impact battery life negatively.

## Quick Reference

```
[methods]
_process_custom_fx(char_fx: CharFXTransform) -> bool [virtual const]
```

## Tutorials

- [BBCode in RichTextLabel]($DOCS_URL/tutorials/ui/bbcode_in_richtextlabel.html)
- [RichTextEffect test project (third-party)](https://github.com/Eoin-ONeill-Yokai/Godot-Rich-Text-Effect-Test-Project)

## Methods

- _process_custom_fx(char_fx: CharFXTransform) -> bool [virtual const]
  Override this method to modify properties in char_fx. The method must return true if the character could be transformed successfully. If the method returns false, it will skip transformation to avoid displaying broken text.
