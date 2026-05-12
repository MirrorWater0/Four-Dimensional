using System;
using System.Collections.Generic;
using Godot;

public partial class PreloadeScene : Node2D
{
    [Export]
    public string MainScenePath = "res://BeginGame/StartInterface.tscn";

    [Export]
    public float BlackScreenMinTime = 0.3f;

    [Export]
    public float FadeOutDuration = 0.25f;

    [Export]
    public float WarmupHoldTime = 0.6f;

    [Export]
    public string[] WarmupScenePaths =
    {
        "res://Map/Map.tscn",
        "res://battle/BattlePreview/BattlePreview.tscn",
        "res://battle/Battle.tscn",
    };

    [Export]
    public bool WarmupAllCharacterScenes = true;

    [Export]
    public bool WarmupAllBattleEffectScenes = true;

    [Export]
    public bool WarmupAllBuffIconScenes = true;

    [Export]
    public bool WarmupAllBattleUIScenes = false;

    [Export]
    public bool WarmupAllShaders = true;

    [Export]
    public bool WarmupAllSkills = true;

    [Export]
    public bool PreloadSkillArtTextures = true;

    [Export]
    public int ShaderWarmupMaxCount = 0;

    [Export]
    public int ShaderWarmupFramesPerShader = 1;

    [Export]
    public int ShaderWarmupGapFrames = 1;

    [Export]
    public int WarmupSceneYieldFrames = 1;

    [Export]
    public int PreloadSceneBatchSize = 8;

    [Export]
    public int SkillWarmupBatchSize = 12;

    [Export]
    public int TexturePreloadBatchSize = 10;

    [Export]
    public bool LogPreloadedResources = false;

    private Node _sceneHolder;
    private Node _warmupHolder;
    private ColorRect _shaderWarmupRect;
    private readonly List<Node> _warmupInstances = new();

    // Store references to keep them in memory
    public static Dictionary<string, PackedScene> PreloadedScenes =
        new Dictionary<string, PackedScene>();

    // Preloaded textures cache to avoid runtime GD.Load stalls in exported builds
    public static Dictionary<string, Texture2D> PreloadedTextures =
        new Dictionary<string, Texture2D>();

    public static PackedScene GetPackedScene(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (PreloadedScenes.TryGetValue(path, out var scene) && scene != null)
            return scene;

        scene = GD.Load<PackedScene>(path);
        if (scene != null)
            PreloadedScenes[path] = scene;
        return scene;
    }

    public static Texture2D GetTexture(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (PreloadedTextures.TryGetValue(path, out var texture) && texture != null)
            return texture;

        if (!ResourceLoader.Exists(path))
            return null;

        texture = GD.Load<Texture2D>(path);
        if (texture != null)
            PreloadedTextures[path] = texture;
        return texture;
    }

    public override async void _Ready()
    {
        UserSettings.EnsureLoaded();

        _sceneHolder = GetNodeOrNull<Node>("SceneHolder");
        _warmupHolder = GetNodeOrNull<Node>("WarmupHolder");
        _shaderWarmupRect = GetNodeOrNull<ColorRect>("FadeLayer/ShaderWarmupRect");
        var transitionLayer = SceneTransitionLayer.Ensure(this, deferAddToRoot: true);

        transitionLayer?.ShowBlackImmediate();

        // Ensure the black frame is rendered before heavy loading.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        GD.Print("--- Start Preloading Scenes ---");
        await ScanAndLoadAsync("res://");
        GD.Print($"--- Finished Preloading. Loaded {PreloadedScenes.Count} scenes ---");

        GD.Print("--- Start Preloading Textures ---");
        await PreloadTexturesAsync();
        GD.Print($"--- Finished Preloading. Loaded {PreloadedTextures.Count} textures ---");

        await WarmupInstantiateScenesAsync();
        await WarmupSkillsAsync();
        await WarmupShadersAsync();
        LoadMainSceneIntoHolder();

        if (BlackScreenMinTime > 0f)
            await ToSignal(
                GetTree().CreateTimer(BlackScreenMinTime),
                SceneTreeTimer.SignalName.Timeout
            );

        if (WarmupHoldTime > 0f)
            await ToSignal(
                GetTree().CreateTimer(WarmupHoldTime),
                SceneTreeTimer.SignalName.Timeout
            );

        CleanupWarmupInstances();

        if (transitionLayer != null)
            await transitionLayer.FadeFromBlackAsync(FadeOutDuration);
    }

