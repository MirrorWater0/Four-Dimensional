# EditorVCSInterface

## Meta

- Name: EditorVCSInterface
- Source: EditorVCSInterface.xml
- Inherits: Object
- Inheritance Chain: EditorVCSInterface -> Object

## Brief Description

Version Control System (VCS) interface, which reads and writes to the local VCS in use.

## Description

Defines the API that the editor uses to extract information from the underlying VCS. The implementation of this API is included in VCS plugins, which are GDExtension plugins that inherit EditorVCSInterface and are attached (on demand) to the singleton instance of EditorVCSInterface. Instead of performing the task themselves, all the virtual functions listed below are calling the internally overridden functions in the VCS plugins to provide a plug-n-play experience. A custom VCS plugin is supposed to inherit from EditorVCSInterface and override each of these virtual functions.

## Quick Reference

```
[methods]
_checkout_branch(branch_name: String) -> bool [virtual required]
_commit(msg: String) -> void [virtual required]
_create_branch(branch_name: String) -> void [virtual required]
_create_remote(remote_name: String, remote_url: String) -> void [virtual required]
_discard_file(file_path: String) -> void [virtual required]
_fetch(remote: String) -> void [virtual required]
_get_branch_list() -> String[] [virtual required]
_get_current_branch_name() -> String [virtual required]
_get_diff(identifier: String, area: int) -> Dictionary[] [virtual required]
_get_line_diff(file_path: String, text: String) -> Dictionary[] [virtual required]
_get_modified_files_data() -> Dictionary[] [virtual required]
_get_previous_commits(max_commits: int) -> Dictionary[] [virtual required]
_get_remotes() -> String[] [virtual required]
_get_vcs_name() -> String [virtual required]
_initialize(project_path: String) -> bool [virtual required]
_pull(remote: String) -> void [virtual required]
_push(remote: String, force: bool) -> void [virtual required]
_remove_branch(branch_name: String) -> void [virtual required]
_remove_remote(remote_name: String) -> void [virtual required]
_set_credentials(username: String, password: String, ssh_public_key_path: String, ssh_private_key_path: String, ssh_passphrase: String) -> void [virtual required]
_shut_down() -> bool [virtual required]
_stage_file(file_path: String) -> void [virtual required]
_unstage_file(file_path: String) -> void [virtual required]
add_diff_hunks_into_diff_file(diff_file: Dictionary, diff_hunks: Dictionary[]) -> Dictionary
add_line_diffs_into_diff_hunk(diff_hunk: Dictionary, line_diffs: Dictionary[]) -> Dictionary
create_commit(msg: String, author: String, id: String, unix_timestamp: int, offset_minutes: int) -> Dictionary
create_diff_file(new_file: String, old_file: String) -> Dictionary
create_diff_hunk(old_start: int, new_start: int, old_lines: int, new_lines: int) -> Dictionary
create_diff_line(new_line_no: int, old_line_no: int, content: String, status: String) -> Dictionary
create_status_file(file_path: String, change_type: int (EditorVCSInterface.ChangeType), area: int (EditorVCSInterface.TreeArea)) -> Dictionary
popup_error(msg: String) -> void
```

## Tutorials

- [Version control systems]($DOCS_URL/tutorials/best_practices/version_control_systems.html)

## Methods

- _checkout_branch(branch_name: String) -> bool [virtual required]
  Checks out a branch_name in the VCS.

- _commit(msg: String) -> void [virtual required]
  Commits the currently staged changes and applies the commit msg to the resulting commit.

- _create_branch(branch_name: String) -> void [virtual required]
  Creates a new branch named branch_name in the VCS.

- _create_remote(remote_name: String, remote_url: String) -> void [virtual required]
  Creates a new remote destination with name remote_name and points it to remote_url. This can be an HTTPS remote or an SSH remote.

- _discard_file(file_path: String) -> void [virtual required]
  Discards the changes made in a file present at file_path.

- _fetch(remote: String) -> void [virtual required]
  Fetches new changes from the remote, but doesn't write changes to the current working directory. Equivalent to git fetch.

- _get_branch_list() -> String[] [virtual required]
  Gets an instance of an Array of Strings containing available branch names in the VCS.

- _get_current_branch_name() -> String [virtual required]
  Gets the current branch name defined in the VCS.

- _get_diff(identifier: String, area: int) -> Dictionary[] [virtual required]
  Returns an array of Dictionary items (see create_diff_file(), create_diff_hunk(), create_diff_line(), add_line_diffs_into_diff_hunk() and add_diff_hunks_into_diff_file()), each containing information about a diff. If identifier is a file path, returns a file diff, and if it is a commit identifier, then returns a commit diff.

- _get_line_diff(file_path: String, text: String) -> Dictionary[] [virtual required]
  Returns an Array of Dictionary items (see create_diff_hunk()), each containing a line diff between a file at file_path and the text which is passed in.

