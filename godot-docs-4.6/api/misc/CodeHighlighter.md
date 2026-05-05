# CodeHighlighter

## Meta

- Name: CodeHighlighter
- Source: CodeHighlighter.xml
- Inherits: SyntaxHighlighter
- Inheritance Chain: CodeHighlighter -> SyntaxHighlighter -> Resource -> RefCounted -> Object

## Brief Description

A syntax highlighter intended for code.

## Description

By adjusting various properties of this resource, you can change the colors of strings, comments, numbers, and other text patterns inside a TextEdit control.

## Quick Reference

```
[methods]
add_color_region(start_key: String, end_key: String, color: Color, line_only: bool = false) -> void
add_keyword_color(keyword: String, color: Color) -> void
add_member_keyword_color(member_keyword: String, color: Color) -> void
clear_color_regions() -> void
clear_keyword_colors() -> void
clear_member_keyword_colors() -> void
get_keyword_color(keyword: String) -> Color [const]
get_member_keyword_color(member_keyword: String) -> Color [const]
has_color_region(start_key: String) -> bool [const]
has_keyword_color(keyword: String) -> bool [const]
has_member_keyword_color(member_keyword: String) -> bool [const]
remove_color_region(start_key: String) -> void
remove_keyword_color(keyword: String) -> void
remove_member_keyword_color(member_keyword: String) -> void

[properties]
color_regions: Dictionary = {}
function_color: Color = Color(0, 0, 0, 1)
keyword_colors: Dictionary = {}
member_keyword_colors: Dictionary = {}
member_variable_color: Color = Color(0, 0, 0, 1)
number_color: Color = Color(0, 0, 0, 1)
symbol_color: Color = Color(0, 0, 0, 1)
```

## Methods

- add_color_region(start_key: String, end_key: String, color: Color, line_only: bool = false) -> void
  Adds a color region (such as for comments or strings) from start_key to end_key. Both keys should be symbols, and start_key must not be shared with other delimiters. If line_only is true or end_key is an empty String, the region does not carry over to the next line.

- add_keyword_color(keyword: String, color: Color) -> void
  Sets the color for a keyword. The keyword cannot contain any symbols except '_'.

- add_member_keyword_color(member_keyword: String, color: Color) -> void
  Sets the color for a member keyword. The member keyword cannot contain any symbols except '_'. It will not be highlighted if preceded by a '.'.

- clear_color_regions() -> void
  Removes all color regions.

- clear_keyword_colors() -> void
  Removes all keywords.

- clear_member_keyword_colors() -> void
  Removes all member keywords.

- get_keyword_color(keyword: String) -> Color [const]
  Returns the color for a keyword.

- get_member_keyword_color(member_keyword: String) -> Color [const]
  Returns the color for a member keyword.

- has_color_region(start_key: String) -> bool [const]
  Returns true if the start key exists, else false.

- has_keyword_color(keyword: String) -> bool [const]
  Returns true if the keyword exists, else false.

- has_member_keyword_color(member_keyword: String) -> bool [const]
  Returns true if the member keyword exists, else false.

- remove_color_region(start_key: String) -> void
  Removes the color region that uses that start key.

- remove_keyword_color(keyword: String) -> void
  Removes the keyword.

- remove_member_keyword_color(member_keyword: String) -> void
  Removes the member keyword.

## Properties

- color_regions: Dictionary = {} [set set_color_regions; get get_color_regions]
  Sets the color regions. All existing regions will be removed. The Dictionary key is the region start and end key, separated by a space. The value is the region color.

- function_color: Color = Color(0, 0, 0, 1) [set set_function_color; get get_function_color]
  Sets color for functions. A function is a non-keyword string followed by a '('.

- keyword_colors: Dictionary = {} [set set_keyword_colors; get get_keyword_colors]
  Sets the keyword colors. All existing keywords will be removed. The Dictionary key is the keyword. The value is the keyword color.

- member_keyword_colors: Dictionary = {} [set set_member_keyword_colors; get get_member_keyword_colors]
  Sets the member keyword colors. All existing member keyword will be removed. The Dictionary key is the member keyword. The value is the member keyword color.

- member_variable_color: Color = Color(0, 0, 0, 1) [set set_member_variable_color; get get_member_variable_color]
  Sets color for member variables. A member variable is non-keyword, non-function string proceeded with a '.'.

- number_color: Color = Color(0, 0, 0, 1) [set set_number_color; get get_number_color]
  Sets the color for numbers.

- symbol_color: Color = Color(0, 0, 0, 1) [set set_symbol_color; get get_symbol_color]
  Sets the color for symbols.
