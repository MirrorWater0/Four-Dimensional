using System;
using System.Collections.Generic;
using Godot;

public partial class PlayerResourceState : CanvasLayer
{
    private static readonly PackedScene MenuScene = GD.Load<PackedScene>("res://Menu/Menu.tscn");
    private const long MaxDisplayHours = 99;
    private static readonly Dictionary<string, Texture2D> PortraitCache = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> MapPortraitPaths = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["res://asset/PlayerCharater/Kasiya/KasiyaPortrait.png"] =
            "res://asset/PlayerCharater/Kasiya/KasiyaMapPortrait_v1.png",
        ["res://asset/PlayerCharater/Mariya/MariyaPortrait.png"] =
            "res://asset/PlayerCharater/Mariya/MariyaMapPortrait_v1.png",
        ["res://asset/PlayerCharater/Nightingale/NightingalePortrait.png"] =
            "res://asset/PlayerCharater/Nightingale/NightingaleMapPortrait_v1.png",
        ["res://asset/PlayerCharater/Echo/EchoPortrait.png"] =
            "res://asset/PlayerCharater/Echo/EchoMapPortrait_v1.png",
    };

    private readonly List<PartyLifeSlot> _partyLifeSlots = new();
    private readonly List<ResourceVisibilitySnapshot> _mapPeekHiddenResources = new();
    private Tip _mapPeekTip;
    private bool _mapPeekButtonHovered;
    private Tween _mapPeekHoverTween;
    private int _activeBattleCount;

    private sealed class PartyLifeSlot
    {
        public Control Root;
        public TextureRect Portrait;
        public ProgressBar LifeBar;
        public Label NameLabel;
        public Label ValueLabel;
    }

    private sealed class ResourceVisibilitySnapshot
    {
        public CanvasItem Item;
        public bool Visible;
    }

    public int ElectricityCoin
    {
        get => GameInfo.ElectricityCoin;
        set
        {
            CreateTween()
                .TweenMethod(
                    Callable.From<float>(value =>
                        ElectricityCoinIcon.GetChild<Label>(0).Text = value.ToString()
                    ),
                    GameInfo.ElectricityCoin,
                    value,
                    0.4f
                );
            GameInfo.ElectricityCoin = value;
        }
    }

    public int TransitionEnergy
    {
        get => GameInfo.GetPartyLife();
        set
        {
            GameInfo.SetPartyLifeTotal(value);
            RefreshPartyLifeResource();
        }
    }

    public int CoreEnergy
    {
        get => TransitionEnergy;
        set => TransitionEnergy = value;
    }

    public int TransistionEnergyMax => GameInfo.GetPartyMaxLife();
    public int CoreEnergyMax => GameInfo.GetPartyMaxLife();

    public Control TransitionEnergyControl => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<Control>("TransitionEnergyControl");

    private Label CoreEnergyNameLabel => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = TransitionEnergyControl?.GetNodeOrNull<Label>("Label");

    private Label DifficultyLabel => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = TransitionEnergyControl?.GetNodeOrNull<Label>("Difficulty");

    public ColorRect ElectricityCoinIcon => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<ColorRect>("ElectricityCoin");

    public List<Relic> RelicList = new();
    public GridContainer RelicContainer => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<GridContainer>("RelicContainer");
    public List<ConsumeItem> Items = new();
    public HBoxContainer ItemContainer => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<HBoxContainer>("ItemsHContainer");
    private Button MenuButton => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<Button>("MenuButton");
    private Button MapPeekButton => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<Button>("MapPeekButton");
    private ColorRect MapPeekIcon => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = MapPeekButton?.GetNodeOrNull<ColorRect>("Icon");
    private Map MapNode => field is not null && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<Map>("/root/Map");
    private Label TimerValueLabel => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<Label>("StatusPanel/StatusMargin/StatusRow/TimerBlock/Value");
    private Timer SessionTimer => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<Timer>("SessionTimer");
    private CanvasLayer MenuLayer => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<CanvasLayer>("/root/Map/MenuLayer");
    private CanvasLayer FrontUiLayer => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<CanvasLayer>("/root/Map/BattleReadyLayer");
    public Node FrontUiRoot
    {
        get
        {
            CanvasLayer frontLayer = FrontUiLayer;
            return frontLayer != null ? frontLayer : GetTree()?.Root;
        }
    }
    private Menu MenuOverlay => field is not null && GodotObject.IsInstanceValid(field)
        ? field
        : field = MenuLayer?.GetNodeOrNull<Menu>("Menu");
    private DebugConsole DebugConsoleNode =>
        field is not null && GodotObject.IsInstanceValid(field)
            ? field
            : field = GetNodeOrNull<DebugConsole>("/root/Map/DebugConsole");

    public override void _Ready()
    {
        SetProcessUnhandledInput(false);
        EnsureMenuOverlay();
        ElectricityCoin = GameInfo.ElectricityCoin;
        InitTransitionEnergyMax();
        RefreshPartyLifeResource();
        InitRelic();
        InitItems();
        RefreshStatusPanel();
        if (MenuButton != null)
            MenuButton.Pressed += OnMenuButtonPressed;
        if (MapPeekButton != null)
        {
            MapPeekButton.Pressed += OnMapPeekButtonPressed;
            MapPeekButton.MouseEntered += ShowMapPeekTip;
            MapPeekButton.MouseExited += HideMapPeekTip;
            MapPeekButton.TreeExiting += HideMapPeekTip;
            RefreshMapPeekButton();
        }
        if (SessionTimer != null)
        {
            SessionTimer.Timeout += OnSessionTimerTimeout;
            SessionTimer.Start();
        }
    }

    public override void _ExitTree()
    {
        SetProcessUnhandledInput(false);
        HideMapPeekTip();
        base._ExitTree();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (
            MapNode?.IsMapPeekModeActive != true
            || @event is not InputEventKey keyEvent
            || !keyEvent.Pressed
            || keyEvent.Echo
            || keyEvent.Keycode != Key.Escape
        )
        {
            return;
        }

        GetViewport().SetInputAsHandled();
        CloseMapPeekMode();
    }

    public void InitRelic()
    {
        GameInfo.NormalizeRelics();
        foreach (RelicStack stack in GameInfo.Relics)
        {
            Relic relicInst = Relic.Create(stack.ID);
            relicInst.Num = stack.Count;
            relicInst.IconAdd(this);
            RelicList.Add(relicInst);
        }
    }

    public void InitItems()
    {
        Items.Clear();
        if (GameInfo.Items == null || GameInfo.Items.Count == 0)
            return;

        foreach (var item in GameInfo.Items)
            ConsumeItem.AddItem(this, item, syncGameInfo: false);
    }

    public void SetItemsEnabled(bool enabled)
    {
        var itemContainer = ItemContainer;
        if (
            itemContainer == null
            || !GodotObject.IsInstanceValid(itemContainer)
            || !itemContainer.IsInsideTree()
        )
            return;

        for (int i = itemContainer.GetChildCount() - 1; i >= 0; i--)
        {
            if (itemContainer.GetChild(i) is ItemContainer container)
                container.SetEnabled(enabled);
        }
    }

    public void InitTransitionEnergyMax(bool resetPreviousValue = true)
    {
        if (TransitionEnergyControl == null || !GodotObject.IsInstanceValid(TransitionEnergyControl))
            return;

        if (CoreEnergyNameLabel != null)
            CoreEnergyNameLabel.Text = I18n.Tr("ui.common.party_life", "队伍生命");
        EnsurePartyLifeSlots();
    }

    private void RefreshTransitionEnergyUI(int value)
    {
        if (TransitionEnergyControl == null || !GodotObject.IsInstanceValid(TransitionEnergyControl))
            return;

        GameInfo.NormalizePlayerCharacters();
        EnsurePartyLifeSlots();

        int totalLife = GameInfo.GetPartyLife();
        int totalMaxLife = Math.Max(1, GameInfo.GetPartyMaxLife());
        GameInfo.TransitionEnergy = totalLife;
        GameInfo.TransitionEnergyMax = totalMaxLife;

        var players = GameInfo.PlayerCharacters ?? Array.Empty<PlayerInfoStructure>();
        for (int i = 0; i < _partyLifeSlots.Count; i++)
        {
            var slot = _partyLifeSlots[i];
            if (slot?.Root == null || !GodotObject.IsInstanceValid(slot.Root))
                continue;

            bool hasPlayer = i < players.Length;
            slot.Root.Visible = hasPlayer;
            if (!hasPlayer)
                continue;

            var info = players[i];
            int lifeMax = Math.Max(1, info.LifeMax);
            int life = Math.Clamp(info.Life, 0, lifeMax);
            slot.NameLabel.Text = BuildPartyLifeSlotName(info, i);
            slot.ValueLabel.Text = $"{life}/{lifeMax}";
            slot.LifeBar.MaxValue = lifeMax;
            slot.LifeBar.Value = life;
            slot.Portrait.Texture = LoadPortraitTexture(GetMapPortraitPath(info));
            slot.Root.TooltipText = $"{slot.NameLabel.Text}  {life}/{lifeMax}";
        }
    }

    public void RefreshDebugView()
    {
        ElectricityCoinIcon.GetChild<Label>(0).Text = GameInfo.ElectricityCoin.ToString();
        InitTransitionEnergyMax();
        RefreshPartyLifeResource();
        ClearRelicIcons();
        InitRelic();
        ClearItemIcons();
        InitItems();
        RefreshStatusPanel();
    }

    public void RefreshPartyLifeResource()
    {
        InitTransitionEnergyMax(resetPreviousValue: false);
        RefreshTransitionEnergyUI(GameInfo.GetPartyLife());
    }

    private void EnsurePartyLifeSlots()
    {
        var control = TransitionEnergyControl;
        if (control == null || !GodotObject.IsInstanceValid(control))
            return;

        GameInfo.NormalizePlayerCharacters();
        int requiredCount = GameInfo.PlayerCharacters?.Length ?? 0;
        if (_partyLifeSlots.Count == 0)
            BindPartyLifeSlots();

        for (int i = 0; i < _partyLifeSlots.Count; i++)
        {
            var slot = _partyLifeSlots[i];
            if (slot?.Root == null || !GodotObject.IsInstanceValid(slot.Root))
                continue;
            slot.Root.Visible = i < requiredCount;
        }
    }

    private void BindPartyLifeSlots()
    {
        _partyLifeSlots.Clear();
        for (int i = 1; i <= GameInfo.DefaultPlayerPartySize; i++)
        {
            var root = TransitionEnergyControl.GetNodeOrNull<Control>($"PartyLifeSlot{i}");
            if (root == null)
                continue;

            _partyLifeSlots.Add(
                new PartyLifeSlot
                {
                    Root = root,
                    Portrait = root.GetNodeOrNull<TextureRect>(
                        "Panel/Margin/Content/Portrait"
                    ),
                    LifeBar = root.GetNodeOrNull<ProgressBar>(
                        "Panel/Margin/Content/Body/LifeBar"
                    ),
                    NameLabel = root.GetNodeOrNull<Label>(
                        "Panel/Margin/Content/Body/BottomRow/Name"
                    ),
                    ValueLabel = root.GetNodeOrNull<Label>(
                        "Panel/Margin/Content/Body/BottomRow/Value"
                    ),
                }
            );
        }
    }

    private static string BuildPartyLifeSlotName(PlayerInfoStructure info, int index)
    {
        if (!string.IsNullOrWhiteSpace(info.CharacterName))
            return info.CharacterName;

        return $"P{index + 1}";
    }

    private static Texture2D LoadPortraitTexture(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (PortraitCache.TryGetValue(path, out Texture2D cached))
            return cached;

        Texture2D texture = null;
        if (PreloadeScene.PreloadedTextures.TryGetValue(path, out Texture2D preloaded))
            texture = preloaded;
        else if (ResourceLoader.Exists(path))
            texture = GD.Load<Texture2D>(path);

        PortraitCache[path] = texture;
        return texture;
    }

    private static string GetMapPortraitPath(PlayerInfoStructure info)
    {
        string portraitPath = info.PortaitPath;
        if (
            !string.IsNullOrWhiteSpace(portraitPath)
            && MapPortraitPaths.TryGetValue(portraitPath, out string mapPortraitPath)
            && ResourceLoader.Exists(mapPortraitPath)
        )
        {
            return mapPortraitPath;
        }

        return portraitPath;
    }

    private void OnMenuButtonPressed()
    {
        MenuOverlay?.Toggle();
    }

    private void OnMapPeekButtonPressed()
    {
        var map = MapNode;
        if (map == null || !GodotObject.IsInstanceValid(map))
            return;

        if (map.IsMapPeekModeActive)
            CloseMapPeekMode();
        else
            OpenMapPeekMode();
    }

    private void OpenMapPeekMode()
    {
        MapNode?.EnterMapPeekMode();
        SetResourceChromeVisibleForMapPeek(false);
        RefreshMapPeekButton();
    }

    public void CloseMapPeekMode()
    {
        MapNode?.ExitMapPeekMode();
        SetResourceChromeVisibleForMapPeek(true);
        RefreshPartyStatusVisibility();
        RefreshMapPeekButton();
    }

    public void SetMapPeekInputActive(bool active)
    {
        SetProcessUnhandledInput(active);
    }

    public void SetBattleActive(bool active)
    {
        int previousBattleCount = _activeBattleCount;
        _activeBattleCount = active
            ? _activeBattleCount + 1
            : Math.Max(0, _activeBattleCount - 1);
        bool hasActiveBattle = _activeBattleCount > 0;
        Map map = MapNode;
        map?.SetBattleWorldSuspended(hasActiveBattle);
        if (
            previousBattleCount > 0
            && !hasActiveBattle
            && map?.IsMapPeekModeActive == true
        )
        {
            CloseMapPeekMode();
        }
        RefreshPartyStatusVisibility();
    }

    private void RefreshPartyStatusVisibility()
    {
        Control partyStatus = TransitionEnergyControl;
        if (partyStatus == null || !GodotObject.IsInstanceValid(partyStatus))
            return;

        partyStatus.Visible = _activeBattleCount == 0 && MapNode?.IsMapPeekModeActive != true;
    }

    private void RefreshMapPeekButton()
    {
        if (MapPeekButton == null)
            return;

        bool active = MapNode?.IsMapPeekModeActive == true;
        MapPeekButton.Text = string.Empty;
        MapPeekButton.TooltipText = string.Empty;

        if (MapPeekIcon?.Material is ShaderMaterial material)
        {
            material.SetShaderParameter("active", active ? 1.0f : 0.0f);
            material.SetShaderParameter("hover_amount", _mapPeekButtonHovered ? 1.0f : 0.0f);
        }

        if (_mapPeekButtonHovered)
            ShowMapPeekTip();
    }

    private string GetMapPeekTipText()
    {
        bool active = MapNode?.IsMapPeekModeActive == true;
        return active
            ? I18n.Tr("ui.map.peek_close_tip", "返回当前界面")
            : I18n.Tr("ui.map.peek_open_tip", "查看地图");
    }

    private void ShowMapPeekTip()
    {
        _mapPeekButtonHovered = true;
        SetMapPeekHoverVisual(true);
        if (MapPeekButton == null || !MapPeekButton.Visible)
            return;

        var tip = EnsureMapPeekTip();
        if (tip == null)
            return;

        tip.FollowMouse = true;
        tip.SetText(GetMapPeekTipText());
    }

    private void HideMapPeekTip()
    {
        _mapPeekButtonHovered = false;
        SetMapPeekHoverVisual(false);
        if (_mapPeekTip != null && GodotObject.IsInstanceValid(_mapPeekTip))
            _mapPeekTip.HideTooltip();
    }

    private void SetMapPeekHoverVisual(bool hovered)
    {
        if (MapPeekIcon?.Material is not ShaderMaterial material)
            return;

        _mapPeekHoverTween?.Kill();
        _mapPeekHoverTween = CreateTween();
        float target = hovered ? 1.0f : 0.0f;
        _mapPeekHoverTween
            .TweenMethod(
                Callable.From<float>(value => material.SetShaderParameter("hover_amount", value)),
                GetShaderFloat(material, "hover_amount"),
                target,
                hovered ? 0.18f : 0.14f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(hovered ? Tween.EaseType.Out : Tween.EaseType.InOut);
    }

    private static float GetShaderFloat(ShaderMaterial material, string parameterName)
    {
        if (material == null)
            return 0.0f;

        Variant value = material.GetShaderParameter(parameterName);
        return value.VariantType == Variant.Type.Float ? value.AsSingle() : 0.0f;
    }

    private Tip EnsureMapPeekTip()
    {
        if (_mapPeekTip != null && GodotObject.IsInstanceValid(_mapPeekTip))
            return _mapPeekTip;

        var root = GetTree()?.Root;
        if (root == null)
            return null;

        var layer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (layer == null)
        {
            layer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
            root.AddChild(layer);
        }

        var existing = layer.GetNodeOrNull<Tip>("MapPeekTip");
        if (existing != null)
        {
            _mapPeekTip = existing;
            return _mapPeekTip;
        }

        var tipScene = GD.Load<PackedScene>("res://battle/UIScene/Tip.tscn");
        var tip = tipScene?.Instantiate<Tip>();
        if (tip == null)
            return null;

        tip.Name = "MapPeekTip";
        tip.FollowMouse = true;
        tip.AnchorOffset = new Vector2(20f, 20f);
        layer.AddChild(tip);
        _mapPeekTip = tip;
        return _mapPeekTip;
    }

    private void SetResourceChromeVisibleForMapPeek(bool visible)
    {
        if (visible)
        {
            RestoreMapPeekResourceVisibility();
            return;
        }

        HideResourceForMapPeek(GetNodeOrNull<CanvasItem>("TopPartyBackplate"));
        HideResourceForMapPeek(GetNodeOrNull<CanvasItem>("RegionLabel"));
        HideResourceForMapPeek(GetNodeOrNull<CanvasItem>("StatusPanel"));
        HideResourceForMapPeek(TransitionEnergyControl);
        HideResourceForMapPeek(ElectricityCoinIcon);
        HideResourceForMapPeek(RelicContainer);
        HideResourceForMapPeek(ItemContainer);
        HideResourceForMapPeek(MenuButton);
    }

    private void HideResourceForMapPeek(CanvasItem item)
    {
        if (
            item == null
            || !GodotObject.IsInstanceValid(item)
            || item.IsQueuedForDeletion()
            || item == MapPeekButton
            || !item.Visible
        )
        {
            return;
        }

        _mapPeekHiddenResources.Add(
            new ResourceVisibilitySnapshot { Item = item, Visible = item.Visible }
        );
        item.Visible = false;
    }

    private void RestoreMapPeekResourceVisibility()
    {
        for (int i = _mapPeekHiddenResources.Count - 1; i >= 0; i--)
        {
            var snapshot = _mapPeekHiddenResources[i];
            if (
                snapshot?.Item == null
                || !GodotObject.IsInstanceValid(snapshot.Item)
                || snapshot.Item.IsQueuedForDeletion()
            )
            {
                continue;
            }

            snapshot.Item.Visible = snapshot.Visible;
        }

        _mapPeekHiddenResources.Clear();
    }

    private void EnsureMenuOverlay()
    {
        if (MenuLayer == null || MenuOverlay != null)
            return;

        var menu = MenuScene?.Instantiate<Menu>();
        if (menu == null)
            return;

        menu.Name = "Menu";
        MenuLayer.AddChild(menu);
    }

    private void ClearRelicIcons()
    {
        RelicList.Clear();
        var relicContainer = RelicContainer;
        if (
            relicContainer == null
            || !GodotObject.IsInstanceValid(relicContainer)
            || !relicContainer.IsInsideTree()
        )
            return;

        for (int i = relicContainer.GetChildCount() - 1; i >= 0; i--)
        {
            Node child = relicContainer.GetChild(i);
            if (child is Label)
                continue;
            relicContainer.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void ClearItemIcons()
    {
        Items.Clear();
        var itemContainer = ItemContainer;
        if (
            itemContainer == null
            || !GodotObject.IsInstanceValid(itemContainer)
            || !itemContainer.IsInsideTree()
        )
            return;

        for (int i = 0; i < itemContainer.GetChildCount(); i++)
        {
            if (itemContainer.GetChild(i) is not ItemContainer container)
                continue;

            container.ClearItemIcon();
        }
    }

    private void OnSessionTimerTimeout()
    {
        GameInfo.SessionPlaySeconds += 1;
        RefreshTimerDisplay();
    }

    private void RefreshStatusPanel()
    {
        RefreshTimerDisplay();
    }

    private void RefreshTimerDisplay()
    {
        if (TimerValueLabel == null)
            return;

        long totalSeconds = Math.Max(0, GameInfo.SessionPlaySeconds);
        long hours = Math.Min(MaxDisplayHours, totalSeconds / 3600);
        long minutes = (totalSeconds % 3600) / 60;
        long seconds = totalSeconds % 60;

        TimerValueLabel.Text = $"{hours:00}:{minutes:00}:{seconds:00}";
        TimerValueLabel.Modulate = new Color(0.92f, 0.96f, 1f, 1f);
    }

}
