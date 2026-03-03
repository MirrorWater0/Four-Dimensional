using System.Collections.Generic;
using Godot;

public partial class EquipmentButton : Button
{
    private const float PartMoveDuration = 0.24f;
    private const float PartHideDuration = 0.2f;
    private const float PartStagger = 0.04f;
    private const float BackgroundFadeDuration = 0.32f;

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
    private EquipmentInterface CurrentUI;
    private List<PartState> _parts = new();
    private Tween _activeTween;
    private UiAnimPhase _phase = UiAnimPhase.Idle;
    private uint _showRequestVersion;

    private readonly (string path, Vector2 offset)[] _partSpecs =
    [
        ("BG", new Vector2(0, 28)),
        ("Overlay", new Vector2(0, 36)),
        ("GearLeft", new Vector2(-220, 130)),
        ("GearRight", new Vector2(240, -150)),
        ("RootMargin/MainVBox/TopBar", new Vector2(0, -96)),
        ("RootMargin/MainVBox/ContentRow/LeftPanel", new Vector2(-180, 0)),
        ("RootMargin/MainVBox/ContentRow/CenterPanel", new Vector2(0, 128)),
        ("RootMargin/MainVBox/ContentRow/RightPanel", new Vector2(190, 0)),
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
    }

    private void ToggleEquipmentInterface()
    {
        if (IsUiAlive())
        {
            if (_phase == UiAnimPhase.Hiding)
                ShowEquipmentInterface(true);
            else
                HideEquipmentInterface();
        }
        else
        {
            ShowEquipmentInterface(true);
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
            float delay = i * PartStagger;
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
                    isBackground ? BackgroundFadeDuration : PartMoveDuration * 0.9f
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
            int reverseIndex = _parts.Count - 1 - i;
            float delay = reverseIndex * PartStagger;
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
                    isBackground ? BackgroundFadeDuration : PartHideDuration * 0.9f
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

    private void BindCloseButtonWithAnimation(EquipmentInterface ui)
    {
        var closeButton = ui.GetNodeOrNull<Button>("RootMargin/MainVBox/TopBar/CloseButton");
        if (closeButton == null)
            return;

        // Replace direct close with animated close.
        closeButton.Pressed -= ui.QueueFree;
        closeButton.Pressed += HideEquipmentInterface;
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
        BindCloseButtonWithAnimation(CurrentUI);
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

    private void OnUiTreeExited()
    {
        if (CurrentUI != null)
            CurrentUI.TreeExited -= OnUiTreeExited;
        KillActiveTween();
        _parts.Clear();
        _phase = UiAnimPhase.Idle;
        CurrentUI = null;
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
