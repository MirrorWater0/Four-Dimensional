using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class SpaceStationShop : Control
{
    private const int ShopCharacterCount = 4;
    private const int SkillOffersPerCharacter = 2;
    private const int SkillOfferCount = ShopCharacterCount * SkillOffersPerCharacter;
    private const int SkillOfferPrice = 42;
    private const float ModuleSelectorTweenDuration = 0.1f;
    private const float ModuleContentFadeOutDuration = 0.045f;
    private const float ModuleContentFadeInDuration = 0.08f;
    private const float ModuleItemTweenDuration = 0.11f;
    private const float ModuleItemStagger = 0.01f;
    private const float ModuleItemEnterOffsetX = 64f;
    private const float ModuleItemEnterOffsetY = 10f;
    private const float ModuleItemExitOffsetX = 28f;
    private const float ModuleItemExitOffsetY = 8f;
    private const float StatPanelHoverDuration = 0.14f;
    private static readonly Vector2 SkillCardBaseDisplaySize = new(250f, 400f);
    private static readonly (ItemID ItemId, int Price)[] PotionCatalog =
    [
        (ItemID.Health, 18),
        (ItemID.Guard, 20),
        (ItemID.Haste, 22),
        (ItemID.Fury, 24),
        (ItemID.Vitality, 24),
        (ItemID.Health, 18),
        (ItemID.Guard, 20),
        (ItemID.Haste, 22),
        (ItemID.Fury, 24),
    ];

    [Export(PropertyHint.Range, "0.35,0.80,0.01")]
    public float ShopSkillCardScale { get; set; } = 0.80f;

    private static readonly PackedScene ShopScene = GD.Load<PackedScene>(
        "res://Shop/SpaceStationShop.tscn"
    );
    private static readonly PackedScene CardSlotScene = GD.Load<PackedScene>(
        "res://Equipment/CardSlot.tscn"
    );
    private static readonly PackedScene StatCharacterPanelScene = GD.Load<PackedScene>(
        "res://Shop/StatCharacterPanel.tscn"
    );
    private static readonly PackedScene SkillCardScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Reward/SkillCard.tscn"
    );

    private enum OfferKind
    {
        Equipment,
        Relic,
        Skill,
        Item,
    }

    private enum ShopModule
    {
        Stat,
        Skill,
        Equipment,
        Relic,
        Potion,
    }

    private readonly struct AssemblyItem(Control control, Vector2 offset, float delay)
    {
        public Control Control { get; } = control;
        public Vector2 Offset { get; } = offset;
        public float Delay { get; } = delay;
    }

    private sealed class CatalogOffer
    {
        public OfferKind Kind;
        public string Title;
        public string Detail;
        public int Price;
        public bool Sold;
        public Equipment Equipment;
        public RelicID RelicId;
        public ItemID ItemId;
        public Control View;
        public CardSlot Card;
        public PanelContainer IconFrame;
        public ColorRect IconRect;
        public Label PriceLabel;
    }

    private sealed class StatOffer
    {
        public int PlayerIndex;
        public string CharacterName;
        public PropertyType PropertyType;
        public int PropertyValue;
        public int Price;
        public bool Sold;
        public PanelContainer View;
        public Label PriceLabel;
    }

    private sealed class SkillOffer
    {
        public int PlayerIndex;
        public SkillID? SkillId;
        public int Price;
        public bool Sold;
        public Control View;
        public SkillCard Card;
        public Label PriceLabel;
    }

    private Button CloseButton => field ??= GetNode<Button>("Panel/Decor/CloseButton");
    private Button HideButton => field ??= GetNode<Button>("Panel/Decor/HideButton");
    private ColorRect BG => field ??= GetNode<ColorRect>("BG");
    private PanelContainer PanelNode => field ??= GetNode<PanelContainer>("Panel");
    private Control ModulePanelNode =>
        field ??= GetNode<Control>("Panel/MainLayout/ModulePanel");
    private Control ContentAreaNode =>
        field ??= GetNode<Control>("Panel/MainLayout/ContentArea");
    private Control HeaderBackplate =>
        field ??= GetNode<Control>("Panel/Decor/HeaderBackplate");
    private Control HeaderNode => field ??= GetNode<Control>("Panel/Decor/Header");
    private Button StatModuleButton =>
        field
            ??= GetNode<Button>(
                "Panel/MainLayout/ModulePanel/ModuleBar/ModuleSelector/ButtonsMargin/Buttons/StatButton"
            );
    private Button SkillModuleButton =>
        field
            ??= GetNode<Button>(
                "Panel/MainLayout/ModulePanel/ModuleBar/ModuleSelector/ButtonsMargin/Buttons/SkillButton"
            );
    private Button EquipmentModuleButton =>
        field
            ??= GetNode<Button>(
                "Panel/MainLayout/ModulePanel/ModuleBar/ModuleSelector/ButtonsMargin/Buttons/EquipmentButton"
            );
    private Button RelicModuleButton =>
        field
            ??= GetNode<Button>(
                "Panel/MainLayout/ModulePanel/ModuleBar/ModuleSelector/ButtonsMargin/Buttons/RelicButton"
            );
    private Button PotionModuleButton =>
        field
            ??= GetNode<Button>(
                "Panel/MainLayout/ModulePanel/ModuleBar/ModuleSelector/ButtonsMargin/Buttons/PotionButton"
            );
    private Control ModuleSelector =>
        field ??= GetNode<Control>("Panel/MainLayout/ModulePanel/ModuleBar/ModuleSelector");
    private PanelContainer ModuleSelectorThumb =>
        field
            ??= GetNode<PanelContainer>(
                "Panel/MainLayout/ModulePanel/ModuleBar/ModuleSelector/Thumb"
            );
    private Label StatusLabel => field ??= GetNode<Label>("Panel/Decor/FooterHint");
    private Label CatalogTitle => field ??= GetNode<Label>("Panel/Decor/CatalogTitle");
    private Label CatalogHint => field ??= GetNode<Label>("Panel/Decor/CatalogHint");
    private Control TopLine => field ??= GetNode<Control>("Panel/Decor/TopLine");
    private Control FooterLine => field ??= GetNode<Control>("Panel/Decor/FooterLine");
    private Control StatPanel => field ??= GetNode<Control>("Panel/MainLayout/ContentArea/StatPanel");
    private GridContainer StatOffersContainer =>
        field
            ??= GetNode<GridContainer>(
                "Panel/MainLayout/ContentArea/StatPanel/StatMargin/StatOffers"
            );
    private GridContainer CatalogGrid =>
        field
            ??= GetNode<GridContainer>(
                "Panel/MainLayout/ContentArea/CatalogViewport/CatalogMargin/CatalogGrid"
            );
    private GridContainer SkillOffersContainer =>
        field
            ??= GetNode<GridContainer>(
                "Panel/MainLayout/ContentArea/SkillPanel/Margin/VBox/SkillOffers"
            );
    private Control CatalogViewport =>
        field ??= GetNode<Control>("Panel/MainLayout/ContentArea/CatalogViewport");
    private Control SkillPanel => field ??= GetNode<Control>("Panel/MainLayout/ContentArea/SkillPanel");
    private Map MapNode => field ??= GetNodeOrNull<Map>("/root/Map");
    private PlayerResourceState ResourceState => field ??= GetNodeOrNull<PlayerResourceState>("/root/Map/PlayerResourceState");

    private readonly List<StatOffer> _statOffers = new();
    private readonly List<CatalogOffer> _catalogOffers = new();
    private readonly List<SkillOffer> _skillOffers = new();
    public LevelNode WhichNode { get; set; }
    private ShopModule _currentModule = ShopModule.Stat;
    private Tween _moduleSelectorTween;
    private Tween _moduleContentTween;
    private Tween _transitionTween;
    private bool _moduleSelectorPositioned;
    private bool _isClosing;
    private bool _isHidden;
    private bool _isTransitioning;
    private Tip _shopRelicTip;
    private Control _inputBlocker;
    private readonly Dictionary<Control, Vector2> _assemblyBasePositions = new();
    private readonly Dictionary<Control, Tween> _statPanelHoverTweens = new();
    private Vector2 _panelBasePosition;

    public static SpaceStationShop Show(Node caller)
    {
        var root = caller.GetTree().Root;
        var existing = root.GetNodeOrNull<SpaceStationShop>("Map/SiteUI/SpaceStationShop")
            ?? root.GetNodeOrNull<SpaceStationShop>("SpaceStationShop");
        if (existing != null)
        {
            if (existing._isHidden)
                existing.ReopenFromHidden();
            return existing;
        }

        var shop = ShopScene.Instantiate<SpaceStationShop>();
        shop.Name = "SpaceStationShop";
        var siteUi = root.GetNodeOrNull<Node>("Map/SiteUI");
        if (siteUi != null)
            siteUi.AddChild(shop);
        else
            root.AddChild(shop);
        return shop;
    }

    private void ReopenFromHidden()
    {
        if (!IsInsideTree() || !_isHidden)
            return;

        _isHidden = false;
        _isClosing = false;
        Visible = true;
        MouseFilter = MouseFilterEnum.Stop;
        HideRelicTip();
        _moduleSelectorTween?.Kill();
        _moduleSelectorTween = null;
        KillModuleContentTween();
        KillTransitionTween();

        if (GetParent() is Node parent)
            parent.MoveChild(this, parent.GetChildCount() - 1);

        ApplyModuleVisibility();
        UpdateModuleButtons();
        SnapModuleSelectorToCurrentButton();
        RestoreModuleTransitionVisualState(_currentModule);
        RefreshShopState();
        RestoreAssemblyVisualState();
        CaptureTransitionBases();
        ApplyPreIntroVisualState();
        SetUiInteractive(false);
        PlayIntroAnimation();
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        CloseButton.Pressed += Close;
        HideButton.Pressed += HideOnly;
        WireModuleButtons();
        BindModuleSelector();
        BuildShop();
        PanelNode.ItemRectChanged += UpdatePanelPivot;
        UpdatePanelPivot();
        ApplyPreIntroVisualState();
        EnsureInputBlocker();
        SetUiInteractive(false);
        CallDeferred(nameof(BeginIntroAnimation));
    }

    public override void _ExitTree()
    {
        _moduleSelectorTween?.Kill();
        _moduleContentTween?.Kill();
        _transitionTween?.Kill();
        foreach (var tween in _statPanelHoverTweens.Values)
            tween?.Kill();
        _statPanelHoverTweens.Clear();
        HideRelicTip();
    }

    private void BuildShop()
    {
        ClearStatOffers();
        ClearCatalogCards();
        ClearSkillCards();
        _statOffers.Clear();
        _catalogOffers.Clear();
        _skillOffers.Clear();

        BuildStatOffers();
        BuildEquipmentOffers();
        BuildRelicOffers();
        BuildPotionOffers();
        BuildSkillOffers();

        SetModule(ShopModule.Stat, animateSelector: false);
        SnapModuleContentVisualState();
        SetStatus("浏览空间站补给目录。");
        RefreshShopState();
    }

    private void BuildStatOffers()
    {
        var players = GameInfo.PlayerCharacters ?? Array.Empty<PlayerInfoStructure>();

        for (int i = 0; i < ShopCharacterCount; i++)
        {
            string characterName =
                i < players.Length && !string.IsNullOrWhiteSpace(players[i].CharacterName)
                    ? players[i].CharacterName
                    : $"角色 {i + 1}";

            var panel = CreateStatCharacterPanel(characterName);
            StatOffersContainer.AddChild(panel);

            var optionGrid = panel.GetNode<GridContainer>("Margin/VBox/Options");
            AddStatOffer(optionGrid.GetNode<PanelContainer>("Power"), i, characterName, PropertyType.Power, 1, 26);
            AddStatOffer(
                optionGrid.GetNode<PanelContainer>("Survivability"),
                i,
                characterName,
                PropertyType.Survivability,
                1,
                26
            );
            AddStatOffer(optionGrid.GetNode<PanelContainer>("Speed"), i, characterName, PropertyType.Speed, 1, 32);
        }
    }

    private void BuildEquipmentOffers()
    {
        var rng = new Random(GameInfo.Seed ^ 0x51A7);
        var equipmentPool = Equipment.GetCatalogClones().OrderBy(_ => rng.Next()).Take(4).ToArray();
        for (int i = 0; i < equipmentPool.Length; i++)
        {
            var equipment = equipmentPool[i];
            AddEquipmentOffer(equipment, ComputeEquipmentPrice(equipment));
        }
    }

    private void BuildRelicOffers()
    {
        var relicPool = Relic.GetUnownedOfferPool();
        for (int i = 0; i < relicPool.Length; i++)
        {
            AddRelicOffer(relicPool[i], 88);
        }
    }

    private void BuildPotionOffers()
    {
        for (int i = 0; i < PotionCatalog.Length; i++)
        {
            var stock = PotionCatalog[i];
            AddPotionOffer(stock.ItemId, stock.Price);
        }
    }

    private void BuildSkillOffers()
    {
        var players = GameInfo.PlayerCharacters ?? Array.Empty<PlayerInfoStructure>();
        var rng = new Random(GameInfo.Seed ^ 0x7E11);
        Vector2 scale = GetShopSkillCardScaleVector();
        Vector2 cardSize = GetShopSkillCardDisplaySize();

        for (int playerIndex = 0; playerIndex < ShopCharacterCount; playerIndex++)
        {
            int offerPlayerIndex = playerIndex < players.Length ? playerIndex : -1;
            var pickedSkills = PickSkillOffersForPlayer(players, playerIndex, rng);

            for (int slotIndex = 0; slotIndex < SkillOffersPerCharacter; slotIndex++)
            {
                var card = SkillCardScene.Instantiate<SkillCard>();
                card.Name = $"SkillOffer{_skillOffers.Count + 1}";
                card.ConfigureDisplayScale(scale);

                var cardHolder = CreateSkillOfferCardHolder(card, cardSize, scale);

                var priceLabel = CreateSkillOfferPriceLabel();
                var tile = CreateSkillOfferTile(cardHolder, priceLabel, cardSize);
                SkillOffersContainer.AddChild(tile);

                int animationIndex = _skillOffers.Count;
                card.CallDeferred(nameof(SkillCard.StartAnimation), 0.04f * animationIndex + 0.08f);

                var offer = new SkillOffer
                {
                    PlayerIndex = offerPlayerIndex,
                    SkillId = pickedSkills[slotIndex],
                    Price = SkillOfferPrice,
                    View = tile,
                    Card = card,
                    PriceLabel = priceLabel,
                };
                _skillOffers.Add(offer);
                card.Button.Pressed += () => OnSkillOfferPressed(offer);
            }
        }
    }

    private static SkillID?[] PickSkillOffersForPlayer(
        PlayerInfoStructure[] players,
        int playerIndex,
        Random rng
    )
    {
        SkillID?[] result = new SkillID?[SkillOffersPerCharacter];
        if (players == null || playerIndex < 0 || playerIndex >= players.Length)
            return result;

        var info = players[playerIndex];
        var pool = info.AllSkills?.Distinct().ToList() ?? new List<SkillID>();
        if (pool.Count == 0)
            return result;

        var gained = info.GainedSkills ?? new List<SkillID>();
        var available = pool.Where(id => !gained.Contains(id)).ToList();
        if (available.Count == 0)
            return result;

        ShuffleInPlace(available, rng);
        for (int i = 0; i < Math.Min(SkillOffersPerCharacter, available.Count); i++)
            result[i] = available[i];

        return result;
    }

    private void AddEquipmentOffer(Equipment equipment, int price)
    {
        if (equipment == null)
            return;

        var offer = new CatalogOffer
        {
            Kind = OfferKind.Equipment,
            Title = $"装备 · {equipment.DisplayName}",
            Detail = BuildEquipmentBonusInline(equipment),
            Equipment = equipment,
            Price = price,
        };
        var card = CardSlotScene.Instantiate<CardSlot>();
        card.Name = $"CatalogOffer{_catalogOffers.Count + 1}";
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        card.CustomMinimumSize = new Vector2(0f, 188f);
        CatalogGrid.AddChild(card);
        card.Clicked += () => OnCatalogOfferPressed(offer);
        offer.View = card;
        offer.Card = card;
        _catalogOffers.Add(offer);
    }

    private void AddRelicOffer(RelicID relicId, int price)
    {
        var relic = new Relic(relicId);
        var offer = new CatalogOffer
        {
            Kind = OfferKind.Relic,
            Title = relic.RelicName,
            Detail = relic.RelicDescription,
            RelicId = relicId,
            Price = price,
        };
        _catalogOffers.Add(offer);
        SetupRelicCard(offer);
    }

    private void AddPotionOffer(ItemID itemId, int price)
    {
        var offer = new CatalogOffer
        {
            Kind = OfferKind.Item,
            Title = ConsumeItem.GetItemName(itemId),
            Detail = ConsumeItem.GetItemDescription(itemId),
            ItemId = itemId,
            Price = price,
        };
        _catalogOffers.Add(offer);
        SetupPotionCard(offer);
    }

    private void AddStatOffer(
        PanelContainer tile,
        int playerIndex,
        string characterName,
        PropertyType propertyType,
        int value,
        int price
    )
    {
        if (tile == null)
            return;

        var offer = new StatOffer
        {
            PlayerIndex = playerIndex,
            CharacterName = characterName,
            PropertyType = propertyType,
            PropertyValue = value,
            Price = price,
        };
        SetupStatOfferTile(tile, offer);
        _statOffers.Add(offer);
    }

    private void SetupRelicCard(CatalogOffer offer)
    {
        if (offer == null)
            return;

        var frame = new PanelContainer
        {
            Name = $"RelicOffer{_catalogOffers.Count}",
            CustomMinimumSize = new Vector2(72f, 72f),
            MouseFilter = MouseFilterEnum.Stop,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        frame.AddThemeStyleboxOverride("panel", CreateRelicIconFrameStyle());

        var icon = Relic.IconScene.Instantiate<ColorRect>();
        icon.Name = "RelicIcon";
        icon.Color = Colors.White;
        icon.MouseFilter = MouseFilterEnum.Ignore;
        icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        icon.OffsetLeft = 6f;
        icon.OffsetTop = 6f;
        icon.OffsetRight = -6f;
        icon.OffsetBottom = -6f;
        var label = icon.GetNodeOrNull<Label>("Label");
        if (label != null)
            label.Text = Relic.FormatCountLabel(GetRelicAddAmount(offer.RelicId));
        var panel = icon.GetNodeOrNull<Panel>("Panel");
        if (panel != null)
            panel.Visible = false;
        frame.AddChild(icon);

        ApplyRelicIconShader(icon, offer.RelicId);

        frame.GuiInput += @event =>
        {
            if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
                return;
            OnCatalogOfferPressed(offer);
        };
        frame.MouseEntered += () => ShowRelicTip(offer);
        frame.MouseExited += HideRelicTip;

        CatalogGrid.AddChild(frame);
        offer.View = frame;
        offer.IconFrame = frame;
        offer.IconRect = icon;
    }

    private void SetupPotionCard(CatalogOffer offer)
    {
        if (offer == null)
            return;

        var frame = new PanelContainer
        {
            Name = $"PotionOffer{_catalogOffers.Count}",
            CustomMinimumSize = new Vector2(0f, 138f),
            MouseFilter = MouseFilterEnum.Stop,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        frame.AddThemeStyleboxOverride("panel", CreatePotionTileStyle());

        var margin = new MarginContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        frame.AddChild(margin);

        var vbox = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        vbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(vbox);

        var iconCenter = new CenterContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        vbox.AddChild(iconCenter);

        var iconFrame = new PanelContainer
        {
            CustomMinimumSize = new Vector2(82f, 82f),
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        iconFrame.AddThemeStyleboxOverride("panel", CreatePotionIconFrameStyle());
        iconCenter.AddChild(iconFrame);

        var icon = new ColorRect
        {
            Name = "PotionIcon",
            Color = Colors.White,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        icon.OffsetLeft = 10f;
        icon.OffsetTop = 10f;
        icon.OffsetRight = -10f;
        icon.OffsetBottom = -10f;
        ConsumeItem.ConfigureIcon(icon, offer.ItemId);
        iconFrame.AddChild(icon);

        var priceLabel = CreatePotionPriceLabel();
        vbox.AddChild(priceLabel);

        frame.GuiInput += @event =>
        {
            if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
                return;
            OnCatalogOfferPressed(offer);
        };
        frame.MouseEntered += () => ShowCatalogTip(BuildItemTooltip(offer));
        frame.MouseExited += HideRelicTip;

        CatalogGrid.AddChild(frame);
        offer.View = frame;
        offer.IconFrame = iconFrame;
        offer.IconRect = icon;
        offer.PriceLabel = priceLabel;
    }

    private PanelContainer CreateStatCharacterPanel(string characterName)
    {
        var panel = StatCharacterPanelScene.Instantiate<PanelContainer>();
        panel.GetNode<Label>("Margin/VBox/Title").Text = characterName;
        return panel;
    }

    private void SetupStatOfferTile(PanelContainer tile, StatOffer offer)
    {
        tile.MouseFilter = MouseFilterEnum.Stop;
        offer.View = tile;

        var label = tile.GetNode<Label>("Margin/VBox/ValueLabel");
        label.Text = $"{offer.PropertyType.GetDescription()} +{offer.PropertyValue}";

        var price = tile.GetNode<Label>("Margin/VBox/PriceLabel");
        offer.PriceLabel = price;
        SetupStatOfferHover(tile);

        tile.GuiInput += @event =>
        {
            if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
                return;
            OnStatOfferPressed(offer);
        };
    }

    private void SetupStatOfferHover(PanelContainer tile)
    {
        if (tile == null)
            return;

        var hoverFrame = EnsureStatOfferHoverFrame(tile);
        if (hoverFrame == null)
            return;

        hoverFrame.MouseFilter = MouseFilterEnum.Ignore;
        hoverFrame.Visible = true;
        SetControlAlpha(hoverFrame, 0.0f);

        tile.MouseEntered += () => AnimateStatOfferHover(tile, true);
        tile.MouseExited += () => AnimateStatOfferHover(tile, false);
    }

    private PanelContainer EnsureStatOfferHoverFrame(PanelContainer tile)
    {
        var existing = tile.GetNodeOrNull<PanelContainer>("HoverFrame");
        if (existing != null)
            return existing;

        var hoverFrame = new PanelContainer
        {
            Name = "HoverFrame",
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 1,
        };
        hoverFrame.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        hoverFrame.AddThemeStyleboxOverride("panel", CreateStatOfferHoverStyle());
        tile.AddChild(hoverFrame);
        tile.MoveChild(hoverFrame, tile.GetChildCount() - 1);
        return hoverFrame;
    }

    private void AnimateStatOfferHover(PanelContainer tile, bool hovered)
    {
        if (tile == null || !GodotObject.IsInstanceValid(tile))
            return;

        var hoverFrame = tile.GetNodeOrNull<Control>("HoverFrame");
        if (hoverFrame == null || !GodotObject.IsInstanceValid(hoverFrame))
            return;

        if (_statPanelHoverTweens.TryGetValue(tile, out var previousTween))
        {
            previousTween?.Kill();
            _statPanelHoverTweens.Remove(tile);
        }

        var tween = CreateTween();
        _statPanelHoverTweens[tile] = tween;
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(
            hoverFrame,
            "modulate:a",
            hovered ? 1.0f : 0.0f,
            StatPanelHoverDuration
        );
        tween.Finished += () =>
        {
            if (_statPanelHoverTweens.TryGetValue(tile, out var activeTween) && activeTween == tween)
                _statPanelHoverTweens.Remove(tile);
        };
    }

    private static Control CreateSkillOfferCardHolder(SkillCard card, Vector2 cardSize, Vector2 scale)
    {
        var holder = new Control
        {
            CustomMinimumSize = cardSize,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };

        card.Position = -0.5f * (Vector2.One - scale) * SkillCardBaseDisplaySize;
        holder.AddChild(card);
        return holder;
    }

    private static VBoxContainer CreateSkillOfferTile(
        Control cardHolder,
        Label priceLabel,
        Vector2 cardSize
    )
    {
        var tile = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(0f, cardSize.Y + 34f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        tile.AddThemeConstantOverride("separation", 6);

        var cardCenter = new CenterContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        cardCenter.AddChild(cardHolder);
        tile.AddChild(cardCenter);
        tile.AddChild(priceLabel);

        return tile;
    }

    private static Label CreateSkillOfferPriceLabel()
    {
        var label = new Label
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", 13);
        label.AddThemeConstantOverride("outline_size", 2);
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.84f));
        return label;
    }

    private static Label CreatePotionPriceLabel()
    {
        var label = new Label
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", 13);
        label.AddThemeConstantOverride("outline_size", 2);
        label.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.05f, 0.08f, 0.88f));
        return label;
    }

    private void OnStatOfferPressed(StatOffer offer)
    {
        if (offer == null || offer.Sold)
            return;
        if (!CanApplyStatOffer(offer))
        {
            SetStatus($"{offer.CharacterName} 当前不可进行属性提升。");
            RefreshShopState();
            return;
        }
        if (!TrySpendCurrency(offer.Price))
            return;
        if (!ApplyPropertyToPlayer(offer.PlayerIndex, offer.PropertyType, offer.PropertyValue))
            return;

        offer.Sold = true;
        SetStatus(
            $"已为 {offer.CharacterName} 提升 {offer.PropertyType.GetDescription()} +{offer.PropertyValue}"
        );
        RefreshShopState();
    }

    private void OnCatalogOfferPressed(CatalogOffer offer)
    {
        if (offer == null || offer.Sold)
            return;
        if (!CanPurchaseCatalogOffer(offer))
            return;
        if (!TrySpendCurrency(offer.Price))
            return;
        if (!ApplyCatalogOffer(offer))
            return;

        offer.Sold = true;
        SetStatus($"已购入：{offer.Title}");
        RefreshShopState();
    }

    private void OnSkillOfferPressed(SkillOffer offer)
    {
        if (offer == null || offer.Sold || offer.SkillId == null)
            return;
        if (!TrySpendCurrency(offer.Price))
            return;
        if (!GrantSkillOffer(offer))
            return;

        offer.Sold = true;
        var name =
            GameInfo.PlayerCharacters != null
            && offer.PlayerIndex >= 0
            && offer.PlayerIndex < GameInfo.PlayerCharacters.Length
                ? GameInfo.PlayerCharacters[offer.PlayerIndex].CharacterName
                : "角色";
        SetStatus($"已购入：{name} 的技能卡");
        RefreshShopState();
    }

    private bool ApplyCatalogOffer(CatalogOffer offer)
    {
        switch (offer.Kind)
        {
            case OfferKind.Equipment:
                if (offer.Equipment == null)
                    return false;
                GameInfo.OwnedEquipments ??= new List<Equipment>();
                GameInfo.OwnedEquipments.Add(Equipment.Clone(offer.Equipment));
                return true;
            case OfferKind.Relic:
                GrantRelic(offer.RelicId);
                return true;
            case OfferKind.Item:
                return GrantItemOffer(offer.ItemId);
            default:
                return false;
        }
    }

    private bool GrantSkillOffer(SkillOffer offer)
    {
        if (offer.SkillId == null)
            return false;

        var players = GameInfo.PlayerCharacters;
        if (players == null || offer.PlayerIndex < 0 || offer.PlayerIndex >= players.Length)
            return false;

        var info = players[offer.PlayerIndex];
        info.GainedSkills ??= new List<SkillID>();
        if (!info.GainedSkills.Contains(offer.SkillId.Value))
            info.GainedSkills.Add(offer.SkillId.Value);
        players[offer.PlayerIndex] = info;
        GameInfo.PlayerCharacters = players;
        return true;
    }

    private bool ApplyPropertyToPlayer(int playerIndex, PropertyType type, int value)
    {
        var players = GameInfo.PlayerCharacters;
        if (players == null || playerIndex < 0 || playerIndex >= players.Length)
            return false;

        var info = players[playerIndex];
        switch (type)
        {
            case PropertyType.Power:
                info.Power += value;
                break;
            case PropertyType.Survivability:
                info.Survivability += value;
                break;
            case PropertyType.Speed:
                info.Speed += value;
                break;
            case PropertyType.MaxLife:
                info.LifeMax += value;
                break;
        }
        players[playerIndex] = info;

        GameInfo.PlayerCharacters = players;
        return true;
    }

    private void GrantRelic(RelicID relicId)
    {
        if (ResourceState == null)
        {
            GameInfo.Relics ??= new Dictionary<RelicID, int>();
            int addedAmount = GetRelicAddAmount(relicId);
            if (!GameInfo.Relics.TryGetValue(relicId, out int amount))
                GameInfo.Relics[relicId] = addedAmount;
            else
                GameInfo.Relics[relicId] = amount + addedAmount;
            return;
        }

        var existing = ResourceState.RelicList?.FirstOrDefault(relic => relic.ID == relicId);
        if (existing == null)
        {
            Relic.RelicAdd(ResourceState, relicId);
            return;
        }

        int addNum = GetRelicAddAmount(relicId);
        existing.Num += addNum;
        GameInfo.Relics[relicId] = existing.Num;
        existing.UpdateIconLabel();
    }

    private bool CanPurchaseCatalogOffer(CatalogOffer offer)
    {
        if (offer == null)
            return false;

        if (offer.Kind != OfferKind.Item)
            return true;

        if (CanStoreItem())
            return true;

        SetStatus($"道具栏已满，最多只能携带 {GameInfo.ItemsMaxCount} 个道具。");
        RefreshShopState();
        return false;
    }

    private bool CanStoreItem()
    {
        int itemCount = ResourceState?.Items?.Count ?? GameInfo.Items?.Count ?? 0;
        return itemCount < GameInfo.ItemsMaxCount;
    }

    private bool GrantItemOffer(ItemID itemId)
    {
        if (!CanStoreItem())
            return false;

        if (ResourceState != null)
        {
            ConsumeItem.AddItem(ResourceState, itemId, syncGameInfo: true);
            return true;
        }

        GameInfo.Items ??= new List<ItemID>();
        GameInfo.Items.Add(itemId);
        return true;
    }

    private bool TrySpendCurrency(int price)
    {
        int currency = GetCurrentCurrency();
        if (currency < price)
        {
            SetStatus($"电力币不足，需要 {price}。");
            RefreshShopState();
            return false;
        }

        SetCurrentCurrency(currency - price);
        return true;
    }

    private int GetCurrentCurrency()
    {
        return ResourceState?.ElectricityCoin ?? GameInfo.ElectricityCoin;
    }

    private void SetCurrentCurrency(int value)
    {
        if (ResourceState != null)
            ResourceState.ElectricityCoin = value;
        else
            GameInfo.ElectricityCoin = value;
    }

    private void RefreshShopState()
    {
        for (int i = 0; i < _statOffers.Count; i++)
            RefreshStatOfferCard(_statOffers[i]);

        for (int i = 0; i < _catalogOffers.Count; i++)
            RefreshCatalogOfferCard(_catalogOffers[i]);

        for (int i = 0; i < _skillOffers.Count; i++)
            RefreshSkillOfferCard(_skillOffers[i]);
    }

    private void RefreshStatOfferCard(StatOffer offer)
    {
        if (offer?.View == null || !GodotObject.IsInstanceValid(offer.View))
            return;

        bool canBuy = !offer.Sold && CanApplyStatOffer(offer) && GetCurrentCurrency() >= offer.Price;
        offer.View.MouseFilter = offer.Sold ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
        offer.View.Modulate = offer.Sold
            ? new Color(0.66f, 0.72f, 0.82f, 0.52f)
            : (canBuy ? Colors.White : new Color(0.86f, 0.88f, 0.94f, 0.76f));

        if (offer.PriceLabel != null)
        {
            offer.PriceLabel.Text = offer.Sold
                ? "已购"
                : canBuy
                    ? $"{offer.Price} 电力币"
                    : $"需 {offer.Price}";
            offer.PriceLabel.AddThemeColorOverride(
                "font_color",
                offer.Sold
                    ? new Color(0.74f, 0.78f, 0.86f, 0.72f)
                    : (canBuy ? new Color(1f, 0.92f, 0.72f, 1f) : new Color(1f, 0.76f, 0.7f, 0.92f))
            );
        }
    }

    private void RefreshCatalogOfferCard(CatalogOffer offer)
    {
        if (offer?.View == null || !GodotObject.IsInstanceValid(offer.View))
            return;

        bool canBuy =
            !offer.Sold
            && GetCurrentCurrency() >= offer.Price
            && (offer.Kind != OfferKind.Item || CanStoreItem());
        if (offer.Kind == OfferKind.Relic)
        {
            offer.View.Modulate = offer.Sold
                ? new Color(0.64f, 0.7f, 0.8f, 0.42f)
                : (canBuy ? Colors.White : new Color(0.9f, 0.9f, 0.96f, 0.7f));
            return;
        }

        if (offer.Kind == OfferKind.Item)
        {
            offer.View.MouseFilter = offer.Sold ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
            offer.View.Modulate = offer.Sold
                ? new Color(0.64f, 0.7f, 0.8f, 0.48f)
                : (canBuy ? Colors.White : new Color(0.86f, 0.89f, 0.94f, 0.72f));

            if (offer.PriceLabel != null && GodotObject.IsInstanceValid(offer.PriceLabel))
            {
                offer.PriceLabel.Text = offer.Sold ? "已售" : $"{offer.Price} 电力币";
                offer.PriceLabel.AddThemeColorOverride(
                    "font_color",
                    offer.Sold
                        ? new Color(0.74f, 0.8f, 0.88f, 0.72f)
                        : canBuy
                            ? new Color(1f, 0.88f, 0.4f, 0.98f)
                            : new Color(0.92f, 0.76f, 0.58f, 0.9f)
                );
            }
            return;
        }

        if (offer.Card == null || !GodotObject.IsInstanceValid(offer.Card))
            return;

        offer.Card.SetInteractable(canBuy);
        string stateLine = offer.Sold ? "已售罄" : canBuy ? "点击购买" : "余额不足";
        offer.Card.label.Text =
            $"{offer.Title}\n{offer.Detail}\n价格 {offer.Price} 电力币\n{stateLine}";
    }

    private void RefreshSkillOfferCard(SkillOffer offer)
    {
        if (offer?.Card == null || !GodotObject.IsInstanceValid(offer.Card))
            return;

        var card = offer.Card;
        var tileView = offer.View;
        string characterName = GetPlayerDisplayName(offer.PlayerIndex);

        if (offer.SkillId == null)
        {
            card.SetSkill(null);
            card.NameLabel.Text = offer.PlayerIndex >= 0 ? "暂无技能" : "空位";
            card.Description.Text =
                offer.PlayerIndex >= 0 ? "该角色当前没有可售技能。" : "当前没有角色入驻此栏位。";
            card.CharacterName.Text = offer.PlayerIndex >= 0 ? characterName : "空位";
            card.Button.Disabled = true;
            card.Modulate = new Color(0.7f, 0.74f, 0.8f, 0.7f);
            if (tileView != null && GodotObject.IsInstanceValid(tileView))
                tileView.Modulate = card.Modulate;
            SetSkillOfferPriceLabel(
                offer,
                offer.PlayerIndex >= 0 ? "未上架" : "--",
                new Color(0.7f, 0.78f, 0.86f, 0.76f)
            );
            return;
        }

        var skill = Skill.GetSkill(offer.SkillId.Value);
        if (skill == null)
        {
            card.SetSkill(null);
            card.NameLabel.Text = "技能异常";
            card.Description.Text = "技能资源加载失败。";
            card.CharacterName.Text = characterName;
            card.Button.Disabled = true;
            card.Modulate = new Color(0.7f, 0.74f, 0.8f, 0.7f);
            if (tileView != null && GodotObject.IsInstanceValid(tileView))
                tileView.Modulate = card.Modulate;
            SetSkillOfferPriceLabel(offer, "不可用", new Color(0.76f, 0.8f, 0.88f, 0.74f));
            return;
        }

        if (
            GameInfo.PlayerCharacters != null
            && offer.PlayerIndex >= 0
            && offer.PlayerIndex < GameInfo.PlayerCharacters.Length
        )
        {
            var info = GameInfo.PlayerCharacters[offer.PlayerIndex];
            skill.SetPreviewStats(info.Power, info.Survivability, 1);
        }
        card.SetSkill(skill);

        bool canBuy = !offer.Sold && GetCurrentCurrency() >= offer.Price;
        card.Button.Disabled = !canBuy;
        card.Modulate = canBuy
            ? Colors.White
            : (offer.Sold ? new Color(0.72f, 0.76f, 0.84f, 0.65f) : new Color(0.82f, 0.84f, 0.9f, 0.82f));
        if (tileView != null && GodotObject.IsInstanceValid(tileView))
            tileView.Modulate = card.Modulate;

        card.CharacterName.Text = characterName;
        if (offer.Sold)
            card.NameLabel.Text = $"{skill.SkillName} [已售]";

        SetSkillOfferPriceLabel(
            offer,
            offer.Sold ? "已售" : canBuy ? $"{offer.Price} 电力币" : $"需 {offer.Price} 电力币",
            offer.Sold
                ? new Color(0.7f, 0.76f, 0.84f, 0.76f)
                : canBuy ? new Color(1f, 0.84f, 0.33f, 0.98f) : new Color(0.94f, 0.72f, 0.48f, 0.94f)
        );
    }

    private static void SetSkillOfferPriceLabel(SkillOffer offer, string text, Color color)
    {
        if (offer?.PriceLabel == null || !GodotObject.IsInstanceValid(offer.PriceLabel))
            return;

        offer.PriceLabel.Text = text ?? string.Empty;
        offer.PriceLabel.AddThemeColorOverride("font_color", color);
    }

    private static StyleBoxFlat CreateRelicIconFrameStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.12f, 0.18f, 0.96f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            BorderColor = new Color(0.95f, 0.78f, 0.42f, 0.9f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
        };
    }

    private static StyleBoxFlat CreatePotionTileStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.1f, 0.14f, 0.88f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color(0.4f, 0.68f, 0.8f, 0.34f),
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
        };
    }

    private static StyleBoxFlat CreateStatOfferHoverStyle()
    {
        return new StyleBoxFlat
        {
            DrawCenter = false,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            BorderColor = new Color(0.86f, 0.95f, 1f, 0.96f),
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
        };
    }

    private static StyleBoxFlat CreatePotionIconFrameStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.16f, 0.24f, 0.96f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            BorderColor = new Color(0.62f, 0.86f, 0.98f, 0.6f),
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
        };
    }

    private static void ApplyRelicIconShader(ColorRect icon, RelicID relicId)
    {
        if (icon == null)
            return;

        ApplyShaderToRect(icon, Relic.GetIconShaderPath(relicId));
    }

    private static void ApplyShaderToRect(ColorRect icon, string shaderPath)
    {
        if (string.IsNullOrWhiteSpace(shaderPath))
        {
            icon.Material = null;
            return;
        }

        var shader = GD.Load<Shader>(shaderPath);
        if (shader == null)
        {
            icon.Material = null;
            return;
        }

        icon.Material = new ShaderMaterial { Shader = shader };
    }

    private void ShowRelicTip(CatalogOffer offer)
    {
        if (offer == null)
            return;

        ShowCatalogTip(BuildRelicTooltip(offer));
    }

    private void ShowCatalogTip(string text)
    {
        var tip = EnsureShopRelicTip();
        if (tip == null)
            return;

        tip.FollowMouse = true;
        tip.Description.Text = text ?? string.Empty;
        tip.Visible = true;
    }

    private void HideRelicTip()
    {
        if (_shopRelicTip != null)
            _shopRelicTip.Visible = false;
    }

    private Tip EnsureShopRelicTip()
    {
        if (_shopRelicTip != null && GodotObject.IsInstanceValid(_shopRelicTip))
            return _shopRelicTip;

        var root = GetTree()?.Root;
        if (root == null)
            return null;

        var layer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (layer == null)
        {
            layer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
            root.AddChild(layer);
        }

        _shopRelicTip =
            layer.GetNodeOrNull<Tip>("RelicTip")
            ?? layer.GetNodeOrNull<Tip>("Tip")
            ?? layer.GetNodeOrNull<Tip>("ShopRelicTip");
        if (_shopRelicTip != null)
            return _shopRelicTip;

        var tipScene = GD.Load<PackedScene>("res://battle/UIScene/Tip.tscn");
        if (tipScene == null)
            return null;

        _shopRelicTip = tipScene.Instantiate<Tip>();
        _shopRelicTip.Name = "ShopRelicTip";
        _shopRelicTip.FollowMouse = true;
        _shopRelicTip.AnchorOffset = new Vector2(20f, 20f);
        layer.AddChild(_shopRelicTip);
        return _shopRelicTip;
    }

    private static string BuildRelicTooltip(CatalogOffer offer)
    {
        var relic = new Relic(offer.RelicId);
        string title = $"[color=#b78cff]{relic.RelicName}[/color]";
        string effect = GlobalFunction.ColorizeNumbers(relic.RelicDescription);
        string price = offer.Sold
            ? "[color=#aab6c6]已售罄[/color]"
            : $"[color=#ffd24a]价格 {offer.Price} 电力币[/color]";
        return $"{title}\n{effect}\n{price}";
    }

    private static string BuildItemTooltip(CatalogOffer offer)
    {
        string title = $"[color=#84d8ff]{ConsumeItem.GetItemName(offer.ItemId)}[/color]";
        string effect = GlobalFunction.ColorizeNumbers(ConsumeItem.GetItemDescription(offer.ItemId));
        string price = offer.Sold
            ? "[color=#aab6c6]已售罄[/color]"
            : $"[color=#ffd24a]价格 {offer.Price} 电力币[/color]";
        string carryHint = $"[color=#9cdacf]购买后进入战斗道具栏，最多携带 {GameInfo.ItemsMaxCount} 个[/color]";
        return $"{title}\n{effect}\n{price}\n{carryHint}";
    }

    private static int GetRelicAddAmount(RelicID relicId)
    {
        return Relic.GetAcquireAmount(relicId);
    }

    private void ClearStatOffers()
    {
        foreach (var child in StatOffersContainer.GetChildren())
            child.QueueFree();
    }

    private void ClearCatalogCards()
    {
        foreach (var child in CatalogGrid.GetChildren())
            child.QueueFree();
    }

    private void ClearSkillCards()
    {
        foreach (var child in SkillOffersContainer.GetChildren())
            child.QueueFree();
    }

    private static string GetPlayerDisplayName(int playerIndex)
    {
        if (
            GameInfo.PlayerCharacters != null
            && playerIndex >= 0
            && playerIndex < GameInfo.PlayerCharacters.Length
        )
        {
            string name = GameInfo.PlayerCharacters[playerIndex].CharacterName;
            if (!string.IsNullOrWhiteSpace(name))
                return name;
        }

        return playerIndex >= 0 ? $"角色 {playerIndex + 1}" : "角色";
    }

    private static void ShuffleInPlace<T>(IList<T> items, Random rng)
    {
        if (items == null || rng == null)
            return;

        for (int i = items.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(i + 1);
            (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
        }
    }

    private Vector2 GetShopSkillCardDisplaySize()
    {
        return SkillCardBaseDisplaySize * GetShopSkillCardScaleVector();
    }

    private Vector2 GetShopSkillCardScaleVector()
    {
        float scale = Mathf.Clamp(ShopSkillCardScale, 0.35f, 0.80f);
        return new Vector2(scale, scale);
    }

    private void SetStatus(string text)
    {
        StatusLabel.Text = text ?? string.Empty;
    }

    private void WireModuleButtons()
    {
        StatModuleButton.Pressed += () => RequestModuleChange(ShopModule.Stat);
        SkillModuleButton.Pressed += () => RequestModuleChange(ShopModule.Skill);
        EquipmentModuleButton.Pressed += () => RequestModuleChange(ShopModule.Equipment);
        RelicModuleButton.Pressed += () => RequestModuleChange(ShopModule.Relic);
        PotionModuleButton.Pressed += () => RequestModuleChange(ShopModule.Potion);
    }

    private void BindModuleSelector()
    {
        ModuleSelector.Resized += OnModuleSelectorLayoutChanged;
        StatModuleButton.Resized += OnModuleSelectorLayoutChanged;
        SkillModuleButton.Resized += OnModuleSelectorLayoutChanged;
        EquipmentModuleButton.Resized += OnModuleSelectorLayoutChanged;
        RelicModuleButton.Resized += OnModuleSelectorLayoutChanged;
        PotionModuleButton.Resized += OnModuleSelectorLayoutChanged;
    }

    private void OnModuleSelectorLayoutChanged()
    {
        CallDeferred(nameof(SnapModuleSelectorToCurrentButton));
    }

    private void SnapModuleSelectorToCurrentButton()
    {
        UpdateModuleSelectorPosition(false);
    }

    private async void BeginIntroAnimation()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        if (!IsInsideTree() || _isClosing)
            return;

        SnapModuleSelectorToCurrentButton();
        CaptureTransitionBases();
        PlayIntroAnimation();
    }

    private void RequestModuleChange(ShopModule module)
    {
        if (_isClosing || _isTransitioning || _currentModule == module)
            return;

        _ = PlayModuleChangeAnimationAsync(module);
    }

    private async Task PlayModuleChangeAnimationAsync(ShopModule module)
    {
        if (!IsInsideTree() || _isClosing || _currentModule == module)
            return;

        int transitionDirection = GetModuleTransitionDirection(_currentModule, module);
        _isTransitioning = true;
        SetUiInteractive(false);
        HideRelicTip();

        var outgoingRoot = GetModuleContentRoot(_currentModule);
        bool useOutgoingSnapshot = ShouldUseModuleTransitionSnapshot(outgoingRoot, module);
        var outgoingVisual = useOutgoingSnapshot
            ? CreateModuleTransitionSnapshot(outgoingRoot)
            : outgoingRoot;
        SetModule(module, animateSelector: true);
        var incomingRoot = GetModuleContentRoot(_currentModule);
        RestoreModuleTransitionVisualState(_currentModule);

        if (
            !useOutgoingSnapshot
            && outgoingVisual != null
            && GodotObject.IsInstanceValid(outgoingVisual)
            && outgoingVisual != incomingRoot
        )
        {
            outgoingVisual.Visible = true;
            SetControlAlpha(outgoingVisual, 1.0f);
        }

        await AnimateModuleRootTransitionAsync(
            outgoingVisual,
            incomingRoot,
            transitionDirection,
            disposeOutgoingVisual: useOutgoingSnapshot
        );

        if (!IsInsideTree() || _isClosing)
            return;

        _isTransitioning = false;
        SetUiInteractive(true);
    }

    private Control CreateModuleTransitionSnapshot(Control sourceRoot)
    {
        if (sourceRoot == null || !GodotObject.IsInstanceValid(sourceRoot) || !sourceRoot.Visible)
            return null;

        if (sourceRoot.GetParent() is not Node parent)
            return null;

        if (sourceRoot.Duplicate() is not Control snapshot)
            return null;

        snapshot.Name = $"{sourceRoot.Name}TransitionSnapshot";
        snapshot.MouseFilter = MouseFilterEnum.Ignore;
        snapshot.ZIndex = sourceRoot.ZIndex + 1;
        snapshot.Position = sourceRoot.Position;
        snapshot.Size = sourceRoot.Size;
        snapshot.Visible = true;
        snapshot.Modulate = sourceRoot.Modulate;
        parent.AddChild(snapshot);
        parent.MoveChild(snapshot, parent.GetChildCount() - 1);
        return snapshot;
    }

    private async Task AnimateModuleRootTransitionAsync(
        Control outgoingVisual,
        Control incomingRoot,
        int transitionDirection,
        bool disposeOutgoingVisual
    )
    {
        KillModuleContentTween();

        Vector2 outgoingBasePosition =
            outgoingVisual != null && GodotObject.IsInstanceValid(outgoingVisual)
                ? outgoingVisual.Position
                : Vector2.Zero;

        Vector2 incomingBasePosition =
            incomingRoot != null && GodotObject.IsInstanceValid(incomingRoot)
                ? incomingRoot.Position
                : Vector2.Zero;

        if (incomingRoot != null && GodotObject.IsInstanceValid(incomingRoot))
        {
            incomingRoot.Visible = true;
            incomingRoot.Position = incomingBasePosition + GetModuleEnterOffset(transitionDirection);
            SetControlAlpha(incomingRoot, 0.0f);
        }

        var tween = CreateTween();
        _moduleContentTween = tween;
        tween.SetParallel(true);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);

        if (incomingRoot != null && GodotObject.IsInstanceValid(incomingRoot))
        {
            tween.TweenProperty(incomingRoot, "position", incomingBasePosition, ModuleContentFadeInDuration);
            tween.TweenProperty(incomingRoot, "modulate:a", 1.0f, ModuleContentFadeInDuration);
        }

        if (outgoingVisual != null && GodotObject.IsInstanceValid(outgoingVisual))
        {
            tween.TweenProperty(
                outgoingVisual,
                "position",
                outgoingBasePosition + GetModuleExitOffset(-transitionDirection),
                ModuleContentFadeOutDuration
            );
            tween.TweenProperty(
                outgoingVisual,
                "modulate:a",
                0.0f,
                ModuleContentFadeOutDuration
            );
        }

        await ToSignal(tween, Tween.SignalName.Finished);

        if (incomingRoot != null && GodotObject.IsInstanceValid(incomingRoot))
        {
            incomingRoot.Position = incomingBasePosition;
            SetControlAlpha(incomingRoot, 1.0f);
        }

        if (outgoingVisual != null && GodotObject.IsInstanceValid(outgoingVisual))
        {
            outgoingVisual.Position = outgoingBasePosition;
            SetControlAlpha(outgoingVisual, 1.0f);
            if (disposeOutgoingVisual)
                outgoingVisual.QueueFree();
            else if (outgoingVisual != incomingRoot)
                outgoingVisual.Visible = false;
        }

        if (_moduleContentTween == tween)
            _moduleContentTween = null;
    }

    private async Task RunModuleFadeAsync(
        Control root,
        Control[] items,
        bool entering,
        int horizontalDirection,
        float duration
    )
    {
        KillModuleContentTween();

        float[] itemTargetAlphas = items.Select(item =>
            item != null && GodotObject.IsInstanceValid(item) ? item.Modulate.A : 1.0f
        ).ToArray();
        Vector2 rootBasePosition =
            root != null && GodotObject.IsInstanceValid(root) ? root.Position : Vector2.Zero;

        SetControlAlpha(CatalogTitle, 1.0f);
        SetControlAlpha(CatalogHint, 1.0f);
        if (root != null && GodotObject.IsInstanceValid(root))
            SetControlAlpha(root, 1.0f);

        if (entering)
        {
            if (root != null && GodotObject.IsInstanceValid(root))
                root.Position = rootBasePosition + GetModuleEnterOffset(horizontalDirection);
            PrepareModuleItemsForTransition(items, entering: true);
        }
        else
        {
            PrepareModuleItemsForTransition(items, entering: false);
        }

        var tween = CreateTween();
        _moduleContentTween = tween;
        tween.SetParallel(true);
        tween.SetEase(entering ? Tween.EaseType.Out : Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Cubic);

        if (root != null && GodotObject.IsInstanceValid(root))
        {
            Vector2 rootTargetPosition = entering
                ? rootBasePosition
                : rootBasePosition + GetModuleExitOffset(horizontalDirection);
            tween.TweenProperty(root, "position", rootTargetPosition, duration);
        }

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item == null || !GodotObject.IsInstanceValid(item))
                continue;

            float delay = ModuleItemStagger * i;
            tween
                .TweenProperty(
                    item,
                    "modulate:a",
                    entering ? itemTargetAlphas[i] : 0.0f,
                    ModuleItemTweenDuration
                )
                .SetDelay(delay);
        }

        await ToSignal(tween, Tween.SignalName.Finished);
        if (root != null && GodotObject.IsInstanceValid(root))
            root.Position = rootBasePosition;
        ResetModuleItemsToBase(
            items,
            entering ? itemTargetAlphas : Enumerable.Repeat(0.0f, itemTargetAlphas.Length).ToArray()
        );

        if (_moduleContentTween == tween)
            _moduleContentTween = null;
    }

    private void SetModule(ShopModule module, bool animateSelector = false)
    {
        _currentModule = module;
        ApplyModuleVisibility();
        UpdateModuleButtons();
        UpdateModuleSelectorPosition(animateSelector);
        RefreshShopState();
    }

    private void ApplyModuleVisibility()
    {
        bool statModule = _currentModule == ShopModule.Stat;
        bool skillModule = _currentModule == ShopModule.Skill;
        bool catalogModule =
            _currentModule == ShopModule.Equipment
            || _currentModule == ShopModule.Relic
            || _currentModule == ShopModule.Potion;
        StatPanel.Visible = statModule;
        CatalogViewport.Visible = catalogModule;
        SkillPanel.Visible = skillModule;
        CatalogGrid.Columns = _currentModule switch
        {
            ShopModule.Relic => 9,
            ShopModule.Potion => 3,
            _ => 2,
        };

        for (int i = 0; i < _catalogOffers.Count; i++)
        {
            var offer = _catalogOffers[i];
            if (offer?.View == null || !GodotObject.IsInstanceValid(offer.View))
                continue;
            offer.View.Visible = ModuleMatchesOffer(_currentModule, offer.Kind);
        }

        switch (_currentModule)
        {
            case ShopModule.Stat:
                CatalogTitle.Text = "属性模块";
                CatalogHint.Text = "四名角色分别陈列，点击图标可永久提升对应角色的单项属性。";
                break;
            case ShopModule.Skill:
                CatalogTitle.Text = "技能模块";
                CatalogHint.Text = "随机展示 8 张技能卡，购买后直接加入对应角色技能池。";
                break;
            case ShopModule.Equipment:
                CatalogTitle.Text = "装备模块";
                CatalogHint.Text = "固定 2x2 网格展示装备，购买后加入库存，可在编队/装备界面分配。";
                break;
            case ShopModule.Relic:
                CatalogTitle.Text = "遗物模块";
                CatalogHint.Text = "遗物以图标矩阵展示，悬停查看效果，点击图标购买。";
                break;
            case ShopModule.Potion:
                CatalogTitle.Text = "道具模块";
                CatalogHint.Text = "3x3 网格陈列战斗道具，价格写在图标下方，购买后进入道具栏。";
                break;
        }
    }

    private void SnapModuleContentVisualState()
    {
        SetControlAlpha(CatalogTitle, 1.0f);
        SetControlAlpha(CatalogHint, 1.0f);
        SetControlAlpha(StatPanel, StatPanel.Visible ? 1.0f : 0.0f);
        SetControlAlpha(SkillPanel, SkillPanel.Visible ? 1.0f : 0.0f);
        SetControlAlpha(CatalogViewport, CatalogViewport.Visible ? 1.0f : 0.0f);
        ResetModuleItemsVisualState(GetModuleTransitionItems(_currentModule));
    }

    private bool ShouldUseModuleTransitionSnapshot(Control outgoingRoot, ShopModule targetModule)
    {
        if (outgoingRoot == null || !GodotObject.IsInstanceValid(outgoingRoot))
            return false;

        if (outgoingRoot == CatalogViewport)
            return true;

        return outgoingRoot == GetModuleContentRoot(targetModule);
    }

    private void RestoreModuleTransitionVisualState(ShopModule module)
    {
        if (module != ShopModule.Skill)
            return;

        for (int i = 0; i < _skillOffers.Count; i++)
        {
            var card = _skillOffers[i]?.Card;
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            card.RestoreDisplayState();
        }
    }

    private Control[] GetModuleTransitionItems(ShopModule module)
    {
        return module switch
        {
            ShopModule.Stat => _statOffers
                .Where(offer => offer?.View != null && GodotObject.IsInstanceValid(offer.View))
                .Select(offer => (Control)offer.View)
                .ToArray(),
            ShopModule.Skill => _skillOffers
                .Where(offer => offer?.View != null && GodotObject.IsInstanceValid(offer.View))
                .Select(offer => offer.View)
                .ToArray(),
            _ => _catalogOffers
                .Where(offer =>
                    offer?.View != null
                    && GodotObject.IsInstanceValid(offer.View)
                    && ModuleMatchesOffer(module, offer.Kind)
                )
                .Select(offer => offer.View)
                .ToArray(),
        };
    }

    private void PrepareModuleItemsForTransition(Control[] items, bool entering)
    {
        if (items == null)
            return;

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item == null || !GodotObject.IsInstanceValid(item))
                continue;

            if (entering)
                SetControlAlpha(item, 0.0f);
        }
    }

    private void ResetModuleItemsVisualState(Control[] items)
    {
        if (items == null)
            return;

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item == null || !GodotObject.IsInstanceValid(item))
                continue;

            SetControlAlpha(item, 1.0f);
        }
    }

    private static void ResetModuleItemsToBase(Control[] items, float[] targetAlphas)
    {
        if (items == null)
            return;

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item == null || !GodotObject.IsInstanceValid(item))
                continue;

            SetControlAlpha(item, targetAlphas[i]);
        }
    }

    private static Vector2 GetModuleEnterOffset(int horizontalDirection)
    {
        return new Vector2(
            horizontalDirection * ModuleItemEnterOffsetX,
            ModuleItemEnterOffsetY
        );
    }

    private static Vector2 GetModuleExitOffset(int horizontalDirection)
    {
        return new Vector2(
            horizontalDirection * ModuleItemExitOffsetX,
            ModuleItemExitOffsetY
        );
    }

    private static int GetModuleTransitionDirection(ShopModule from, ShopModule to)
    {
        int delta = GetModuleOrderIndex(to) - GetModuleOrderIndex(from);
        return delta == 0 ? 1 : Math.Sign(delta);
    }

    private static int GetModuleOrderIndex(ShopModule module)
    {
        return module switch
        {
            ShopModule.Stat => 0,
            ShopModule.Skill => 1,
            ShopModule.Equipment => 2,
            ShopModule.Relic => 3,
            ShopModule.Potion => 4,
            _ => 0,
        };
    }

    private void UpdateModuleButtons()
    {
        SetModuleButtonState(StatModuleButton, _currentModule == ShopModule.Stat);
        SetModuleButtonState(SkillModuleButton, _currentModule == ShopModule.Skill);
        SetModuleButtonState(EquipmentModuleButton, _currentModule == ShopModule.Equipment);
        SetModuleButtonState(RelicModuleButton, _currentModule == ShopModule.Relic);
        SetModuleButtonState(PotionModuleButton, _currentModule == ShopModule.Potion);
    }

    private void UpdateModuleSelectorPosition(bool animate)
    {
        if (!IsInsideTree())
            return;

        var targetButton = GetModuleButton(_currentModule);
        if (targetButton == null || !GodotObject.IsInstanceValid(targetButton))
            return;

        Rect2 selectorRect = ModuleSelector.GetGlobalRect();
        Rect2 buttonRect = targetButton.GetGlobalRect();
        if (selectorRect.Size.X <= 0f || buttonRect.Size.Y <= 0f)
        {
            CallDeferred(nameof(SnapModuleSelectorToCurrentButton));
            return;
        }

        Vector2 targetPosition = buttonRect.Position - selectorRect.Position;
        Vector2 targetSize = buttonRect.Size;

        _moduleSelectorTween?.Kill();
        if (animate && _moduleSelectorPositioned)
        {
            _moduleSelectorTween = CreateTween();
            _moduleSelectorTween.SetTrans(Tween.TransitionType.Cubic);
            _moduleSelectorTween.SetEase(Tween.EaseType.Out);
            _moduleSelectorTween.SetParallel(true);
            _moduleSelectorTween.TweenProperty(
                ModuleSelectorThumb,
                "position",
                targetPosition,
                ModuleSelectorTweenDuration
            );
            _moduleSelectorTween.TweenProperty(
                ModuleSelectorThumb,
                "size",
                targetSize,
                ModuleSelectorTweenDuration
            );
        }
        else
        {
            ModuleSelectorThumb.Position = targetPosition;
            ModuleSelectorThumb.Size = targetSize;
        }

        ModuleSelectorThumb.Visible = true;
        _moduleSelectorPositioned = true;
    }

    private void CaptureTransitionBases()
    {
        UpdatePanelPivot();
        _panelBasePosition = PanelNode.Position;
        _assemblyBasePositions.Clear();

        foreach (var item in GetAssemblyItems())
        {
            if (item.Control == null || !GodotObject.IsInstanceValid(item.Control))
                continue;
            _assemblyBasePositions[item.Control] = item.Control.Position;
        }
    }

    private void ApplyPreIntroVisualState()
    {
        SetControlAlpha(BG, 0.0f);
        SetControlAlpha(PanelNode, 0.0f);

        foreach (var item in GetAssemblyItems())
            SetControlAlpha(item.Control, 0.0f);
    }

    private void RestoreAssemblyVisualState()
    {
        PanelNode.Position = _panelBasePosition;
        PanelNode.Scale = Vector2.One;
        SetControlAlpha(BG, 1.0f);
        SetControlAlpha(PanelNode, 1.0f);

        foreach (var item in GetAssemblyItems())
        {
            if (_assemblyBasePositions.TryGetValue(item.Control, out var basePos))
                item.Control.Position = basePos;

            SetControlAlpha(item.Control, 1.0f);
        }

        SnapModuleContentVisualState();
    }

    private void PlayIntroAnimation()
    {
        KillTransitionTween();
        _isTransitioning = true;
        SetUiInteractive(false);

        SetControlAlpha(BG, 0.0f);
        PanelNode.Position = _panelBasePosition + new Vector2(0f, 28f);
        PanelNode.Scale = new Vector2(0.97f, 0.97f);
        SetControlAlpha(PanelNode, 0.0f);

        foreach (var item in GetAssemblyItems())
        {
            if (!_assemblyBasePositions.TryGetValue(item.Control, out var basePos))
                continue;

            item.Control.Position = basePos + item.Offset;
            SetControlAlpha(item.Control, 0.0f);
        }

        var tween = CreateTween();
        _transitionTween = tween;
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(BG, "modulate:a", 1.0f, 0.26f);
        tween.TweenProperty(PanelNode, "position", _panelBasePosition, 0.34f);
        tween.TweenProperty(PanelNode, "scale", Vector2.One, 0.34f);
        tween.TweenProperty(PanelNode, "modulate:a", 1.0f, 0.28f);

        foreach (var item in GetAssemblyItems())
        {
            if (!_assemblyBasePositions.TryGetValue(item.Control, out var basePos))
                continue;

            tween.TweenProperty(item.Control, "position", basePos, 0.3f).SetDelay(item.Delay);
            tween.TweenProperty(item.Control, "modulate:a", 1.0f, 0.24f).SetDelay(item.Delay);
        }

        tween.Finished += () =>
        {
            if (_transitionTween != tween)
                return;

            _transitionTween = null;
            _isTransitioning = false;
            SetUiInteractive(!_isClosing);
        };
    }

    private void PlayCloseAnimation(Action onComplete)
    {
        _moduleSelectorTween?.Kill();
        _moduleSelectorTween = null;
        KillModuleContentTween();
        KillTransitionTween();
        CaptureTransitionBases();

        _isTransitioning = true;
        SetUiInteractive(false);

        var items = GetAssemblyItems();
        var tween = CreateTween();
        _transitionTween = tween;
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(BG, "modulate:a", 0.0f, 0.22f);
        tween.TweenProperty(PanelNode, "position", _panelBasePosition + new Vector2(0f, 22f), 0.22f);
        tween.TweenProperty(PanelNode, "scale", new Vector2(0.96f, 0.96f), 0.22f);
        tween.TweenProperty(PanelNode, "modulate:a", 0.0f, 0.2f);

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (!_assemblyBasePositions.TryGetValue(item.Control, out var basePos))
                continue;

            float delay = GetHideDelay(i, items.Length);
            tween
                .TweenProperty(item.Control, "position", basePos + item.Offset * 0.75f, 0.18f)
                .SetDelay(delay);
            tween.TweenProperty(item.Control, "modulate:a", 0.0f, 0.16f).SetDelay(delay);
        }

        tween.Finished += () =>
        {
            if (_transitionTween != tween)
                return;

            _transitionTween = null;
            _isTransitioning = false;
            onComplete?.Invoke();
        };
    }

    private AssemblyItem[] GetAssemblyItems()
    {
        return
        [
            new AssemblyItem(HeaderBackplate, new Vector2(0f, -24f), 0.00f),
            new AssemblyItem(HeaderNode, new Vector2(0f, -20f), 0.04f),
            new AssemblyItem(HideButton, new Vector2(36f, 0f), 0.08f),
            new AssemblyItem(CloseButton, new Vector2(42f, 0f), 0.08f),
            new AssemblyItem(CatalogTitle, new Vector2(0f, -18f), 0.1f),
            new AssemblyItem(CatalogHint, new Vector2(0f, -14f), 0.14f),
            new AssemblyItem(ModulePanelNode, new Vector2(-84f, 18f), 0.08f),
            new AssemblyItem(ContentAreaNode, new Vector2(98f, 20f), 0.12f),
            new AssemblyItem(TopLine, new Vector2(0f, -12f), 0.18f),
            new AssemblyItem(FooterLine, new Vector2(0f, 12f), 0.2f),
            new AssemblyItem(StatusLabel, new Vector2(0f, 16f), 0.24f),
        ];
    }

    private void KillTransitionTween()
    {
        if (_transitionTween == null || !GodotObject.IsInstanceValid(_transitionTween))
            return;

        _transitionTween.Kill();
        _transitionTween = null;
    }

    private void SetUiInteractive(bool interactive)
    {
        var blocker = EnsureInputBlocker();
        if (blocker != null)
            blocker.Visible = !interactive;

        CloseButton.Disabled = !interactive;
        HideButton.Disabled = !interactive;
    }

    private Control EnsureInputBlocker()
    {
        if (_inputBlocker != null && GodotObject.IsInstanceValid(_inputBlocker))
            return _inputBlocker;

        _inputBlocker = new Control
        {
            Name = "InputBlocker",
            MouseFilter = MouseFilterEnum.Stop,
            Visible = false,
            FocusMode = FocusModeEnum.None,
        };
        _inputBlocker.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_inputBlocker);
        MoveChild(_inputBlocker, GetChildCount() - 1);
        return _inputBlocker;
    }

    private void UpdatePanelPivot()
    {
        PanelNode.PivotOffset = PanelNode.Size * 0.5f;
    }

    private static void SetControlAlpha(Control control, float alpha)
    {
        if (control == null)
            return;

        control.Modulate = control.Modulate with { A = alpha };
    }

    private static float GetHideDelay(int index, int count)
    {
        return 0.03f * Math.Max(0, count - 1 - index);
    }

    private Button GetModuleButton(ShopModule module)
    {
        return module switch
        {
            ShopModule.Stat => StatModuleButton,
            ShopModule.Skill => SkillModuleButton,
            ShopModule.Equipment => EquipmentModuleButton,
            ShopModule.Relic => RelicModuleButton,
            ShopModule.Potion => PotionModuleButton,
            _ => StatModuleButton,
        };
    }

    private Control GetModuleContentRoot(ShopModule module)
    {
        return module switch
        {
            ShopModule.Stat => StatPanel,
            ShopModule.Skill => SkillPanel,
            ShopModule.Equipment => CatalogViewport,
            ShopModule.Relic => CatalogViewport,
            ShopModule.Potion => CatalogViewport,
            _ => CatalogViewport,
        };
    }

    private static void SetModuleButtonState(Button button, bool active)
    {
        if (button == null)
            return;

        button.SetPressedNoSignal(active);
        button.Modulate = Colors.White;
    }

    private static bool ModuleMatchesOffer(ShopModule module, OfferKind kind)
    {
        return module switch
        {
            ShopModule.Skill => kind == OfferKind.Skill,
            ShopModule.Equipment => kind == OfferKind.Equipment,
            ShopModule.Relic => kind == OfferKind.Relic,
            ShopModule.Potion => kind == OfferKind.Item,
            _ => false,
        };
    }

    private void KillModuleContentTween()
    {
        if (_moduleContentTween == null || !GodotObject.IsInstanceValid(_moduleContentTween))
            return;

        _moduleContentTween.Kill();
        _moduleContentTween = null;
    }

    private void Close()
    {
        if (_isClosing)
            return;

        _isClosing = true;
        HideRelicTip();
        PlayCloseAnimation(() =>
        {
            if (WhichNode != null && WhichNode.State != LevelNode.LevelState.Completed)
                WhichNode.Completed();
            QueueFree();
        });
    }

    private void HideOnly()
    {
        if (_isClosing || _isHidden)
            return;

        _isClosing = true;
        HideRelicTip();
        PlayCloseAnimation(() =>
        {
            _isClosing = false;
            _isHidden = true;
            SetUiInteractive(false);
            Visible = false;
        });
    }

    private static int ComputeEquipmentPrice(Equipment equipment)
    {
        if (equipment == null)
            return 0;

        int score =
            Math.Abs(equipment.Power) * 10
            + Math.Abs(equipment.Survivability) * 8
            + Math.Abs(equipment.Speed) * 12
            + Math.Abs(equipment.MaxLife) * 2;
        return Math.Clamp(28 + score, 36, 72);
    }

    private static string BuildEquipmentBonusInline(Equipment equipment)
    {
        if (equipment == null)
            return string.Empty;

        var parts = new List<string>();
        AddStat(parts, equipment.Power, "力量");
        AddStat(parts, equipment.Survivability, "生存");
        AddStat(parts, equipment.Speed, "速度");
        AddStat(parts, equipment.MaxLife, "生命");
        return string.Join("  ", parts);
    }

    private static void AddStat(List<string> parts, int value, string label)
    {
        if (value == 0)
            return;

        string prefix = value > 0 ? "+" : string.Empty;
        parts.Add($"{prefix}{value} {label}");
    }

    private static bool CanApplyStatOffer(StatOffer offer)
    {
        return offer != null
            && GameInfo.PlayerCharacters != null
            && offer.PlayerIndex >= 0
            && offer.PlayerIndex < GameInfo.PlayerCharacters.Length;
    }
}
