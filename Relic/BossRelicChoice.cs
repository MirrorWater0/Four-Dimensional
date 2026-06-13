using System;
using System.Linq;
using Godot;

public partial class BossRelicChoice : CanvasLayer
{
    private const string NodeName = "BossRelicChoice";
    private const int BossRelicOfferCount = 3;
    private const int BossRelicOfferSeedSalt = unchecked((int)0xB05571C);
    private static readonly Vector2 CardSize = new(300f, 390f);
    private static readonly Color CardBackground = new(0.035f, 0.055f, 0.09f, 0.96f);
    private static readonly Color CardBorder = new(0.42f, 0.72f, 1f, 0.46f);
    private static readonly Color CardHoverBorder = new(1f, 0.82f, 0.38f, 0.92f);

    private Control _root;
    private ColorRect _mask;
    private HBoxContainer _cardRow;
    private bool _picked;

    public static bool ShouldShowPendingChoice()
    {
        if (GameInfo.RunFinished || GameInfo.CurrentLevel != 1)
            return false;

        if (GameInfo.PendingBossRelicChoice)
            return true;

        // Migration for saves made after entering region 2 before this flag existed.
        return HasCompletedRegionOneBoss() && !HasOwnedBossRelic();
    }

    public static BossRelicChoice Show(Node caller)
    {
        var root = caller?.GetTree()?.Root;
        if (root == null)
            return null;

        var siteUi =
            root.GetNodeOrNull<CanvasLayer>("Map/SiteUI")
            ?? root.GetNodeOrNull<CanvasLayer>("/root/Map/SiteUI");
        if (siteUi == null)
            return null;

        var existing = siteUi.GetNodeOrNull<BossRelicChoice>(NodeName);
        if (existing != null && !existing.IsQueuedForDeletion())
        {
            existing.Open();
            return existing;
        }

        var choice = new BossRelicChoice { Name = NodeName, Layer = 1, Visible = false };
        siteUi.AddChild(choice);
        choice.Open();
        return choice;
    }

    public override void _Ready()
    {
        BuildUi();
    }

    public void Open()
    {
        BuildUi();
        if (!PopulateRelics())
        {
            GameInfo.PendingBossRelicChoice = false;
            SaveSystem.SaveAllInBackground();
            return;
        }

        Visible = true;
        PlayIntro();
    }