- _get_modified_files_data() -> Dictionary[] [virtual required]
  Returns an Array of Dictionary items (see create_status_file()), each containing the status data of every modified file in the project folder.

- _get_previous_commits(max_commits: int) -> Dictionary[] [virtual required]
  Returns an Array of Dictionary items (see create_commit()), each containing the data for a past commit.

- _get_remotes() -> String[] [virtual required]
  Returns an Array of Strings, each containing the name of a remote configured in the VCS.

- _get_vcs_name() -> String [virtual required]
  Returns the name of the underlying VCS provider.

- _initialize(project_path: String) -> bool [virtual required]
  Initializes the VCS plugin when called from the editor. Returns whether or not the plugin was successfully initialized. A VCS project is initialized at project_path.

- _pull(remote: String) -> void [virtual required]
  Pulls changes from the remote. This can give rise to merge conflicts.

- _push(remote: String, force: bool) -> void [virtual required]
  Pushes changes to the remote. If force is true, a force push will override the change history already present on the remote.

- _remove_branch(branch_name: String) -> void [virtual required]
  Remove a branch from the local VCS.

- _remove_remote(remote_name: String) -> void [virtual required]
  Remove a remote from the local VCS.

- _set_credentials(username: String, password: String, ssh_public_key_path: String, ssh_private_key_path: String, ssh_passphrase: String) -> void [virtual required]
  Set user credentials in the underlying VCS. username and password are used only during HTTPS authentication unless not already mentioned in the remote URL. ssh_public_key_path, ssh_private_key_path, and ssh_passphrase are only used during SSH authentication.

- _shut_down() -> bool [virtual required]
  Shuts down VCS plugin instance. Called when the user either closes the editor or shuts down the VCS plugin through the editor UI.

- _stage_file(file_path: String) -> void [virtual required]
  Stages the file present at file_path to the staged area.

- _unstage_file(file_path: String) -> void [virtual required]
  Unstages the file present at file_path from the staged area to the unstaged area.

- add_diff_hunks_into_diff_file(diff_file: Dictionary, diff_hunks: Dictionary[]) -> Dictionary
  Helper function to add an array of diff_hunks into a diff_file.

- add_line_diffs_into_diff_hunk(diff_hunk: Dictionary, line_diffs: Dictionary[]) -> Dictionary
  Helper function to add an array of line_diffs into a diff_hunk.

- create_commit(msg: String, author: String, id: String, unix_timestamp: int, offset_minutes: int) -> Dictionary
  Helper function to create a commit Dictionary item. msg is the commit message of the commit. author is a single human-readable string containing all the author's details, e.g. the email and name configured in the VCS. id is the identifier of the commit, in whichever format your VCS may provide an identifier to commits. unix_timestamp is the UTC Unix timestamp of when the commit was created. offset_minutes is the timezone offset in minutes, recorded from the system timezone where the commit was created.

- create_diff_file(new_file: String, old_file: String) -> Dictionary
  Helper function to create a Dictionary for storing old and new diff file paths.

- create_diff_hunk(old_start: int, new_start: int, old_lines: int, new_lines: int) -> Dictionary
  Helper function to create a Dictionary for storing diff hunk data. old_start is the starting line number in old file. new_start is the starting line number in new file. old_lines is the number of lines in the old file. new_lines is the number of lines in the new file.

- create_diff_line(new_line_no: int, old_line_no: int, content: String, status: String) -> Dictionary
  Helper function to create a Dictionary for storing a line diff. new_line_no is the line number in the new file (can be -1 if the line is deleted). old_line_no is the line number in the old file (can be -1 if the line is added). content is the diff text. status is a single character string which stores the line origin.

- create_status_file(file_path: String, change_type: int (EditorVCSInterface.ChangeType), area: int (EditorVCSInterface.TreeArea)) -> Dictionary
  Helper function to create a Dictionary used by editor to read the status of a file.

- popup_error(msg: String) -> void
  Pops up an error message in the editor which is shown as coming from the underlying VCS. Use this to show VCS specific error messages.

## Constants

### Enum ChangeType

- CHANGE_TYPE_NEW = 0
  A new file has been added.

- CHANGE_TYPE_MODIFIED = 1
  An earlier added file has been modified.

- CHANGE_TYPE_RENAMED = 2
  An earlier added file has been renamed.

- CHANGE_TYPE_DELETED = 3
  An earlier added file has been deleted.

- CHANGE_TYPE_TYPECHANGE = 4
  An earlier added file has been typechanged.

- CHANGE_TYPE_UNMERGED = 5
  A file is left unmerged.

### Enum TreeArea

- TREE_AREA_COMMIT = 0
  A commit is encountered from the commit area.

- TREE_AREA_STAGED = 1
  A file is encountered from the staged area.

- TREE_AREA_UNSTAGED = 2
  A file is encountered from the unstaged area.
