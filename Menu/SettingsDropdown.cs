using System;
using System.Collections.Generic;
using Godot;

public partial class SettingsDropdown : Button
{
    [Signal]
    public delegate void ItemSelectedEventHandler(long index);

    private sealed class DropdownItem
    {
        public string Text;
        public int Id;
        public Variant Metadata;
    }

    private static SettingsDropdown _openDropdown;

    private readonly List<DropdownItem> _items = new();
    private readonly List<Button> _itemButtons = new();
    private PanelContainer _popupPanel;
    private VBoxContainer _popupList;
    private int _selectedIndex = -1;

    public int ItemCount => _items.Count;
    public int Selected => _selectedIndex;

    public override void _Ready()
    {
        Alignment = HorizontalAlignment.Left;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        ToggleMode = false;

        Pressed += TogglePopup;
        Resized += UpdatePopupPosition;
        VisibilityChanged += OnVisibilityChanged;

        EnsurePopup();
        RefreshButtonText();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
            return;
        if (!_popupPanel?.Visible == true)
            return;

        Vector2 mousePosition = GetViewport().GetMousePosition();
        if (GetGlobalRect().HasPoint(mousePosition) || _popupPanel.GetGlobalRect().HasPoint(mousePosition))
            return;

        ClosePopup();
    }

    public override void _ExitTree()
    {
        if (_openDropdown == this)
            _openDropdown = null;
    }

    public void Clear()
    {
        _items.Clear();
        _selectedIndex = -1;
        RebuildPopup();
        RefreshButtonText();
    }

    public void AddItem(string text, int id = -1)
    {
        AddItem(text, id, Variant.CreateFrom(id >= 0 ? id : _items.Count));
    }

    public void AddItem(string text, int id, Variant metadata)
    {
        _items.Add(
            new DropdownItem
            {
                Text = text ?? string.Empty,
                Id = id >= 0 ? id : _items.Count,
                Metadata = metadata,
            }
        );
        RebuildPopup();
        if (_selectedIndex < 0 && _items.Count > 0)
            Select(0);
    }

    public int GetItemId(int index)
    {
        return index >= 0 && index < _items.Count ? _items[index].Id : -1;
    }

    public Variant GetItemMetadata(int index)
    {
        return index >= 0 && index < _items.Count ? _items[index].Metadata : default;
    }

    public void SetItemMetadata(int index, Variant metadata)
    {
        if (index < 0 || index >= _items.Count)
            return;

        _items[index].Metadata = metadata;
        RefreshButtonText();
        RefreshItemButtonTexts();
    }

    public void Select(int index)
    {
        Select(index, emitSignal: false);
    }

    public void Select(int index, bool emitSignal)
    {
        if (index < 0 || index >= _items.Count)
            return;

        _selectedIndex = index;
        RefreshButtonText();
        RefreshItemButtonTexts();

        if (emitSignal)
            EmitSignal(SignalName.ItemSelected, (long)index);
    }

    public void ClosePopup()
    {
        if (_popupPanel == null)
            return;

        _popupPanel.Visible = false;
        if (_openDropdown == this)
            _openDropdown = null;
    }

    private void TogglePopup()
    {
        EnsurePopup();
        if (_popupPanel == null)
            return;

        if (_popupPanel.Visible)
        {
            ClosePopup();
            return;
        }

        if (_openDropdown != null && _openDropdown != this)
            _openDropdown.ClosePopup();

        _openDropdown = this;
        UpdatePopupPosition();
        _popupPanel.Visible = true;
    }

    private void EnsurePopup()
    {
        if (_popupPanel != null && GodotObject.IsInstanceValid(_popupPanel))
            return;

        _popupList = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Stop,
        };
        _popupList.AddThemeConstantOverride("separation", 2);

        _popupPanel = new PanelContainer
        {
            Name = "PopupPanel",
            TopLevel = true,
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
            ZIndex = 500,
        };
        _popupPanel.AddChild(_popupList);
        AddChild(_popupPanel);

