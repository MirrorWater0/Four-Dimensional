# CodeEdit

## Meta

- Name: CodeEdit
- Source: CodeEdit.xml
- Inherits: TextEdit
- Inheritance Chain: CodeEdit -> TextEdit -> Control -> CanvasItem -> Node -> Object

## Brief Description

A multiline text editor designed for editing code.

## Description

CodeEdit is a specialized TextEdit designed for editing plain text code files. It has many features commonly found in code editors such as line numbers, line folding, code completion, indent management, and string/comment management. **Note:** Regardless of locale, CodeEdit will by default always use left-to-right text direction to correctly display source code.

## Quick Reference

```
[methods]
_confirm_code_completion(replace: bool) -> void [virtual]
_filter_code_completion_candidates(candidates: Dictionary[]) -> Dictionary[] [virtual const]
_request_code_completion(force: bool) -> void [virtual]
add_auto_brace_completion_pair(start_key: String, end_key: String) -> void
add_code_completion_option(type: int (CodeEdit.CodeCompletionKind), display_text: String, insert_text: String, text_color: Color = Color(1, 1, 1, 1), icon: Resource = null, value: Variant = null, location: int = 1024) -> void
add_comment_delimiter(start_key: String, end_key: String, line_only: bool = false) -> void
add_string_delimiter(start_key: String, end_key: String, line_only: bool = false) -> void
can_fold_line(line: int) -> bool [const]
cancel_code_completion() -> void
clear_bookmarked_lines() -> void
clear_breakpointed_lines() -> void
clear_comment_delimiters() -> void
clear_executing_lines() -> void
clear_string_delimiters() -> void
confirm_code_completion(replace: bool = false) -> void
convert_indent(from_line: int = -1, to_line: int = -1) -> void
create_code_region() -> void
delete_lines() -> void
do_indent() -> void
duplicate_lines() -> void
duplicate_selection() -> void
fold_all_lines() -> void
fold_line(line: int) -> void
get_auto_brace_completion_close_key(open_key: String) -> String [const]
get_bookmarked_lines() -> PackedInt32Array [const]
get_breakpointed_lines() -> PackedInt32Array [const]
get_code_completion_option(index: int) -> Dictionary [const]
get_code_completion_options() -> Dictionary[] [const]
get_code_completion_selected_index() -> int [const]
get_code_region_end_tag() -> String [const]
get_code_region_start_tag() -> String [const]
get_delimiter_end_key(delimiter_index: int) -> String [const]
get_delimiter_end_position(line: int, column: int) -> Vector2 [const]
get_delimiter_start_key(delimiter_index: int) -> String [const]
get_delimiter_start_position(line: int, column: int) -> Vector2 [const]
get_executing_lines() -> PackedInt32Array [const]
get_folded_lines() -> int[] [const]
get_text_for_code_completion() -> String [const]
get_text_for_symbol_lookup() -> String [const]
get_text_with_cursor_char(line: int, column: int) -> String [const]
has_auto_brace_completion_close_key(close_key: String) -> bool [const]
has_auto_brace_completion_open_key(open_key: String) -> bool [const]
has_comment_delimiter(start_key: String) -> bool [const]
has_string_delimiter(start_key: String) -> bool [const]
indent_lines() -> void
is_in_comment(line: int, column: int = -1) -> int [const]
is_in_string(line: int, column: int = -1) -> int [const]
is_line_bookmarked(line: int) -> bool [const]
is_line_breakpointed(line: int) -> bool [const]
is_line_code_region_end(line: int) -> bool [const]
is_line_code_region_start(line: int) -> bool [const]
is_line_executing(line: int) -> bool [const]
is_line_folded(line: int) -> bool [const]
move_lines_down() -> void
move_lines_up() -> void
remove_comment_delimiter(start_key: String) -> void
remove_string_delimiter(start_key: String) -> void
request_code_completion(force: bool = false) -> void
set_code_completion_selected_index(index: int) -> void
set_code_hint(code_hint: String) -> void
set_code_hint_draw_below(draw_below: bool) -> void
set_code_region_tags(start: String = "region", end: String = "endregion") -> void
set_line_as_bookmarked(line: int, bookmarked: bool) -> void
set_line_as_breakpoint(line: int, breakpointed: bool) -> void
set_line_as_executing(line: int, executing: bool) -> void
set_symbol_lookup_word_as_valid(valid: bool) -> void
toggle_foldable_line(line: int) -> void
toggle_foldable_lines_at_carets() -> void
unfold_all_lines() -> void
unfold_line(line: int) -> void
unindent_lines() -> void
update_code_completion_options(force: bool) -> void

[properties]
auto_brace_completion_enabled: bool = false
auto_brace_completion_highlight_matching: bool = false
auto_brace_completion_pairs: Dictionary = { "\"": "\"", "'": "'", "(": ")", "[": "]", "{": "}" }
code_completion_enabled: bool = false
code_completion_prefixes: String[] = []
delimiter_comments: String[] = []
delimiter_strings: String[] = ["' '", "\" \""]
gutters_draw_bookmarks: bool = false
gutters_draw_breakpoints_gutter: bool = false
gutters_draw_executing_lines: bool = false
gutters_draw_fold_gutter: bool = false
gutters_draw_line_numbers: bool = false
gutters_line_numbers_min_digits: int = 3
gutters_zero_pad_line_numbers: bool = false
indent_automatic: bool = false
indent_automatic_prefixes: String[] = [":", "{", "[", "("]
indent_size: int = 4
indent_use_spaces: bool = false
layout_direction: int (Control.LayoutDirection) = 2
line_folding: bool = false
line_length_guidelines: int[] = []
symbol_lookup_on_click: bool = false
symbol_tooltip_on_hover: bool = false
text_direction: int (Control.TextDirection) = 1
```