    private void LoadMainSceneIntoHolder()
    {
        if (_sceneHolder == null)
            return;
        if (string.IsNullOrWhiteSpace(MainScenePath))
            return;

        var packed = GD.Load<PackedScene>(MainScenePath);
        if (packed == null)
        {
            GD.PushError($"Failed to load main scene: {MainScenePath}");
            return;
        }

        var instance = packed.Instantiate();
        _sceneHolder.AddChild(instance);
    }

    private async System.Threading.Tasks.Task WarmupInstantiateScenesAsync()
    {
        if (_warmupHolder == null)
            return;

        var paths = BuildWarmupSceneList();
        if (paths.Count == 0)
            return;

        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            if (string.Equals(path, MainScenePath, StringComparison.OrdinalIgnoreCase))
                continue;

            var packed = PreloadedScenes.TryGetValue(path, out var cached) ? cached : GD.Load<PackedScene>(path);
            if (packed == null)
                continue;

            Node instance = null;
            try
            {
                instance = packed.Instantiate();
            }
            catch (Exception e)
            {
                GD.PrintErr($"Warmup instantiate failed: {path} ({e.Message})");
            }

            if (instance == null)
                continue;

            if (instance is Map map)
                map.WarmupMode = true;
            else if (instance is BattlePreview preview)
                preview.WarmupMode = true;
            else if (instance is Battle battle)
                battle.WarmupMode = true;
            else if (instance is Character character)
                character.WarmupMode = true;

            instance.ProcessMode = ProcessModeEnum.Disabled;
            instance.SetProcess(false);
            instance.SetPhysicsProcess(false);
            instance.SetProcessInput(false);
            instance.SetProcessUnhandledInput(false);

            _warmupHolder.AddChild(instance);
            _warmupInstances.Add(instance);

            if (WarmupSceneYieldFrames > 0)
            {
                for (int i = 0; i < WarmupSceneYieldFrames; i++)
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }
    }

    private List<string> BuildWarmupSceneList()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (WarmupScenePaths != null)
        {
            foreach (var path in WarmupScenePaths)
            {
                if (!string.IsNullOrWhiteSpace(path))
                    result.Add(path);
            }
        }

        if (WarmupAllCharacterScenes)
        {
            if (PreloadedScenes.Count > 0)
            {
                foreach (var kvp in PreloadedScenes)
                {
                    if (kvp.Key.StartsWith("res://character/", StringComparison.OrdinalIgnoreCase))
                        result.Add(kvp.Key);
                }
            }
            else
            {
                foreach (var path in ScanScenePaths("res://character"))
                    result.Add(path);
            }
        }

        if (WarmupAllBattleEffectScenes)
        {
            foreach (var path in ScanScenePaths("res://battle/Effect"))
                result.Add(path);
        }

        if (WarmupAllBuffIconScenes)
        {
            foreach (var path in ScanScenePaths("res://battle/buff"))
                result.Add(path);
        }

        if (WarmupAllBattleUIScenes)
        {
            foreach (var path in ScanScenePaths("res://battle/UIScene"))
                result.Add(path);
        }

        if (!string.IsNullOrWhiteSpace(MainScenePath))
            result.Remove(MainScenePath);

        result.Remove("res://PreloadeScene.tscn");

