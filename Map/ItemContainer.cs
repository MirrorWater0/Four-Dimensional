using System;
using Godot;

public partial class ItemContainer : Panel
{
    private static readonly Color SelectedBorderColor = new("#c9f4ff");
    private static readonly Color HoverBorderColor = new("#63f2d4");

    private static ItemContainer _selected;
    private static bool _isUsing;
    private static ItemContainer _openDiscardMenuOwner;

    private Control IconHost => field ??= GetNodeOrNull<Control>("SlotRoot/IconHost");
    private Button DiscardButton => field ??= GetNodeOrNull<Button>("DiscardButton");

    private StyleBoxFlat _runtimeStyle;
    private Color _defaultBorderColor = new("#ffffff");
    private bool _isHovered;
    private bool _enabled;
    private Tip _itemTip;
    private ConsumeItem _discardMenuItem;

    public override void _Ready()
    {
        FocusMode = FocusModeEnum.None;
        MouseEntered += () => SetHoverState(true);
        MouseExited += () => SetHoverState(false);
        GuiInput += OnGuiInput;
        if (IconHost != null)
            IconHost.ChildExitingTree += OnIconChildExiting;
        if (DiscardButton != null)
            DiscardButton.Pressed += OnDiscardButtonPressed;
        InitializeStyle();
        HideDiscardMenuInstance();
        SetEnabled(false);
    }

    public override void _ExitTree()
    {
        if (_openDiscardMenuOwner == this)
            _openDiscardMenuOwner = null;
    }

    public override void _Input(InputEvent @event)
    {
        if (_openDiscardMenuOwner != this || DiscardButton == null || !DiscardButton.Visible)
            return;
        if (@event is not InputEventMouseButton { Pressed: true } mouseButton)
            return;
        if (IsPointInsideSelfOrDiscardMenu(mouseButton.GlobalPosition))
            return;

        HideDiscardMenuInstance();
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton)
            return;
        if (!mouseButton.Pressed)
            return;

        if (_isUsing)
            return;

        if (mouseButton.ButtonIndex == MouseButton.Right)
        {
            ShowDiscardMenu();
            AcceptEvent();
            return;
        }

        if (mouseButton.ButtonIndex != MouseButton.Left)
            return;

        if (!_enabled)
            return;

        HideOpenDiscardMenu();
        var item = GetAssignedItem();
        if (item == null)
        {
            ClearSelection(this);
            return;
        }

        SetSelected(this);
        _ = UseSelectedItemAsync(item);
    }

    private void ShowDiscardMenu()
    {
        var item = GetAssignedItem();
        if (item == null)
        {
            HideOpenDiscardMenu();
            return;
        }

        if (DiscardButton == null)
            return;

        HideOpenDiscardMenu();
        HideItemTip();
        _openDiscardMenuOwner = this;
        _discardMenuItem = item;
        DiscardButton.Visible = true;
        DiscardButton.MoveToFront();
    }

    private bool IsPointInsideSelfOrDiscardMenu(Vector2 globalPosition)
    {
        if (GetGlobalRect().HasPoint(globalPosition))
            return true;
        return DiscardButton != null && DiscardButton.GetGlobalRect().HasPoint(globalPosition);
    }

    private void OnDiscardButtonPressed()
    {
        var item = _discardMenuItem;
        HideDiscardMenuInstance();
        if (item == null)
            return;

        HideItemTip();
        item.Discard();
        ClearSelection(this);
    }

    private static void HideOpenDiscardMenu()
    {
        _openDiscardMenuOwner?.HideDiscardMenuInstance();
        _openDiscardMenuOwner = null;
    }

    private void HideDiscardMenuInstance()
    {
        if (DiscardButton != null && GodotObject.IsInstanceValid(DiscardButton))
            DiscardButton.Visible = false;
        _discardMenuItem = null;
        if (_openDiscardMenuOwner == this)
            _openDiscardMenuOwner = null;
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
        {
            ClearSelection(this);
            HideDiscardMenuInstance();
        }
    }

    public bool HasItemIcon()
    {
        return GetItemIcon() != null;
    }

    public void AddItemIcon(Node icon)
    {
        if (icon == null || IconHost == null)
            return;

        IconHost.AddChild(icon);
        if (icon is Control control)
        {
            control.MouseFilter = MouseFilterEnum.Ignore;
            control.SetAnchorsPreset(LayoutPreset.FullRect);
            control.OffsetLeft = 0f;
            control.OffsetTop = 0f;
            control.OffsetRight = 0f;
            control.OffsetBottom = 0f;
        }
    }

    public void ClearItemIcon()
    {
        if (IconHost == null)
            return;

        for (int i = IconHost.GetChildCount() - 1; i >= 0; i--)
        {
            Node child = IconHost.GetChild(i);
            IconHost.RemoveChild(child);
            child.QueueFree();
        }

        HideItemTip();
        HideDiscardMenuInstance();
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

    private void OnIconChildExiting(Node child)
    {
        if (_selected != this)
            return;
        CallDeferred(nameof(ClearIfEmpty));
    }

    private void ClearIfEmpty()
    {
        if (_selected == this && !HasItemIcon())
            ClearSelection(this);
        HideItemTip();
        if (!HasItemIcon())
            HideDiscardMenuInstance();
    }

    private ConsumeItem GetAssignedItem()
    {
        var playerResource = GetPlayerResourceState();
        if (playerResource == null)
            return null;

        var icon = GetItemIcon();
        if (icon == null)
            return null;

        foreach (var item in playerResource.Items)
        {
            if (item != null && item.Icon == icon)
                return item;
        }

        return null;
    }

    private Node GetItemIcon()
    {
        if (IconHost == null || IconHost.GetChildCount() == 0)
            return null;

        return IconHost.GetChild(0);
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
        tip.SetText(item.BuildTooltipText());
    }

    private void HideItemTip()
    {
        _itemTip?.HideTooltip();
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