## Methods

- _confirm_code_completion(replace: bool) -> void [virtual]
  Override this method to define how the selected entry should be inserted. If replace is true, any existing text should be replaced.

- _filter_code_completion_candidates(candidates: Dictionary[]) -> Dictionary[] [virtual const]
  Override this method to define what items in candidates should be displayed. Both candidates and the return is an Array of Dictionary, see get_code_completion_option() for Dictionary content.

- _request_code_completion(force: bool) -> void [virtual]
  Override this method to define what happens when the user requests code completion. If force is true, any checks should be bypassed.

- add_auto_brace_completion_pair(start_key: String, end_key: String) -> void
  Adds a brace pair. Both the start and end keys must be symbols. Only the start key has to be unique.

- add_code_completion_option(type: int (CodeEdit.CodeCompletionKind), display_text: String, insert_text: String, text_color: Color = Color(1, 1, 1, 1), icon: Resource = null, value: Variant = null, location: int = 1024) -> void
  Submits an item to the queue of potential candidates for the autocomplete menu. Call update_code_completion_options() to update the list. location indicates location of the option relative to the location of the code completion query. See CodeEdit.CodeCompletionLocation for how to set this value. **Note:** This list will replace all current candidates.

- add_comment_delimiter(start_key: String, end_key: String, line_only: bool = false) -> void
  Adds a comment delimiter from start_key to end_key. Both keys should be symbols, and start_key must not be shared with other delimiters. If line_only is true or end_key is an empty String, the region does not carry over to the next line.

- add_string_delimiter(start_key: String, end_key: String, line_only: bool = false) -> void
  Defines a string delimiter from start_key to end_key. Both keys should be symbols, and start_key must not be shared with other delimiters. If line_only is true or end_key is an empty String, the region does not carry over to the next line.

- can_fold_line(line: int) -> bool [const]
  Returns true if the given line is foldable. A line is foldable if it is the start of a valid code region (see get_code_region_start_tag()), if it is the start of a comment or string block, or if the next non-empty line is more indented (see TextEdit.get_indent_level()).

- cancel_code_completion() -> void
  Cancels the autocomplete menu.

- clear_bookmarked_lines() -> void
  Clears all bookmarked lines.

- clear_breakpointed_lines() -> void
  Clears all breakpointed lines.