        return new List<string>(result);
    }

    private List<string> ScanScenePaths(string dirPath)
    {
        var results = new List<string>();
        using var dir = DirAccess.Open(dirPath);
        if (dir == null)
            return results;

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (dir.CurrentIsDir())
            {
                if (!fileName.StartsWith("."))
                {
                    results.AddRange(ScanScenePaths(dirPath.PathJoin(fileName)));
                }
            }
            else if (fileName.EndsWith(".tscn") || fileName.EndsWith(".scn"))
            {
                results.Add(dirPath.PathJoin(fileName));
            }

            fileName = dir.GetNext();
        }

        return results;
    }

    private List<string> ScanShaderPaths(string dirPath)
    {
        var results = new List<string>();
        using var dir = DirAccess.Open(dirPath);
        if (dir == null)
            return results;

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (dir.CurrentIsDir())
            {
                if (!fileName.StartsWith("."))
                {
                    results.AddRange(ScanShaderPaths(dirPath.PathJoin(fileName)));
                }
            }
            else if (fileName.EndsWith(".gdshader") || fileName.EndsWith(".shader"))
            {
                results.Add(dirPath.PathJoin(fileName));
            }

            fileName = dir.GetNext();
        }

        return results;
    }

    private List<string> ScanTexturePaths(string dirPath)
    {
        var results = new List<string>();
        using var dir = DirAccess.Open(dirPath);
        if (dir == null)
            return results;

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (dir.CurrentIsDir())
            {
                if (!fileName.StartsWith("."))
                    results.AddRange(ScanTexturePaths(dirPath.PathJoin(fileName)));
            }
            else if (
                fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
            )
            {
                results.Add(dirPath.PathJoin(fileName));
            }

            fileName = dir.GetNext();
        }

        return results;
    }

    private async System.Threading.Tasks.Task PreloadTexturesAsync()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddCharacterPortraitPaths(paths);

        if (PreloadSkillArtTextures)
        {
            foreach (var path in ScanTexturePaths("res://asset/CardPicture"))
                paths.Add(path);
            foreach (var path in ScanTexturePaths("res://asset/svg/SkillIcon"))
                paths.Add(path);
        }

        int batchSize = Math.Max(1, TexturePreloadBatchSize);
        int loaded = 0;
        foreach (var path in paths)
        {
            LoadTexture(path);
            loaded++;
            if (loaded % batchSize == 0)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private async System.Threading.Tasks.Task WarmupShadersAsync()
    {
        if (!WarmupAllShaders)
            return;
        if (_shaderWarmupRect == null)
            return;

        var shaderPaths = ScanShaderPaths("res://shader");
        if (shaderPaths.Count == 0)
            return;

        _shaderWarmupRect.Visible = true;

        int warmed = 0;
        foreach (var path in shaderPaths)
        {
            if (ShaderWarmupMaxCount > 0 && warmed >= ShaderWarmupMaxCount)
                break;

            var shader = GD.Load<Shader>(path);
            if (shader == null)
                continue;

            var material = new ShaderMaterial();
            material.Shader = shader;
            _shaderWarmupRect.Material = material;

            int frames = Math.Max(1, ShaderWarmupFramesPerShader);
            for (int i = 0; i < frames; i++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            warmed++;

            if (ShaderWarmupGapFrames > 0)
            {
                for (int i = 0; i < ShaderWarmupGapFrames; i++)
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }

        _shaderWarmupRect.Material = null;
        _shaderWarmupRect.Visible = false;
    }

    private async System.Threading.Tasks.Task WarmupSkillsAsync()
    {
        if (!WarmupAllSkills)
            return;

        try
        {
            var ids = (SkillID[])Enum.GetValues(typeof(SkillID));
            int warmed = 0;
            foreach (var id in ids)
            {
                var skill = Skill.GetSkill(id);
                if (skill == null)
                    continue;

                skill.SetPreviewStats(10, 10, 1);
                try
                {
                    skill.UpdateDescription();
                }
                catch (Exception e)
                {
                    GD.PrintErr($"Warmup skill failed: {id} ({e.Message})");
                }

                warmed++;
                if (SkillWarmupBatchSize > 0 && warmed % SkillWarmupBatchSize == 0)
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Warmup skills failed: {e.Message}");
        }
    }

    private void CleanupWarmupInstances()
    {
        if (_warmupInstances.Count == 0)
            return;

        foreach (var instance in _warmupInstances)
        {
            if (GodotObject.IsInstanceValid(instance))
                instance.QueueFree();
        }
        _warmupInstances.Clear();
    }

    private async System.Threading.Tasks.Task ScanAndLoadAsync(string dirPath)
    {
        var paths = ScanScenePaths(dirPath);
        int batchSize = Math.Max(1, PreloadSceneBatchSize);
        for (int i = 0; i < paths.Count; i++)
        {
            LoadResource(paths[i]);
            if ((i + 1) % batchSize == 0)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private void ScanAndLoad(string dirPath)
    {
        using var dir = DirAccess.Open(dirPath);
        if (dir == null)
        {
            GD.PrintErr($"Could not open directory: {dirPath}");
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();

        while (fileName != "")
        {
            if (dir.CurrentIsDir())
            {
                // Skip hidden folders (like .godot) which start with .
                if (!fileName.StartsWith("."))
                {
                    ScanAndLoad(dirPath.PathJoin(fileName));
                }
            }
            else
            {
                if (fileName.EndsWith(".tscn") || fileName.EndsWith(".scn"))
                {
                    string fullPath = dirPath.PathJoin(fileName);
                    LoadResource(fullPath);
                }
            }
            fileName = dir.GetNext();
        }
    }

    private void LoadResource(string path)
    {
        try
        {
            // Skip loading the preload scene itself to avoid redundancy/issues
            if (path.Contains("PreloadeScene.tscn"))
                return;

            // Load and cache the scene
            var resource = GD.Load<PackedScene>(path);
            if (resource != null)
            {
                PreloadedScenes[path] = resource;
                if (LogPreloadedResources)
                    GD.Print($"Preloaded: {path}");
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to load {path}: {e.Message}");
        }
    }

    private void AddCharacterPortraitPaths(ISet<string> paths)
    {
        if (paths == null)
            return;

        // Player characters
        string[] playerPortraits = new string[]
        {
            "res://asset/PlayerCharater/Echo/EchoPortrait.png",
            "res://asset/PlayerCharater/Kasiya/KasiyaPortrait.png",
            "res://asset/PlayerCharater/Mariya/MariyaPortrait.png",
            "res://asset/PlayerCharater/Nightingale/NightingalePortrait.png",
        };

        // Enemy characters
        string[] enemyPortraits = new string[]
        {
            "res://asset/EnemyCharater/AlienBody.png",
            "res://asset/EnemyCharater/Armon.png",
            "res://asset/EnemyCharater/Arrogance.png",
            "res://asset/EnemyCharater/BlackHawk.png",
            "res://asset/EnemyCharater/Evil.png",
            "res://asset/EnemyCharater/FearWorm.png",
            "res://asset/EnemyCharater/Ferociouess.png",
            "res://asset/EnemyCharater/Inexorability.png",
            "res://asset/EnemyCharater/RedHusk.png",
            "res://asset/EnemyCharater/Turbine.png",
            "res://asset/EnemyCharater/War.png",
        };

        foreach (var path in playerPortraits)
            paths.Add(path);

        foreach (var path in enemyPortraits)
            paths.Add(path);
    }

    private void LoadTexture(string path)
    {
        try
        {
            if (ResourceLoader.Exists(path))
            {
                var texture = GD.Load<Texture2D>(path);
                if (texture != null)
                {
                    PreloadedTextures[path] = texture;
                    if (LogPreloadedResources)
                        GD.Print($"Preloaded texture: {path}");
                }
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to preload texture {path}: {e.Message}");
        }
    }
}
