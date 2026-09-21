using System;
using System.Collections.Generic;
using Godot;

public partial class PreloadeScene : Node2D
{
    private static bool _exitCleanupPerformed;

    [Export]
    public string MainScenePath = "res://BeginGame/StartInterface.tscn";

    [Export]
    public float BlackScreenMinTime = 0.3f;

    [Export]
    public float FadeOutDuration = 0.25f;

    [Export]
    public float WarmupHoldTime = 0.6f;

    [Export]
    public bool WarmupBeforeMainScene = true;

    [Export]
    public bool RunBackgroundWarmupAfterMainScene = false;

    [Export]
    public float BackgroundWarmupDelay = 2f;

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
    public bool PreloadSkillIconTextures = true;

    [Export]
    public int ShaderWarmupMaxCount = 0;

    [Export]
    public int ShaderWarmupFramesPerShader = 1;

    [Export]
    public int ShaderWarmupGapFrames = 1;

    [Export]
    public int WarmupSceneYieldFrames = 1;

    [Export(PropertyHint.Range, "1,16,0.5")]
    public float ForegroundWarmupFrameBudgetMilliseconds = 3f;

    [Export(PropertyHint.Range, "1,16,0.5")]
    public float BackgroundWarmupFrameBudgetMilliseconds = 2f;

    [Export]
    public bool LogPreloadedResources = false;

    private Node _sceneHolder;
    private Node _warmupHolder;
    private ColorRect _shaderWarmupRect;
    private SceneTransitionLayer _transitionLayer;
    private readonly List<Node> _warmupInstances = new();
    private bool _backgroundWarmupRunning;
    private bool _warmupFinished;

    // Store references to keep them in memory
    public static Dictionary<string, PackedScene> PreloadedScenes =
        new Dictionary<string, PackedScene>();

    // Preloaded textures cache to avoid runtime GD.Load stalls in exported builds
    public static Dictionary<string, Texture2D> PreloadedTextures =
        new Dictionary<string, Texture2D>();

    private static readonly string[] SkillTextureExtensions = [".png", ".jpg", ".jpeg", ".webp"];

    private static readonly SkillID[] SharedBasicSkillArtIds =
    [
        SkillID.BasicAttack,
        SkillID.BasicDefense,
        SkillID.BasicGuard,
        SkillID.BasicSpecial,
    ];

    public static void ReleaseCachedResources()
    {
        if (_exitCleanupPerformed)
            return;

        _exitCleanupPerformed = true;

        PreloadedTextures.Clear();
        PreloadedScenes.Clear();
        StarfieldBackground3D.ReleaseCachedResources();

        SkillCard.ClearSharedCaches();
    }

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
        UserSettings.ApplyWindowSettings(GetWindow());
        I18n.SetLocale(UserSettings.Locale);

        _sceneHolder = GetNodeOrNull<Node>("SceneHolder");
        _warmupHolder = GetNodeOrNull<Node>("WarmupHolder");
        _shaderWarmupRect = GetNodeOrNull<ColorRect>("FadeLayer/ShaderWarmupRect");
        _transitionLayer = SceneTransitionLayer.Ensure(this, deferAddToRoot: true);

        _transitionLayer?.ShowBlackImmediate();
        _transitionLayer?.SetLoadingSpinnerEnabled(true);
        SetWarmupProgress(0.01f, "正在载入主界面…");

        // Render the loading overlay before asking the resource loader to do any work.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        PackedScene mainScene = await LoadMainSceneAsync();
        if (mainScene == null)
            return;

        if (WarmupBeforeMainScene)
            await RunWarmupPipelineAsync(background: false);

        if (BlackScreenMinTime > 0f)
            await ToSignal(
                GetTree().CreateTimer(BlackScreenMinTime),
                SceneTreeTimer.SignalName.Timeout
            );

        if (WarmupBeforeMainScene && WarmupHoldTime > 0f)
            await ToSignal(
                GetTree().CreateTimer(WarmupHoldTime),
                SceneTreeTimer.SignalName.Timeout
            );

