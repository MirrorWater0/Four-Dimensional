# PROJECT KNOWLEDGE BASE

**Generated:** 2026-05-14

## OVERVIEW
Project: **Four-Dimensional** (assembly name: `tower`)
A turn-based RPG / deck-building roguelite with anime-style characters. Players build a party, traverse a node-based map, and engage in tactical battles using a card/skill drawing system inspired by Slay the Spire.

Stack: Godot 4.6 (C# / .NET 8.0), Spine runtime, custom Godot shaders

## STRUCTURE
```
Four-Dimensional/
├── project.godot              # Godot project config (main scene: PreloadeScene.tscn)
├── tower.csproj / tower.sln   # .NET 8.0 solution (Godot.NET.Sdk/4.6.0)
├── export_presets.cfg         # Windows Desktop export preset
│
├── BeginGame/                 # Start screen & character selection overlay
├── battle/                    # Core battle system
│   ├── buff/                  # Buff implementations & state icons
│   ├── Effect/                # VFX scenes (particles, hit effects)
│   └── UIScene/               # Battle UI: tooltips, game over, rewards, manual target
├── character/                 # Character hierarchy
│   ├── EnemyCharacter/        # Enemy scenes + EnemyRegedit configs
│   ├── PlayerCharacter/       # Player scenes + GameInfo (static game state)
│   │   ├── Echo/ Kasiya/ Mariya/ Nightingale/
│   └── SkillBase/             # Skill.cs base class & all skill implementations
├── Map/                       # Overworld map, camera, sites, level progression
│   ├── Site/                  # Individual map node types
│   └── UI/                    # Map HUD
├── Menu/                      # In-game menus
├── Shop/                      # Shop interface
├── Event/                     # Random event scenes
├── Equipment/                 # Equipment system
├── Relic/                     # Relic/passive item system
├── ConsumeItems/              # Consumable items
├── LabelNode/                 # Floating number/label nodes
├── MouseTrail/                # Mouse trail effect (autoloaded)
│
├── asset/                     # Art assets
│   ├── CardPicture/           # Skill card art (per-character subdirs)
│   ├── PlayerCharater/        # Character sprites & portraits (note: spelling)
│   ├── EnemyCharater/         # Enemy sprites & portraits
│   ├── Effect/                # Battle VFX frames
│   ├── Spine/                 # Spine skeletal animation data
│   ├── UI/                    # UI textures & buttons
│   ├── font/                  # Game fonts
│   └── generated/             # AI-generated temp artwork
├── shader/                    # Custom Godot shaders (UI, buff icons, map, particles)
├── scripts/                   # Utility scripts (e.g. PowerShell)
├── docs/                      # Project documentation
│   └── IMAGE2_USAGE.md        # AI image generation workflow (gpt-image-2)
├── tools/                     # Dev tools
└── tres/                      # Shared .tres resource files
```

## COMMANDS
| Action | Command |
|--------|---------|
| Open Editor | Launch via configured Godot 4.6+ mono editor |
| Build | `dotnet build tower.sln` or build from Godot editor |
| Run (Editor) | F5 in Godot editor, or `godot --path .` |
| Export | Use Godot editor Export menu (preset: Windows Desktop) |

> The project relies on Godot 4.6 with .NET support. The VS Code workspace setting points to `Godot_v4.5.1-stable_mono_win64` but the csproj targets `Godot.NET.Sdk/4.6.0`.

## CODING STANDARDS
*   **Language**: C# 12 (preview) on .NET 8.0
*   **Style**:
    *   `public partial class` for all Godot node scripts.
    *   PascalCase for types, public members, and methods.
    *   camelCase for locals; `_camelCase` for private fields.
    *   Lazy node access via `field ??= GetNode<...>("Path")` pattern.
    *   Extensive use of `GodotObject.IsInstanceValid()` before operating on nodes.
    *   Async/await with `Task` and Godot `ToSignal` for coroutine-like flows.
    *   LINQ-heavy queries for filtering and sorting.
    *   BBCode (`[color=...]`, `[b]`, etc.) used in tooltips and dynamic labels.
*   **Rules**: No `.editorconfig`, StyleCop, or CSharpier config found. Formatting is manual. Keep existing brace style (K&R, opening brace on same line).
*   **Signals**: Godot signals declared with `[Signal] public delegate void ...Handler(...)` and emitted via `EmitSignal(SignalName.XXX, args)`.
*   **Exports**: `[Export]` and `[Export(PropertyHint.Range, "...")]` used for inspector-configurable fields.

## WHERE TO LOOK
*   **Source**: `character/`, `battle/`, `Map/`, `Shop/`, `Event/`, `Relic/`, `Equipment/`, `ConsumeItems/`, `Menu/`, `BeginGame/`
*   **Tests**: None observed
*   **Docs**: `docs/IMAGE2_USAGE.md`
*   **Global State**: `character/PlayerCharacter/GameInfo.cs` (static session data)
*   **Save System**: `SaveSystem.cs` (root level)
*   **Scene Transition**: `PreloadeScene.cs`, `SceneTransitionLayer.cs` (root level)
*   **Shared Utils**: `character/PlayerCharacter/GameInfo.cs` contains `GlobalFunction`

## NOTES
*   **Spine Integration**: The project includes `spine_godot_extension` (binaries in `bin/`). Characters may use Spine skeletons.
*   **WarmupMode**: Many scenes expose `[Export] public bool WarmupMode` to disable gameplay logic for UI previews or testing.
*   **Card Art Pipeline**: Skill card images are expected at `asset/CardPicture/{CharacterName}/{SkillId}.png`, falling back to flat `asset/CardPicture/` paths.
*   **Action Point System**: Battles resolve via an "Action Point" (行动点数) threshold system based on team speed, not strict turn order.
*   **Buff Architecture**: Characters maintain separate typed buff lists (`StartActionBuffs`, `EndActionBuffs`, `AttackBuffs`, `HurtBuffs`, `SpecialBuffs`, `SkillBuffs`, `DyingBuffs`). Buffs are iterated over snapshots because buffs can remove themselves during triggering.
*   **AI Art Workflow**: The project has an established image-generation pipeline using `gpt-image-2` via a custom proxy. See `docs/IMAGE2_USAGE.md` for character constraints and prompt templates.
*   **Typo Convention**: Directory and some class names use `Charater` instead of `Character` consistently (e.g., `asset/PlayerCharater/`). Do not "fix" these unless explicitly asked, as code references match the existing paths.
*   **Localization**: UI strings and descriptions are primarily in Simplified Chinese; code identifiers are in English.