        ApplyPopupPanelStyle();
        RebuildPopup();
    }

    private void ApplyPopupPanelStyle()
    {
        if (_popupPanel == null)
            return;

        var style = GetThemeStylebox("normal");
        if (style is StyleBoxFlat baseFlat)
        {
            var popupStyle = (StyleBoxFlat)baseFlat.Duplicate();
            popupStyle.ContentMarginLeft = 6f;
            popupStyle.ContentMarginTop = 6f;
            popupStyle.ContentMarginRight = 6f;
            popupStyle.ContentMarginBottom = 6f;
            popupStyle.BgColor = new Color(0.16f, 0.21f, 0.28f, 0.98f);
            popupStyle.BorderColor = new Color(0.72f, 0.83f, 0.93f, 0.38f);
            popupStyle.ShadowSize = 12;
            popupStyle.ShadowColor = new Color(0.01f, 0.02f, 0.04f, 0.45f);
            popupStyle.ShadowOffset = new Vector2(0, 6);
            _popupPanel.AddThemeStyleboxOverride("panel", popupStyle);
            return;
        }

        var fallback = new StyleBoxFlat
        {
            BgColor = new Color(0.16f, 0.21f, 0.28f, 0.98f),
            BorderColor = new Color(0.72f, 0.83f, 0.93f, 0.38f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 6f,
            ContentMarginTop = 6f,
            ContentMarginRight = 6f,
            ContentMarginBottom = 6f,
        };
        _popupPanel.AddThemeStyleboxOverride("panel", fallback);
    }

    private void RebuildPopup()
    {
        if (_popupList == null)
            return;

        foreach (Node child in _popupList.GetChildren())
            child.QueueFree();
        _itemButtons.Clear();

        for (int i = 0; i < _items.Count; i++)
        {
            int capturedIndex = i;
            var itemButton = new Button
            {
                CustomMinimumSize = new Vector2(0f, 42f),
                Text = string.Empty,
                Alignment = HorizontalAlignment.Left,
                FocusMode = FocusModeEnum.None,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseDefaultCursorShape = CursorShape.PointingHand,
            };
            ApplyItemButtonTheme(itemButton);
            itemButton.Pressed += () =>
            {
                Select(capturedIndex, emitSignal: true);
                ClosePopup();
            };
            _popupList.AddChild(itemButton);
            _itemButtons.Add(itemButton);
        }

        RefreshItemButtonTexts();
        UpdatePopupPosition();
    }

    private void ApplyItemButtonTheme(Button itemButton)
    {
        CopyThemeIfPresent(itemButton, "font", "font");
        CopyThemeIfPresent(itemButton, "font_size", "font_size");
        CopyThemeIfPresent(itemButton, "font_color", "font_color");
        CopyThemeIfPresent(itemButton, "font_hover_color", "font_hover_color");
        CopyThemeIfPresent(itemButton, "font_pressed_color", "font_pressed_color");

        if (GetThemeStylebox("normal") is StyleBoxFlat normalFlat)
        {
            var normal = (StyleBoxFlat)normalFlat.Duplicate();
            normal.BgColor = new Color(0.17f, 0.22f, 0.29f, 0.92f);
            normal.ContentMarginLeft = 16f;
            normal.ContentMarginTop = 10f;
            normal.ContentMarginRight = 16f;
            normal.ContentMarginBottom = 10f;
            itemButton.AddThemeStyleboxOverride("normal", normal);
        }

        if (GetThemeStylebox("hover") is StyleBoxFlat hoverFlat)
        {
            var hover = (StyleBoxFlat)hoverFlat.Duplicate();
            hover.ContentMarginLeft = 16f;
            hover.ContentMarginTop = 10f;
            hover.ContentMarginRight = 16f;
            hover.ContentMarginBottom = 10f;
            itemButton.AddThemeStyleboxOverride("hover", hover);
            itemButton.AddThemeStyleboxOverride("focus", hover);
        }

        if (GetThemeStylebox("pressed") is StyleBoxFlat pressedFlat)
        {
            var pressed = (StyleBoxFlat)pressedFlat.Duplicate();
            pressed.ContentMarginLeft = 16f;
            pressed.ContentMarginTop = 10f;
            pressed.ContentMarginRight = 16f;
            pressed.ContentMarginBottom = 10f;
            itemButton.AddThemeStyleboxOverride("pressed", pressed);
        }
    }

    private void CopyThemeIfPresent(Control control, string overrideName, string themeItemName)
    {
        if (HasThemeFontOverride(themeItemName))
            control.AddThemeFontOverride(overrideName, GetThemeFont(themeItemName));
        if (HasThemeFontSizeOverride(themeItemName))
            control.AddThemeFontSizeOverride(overrideName, GetThemeFontSize(themeItemName));
        if (HasThemeColorOverride(themeItemName))
            control.AddThemeColorOverride(overrideName, GetThemeColor(themeItemName));
    }

    private void RefreshButtonText()
    {
        string selectedText =
            _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex].Text : string.Empty;
        Text = string.IsNullOrEmpty(selectedText) ? "▼" : $"{selectedText}  ▼";
    }

    private void RefreshItemButtonTexts()
    {
        for (int i = 0; i < _itemButtons.Count && i < _items.Count; i++)
        {
            Button button = _itemButtons[i];
            bool isSelected = i == _selectedIndex;
            button.Text = isSelected ? $"● {_items[i].Text}" : $"  {_items[i].Text}";
        }
    }

    private void UpdatePopupPosition()
    {
        if (_popupPanel == null || _popupList == null)
            return;

        Vector2 popupSize = _popupList.GetCombinedMinimumSize() + new Vector2(12f, 12f);
        float width = Mathf.Max(Size.X, popupSize.X);
        float height = popupSize.Y;
        Vector2 globalPosition = GlobalPosition + new Vector2(0f, Size.Y + 4f);
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;

        if (globalPosition.X + width > viewportSize.X - 12f)
            globalPosition.X = Mathf.Max(12f, viewportSize.X - width - 12f);
        if (globalPosition.Y + height > viewportSize.Y - 12f)
            globalPosition.Y = Mathf.Max(12f, GlobalPosition.Y - height - 4f);

        _popupPanel.Position = globalPosition;
        _popupPanel.Size = new Vector2(width, height);
    }

    private void OnVisibilityChanged()
    {
        if (!Visible)
            ClosePopup();
    }
}