        if (WarmupBeforeMainScene)
            CleanupWarmupInstances();

        // Scene instantiation can run arbitrary _Ready code and cannot be interrupted.
        // Keep the loading page visible, but stop the spinner before this static stage.
        _transitionLayer?.SetLoadingSpinnerEnabled(false);
        SetWarmupProgress(0.98f, "正在创建主界面…");
        Node mainSceneInstance = LoadMainSceneIntoHolder(mainScene);
        await WaitForMainScenePresentationAsync(mainSceneInstance);

        _transitionLayer?.HideLoadingProgress();
        if (_transitionLayer != null)
            await _transitionLayer.FadeFromBlackAsync(FadeOutDuration);

        if (RunBackgroundWarmupAfterMainScene)
            StartBackgroundWarmup();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            ReleaseCachedResources();
    }

    private bool CanContinueWarmup()
    {
        return GodotObject.IsInstanceValid(this) && IsInsideTree() && !_exitCleanupPerformed;
    }

    private async System.Threading.Tasks.Task<PackedScene> LoadMainSceneAsync()
    {
        if (string.IsNullOrWhiteSpace(MainScenePath))
            return null;

        Error requestError = ResourceLoader.LoadThreadedRequest(MainScenePath);
        if (requestError == Error.Ok)
        {
            while (CanContinueWarmup())
            {
                ResourceLoader.ThreadLoadStatus status = ResourceLoader.LoadThreadedGetStatus(MainScenePath);
                if (status == ResourceLoader.ThreadLoadStatus.Loaded)
                    return ResourceLoader.LoadThreadedGet(MainScenePath) as PackedScene;

                if (status == ResourceLoader.ThreadLoadStatus.Failed)
                    break;

                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }

        // This fallback also supports platforms where threaded loading is unavailable.
        return GD.Load<PackedScene>(MainScenePath);
    }

    private Node LoadMainSceneIntoHolder(PackedScene packed)
    {
        if (_sceneHolder == null)
            return null;
        if (packed == null)
        {
            GD.PushError($"Failed to load main scene: {MainScenePath}");
            return null;
        }

        var instance = packed.Instantiate();
        _sceneHolder.AddChild(instance);
        return instance;
    }

    private async System.Threading.Tasks.Task WaitForMainScenePresentationAsync(Node mainScene)
    {
        if (mainScene is not StartInterface startInterface)
            return;

        const int frameLimit = 120;
        for (int frame = 0; frame < frameLimit; frame++)
        {
            if (!CanContinueWarmup())
                return;

            if (startInterface.IsPresentationReady)
            {
                // Submit one complete title frame while the static loading page is still up.
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                return;
            }

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        GD.PushWarning("PreloadeScene: main scene presentation was not ready before timeout.");
    }

    private void StartBackgroundWarmup()
    {
        if (_backgroundWarmupRunning || _warmupFinished)
            return;
        if (!IsInsideTree())
            return;

        _backgroundWarmupRunning = true;
        _ = RunBackgroundWarmupAsync();
    }

    private async System.Threading.Tasks.Task RunBackgroundWarmupAsync()
    {
        try
        {
            if (BackgroundWarmupDelay > 0f)
            {
                await ToSignal(
                    GetTree().CreateTimer(BackgroundWarmupDelay),
                    SceneTreeTimer.SignalName.Timeout
                );
                if (!CanContinueWarmup())
                    return;
            }

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (!CanContinueWarmup())
                return;

            await RunWarmupPipelineAsync(background: true);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Background warmup failed: {e.Message}");
        }
        finally
        {
            CleanupWarmupInstances();
            HideShaderWarmupRect();
            _backgroundWarmupRunning = false;
        }
    }

    private async System.Threading.Tasks.Task RunWarmupPipelineAsync(bool background)
    {
        if (_warmupFinished)
            return;
        if (!CanContinueWarmup())
            return;

        string logPrefix = background ? "Background " : "";

        SetWarmupProgress(background, 0.06f, "正在读取场景资源…");
        GD.Print($"--- Start {logPrefix}Preloading Scenes ---");
        await ScanAndLoadAsync("res://", background);
        if (!CanContinueWarmup())
            return;
        GD.Print($"--- Finished {logPrefix}Preloading. Loaded {PreloadedScenes.Count} scenes ---");

        SetWarmupProgress(background, 0.24f, "正在载入角色与卡牌资源…");
        GD.Print($"--- Start {logPrefix}Preloading Textures ---");
        await PreloadTexturesAsync(background);
        if (!CanContinueWarmup())
            return;
        GD.Print($"--- Finished {logPrefix}Preloading. Loaded {PreloadedTextures.Count} textures ---");

        // Scene instantiation and first-use pipeline compilation cannot be preempted by
        // a frame budget. Keep the loading page up, but stop the spinner before entering
        // this static submission phase so players never see a frozen loading animation.
        if (!background)
            _transitionLayer?.SetLoadingSpinnerEnabled(false);

        SetWarmupProgress(background, 0.44f, "正在预热战斗特效与星空…");
        await BattleStartResourcePreloader.PrewarmStartupResourcesAsync(this);
        if (!CanContinueWarmup())
            return;

        SetWarmupProgress(background, 0.60f, "正在初始化游戏场景…");
        await WarmupInstantiateScenesAsync(background);
        if (!CanContinueWarmup())
            return;
        SetWarmupProgress(background, 0.74f, "正在准备技能数据…");
        await WarmupSkillsAsync(background);
        if (!CanContinueWarmup())
            return;
        SetWarmupProgress(background, 0.88f, "正在编译视觉效果…");
        await WarmupShadersAsync(background);
        if (!CanContinueWarmup())
            return;
        SkillCard.PrewarmExhaustEffect();

        _warmupFinished = true;
        SetWarmupProgress(background, 1f, "准备完成");
    }

    private void SetWarmupProgress(float progress, string status)
    {
        _transitionLayer?.ShowLoadingProgress(progress, status);
    }

    private void SetWarmupProgress(bool background, float progress, string status)
    {
        if (!background)
            SetWarmupProgress(progress, status);
    }

    private async System.Threading.Tasks.Task WarmupInstantiateScenesAsync(bool background)
    {
        if (_warmupHolder == null)
            return;

        var paths = BuildWarmupSceneList();
        if (paths.Count == 0)
            return;

        ulong sliceStartUsec = Time.GetTicksUsec();
        foreach (var path in paths)
        {
            if (!CanContinueWarmup())
                return;

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

            sliceStartUsec = await YieldWhenFrameBudgetExceededAsync(
                sliceStartUsec,
                background
            );
            if (sliceStartUsec == 0)
                return;

            if (WarmupSceneYieldFrames > 0)
            {
                for (int i = 0; i < WarmupSceneYieldFrames; i++)
                {
                    if (!CanContinueWarmup())
                        return;
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    sliceStartUsec = Time.GetTicksUsec();
                }
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
            AddCachedScenesWithPrefix(result, "res://battle/Effect/");
        }

        if (WarmupAllBuffIconScenes)
        {
            AddCachedScenesWithPrefix(result, "res://battle/buff/");
        }

        if (WarmupAllBattleUIScenes)
        {
            AddCachedScenesWithPrefix(result, "res://battle/UIScene/");
        }

        if (!string.IsNullOrWhiteSpace(MainScenePath))
            result.Remove(MainScenePath);

        result.Remove("res://PreloadeScene.tscn");

        return new List<string>(result);
    }

    private void AddCachedScenesWithPrefix(ISet<string> results, string prefix)
    {
        if (results == null || string.IsNullOrWhiteSpace(prefix))
            return;

        if (PreloadedScenes.Count > 0)
        {
            foreach (var path in PreloadedScenes.Keys)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    results.Add(path);
            }
            return;
        }

        foreach (var path in ScanScenePaths(prefix.TrimEnd('/')))
            results.Add(path);
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
                string childPath = dirPath.PathJoin(fileName);
                if (!fileName.StartsWith(".") && !IsGeneratedPreloadDirectory(childPath))
                {
                    results.AddRange(ScanScenePaths(childPath));
                }
            }
            else
            {
                string resourcePath = GetListedResourcePath(dirPath, fileName, ".tscn", ".scn");
                if (resourcePath != null)
                    results.Add(resourcePath);
            }

            fileName = dir.GetNext();
        }

        return results;
    }

    private static bool IsGeneratedPreloadDirectory(string path)
    {
        return path.StartsWith("res://android/build", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("res://addons", StringComparison.OrdinalIgnoreCase);
    }

    private async System.Threading.Tasks.Task PreloadTexturesAsync(bool background)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddCharacterPortraitPaths(paths);
        ulong sliceStartUsec = Time.GetTicksUsec();

        if (PreloadSkillIconTextures)
        {
            AddExplicitSkillIconTexturePaths(paths);
            sliceStartUsec = await CollectResourcePathsAsync(
                "res://asset/svg/SkillIcon",
                paths,
                background,
                sliceStartUsec,
                ".png",
                ".jpg",
                ".jpeg",
                ".webp",
                ".svg"
            );
            if (sliceStartUsec == 0)
                return;
        }

        if (PreloadSkillArtTextures)
        {
            AddExplicitSkillArtTexturePaths(paths);
            sliceStartUsec = await CollectResourcePathsAsync(
                "res://asset/CardPicture",
                paths,
                background,
                sliceStartUsec,
                ".png",
                ".jpg",
                ".jpeg",
                ".webp",
                ".svg"
            );
            if (sliceStartUsec == 0)
                return;
        }

        foreach (var path in paths)
        {
            if (!CanContinueWarmup())
                return;

            await LoadTextureAsync(path);
            if (!CanContinueWarmup())
                return;

            sliceStartUsec = await YieldWhenFrameBudgetExceededAsync(sliceStartUsec, background);
            if (sliceStartUsec == 0)
                return;
        }
    }

    private async System.Threading.Tasks.Task WarmupShadersAsync(bool background)
    {
        if (!WarmupAllShaders)
            return;
        if (_shaderWarmupRect == null)
            return;

        var shaderPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ulong sliceStartUsec = await CollectResourcePathsAsync(
            "res://shader",
            shaderPaths,
            background,
            Time.GetTicksUsec(),
            ".gdshader",
            ".shader"
        );
        if (sliceStartUsec == 0)
            return;
        if (shaderPaths.Count == 0)
            return;

        try
        {
            _shaderWarmupRect.Visible = true;

            int warmed = 0;
            foreach (var path in shaderPaths)
            {
                if (!CanContinueWarmup())
                    return;

                if (ShaderWarmupMaxCount > 0 && warmed >= ShaderWarmupMaxCount)
                    break;

                var shader = GD.Load<Shader>(path);
                if (shader == null)
                    continue;

                var material = new ShaderMaterial();
                material.Shader = shader;
                _shaderWarmupRect.Material = material;

                sliceStartUsec = await YieldWhenFrameBudgetExceededAsync(
                    sliceStartUsec,
                    background
                );
                if (sliceStartUsec == 0)
                    return;

                int frames = Math.Max(1, ShaderWarmupFramesPerShader);
                for (int i = 0; i < frames; i++)
                {
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    if (!CanContinueWarmup())
                        return;
                    sliceStartUsec = Time.GetTicksUsec();
                }

                warmed++;

                if (ShaderWarmupGapFrames > 0)
                {
                    for (int i = 0; i < ShaderWarmupGapFrames; i++)
                    {
                        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                        if (!CanContinueWarmup())
                            return;
                        sliceStartUsec = Time.GetTicksUsec();
                    }
                }
            }
        }
        finally
        {
            HideShaderWarmupRect();
        }
    }

    private async System.Threading.Tasks.Task WarmupSkillsAsync(bool background)
    {
        if (!WarmupAllSkills)
            return;

        try
        {
            var ids = (SkillID[])Enum.GetValues(typeof(SkillID));
            ulong sliceStartUsec = Time.GetTicksUsec();
            foreach (var id in ids)
            {
                if (!CanContinueWarmup())
                    return;

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

                sliceStartUsec = await YieldWhenFrameBudgetExceededAsync(
                    sliceStartUsec,
                    background
                );
                if (sliceStartUsec == 0)
                    return;
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

    private void HideShaderWarmupRect()
    {
        if (!GodotObject.IsInstanceValid(_shaderWarmupRect))
            return;

        _shaderWarmupRect.Material = null;
        _shaderWarmupRect.Visible = false;
    }

    private async System.Threading.Tasks.Task ScanAndLoadAsync(string dirPath, bool background)
    {
        await ScanAndLoadDirectoryAsync(dirPath, background, Time.GetTicksUsec());
    }

    private async System.Threading.Tasks.Task<ulong> ScanAndLoadDirectoryAsync(
        string dirPath,
        bool background,
        ulong sliceStartUsec
    )
    {
        using var dir = DirAccess.Open(dirPath);
        if (dir == null)
            return sliceStartUsec;

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (!CanContinueWarmup())
                return 0;

            if (dir.CurrentIsDir())
            {
                string childPath = dirPath.PathJoin(fileName);
                if (!fileName.StartsWith(".") && !IsGeneratedPreloadDirectory(childPath))
                {
                    sliceStartUsec = await ScanAndLoadDirectoryAsync(
                        childPath,
                        background,
                        sliceStartUsec
                    );
                    if (sliceStartUsec == 0)
                        return 0;
                }
            }
            else
            {
                string resourcePath = GetListedResourcePath(dirPath, fileName, ".tscn", ".scn");
                if (resourcePath != null)
                {
                    await LoadSceneResourceAsync(resourcePath);
                    if (!CanContinueWarmup())
                        return 0;
                    sliceStartUsec = Time.GetTicksUsec();
                }
            }

            sliceStartUsec = await YieldWhenFrameBudgetExceededAsync(
                sliceStartUsec,
                background
            );
            if (sliceStartUsec == 0)
                return 0;

            fileName = dir.GetNext();
        }

        return sliceStartUsec;
    }

    private async System.Threading.Tasks.Task<ulong> CollectResourcePathsAsync(
        string dirPath,
        ISet<string> paths,
        bool background,
        ulong sliceStartUsec,
        params string[] allowedExtensions
    )
    {
        if (paths == null)
            return sliceStartUsec;

        using var dir = DirAccess.Open(dirPath);
        if (dir == null)
            return sliceStartUsec;

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (!CanContinueWarmup())
                return 0;

            if (dir.CurrentIsDir())
            {
                string childPath = dirPath.PathJoin(fileName);
                if (!fileName.StartsWith("."))
                {
                    sliceStartUsec = await CollectResourcePathsAsync(
                        childPath,
                        paths,
                        background,
                        sliceStartUsec,
                        allowedExtensions
                    );
                    if (sliceStartUsec == 0)
                        return 0;
                }
            }
            else
            {
                string resourcePath = GetListedResourcePath(dirPath, fileName, allowedExtensions);
                if (resourcePath != null)
                    paths.Add(resourcePath);
            }

            sliceStartUsec = await YieldWhenFrameBudgetExceededAsync(
                sliceStartUsec,
                background
            );
            if (sliceStartUsec == 0)
                return 0;

            fileName = dir.GetNext();
        }

        return sliceStartUsec;
    }

    private async System.Threading.Tasks.Task<ulong> YieldWhenFrameBudgetExceededAsync(
        ulong sliceStartUsec,
        bool background
    )
    {
        if (!CanContinueWarmup())
            return 0;

        float budgetMilliseconds = background
            ? BackgroundWarmupFrameBudgetMilliseconds
            : ForegroundWarmupFrameBudgetMilliseconds;
        ulong budgetUsec = (ulong)Math.Max(1000d, Math.Round(budgetMilliseconds * 1000d));
        if (Time.GetTicksUsec() - sliceStartUsec < budgetUsec)
            return sliceStartUsec;

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        return CanContinueWarmup() ? Time.GetTicksUsec() : 0;
    }

    private async System.Threading.Tasks.Task LoadSceneResourceAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains("PreloadeScene.tscn"))
            return;

        if (PreloadedScenes.TryGetValue(path, out PackedScene cached) && cached != null)
            return;

        Resource resource = await LoadThreadedResourceAsync(path);
        if (resource is PackedScene scene)
        {
            PreloadedScenes[path] = scene;
            if (LogPreloadedResources)
                GD.Print($"Preloaded: {path}");
        }
    }

    private static string GetListedResourcePath(
        string dirPath,
        string fileName,
        params string[] allowedExtensions
    )
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        string resourceFileName = fileName.EndsWith(".remap", StringComparison.OrdinalIgnoreCase)
            ? fileName[..^".remap".Length]
            : fileName;

        foreach (string extension in allowedExtensions)
        {
            if (resourceFileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                return dirPath.PathJoin(resourceFileName);
        }

        return null;
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

    private void AddExplicitSkillArtTexturePaths(ISet<string> paths)
    {
        if (paths == null)
            return;

        foreach (PlayerCharacterKey characterKey in Enum.GetValues<PlayerCharacterKey>())
        {
            string folder = characterKey.ToString();
            foreach (SkillID skillId in Skill.GetPlayerSkillPool(characterKey))
                AddSkillArtTextureCandidates(paths, folder, skillId);

            foreach (SkillID skillId in SharedBasicSkillArtIds)
                AddSkillArtTextureCandidates(paths, folder, skillId);
        }
    }

    private void AddSkillArtTextureCandidates(ISet<string> paths, string folder, SkillID skillId)
    {
        if (paths == null || string.IsNullOrWhiteSpace(folder))
            return;

        string skillFileName = skillId.ToString();
        foreach (string extension in SkillTextureExtensions)
            paths.Add($"res://asset/CardPicture/{folder}/{skillFileName}{extension}");
    }

    private void AddExplicitSkillIconTexturePaths(ISet<string> paths)
    {
        if (paths == null)
            return;

        foreach (SkillID skillId in Enum.GetValues<SkillID>())
            paths.Add($"res://asset/svg/SkillIcon/{skillId}.svg");
    }

    private async System.Threading.Tasks.Task LoadTextureAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (PreloadedTextures.TryGetValue(path, out Texture2D cached) && cached != null)
            return;

        Resource resource = await LoadThreadedResourceAsync(path);
        if (resource is Texture2D texture)
        {
            PreloadedTextures[path] = texture;
            if (LogPreloadedResources)
                GD.Print($"Preloaded texture: {path}");
        }
    }

    private async System.Threading.Tasks.Task<Resource> LoadThreadedResourceAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !ResourceLoader.Exists(path))
            return null;

        Error requestError = ResourceLoader.LoadThreadedRequest(path);
        if (requestError == Error.Ok)
        {
            while (CanContinueWarmup())
            {
                ResourceLoader.ThreadLoadStatus status = ResourceLoader.LoadThreadedGetStatus(path);
                if (status == ResourceLoader.ThreadLoadStatus.Loaded)
                    return ResourceLoader.LoadThreadedGet(path);

                if (status == ResourceLoader.ThreadLoadStatus.Failed)
                    break;

                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }

        if (!CanContinueWarmup())
            return null;

        // Threaded loading can be unavailable for a specific resource or platform.
        return GD.Load<Resource>(path);
    }
}
