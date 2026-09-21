# SteamPipe upload for Four-Dimensional Demo

These files upload the Windows demo export at:

```text
C:\godot_project\export\Four-Dimensional
```

The demo AppID is `4778370`. The depot ID is currently set to `4778371`, which is the usual default depot pattern. If Steamworks > SteamPipe > Depots shows a different depot ID, replace `4778371` in both `.vdf` files and rename `depot_build_4778371.vdf` to match.

The depot configuration uploads the current Windows export only. It excludes Android packages, debug symbols, the debug Spine runtime, and the legacy nested Windows export folder.

Before uploading, the script also places the Spine extension manifest, its release DLL, and Godot's extension list beside the exported game. These files are required by the Windows release build; without them, Spine characters can be replaced by empty placeholders in battle.

The Windows export uses a separate `Four-Dimensional.pck` alongside the executable. This is required because Spine is a native GDExtension: Godot must load the extension manifest and its DLL from the export folder during startup. The upload script requires this PCK and runs the game through the Spine preload before contacting SteamPipe.

## Upload a Steam release build

Close the Godot editor first, then run the upload script. It re-exports the current project, validates that the exported game starts without the known Spine loading error, and only then uploads it. Keeping the editor open can make the export's Spine resource loaders fail at startup, which would cause missing battle characters in the Steam build.

Godot may print `EditorSettings not instantiated yet ... export/android/shutdown_adb_on_exit` during this Windows headless export. The script captures it in `C:\godot_project\export\Four-Dimensional\godot-export.log` and continues when Godot's exit code is successful; it is not a release-export failure.

```powershell
.\scripts\steam_upload\upload_demo_build.ps1
```

The script defaults to the configured Steamworks account `chaosheng35`. To upload with a different account, pass `-SteamUser` explicitly.

To upload an export that has already passed the release-export helper, pass `-SkipExport`. This is intended only for exceptional cases; the normal command above is safer.

If Steamworks > SteamPipe > Depots shows a different depot ID, pass it like this:

```powershell
.\scripts\steam_upload\upload_demo_build.ps1 `
  -SteamUser "another_steamworks_login" `
  -DepotId "YOUR_DEPOT_ID"
```

The script defaults to:

```text
C:\godot_project\steamworks_sdk_164\sdk\tools\ContentBuilder\builder\steamcmd.exe
```

`steamcmd` will ask for your password and Steam Guard code if needed. By default the script does not set the build live automatically; after a successful upload, set the build live manually in Steamworks > SteamPipe > Builds.

SteamCMD can upload a build successfully while Steamworks rejects the final `SetLive` operation (especially for the public `default` branch, depending on account permissions and app release status). The upload script reports that case separately. The reliable workflow is to upload without `-SetLiveBranch`, then set the build live from Steamworks > SteamPipe > Builds.

If you have a permitted non-public branch and want SteamCMD to set it live automatically, pass the branch name:

```powershell
.\scripts\steam_upload\upload_demo_build.ps1 `
  -SteamUser "your_steamworks_login" `
  -SetLiveBranch "default"
```
