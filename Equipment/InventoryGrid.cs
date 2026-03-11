using System.Linq;
using Godot;

/// <summary>
/// InventoryGrid 负责可选装备区的完整布局与滚动：
/// 1) 监听父节点(InventoryScroll)尺寸变化并重排卡片。
/// 2) 统一控制卡片高度、间距与上下左右内边距。
/// 3) 处理滚轮/手势输入，维护滚动偏移并应用到自身 Position。
/// 4) 通过输入阻塞与布局抑制开关，配合外部动画期间的交互控制。
/// </summary>
public partial class InventoryGrid : Control
{
    [Export]
    public float CardHeight = 130.0f;

    [Export]
    public float CardSpacing = 10.0f;

    [Export]
    public float ScrollStep = 64.0f;

    [Export]
    public float PanGestureMultiplier = 12.0f;

    [Export]
    public float HorizontalPadding = 5.0f;

    [Export]
    public float TopPadding = 5.0f;

    [Export]
    public float BottomPadding = 7.0f;

    private Control ScrollViewport => _scrollViewport ??= GetParent() as Control;

    private Control _scrollViewport;
    private float _scrollOffset;
    private float _contentHeight;
    private bool _layoutSuppressed;
    private bool _inputBlocked;

    /// <summary>
    /// 初始化输入处理，并监听父容器尺寸变化用于自动重排。
    /// </summary>
    public override void _Ready()
    {
        SetProcessInput(true);
        if (ScrollViewport != null)
            ScrollViewport.ItemRectChanged += OnScrollViewportRectChanged;
    }

    /// <summary>
    /// 外部可在动画期间阻止滚动输入。
    /// </summary>
    public void SetInputBlocked(bool blocked)
    {
        _inputBlocked = blocked;
    }

    /// <summary>
    /// 外部可在重排动画期间暂时禁止 LayoutCards 自动写位置。
    /// </summary>
    public void SetLayoutSuppressed(bool suppressed)
    {
        _layoutSuppressed = suppressed;
    }

    /// <summary>
    /// 处理滚轮/手势输入，并只在鼠标位于滚动区域内时生效。
    /// </summary>
    public override void _Input(InputEvent @event)
    {
        if (_inputBlocked)
            return;

        if (ScrollViewport == null || !ScrollViewport.Visible)
            return;

        Rect2 scrollRect = ScrollViewport.GetGlobalRect();
        Vector2 mousePos = GetViewport().GetMousePosition();
        if (!scrollRect.HasPoint(mousePos))
            return;

        if (TryHandleScrollInput(@event))
            GetViewport().SetInputAsHandled();
    }

    /// <summary>
    /// 按当前参数重新排列可见卡片，并更新内容高度与滚动边界。
    /// </summary>
    public void LayoutCards(bool force = false)
    {
        if (_layoutSuppressed && !force)
            return;
        if (ScrollViewport == null)
            return;

        float availableWidth = ScrollViewport.Size.X - HorizontalPadding * 2.0f;
        float innerViewportHeight = GetInnerViewportHeight();
        if (availableWidth <= 0.0f || innerViewportHeight <= 0.0f)
            return;

        float fallbackHeight = CardHeight > 0.0f ? CardHeight : 170.0f;
        float y = 0.0f;
        bool hasVisibleCard = false;

        foreach (var child in GetChildren())
        {
            if (child is not Control card)
                continue;
            if (!card.Visible)
                continue;

            if (card.AnchorLeft != card.AnchorRight || card.AnchorTop != card.AnchorBottom)
            {
                card.AnchorLeft = 0.0f;
                card.AnchorRight = 0.0f;
                card.AnchorTop = 0.0f;
                card.AnchorBottom = 0.0f;
            }

            hasVisibleCard = true;

            float cardHeight = fallbackHeight;
            card.Position = new Vector2(0.0f, y);
            card.Size = new Vector2(availableWidth, cardHeight);
            card.CustomMinimumSize = new Vector2(card.CustomMinimumSize.X, cardHeight);

            if (card is CardSlot slot)
                slot.SyncPanelSizeToCard();

            y += cardHeight + CardSpacing;
        }

        if (hasVisibleCard)
            y -= CardSpacing;

        _contentHeight = Mathf.Max(y, innerViewportHeight);
        Size = new Vector2(availableWidth, _contentHeight);
        CustomMinimumSize = Size;

        ClampScrollOffset();
        ApplyScrollOffset();
    }

    /// <summary>
    /// 将输入事件转换为像素滚动位移。
    /// </summary>
    private bool TryHandleScrollInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            float delta = 0.0f;
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
                delta = -ScrollStep;
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
                delta = ScrollStep;

            if (Mathf.Abs(delta) > 0.01f)
            {
                ScrollBy(delta);
                return true;
            }
        }
        else if (@event is InputEventPanGesture panGesture)
        {
            if (Mathf.Abs(panGesture.Delta.Y) > 0.01f)
            {
                ScrollBy(panGesture.Delta.Y * PanGestureMultiplier);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 父容器尺寸变化后触发重排。
    /// </summary>
    private void OnScrollViewportRectChanged()
    {
        LayoutCards();
    }

    /// <summary>
    /// 在合法滚动范围内更新滚动偏移。
    /// </summary>
    private void ScrollBy(float delta)
    {
        float maxScroll = GetMaxScroll();
        if (maxScroll <= 0.0f)
        {
            _scrollOffset = 0.0f;
            ApplyScrollOffset();
            return;
        }

        _scrollOffset = Mathf.Clamp(_scrollOffset + delta, 0.0f, maxScroll);
        ApplyScrollOffset();
    }

    /// <summary>
    /// 计算当前内容可滚动的最大距离。
    /// </summary>
    private float GetMaxScroll()
    {
        float innerViewportHeight = GetInnerViewportHeight();
        if (innerViewportHeight <= 0.0f)
            return 0.0f;
        return Mathf.Max(0.0f, _contentHeight - innerViewportHeight);
    }

    /// <summary>
    /// 计算受上下内边距影响后的可视滚动高度。
    /// </summary>
    private float GetInnerViewportHeight()
    {
        if (ScrollViewport == null)
            return 0.0f;

        return ScrollViewport.Size.Y - TopPadding - BottomPadding;
    }

    /// <summary>
    /// 将滚动偏移限制在合法范围内。
    /// </summary>
    private void ClampScrollOffset()
    {
        float maxScroll = GetMaxScroll();
        if (maxScroll <= 0.0f)
        {
            _scrollOffset = 0.0f;
            return;
        }

        _scrollOffset = Mathf.Clamp(_scrollOffset, 0.0f, maxScroll);
    }

    /// <summary>
    /// 通过移动 Grid 自身位置实现滚动视觉效果。
    /// </summary>
    private void ApplyScrollOffset()
    {
        Position = new Vector2(HorizontalPadding, TopPadding - _scrollOffset);
    }
}
