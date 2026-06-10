# SteamPipe upload for Four-Dimensional Demo

These files upload the Windows demo export at:

```text
C:\godot_project\export\Four-Dimensional
```

The demo AppID is `4778370`. The depot ID is currently set to `4778371`, which is the usual default depot pattern. If Steamworks > SteamPipe > Depots shows a different depot ID, replace `4778371` in both `.vdf` files and rename `depot_build_4778371.vdf` to match.

Run from PowerShell:

```powershell
.\scripts\steam_upload\upload_demo_build.ps1 `
  -SteamUser "your_steamworks_login"
```

If Steamworks > SteamPipe > Depots shows a different depot ID, pass it like this:

```powershell
.\scripts\steam_upload\upload_demo_build.ps1 `
  -SteamUser "your_steamworks_login" `
  -DepotId "YOUR_DEPOT_ID"
```

The script defaults to:

```text
C:\godot_project\steamworks_sdk_164\sdk\tools\ContentBuilder\builder\steamcmd.exe
```

`steamcmd` will ask for your password and Steam Guard code if needed. By default the script does not set the build live automatically; after a successful upload, set the build live manually in Steamworks > SteamPipe > Builds.

If you already have a working branch and want SteamCMD to set it live automatically, pass the branch name:

```powershell
.\scripts\steam_upload\upload_demo_build.ps1 `
  -SteamUser "your_steamworks_login" `
  -SetLiveBranch "default"
```