    private void BuildUi()
    {
        if (_root != null && GodotObject.IsInstanceValid(_root))
            return;

        _root = new Control
        {
            Name = "Root",
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        _mask = new ColorRect
        {
            Name = "Mask",
            Color = new Color(0f, 0f, 0f, 0.72f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _mask.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(_mask);

        var title = new Label
        {
            Text = "选择一件Boss遗物",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            OffsetTop = 136f,
            OffsetBottom = 190f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        title.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        title.AddThemeFontSizeOverride("font_size", 38);
        title.AddThemeColorOverride("font_color", new Color(1f, 0.96f, 0.82f, 1f));
        title.AddThemeColorOverride("font_outline_color", new Color(0.01f, 0.02f, 0.04f, 0.95f));
        title.AddThemeConstantOverride("outline_size", 5);
        _root.AddChild(title);

        var subtitle = new Label
        {
            Text = "区域二即将展开",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            OffsetTop = 190f,
            OffsetBottom = 226f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        subtitle.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        subtitle.AddThemeFontSizeOverride("font_size", 20);
        subtitle.AddThemeColorOverride("font_color", new Color(0.75f, 0.86f, 1f, 0.86f));
        subtitle.AddThemeColorOverride("font_outline_color", new Color(0.01f, 0.02f, 0.04f, 0.9f));
        subtitle.AddThemeConstantOverride("outline_size", 3);
        _root.AddChild(subtitle);

        _cardRow = new HBoxContainer
        {
            Name = "CardRow",
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            OffsetLeft = -560f,
            OffsetTop = -170f,
            OffsetRight = 560f,
            OffsetBottom = 245f,
        };
        _cardRow.AnchorLeft = 0.5f;
        _cardRow.AnchorRight = 0.5f;
        _cardRow.AnchorTop = 0.5f;
        _cardRow.AnchorBottom = 0.5f;
        _cardRow.AddThemeConstantOverride("separation", 38);
        _root.AddChild(_cardRow);
    }

    private bool PopulateRelics()
    {
        foreach (Node child in _cardRow.GetChildren())
            child.QueueFree();

        var rng = CreateBossRelicOfferRandom();
        var relics = Relic
            .GetBossRelicOfferPool()
            .Where(id => !GameInfo.HasRelic(id))
            .OrderBy(_ => rng.Next())
            .Take(BossRelicOfferCount)
            .ToArray();
        if (relics.Length == 0)
        {
            QueueFree();
            return false;
        }

        foreach (var relicId in relics)
            _cardRow.AddChild(CreateRelicCard(relicId));

        return true;
    }

    private static Random CreateBossRelicOfferRandom()
    {
        int seed = GameInfo.Seed ^ (GameInfo.CurrentLevel * 7919) ^ BossRelicOfferSeedSalt;
        return new Random(seed);
    }

    private static bool HasOwnedBossRelic()
    {
        if (GameInfo.Relics == null || GameInfo.Relics.Count == 0)
            return false;

        return Relic.GetBossRelicOfferPool().Any(GameInfo.HasRelic);
    }

    private static bool HasCompletedRegionOneBoss()
    {
        return GameInfo.CompletedLevelNodeRecords?.Values.Any(record =>
            record != null && record.MapLevel == 0 && record.NodeType == LevelNode.LevelType.Boss
        ) == true;
    }

    private Control CreateRelicCard(RelicID relicId)
    {
        var relic = Relic.Create(relicId);
        var button = new Button
        {
            CustomMinimumSize = CardSize,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            FocusMode = Control.FocusModeEnum.None,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
        };
        button.AddThemeStyleboxOverride("normal", CreateCardStyle(CardBorder));
        button.AddThemeStyleboxOverride("hover", CreateCardStyle(CardHoverBorder, 0.06f));
        button.AddThemeStyleboxOverride("pressed", CreateCardStyle(CardHoverBorder, 0.12f));
        button.AddThemeStyleboxOverride("focus", CreateCardStyle(CardHoverBorder, 0.08f));
        button.Pressed += () => PickRelic(relicId);

        var margin = new MarginContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_top", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_bottom", 24);
        button.AddChild(margin);

        var stack = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        stack.AddThemeConstantOverride("separation", 18);
        margin.AddChild(stack);

        var icon = new ColorRect
        {
            CustomMinimumSize = new Vector2(92f, 92f),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        Relic.ApplyIconVisual(icon, relicId);
        stack.AddChild(icon);

        var name = new Label
        {
            Text = relic.RelicName,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(0f, 48f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        name.AddThemeFontSizeOverride("font_size", 28);
        name.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.62f, 1f));
        name.AddThemeColorOverride("font_outline_color", new Color(0.01f, 0.02f, 0.04f, 0.95f));
        name.AddThemeConstantOverride("outline_size", 4);
        stack.AddChild(name);

        var description = new RichTextLabel
        {
            BbcodeEnabled = true,
            Text = GlobalFunction.ColorizeNumbers(relic.RelicDescription),
            FitContent = false,
            ScrollActive = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        description.AddThemeFontSizeOverride("normal_font_size", 21);
        description.AddThemeColorOverride("default_color", new Color(0.88f, 0.94f, 1f, 0.94f));
        description.AddThemeColorOverride("font_outline_color", new Color(0.01f, 0.02f, 0.04f, 0.9f));
        description.AddThemeConstantOverride("outline_size", 2);
        stack.AddChild(description);

        return button;
    }

    private static StyleBoxFlat CreateCardStyle(Color border, float brighten = 0f)
    {
        return new StyleBoxFlat
        {
            BgColor = CardBackground.Lightened(brighten),
            BorderColor = border,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 8,
            ContentMarginLeft = 18,
            ContentMarginTop = 18,
            ContentMarginRight = 18,
            ContentMarginBottom = 18,
        };
    }

    private void PickRelic(RelicID relicId)
    {
        if (_picked)
            return;

        _picked = true;
        var resourceState =
            GetTree()?.Root.GetNodeOrNull<PlayerResourceState>("Map/PlayerResourceState")
            ?? GetTree()?.Root.GetNodeOrNull<PlayerResourceState>("/root/Map/PlayerResourceState");
        if (resourceState != null)
        {
            Relic.RelicAdd(resourceState, relicId);
        }

        GameInfo.PendingBossRelicChoice = false;
        PlayOutro();
        SaveSystem.SaveAllInBackground();
    }

    private void PlayIntro()
    {
        if (_root == null)
            return;

        _root.Modulate = new Color(1f, 1f, 1f, 0f);
        _cardRow.Position += new Vector2(0f, 28f);
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(_root, "modulate:a", 1f, 0.22f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_cardRow, "position", _cardRow.Position - new Vector2(0f, 28f), 0.28f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    private void PlayOutro()
    {
        if (_root == null)
        {
            QueueFree();
            return;
        }

        foreach (Node child in _cardRow.GetChildren())
        {
            if (child is Control control)
                control.MouseFilter = Control.MouseFilterEnum.Ignore;
        }

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(_root, "modulate:a", 0f, 0.18f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(_cardRow, "position", _cardRow.Position + new Vector2(0f, 24f), 0.18f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}
