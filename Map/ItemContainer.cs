using System;
using Godot;

public partial class ItemContainer : PanelContainer
{
    private static readonly Color SelectedBorderColor = new("#7fc8ff");
    private static readonly Color HoverBorderColor = new("#5cff8a");

    private static ItemContainer _selected;
    private static bool _isUsing;

    private StyleBoxFlat _runtimeStyle;
    private Color _defaultBorderColor = new("#ffffff");
    private bool _isHovered;
    private bool _enabled;
    private Tip _itemTip;

    public override void _Ready()
    {
        FocusMode = FocusModeEnum.None;
        MouseEntered += () => SetHoverState(true);
        MouseExited += () => SetHoverState(false);
        GuiInput += OnGuiInput;
        ChildExitingTree += child => OnChildExiting(child);
        InitializeStyle();
        SetEnabled(false);
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton)
            return;
        if (!mouseButton.Pressed || mouseButton.ButtonIndex != MouseButton.Left)
            return;
        if (_isUsing)
            return;
        if (!_enabled)
            return;

        var item = GetAssignedItem();
        if (item == null)
        {
            ClearSelection(this);
            return;
        }

        SetSelected(this);
        _ = UseSelectedItemAsync(item);
    }

    private async System.Threading.Tasks.Task UseSelectedItemAsync(ConsumeItem item)
    {
        if (item == null)
            return;

        _isUsing = true;
        var battle = FindBattle();
        if (battle == null)
        {
            _isUsing = false;
            ClearSelection(this);
            return;
        }

        await item.UseEffect(battle);
        _isUsing = false;

        if (_selected == this)
            ClearSelection(this);
    }

    private void InitializeStyle()
    {
        var baseStyle = GetThemeStylebox("panel") as StyleBoxFlat;
        if (baseStyle == null)
            return;

        _runtimeStyle = (StyleBoxFlat)baseStyle.Duplicate();
        _defaultBorderColor = _runtimeStyle.BorderColor;
        AddThemeStyleboxOverride("panel", _runtimeStyle);
    }

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        MouseFilter = MouseFilterEnum.Stop;
        Modulate = enabled ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0.55f);
        if (!enabled)
            ClearSelection(this);
    }

    private void SetHoverState(bool hovered)
    {
        _isHovered = hovered;
        if (hovered)
            ShowItemTip();
        else
            HideItemTip();

        if (_runtimeStyle == null || _selected == this)
            return;

        _runtimeStyle.BorderColor = hovered ? HoverBorderColor : _defaultBorderColor;
    }

    private static void SetSelected(ItemContainer container)
    {
        if (_selected == container)
            return;

        if (_selected != null)
            _selected.ApplySelection(false);

        _selected = container;
        _selected?.ApplySelection(true);
    }

    private static void ClearSelection(ItemContainer container)
    {
        if (_selected != container)
            return;

        _selected?.ApplySelection(false);
        _selected = null;
    }

    private void ApplySelection(bool selected)
    {
        if (_runtimeStyle == null)
            return;

        _runtimeStyle.BorderColor = selected
            ? SelectedBorderColor
            : (_isHovered ? HoverBorderColor : _defaultBorderColor);
    }

    private void OnChildExiting(Node child)
    {
        if (_selected != this)
            return;
        CallDeferred(nameof(ClearIfEmpty));
    }

    private void ClearIfEmpty()
    {
        if (_selected == this && GetChildCount() == 0)
            ClearSelection(this);
        HideItemTip();
    }

    private ConsumeItem GetAssignedItem()
    {
        var playerResource = GetPlayerResourceState();
        if (playerResource == null)
            return null;

        if (GetChildCount() == 0)
            return null;

        if (GetChild(0) is not Node icon)
            return null;

        foreach (var item in playerResource.Items)
        {
            if (item != null && item.Icon == icon)
                return item;
        }

        return null;
    }

    private void ShowItemTip()
    {
        var item = GetAssignedItem();
        if (item == null)
        {
            HideItemTip();
            return;
        }

        var tip = GetOrCreateItemTip();
        if (tip == null)
            return;

        tip.FollowMouse = true;
        tip.Description.Text = item.BuildTooltipText();
        tip.Visible = true;
    }

    private void HideItemTip()
    {
        if (_itemTip != null)
            _itemTip.Visible = false;
    }

    private Tip GetOrCreateItemTip()
    {
        if (_itemTip != null && GodotObject.IsInstanceValid(_itemTip))
            return _itemTip;

        var root = GetTree()?.Root;
        if (root == null)
            return null;

        var layer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (layer == null)
        {
            layer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
            if (root.IsInsideTree())
                root.AddChild(layer);
            else
                root.CallDeferred(Node.MethodName.AddChild, layer);
        }

        var existing = layer.GetNodeOrNull<Tip>("ItemTip");
        if (existing != null)
        {
            _itemTip = existing;
            return _itemTip;
        }

        var tipScene = GD.Load<PackedScene>("res://battle/UIScene/Tip.tscn");
        if (tipScene == null)
            return null;

        var tip = tipScene.Instantiate<Tip>();
        tip.Name = "ItemTip";
        tip.FollowMouse = true;
        tip.AnchorOffset = new Vector2(20f, 20f);
        layer.AddChild(tip);
        _itemTip = tip;
        return _itemTip;
    }

    private PlayerResourceState GetPlayerResourceState()
    {
        return GetParent()?.GetParent() as PlayerResourceState;
    }

    private static Battle FindBattle()
    {
        var root = Engine.GetMainLoop() as SceneTree;
        if (root == null)
            return null;
        return FindBattleInTree(root.Root);
    }

    private static Battle FindBattleInTree(Node node)
    {
        if (node is Battle battle)
            return battle;

        foreach (var child in node.GetChildren())
        {
            if (child is not Node childNode)
                continue;
            var result = FindBattleInTree(childNode);
            if (result != null)
                return result;
        }

        return null;
    }
}
