using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class EquipmentButton : Button
{
    private const float PartMoveDuration = 0.22f;
    private const float PartFadeDuration = 0.2f;
    private const float PartHideDuration = 0.16f;
    private const float PartStagger = 0.035f;
    private const float BackgroundFadeDuration = 0.26f;
    private const float HoverShaderDuration = 0.2f;

    private static readonly PackedScene EquipmentInterfaceScene = ResourceLoader.Load<PackedScene>(
        "res://Equipment/EquipmentInterface.tscn"
    );

    private enum UiAnimPhase
    {
        Idle,
        Showing,
        Hiding,
    }

    [Export]
    private CanvasLayer SiteUILayer;
    private Map ThisMap => field ??= GetNodeOrNull<Map>("/root/Map");
    private ColorRect FigureRect => field ??= GetNodeOrNull<ColorRect>("ColorRect");
    private Label FigureLabel => field ??= GetNodeOrNull<Label>("Label");
    private EquipmentInterface CurrentUI;
    private Color _originalHoverColor;
    private List<PartState> _parts = new();
    private Tween _activeTween;
    private Tween _figureHoverTween;
    private UiAnimPhase _phase = UiAnimPhase.Idle;
    private uint _showRequestVersion;
    private ShaderMaterial _buttonMaterial;
    private ShaderMaterial _figureMaterial;
    private bool _isHovered;

    private readonly (string path, Vector2 offset)[] _partSpecs =
    [
        ("BG", new Vector2(0, 10)),
        ("Overlay", new Vector2(0, 14)),
        ("GearLeft", new Vector2(-92, 54)),
        ("GearRight", new Vector2(98, -62)),
        ("RootMargin/MainVBox/TopBar", new Vector2(0, -42)),
        ("RootMargin/MainVBox/ContentRow/LeftPanel", new Vector2(-64, 12)),
        ("RootMargin/MainVBox/ContentRow/CenterPanel", new Vector2(0, 34)),
        ("RootMargin/MainVBox/ContentRow/RightPanel", new Vector2(68, 12)),
    ];

    private sealed class PartState
    {
        public Control Node;
        public Vector2 BasePosition;
        public Vector2 BaseGlobalPosition;
        public Vector2 Offset;
        public float BaseAlpha;
        public bool BaseTopLevel;
    }

    public override void _Ready()
    {
        Pressed += ToggleEquipmentInterface;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        if (FigureRect != null)
            FigureRect.MouseFilter = MouseFilterEnum.Ignore;
        if (FigureLabel != null)
            FigureLabel.MouseFilter = MouseFilterEnum.Ignore;

        if (Material is ShaderMaterial material)
        {
            _buttonMaterial = material.Duplicate() as ShaderMaterial;
            if (_buttonMaterial != null)
            {
                _buttonMaterial.ResourceLocalToScene = true;
                Material = _buttonMaterial;
                _originalHoverColor = (Color)_buttonMaterial.GetShaderParameter("color");
            }
        }

        if (FigureRect?.Material is ShaderMaterial figureMaterial)
        {
            _figureMaterial = figureMaterial.Duplicate() as ShaderMaterial;
            if (_figureMaterial != null)
            {
                _figureMaterial.ResourceLocalToScene = true;
                FigureRect.Material = _figureMaterial;
                _figureMaterial.SetShaderParameter("hover", 0.0f);
            }
        }
    }

    private async void ToggleEquipmentInterface()
    {
        if (!IsUiAlive() && SiteUILayer != null)
            CurrentUI = SiteUILayer.GetChildren().OfType<EquipmentInterface>().FirstOrDefault();

        if (IsUiAlive())
        {
            await CloseCurrentUiAsync();
        }
        else
        {
            Task frontUiCloseTask = null;
            if (ThisMap != null && ThisMap.HasFrontUiChildren())
                frontUiCloseTask = ThisMap.CloseFrontUiLayerAsync();

            ShowEquipmentInterface(true);

            if (frontUiCloseTask != null)
                await frontUiCloseTask;
        }
    }

    private async void ShowEquipmentInterface(bool prepareFromOffset)
    {
        uint requestVersion = ++_showRequestVersion;
        if (!EnsureUiReady())
            return;
        KillActiveTween();
        _phase = UiAnimPhase.Showing;

        if (_parts.Count == 0)
            _parts = CollectParts(CurrentUI);

        SetPartsAlpha(0.0f);

        // Give layout control back to containers before sampling base positions.
        RestorePartsTopLevel();

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (requestVersion != _showRequestVersion)
            return;
        if (!IsUiAlive())
            return;

        CapturePartBases();

        _activeTween = CreateTween();
        _activeTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        _activeTween.SetParallel(true);
        for (int i = 0; i < _parts.Count; i++)
        {
            var part = _parts[i];
            float delay = GetShowDelay(part, i);
            bool isBackground = IsBackgroundPart(part);
            if (!isBackground)
                SetPartTopLevel(part, true);

            if (prepareFromOffset || !isBackground)
            {
                if (isBackground)
                    part.Node.Position = part.BasePosition;
                else
                    part.Node.GlobalPosition = part.BaseGlobalPosition + part.Offset;

                var mod = part.Node.Modulate;
                part.Node.Modulate = new Color(mod.R, mod.G, mod.B, 0.0f);
            }

            if (!isBackground)
                _activeTween
                    .TweenProperty(part.Node, "global_position", part.BaseGlobalPosition, PartMoveDuration)
                    .SetDelay(delay);
            _activeTween
                .TweenProperty(
                    part.Node,
                    "modulate:a",
                    part.BaseAlpha,
                    isBackground ? BackgroundFadeDuration : PartFadeDuration
                )
                .SetDelay(isBackground ? 0.0f : delay);
        }
        _activeTween.Finished += () =>
        {
            if (requestVersion != _showRequestVersion)
                return;

            RestorePartsTopLevel();
            _phase = UiAnimPhase.Idle;
            _activeTween = null;
        };
    }

    private void HideEquipmentInterface()
    {
        if (!IsUiAlive())
            return;

        // Cancel any pending show continuation after async frame waits.
        _showRequestVersion++;
        KillActiveTween();

        if (_parts.Count == 0)
            _parts = CollectParts(CurrentUI);

        _phase = UiAnimPhase.Hiding;
        _activeTween = CreateTween();
        _activeTween.SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
        _activeTween.SetParallel(true);
        for (int i = 0; i < _parts.Count; i++)
        {
            var part = _parts[i];
            bool isBackground = IsBackgroundPart(part);
            float delay = GetHideDelay(part, i);
            var hiddenPos = part.BaseGlobalPosition + part.Offset;

            if (!isBackground)
                SetPartTopLevel(part, true);

            if (!isBackground)
                _activeTween
                    .TweenProperty(part.Node, "global_position", hiddenPos, PartHideDuration)
                    .SetDelay(delay);
            _activeTween
                .TweenProperty(
                    part.Node,
                    "modulate:a",
                    0.0f,
                    isBackground ? BackgroundFadeDuration : PartFadeDuration * 0.8f
                )
                .SetDelay(isBackground ? 0.0f : delay);
        }
        _activeTween.Finished += () =>
        {
            if (IsUiAlive())
            {
                RefreshBattlePreviewIfNeeded();
                CurrentUI.QueueFree();
            }
            _phase = UiAnimPhase.Idle;
            _activeTween = null;
        };
    }

    public async Task CloseCurrentUiAsync()
    {
        if (!IsUiAlive() && SiteUILayer != null)
            CurrentUI = SiteUILayer.GetChildren().OfType<EquipmentInterface>().FirstOrDefault();

        if (!IsUiAlive())
        {
            CurrentUI = null;
            return;
        }

        HideEquipmentInterface();
        while (IsUiAlive())
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private bool IsUiAlive()
    {
        return CurrentUI != null && GodotObject.IsInstanceValid(CurrentUI);
    }

    private bool EnsureUiReady()
    {
        if (IsUiAlive())
            return true;

        if (EquipmentInterfaceScene == null || SiteUILayer == null)
            return false;

        var equipmentInterface = EquipmentInterfaceScene.Instantiate<EquipmentInterface>();
        equipmentInterface.Name = "EquipmentInterface";
        SiteUILayer.AddChild(equipmentInterface);

        CurrentUI = equipmentInterface;
        CurrentUI.TreeExited += OnUiTreeExited;
        _parts.Clear();
        return true;
    }

    private void KillActiveTween()
    {
        if (_activeTween == null || !GodotObject.IsInstanceValid(_activeTween))
            return;
        _activeTween.Kill();
        _activeTween = null;
    }

    private List<PartState> CollectParts(EquipmentInterface ui)
    {
        var result = new List<PartState>();
        for (int i = 0; i < _partSpecs.Length; i++)
        {
            var spec = _partSpecs[i];
            var node = ui.GetNodeOrNull<Control>(spec.path);
            if (node == null)
                continue;

            result.Add(
                new PartState
                {
                    Node = node,
                    BasePosition = node.Position,
                    BaseGlobalPosition = node.GlobalPosition,
                    Offset = spec.offset,
                    BaseAlpha = node.Modulate.A,
                    BaseTopLevel = node.TopLevel,
                }
            );
        }
        return result;
    }

    private void CapturePartBases()
    {
        for (int i = 0; i < _parts.Count; i++)
        {
            var part = _parts[i];
            if (part == null || part.Node == null)
                continue;

            part.BasePosition = part.Node.Position;
            part.BaseGlobalPosition = part.Node.GlobalPosition;
        }
    }

    private void SetPartsAlpha(float alpha)
    {
        for (int i = 0; i < _parts.Count; i++)
        {
            var part = _parts[i];
            if (part == null || part.Node == null)
                continue;

            var mod = part.Node.Modulate;
            part.Node.Modulate = new Color(mod.R, mod.G, mod.B, alpha);
        }
    }

    private static void SetPartTopLevel(PartState part, bool topLevel)
    {
        if (part?.Node == null || part.Node.TopLevel == topLevel)
            return;

        var globalPos = part.Node.GlobalPosition;
        part.Node.TopLevel = topLevel;
        part.Node.GlobalPosition = globalPos;
    }

    private void RestorePartsTopLevel()
    {
        for (int i = 0; i < _parts.Count; i++)
        {
            var part = _parts[i];
            if (part == null || part.Node == null || IsBackgroundPart(part))
                continue;

            SetPartTopLevel(part, part.BaseTopLevel);
        }
    }

    private static bool IsBackgroundPart(PartState part)
    {
        if (part?.Node == null)
            return false;

        return part.Node.Name == "BG" || part.Node.Name == "Overlay";
    }

    private static int GetShowOrder(PartState part, int fallbackIndex)
    {
        if (part?.Node == null)
            return fallbackIndex;

        string name = part.Node.Name;
        return name switch
        {
            "BG" => 0,
            "Overlay" => 1,
            "TopBar" => 2,
            "LeftPanel" => 3,
            "CenterPanel" => 4,
            "RightPanel" => 5,
            "GearLeft" => 6,
            "GearRight" => 7,
            _ => 8 + fallbackIndex,
        };
    }

    private static float GetShowDelay(PartState part, int fallbackIndex)
    {
        return GetShowOrder(part, fallbackIndex) * PartStagger;
    }

    private static float GetHideDelay(PartState part, int fallbackIndex)
    {
        int order = GetShowOrder(part, fallbackIndex);
        return (7 - Math.Clamp(order, 0, 7)) * PartStagger;
    }

    private void OnUiTreeExited()
    {
        if (CurrentUI != null)
            CurrentUI.TreeExited -= OnUiTreeExited;
        KillActiveTween();
        _parts.Clear();
        _phase = UiAnimPhase.Idle;
        CurrentUI = null;
    }

    private void OnMouseEntered()
    {
        if (_isHovered)
            return;

        _isHovered = true;
        if (_buttonMaterial == null)
            return;

        _buttonMaterial.SetShaderParameter("color", _originalHoverColor);
        GlobalFunction.TweenShader(this, "dist2", 1f, HoverShaderDuration);
        _buttonMaterial.SetShaderParameter("color", new Color(1, 1, 1, 1));
        GlobalFunction.TweenShader(this, "dist1", 1f, HoverShaderDuration);
        GlobalFunction.TweenShader(this, "outer_ring_dist", 0.43f, HoverShaderDuration);
        GlobalFunction.TweenShader(this, "triangle_dist", 0.45f, HoverShaderDuration);
        GlobalFunction.TweenShader(this, "hover", 1f, HoverShaderDuration);
        TweenFigureHover(1.0f);
    }

    private void OnMouseExited()
    {
        if (!_isHovered)
            return;

        _isHovered = false;
        if (_buttonMaterial == null)
            return;

        _buttonMaterial.SetShaderParameter("color", _originalHoverColor);
        GlobalFunction.TweenShader(this, "dist2", 0.5f, HoverShaderDuration);
        _buttonMaterial.SetShaderParameter("color", _originalHoverColor);
        GlobalFunction.TweenShader(this, "dist1", 0.7f, HoverShaderDuration);
        GlobalFunction.TweenShader(this, "outer_ring_dist", 0.27f, HoverShaderDuration);
        GlobalFunction.TweenShader(this, "triangle_dist", 0.28f, HoverShaderDuration);
        GlobalFunction.TweenShader(this, "hover", 0f, HoverShaderDuration);
        TweenFigureHover(0.0f);
    }

    private void TweenFigureHover(float targetHover)
    {
        if (_figureMaterial == null)
            return;

        if (_figureHoverTween != null && GodotObject.IsInstanceValid(_figureHoverTween))
            _figureHoverTween.Kill();

        _figureHoverTween = CreateTween();
        _figureHoverTween
            .TweenMethod(
                Callable.From<float>(value => _figureMaterial.SetShaderParameter("hover", value)),
                (float)_figureMaterial.GetShaderParameter("hover"),
                targetHover,
                HoverShaderDuration
            );
    }

    private void RefreshBattlePreviewIfNeeded()
    {
        if (!IsUiAlive() || !CurrentUI.HasEquipmentChanges)
            return;

        var preview = GetTree().Root.GetNodeOrNull<BattlePreview>("Map/SiteUI/BattlePreview");
        if (preview != null)
            preview.SetPortraitPostion();
    }
}