- clear_comment_delimiters() -> void
  Removes all comment delimiters.

- clear_executing_lines() -> void
  Clears all executed lines.

- clear_string_delimiters() -> void
  Removes all string delimiters.

- confirm_code_completion(replace: bool = false) -> void
  Inserts the selected entry into the text. If replace is true, any existing text is replaced rather than merged.

- convert_indent(from_line: int = -1, to_line: int = -1) -> void
  Converts the indents of lines between from_line and to_line to tabs or spaces as set by indent_use_spaces. Values of -1 convert the entire text.

- create_code_region() -> void
  Creates a new code region with the selection. At least one single line comment delimiter have to be defined (see add_comment_delimiter()). A code region is a part of code that is highlighted when folded and can help organize your script. Code region start and end tags can be customized (see set_code_region_tags()). Code regions are delimited using start and end tags (respectively region and endregion by default) preceded by one line comment delimiter. (eg. #region and #endregion)

- delete_lines() -> void
  Deletes all lines that are selected or have a caret on them.

- do_indent() -> void
  If there is no selection, indentation is inserted at the caret. Otherwise, the selected lines are indented like indent_lines(). Equivalent to the ProjectSettings.input/ui_text_indent action. The indentation characters used depend on indent_use_spaces and indent_size.

- duplicate_lines() -> void
  Duplicates all lines currently selected with any caret. Duplicates the entire line beneath the current one no matter where the caret is within the line.

- duplicate_selection() -> void
  Duplicates all selected text and duplicates all lines with a caret on them.

- fold_all_lines() -> void
  Folds all lines that are possible to be folded (see can_fold_line()).

- fold_line(line: int) -> void
  Folds the given line, if possible (see can_fold_line()).

- get_auto_brace_completion_close_key(open_key: String) -> String [const]
  Gets the matching auto brace close key for open_key.

- get_bookmarked_lines() -> PackedInt32Array [const]
  Gets all bookmarked lines.

- get_breakpointed_lines() -> PackedInt32Array [const]
  Gets all breakpointed lines.

- get_code_completion_option(index: int) -> Dictionary [const]
  Gets the completion option at index. The return Dictionary has the following key-values: kind: CodeCompletionKind display_text: Text that is shown on the autocomplete menu. insert_text: Text that is to be inserted when this item is selected. font_color: Color of the text on the autocomplete menu. icon: Icon to draw on the autocomplete menu. default_value: Value of the symbol.

- get_code_completion_options() -> Dictionary[] [const]
  Gets all completion options, see get_code_completion_option() for return content.

- get_code_completion_selected_index() -> int [const]
  Gets the index of the current selected completion option.

- get_code_region_end_tag() -> String [const]
  Returns the code region end tag (without comment delimiter).

- get_code_region_start_tag() -> String [const]
  Returns the code region start tag (without comment delimiter).

- get_delimiter_end_key(delimiter_index: int) -> String [const]
  Gets the end key for a string or comment region index.

- get_delimiter_end_position(line: int, column: int) -> Vector2 [const]
  If line column is in a string or comment, returns the end position of the region. If not or no end could be found, both Vector2 values will be -1.

- get_delimiter_start_key(delimiter_index: int) -> String [const]
  Gets the start key for a string or comment region index.

- get_delimiter_start_position(line: int, column: int) -> Vector2 [const]
  If line column is in a string or comment, returns the start position of the region. If not or no start could be found, both Vector2 values will be -1.

- get_executing_lines() -> PackedInt32Array [const]
  Gets all executing lines.

- get_folded_lines() -> int[] [const]
  Returns all lines that are currently folded.

- get_text_for_code_completion() -> String [const]
  Returns the full text with char 0xFFFF at the caret location.

- get_text_for_symbol_lookup() -> String [const]
  Returns the full text with char 0xFFFF at the cursor location.

- get_text_with_cursor_char(line: int, column: int) -> String [const]
  Returns the full text with char 0xFFFF at the specified location.

- has_auto_brace_completion_close_key(close_key: String) -> bool [const]
  Returns true if close key close_key exists.

- has_auto_brace_completion_open_key(open_key: String) -> bool [const]
  Returns true if open key open_key exists.

- has_comment_delimiter(start_key: String) -> bool [const]
  Returns true if comment start_key exists.

- has_string_delimiter(start_key: String) -> bool [const]
  Returns true if string start_key exists.

- indent_lines() -> void
  Indents all lines that are selected or have a caret on them. Uses spaces or a tab depending on indent_use_spaces. See unindent_lines().

- is_in_comment(line: int, column: int = -1) -> int [const]
  Returns delimiter index if line column is in a comment. If column is not provided, will return delimiter index if the entire line is a comment. Otherwise -1.

- is_in_string(line: int, column: int = -1) -> int [const]
  Returns the delimiter index if line column is in a string. If column is not provided, will return the delimiter index if the entire line is a string. Otherwise -1.

- is_line_bookmarked(line: int) -> bool [const]
  Returns true if the given line is bookmarked. See set_line_as_bookmarked().

- is_line_breakpointed(line: int) -> bool [const]
  Returns true if the given line is breakpointed. See set_line_as_breakpoint().

- is_line_code_region_end(line: int) -> bool [const]
  Returns true if the given line is a code region end. See set_code_region_tags().

- is_line_code_region_start(line: int) -> bool [const]
  Returns true if the given line is a code region start. See set_code_region_tags().

- is_line_executing(line: int) -> bool [const]
  Returns true if the given line is marked as executing. See set_line_as_executing().

- is_line_folded(line: int) -> bool [const]
  Returns true if the given line is folded. See fold_line().

- move_lines_down() -> void
  Moves all lines down that are selected or have a caret on them.

- move_lines_up() -> void
  Moves all lines up that are selected or have a caret on them.

- remove_comment_delimiter(start_key: String) -> void
  Removes the comment delimiter with start_key.

- remove_string_delimiter(start_key: String) -> void
  Removes the string delimiter with start_key.

- request_code_completion(force: bool = false) -> void
  Emits code_completion_requested, if force is true will bypass all checks. Otherwise will check that the caret is in a word or in front of a prefix. Will ignore the request if all current options are of type file path, node path, or signal.

- set_code_completion_selected_index(index: int) -> void
  Sets the current selected completion option.

- set_code_hint(code_hint: String) -> void
  Sets the code hint text. Pass an empty string to clear.

- set_code_hint_draw_below(draw_below: bool) -> void
  If true, the code hint will draw below the main caret. If false, the code hint will draw above the main caret. See set_code_hint().

- set_code_region_tags(start: String = "region", end: String = "endregion") -> void
  Sets the code region start and end tags (without comment delimiter).

- set_line_as_bookmarked(line: int, bookmarked: bool) -> void
  Sets the given line as bookmarked. If true and gutters_draw_bookmarks is true, draws the [theme_item bookmark] icon in the gutter for this line. See get_bookmarked_lines() and is_line_bookmarked().

- set_line_as_breakpoint(line: int, breakpointed: bool) -> void
  Sets the given line as a breakpoint. If true and gutters_draw_breakpoints_gutter is true, draws the [theme_item breakpoint] icon in the gutter for this line. See get_breakpointed_lines() and is_line_breakpointed().

- set_line_as_executing(line: int, executing: bool) -> void
  Sets the given line as executing. If true and gutters_draw_executing_lines is true, draws the [theme_item executing_line] icon in the gutter for this line. See get_executing_lines() and is_line_executing().

- set_symbol_lookup_word_as_valid(valid: bool) -> void
  Sets the symbol emitted by symbol_validate as a valid lookup.

- toggle_foldable_line(line: int) -> void
  Toggle the folding of the code block at the given line.

- toggle_foldable_lines_at_carets() -> void
  Toggle the folding of the code block on all lines with a caret on them.

- unfold_all_lines() -> void
  Unfolds all lines that are folded.

- unfold_line(line: int) -> void
  Unfolds the given line if it is folded or if it is hidden under a folded line.

- unindent_lines() -> void
  Unindents all lines that are selected or have a caret on them. Uses spaces or a tab depending on indent_use_spaces. Equivalent to the ProjectSettings.input/ui_text_dedent action. See indent_lines().

- update_code_completion_options(force: bool) -> void
  Submits all completion options added with add_code_completion_option(). Will try to force the autocomplete menu to popup, if force is true. **Note:** This will replace all current candidates.

## Properties

- auto_brace_completion_enabled: bool = false [set set_auto_brace_completion_enabled; get is_auto_brace_completion_enabled]
  If true, uses auto_brace_completion_pairs to automatically insert the closing brace when the opening brace is inserted by typing or autocompletion. Also automatically removes the closing brace when using backspace on the opening brace.

- auto_brace_completion_highlight_matching: bool = false [set set_highlight_matching_braces_enabled; get is_highlight_matching_braces_enabled]
  If true, highlights brace pairs when the caret is on either one, using auto_brace_completion_pairs. If matching, the pairs will be underlined. If a brace is unmatched, it is colored with [theme_item brace_mismatch_color].

- auto_brace_completion_pairs: Dictionary = { "\"": "\"", "'": "'", "(": ")", "[": "]", "{": "}" } [set set_auto_brace_completion_pairs; get get_auto_brace_completion_pairs]
  Sets the brace pairs to be autocompleted. For each entry in the dictionary, the key is the opening brace and the value is the closing brace that matches it. A brace is a String made of symbols. See auto_brace_completion_enabled and auto_brace_completion_highlight_matching.

- code_completion_enabled: bool = false [set set_code_completion_enabled; get is_code_completion_enabled]
  If true, the ProjectSettings.input/ui_text_completion_query action requests code completion. To handle it, see _request_code_completion() or code_completion_requested.

- code_completion_prefixes: String[] = [] [set set_code_completion_prefixes; get get_code_completion_prefixes]
  Sets prefixes that will trigger code completion.

- delimiter_comments: String[] = [] [set set_comment_delimiters; get get_comment_delimiters]
  Sets the comment delimiters. All existing comment delimiters will be removed.

- delimiter_strings: String[] = ["' '", "\" \""] [set set_string_delimiters; get get_string_delimiters]
  Sets the string delimiters. All existing string delimiters will be removed.

- gutters_draw_bookmarks: bool = false [set set_draw_bookmarks_gutter; get is_drawing_bookmarks_gutter]
  If true, bookmarks are drawn in the gutter. This gutter is shared with breakpoints and executing lines. See set_line_as_bookmarked().

- gutters_draw_breakpoints_gutter: bool = false [set set_draw_breakpoints_gutter; get is_drawing_breakpoints_gutter]
  If true, breakpoints are drawn in the gutter. This gutter is shared with bookmarks and executing lines. Clicking the gutter will toggle the breakpoint for the line, see set_line_as_breakpoint().

- gutters_draw_executing_lines: bool = false [set set_draw_executing_lines_gutter; get is_drawing_executing_lines_gutter]
  If true, executing lines are marked in the gutter. This gutter is shared with breakpoints and bookmarks. See set_line_as_executing().

- gutters_draw_fold_gutter: bool = false [set set_draw_fold_gutter; get is_drawing_fold_gutter]
  If true, the fold gutter is drawn. In this gutter, the [theme_item can_fold_code_region] icon is drawn for each foldable line (see can_fold_line()) and the [theme_item folded_code_region] icon is drawn for each folded line (see is_line_folded()). These icons can be clicked to toggle the fold state, see toggle_foldable_line(). line_folding must be true to show icons.

- gutters_draw_line_numbers: bool = false [set set_draw_line_numbers; get is_draw_line_numbers_enabled]
  If true, the line number gutter is drawn. Line numbers start at 1 and are incremented for each line of text. Clicking and dragging in the line number gutter will select entire lines of text.

- gutters_line_numbers_min_digits: int = 3 [set set_line_numbers_min_digits; get get_line_numbers_min_digits]
  The minimum width in digits reserved for the line number gutter.

- gutters_zero_pad_line_numbers: bool = false [set set_line_numbers_zero_padded; get is_line_numbers_zero_padded]
  If true, line numbers drawn in the gutter are zero padded based on the total line count. Requires gutters_draw_line_numbers to be set to true.

- indent_automatic: bool = false [set set_auto_indent_enabled; get is_auto_indent_enabled]
  If true, an extra indent is automatically inserted when a new line is added and a prefix in indent_automatic_prefixes is found. If a brace pair opening key is found, the matching closing brace will be moved to another new line (see auto_brace_completion_pairs).

- indent_automatic_prefixes: String[] = [":", "{", "[", "("] [set set_auto_indent_prefixes; get get_auto_indent_prefixes]
  Prefixes to trigger an automatic indent. Used when indent_automatic is set to true.

- indent_size: int = 4 [set set_indent_size; get get_indent_size]
  Size of the tabulation indent (one Tab press) in characters. If indent_use_spaces is enabled the number of spaces to use.

- indent_use_spaces: bool = false [set set_indent_using_spaces; get is_indent_using_spaces]
  Use spaces instead of tabs for indentation.

- layout_direction: int (Control.LayoutDirection) = 2 [set set_layout_direction; get get_layout_direction; override Control]

- line_folding: bool = false [set set_line_folding_enabled; get is_line_folding_enabled]
  If true, lines can be folded. Otherwise, line folding methods like fold_line() will not work and can_fold_line() will always return false. See gutters_draw_fold_gutter.

- line_length_guidelines: int[] = [] [set set_line_length_guidelines; get get_line_length_guidelines]
  Draws vertical lines at the provided columns. The first entry is considered a main hard guideline and is drawn more prominently.

- symbol_lookup_on_click: bool = false [set set_symbol_lookup_on_click_enabled; get is_symbol_lookup_on_click_enabled]
  Set when a validated word from symbol_validate is clicked, the symbol_lookup should be emitted.

- symbol_tooltip_on_hover: bool = false [set set_symbol_tooltip_on_hover_enabled; get is_symbol_tooltip_on_hover_enabled]
  If true, the symbol_hovered signal is emitted when hovering over a word.

- text_direction: int (Control.TextDirection) = 1 [set set_text_direction; get get_text_direction; override TextEdit]

## Signals

- breakpoint_toggled(line: int)
  Emitted when a breakpoint is added or removed from a line. If the line is removed via backspace, a signal is emitted at the old line.

- code_completion_requested()
  Emitted when the user requests code completion. This signal will not be sent if _request_code_completion() is overridden or code_completion_enabled is false.

- symbol_hovered(symbol: String, line: int, column: int)
  Emitted when the user hovers over a symbol. Unlike Control.mouse_entered, this signal is not emitted immediately, but when the cursor is over the symbol for ProjectSettings.gui/timers/tooltip_delay_sec seconds. **Note:** symbol_tooltip_on_hover must be true for this signal to be emitted.

- symbol_lookup(symbol: String, line: int, column: int)
  Emitted when the user has clicked on a valid symbol.

- symbol_validate(symbol: String)
  Emitted when the user hovers over a symbol. The symbol should be validated and responded to, by calling set_symbol_lookup_word_as_valid(). **Note:** symbol_lookup_on_click must be true for this signal to be emitted.

## Constants

### Enum CodeCompletionKind

- KIND_CLASS = 0
  Marks the option as a class.

- KIND_FUNCTION = 1
  Marks the option as a function.

- KIND_SIGNAL = 2
  Marks the option as a Godot signal.

- KIND_VARIABLE = 3
  Marks the option as a variable.

- KIND_MEMBER = 4
  Marks the option as a member.

- KIND_ENUM = 5
  Marks the option as an enum entry.

- KIND_CONSTANT = 6
  Marks the option as a constant.

- KIND_NODE_PATH = 7
  Marks the option as a Godot node path.

- KIND_FILE_PATH = 8
  Marks the option as a file path.

- KIND_PLAIN_TEXT = 9
  Marks the option as unclassified or plain text.

### Enum CodeCompletionLocation

- LOCATION_LOCAL = 0
  The option is local to the location of the code completion query - e.g. a local variable. Subsequent value of location represent options from the outer class, the exact value represent how far they are (in terms of inner classes).

- LOCATION_PARENT_MASK = 256
  The option is from the containing class or a parent class, relative to the location of the code completion query. Perform a bitwise OR with the class depth (e.g. 0 for the local class, 1 for the parent, 2 for the grandparent, etc.) to store the depth of an option in the class or a parent class.

- LOCATION_OTHER_USER_CODE = 512
  The option is from user code which is not local and not in a derived class (e.g. Autoload Singletons).

- LOCATION_OTHER = 1024
  The option is from other engine code, not covered by the other enum constants - e.g. built-in classes.

## Theme Items

- bookmark_color: Color [color] = Color(0.5, 0.64, 1, 0.8)
  Color of the bookmark icon for bookmarked lines.

- brace_mismatch_color: Color [color] = Color(1, 0.2, 0.2, 1)
  Color of the text to highlight mismatched braces.

- breakpoint_color: Color [color] = Color(0.9, 0.29, 0.3, 1)
  Color of the breakpoint icon for bookmarked lines.

- code_folding_color: Color [color] = Color(0.8, 0.8, 0.8, 0.8)
  Color for all icons related to line folding.

- completion_background_color: Color [color] = Color(0.17, 0.16, 0.2, 1)
  Sets the background Color for the code completion popup.

- completion_existing_color: Color [color] = Color(0.87, 0.87, 0.87, 0.13)
  Background highlight Color for matching text in code completion options.

- completion_scroll_color: Color [color] = Color(1, 1, 1, 0.29)
  Color of the scrollbar in the code completion popup.

- completion_scroll_hovered_color: Color [color] = Color(1, 1, 1, 0.4)
  Color of the scrollbar in the code completion popup when hovered.

- completion_selected_color: Color [color] = Color(0.26, 0.26, 0.27, 1)
  Background highlight Color for the current selected option item in the code completion popup.

- executing_line_color: Color [color] = Color(0.98, 0.89, 0.27, 1)
  Color of the executing icon for executing lines.

- folded_code_region_color: Color [color] = Color(0.68, 0.46, 0.77, 0.2)
  Color of background line highlight for folded code region.

- line_length_guideline_color: Color [color] = Color(0.3, 0.5, 0.8, 0.1)
  Color of the main line length guideline, secondary guidelines will have 50% alpha applied.

- line_number_color: Color [color] = Color(0.67, 0.67, 0.67, 0.4)
  Sets the Color of line numbers.

- completion_lines: int [constant] = 7
  Max number of options to display in the code completion popup at any one time.

- completion_max_width: int [constant] = 50
  Max width of options in the code completion popup. Options longer than this will be cut off.

- completion_scroll_width: int [constant] = 6
  Width of the scrollbar in the code completion popup.

- bookmark: Texture2D [icon]
  Sets a custom Texture2D to draw in the bookmark gutter for bookmarked lines.

- breakpoint: Texture2D [icon]
  Sets a custom Texture2D to draw in the breakpoint gutter for breakpointed lines.

- can_fold: Texture2D [icon]
  Sets a custom Texture2D to draw in the line folding gutter when a line can be folded.

- can_fold_code_region: Texture2D [icon]
  Sets a custom Texture2D to draw in the line folding gutter when a code region can be folded.

- completion_color_bg: Texture2D [icon]
  Background panel for the color preview box in autocompletion (visible when the color is translucent).

- executing_line: Texture2D [icon]
  Icon to draw in the executing gutter for executing lines.

- folded: Texture2D [icon]
  Sets a custom Texture2D to draw in the line folding gutter when a line is folded and can be unfolded.

- folded_code_region: Texture2D [icon]
  Sets a custom Texture2D to draw in the line folding gutter when a code region is folded and can be unfolded.

- folded_eol_icon: Texture2D [icon]
  Sets a custom Texture2D to draw at the end of a folded line.

- completion: StyleBox [style]
  StyleBox for the code completion popup.
