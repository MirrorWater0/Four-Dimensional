using System;
using System.Collections.Generic;
using Godot;

public partial class TargetSelectOverlay : Control
{
    [Signal]
    public delegate void CharacterSelectedEventHandler(int characterIndex);

    [Signal]
    public delegate void SelectionCanceledEventHandler();

    private ColorRect Mask => field ??= GetNode<ColorRect>("Mask");
    private Panel PanelNode => field ??= GetNode<Panel>("Panel");
    private Label TitleLabel => field ??= GetNode<Label>("Panel/Title");
    private Label HintLabel => field ??= GetNode<Label>("Panel/Hint");
    private Button CancelButton => field ??= GetNode<Button>("Panel/Cancel");
    private IReadOnlyList<Button> CharacterButtons =>
        field ??= [
            GetNode<Button>("Panel/SelectChar1"),
            GetNode<Button>("Panel/SelectChar2"),
            GetNode<Button>("Panel/SelectChar3"),
            GetNode<Button>("Panel/SelectChar4"),
        ];

    public bool IsSelectionVisible => Visible;
    private Tween _activeTween;
    private bool _isAnimating;
    private Vector2 _panelBasePosition;
    private Vector2 _titleBasePosition;
    private Vector2 _hintBasePosition;
    private Vector2 _cancelBasePosition;
    private Vector2[] _buttonBasePositions = Array.Empty<Vector2>();
    private float _maskBaseAlpha;

    public override void _Ready()
    {
        CacheBaseLayout();
        Visible = false;
        for (int i = 0; i < CharacterButtons.Count; i++)
        {
            int capturedIndex = i;
            CharacterButtons[i].Pressed += () => OnCharacterPressed(capturedIndex);
        }
        CancelButton.Pressed += OnCancelPressed;
    }

    public void ShowSelection(PlayerInfoStructure[] players, string hintText)
    {
        players ??= Array.Empty<PlayerInfoStructure>();
        HintLabel.Text = string.IsNullOrWhiteSpace(hintText)
            ? "请选择一名角色作为该选项的目标"
            : hintText;

        for (int i = 0; i < CharacterButtons.Count; i++)
        {
            var button = CharacterButtons[i];
            bool exists = i < players.Length;
            button.Visible = exists;
            button.Disabled = !exists;
            if (exists)
            {
                string name = string.IsNullOrWhiteSpace(players[i].CharacterName)
                    ? $"角色 {i + 1}"
                    : players[i].CharacterName;
                button.Text = name;
            }
        }

        PlayAssembleAnimation();
    }

    public void HideSelection()
    {
        if (!Visible && !_isAnimating)
            return;
        PlayDisassembleAnimation();
    }

    private void OnCharacterPressed(int characterIndex)
    {
        if (!Visible || _isAnimating)
            return;
        HideSelection();
        EmitSignal(SignalName.CharacterSelected, characterIndex);
    }

    private void OnCancelPressed()
    {
        if (!Visible || _isAnimating)
            return;
        HideSelection();
        EmitSignal(SignalName.SelectionCanceled);
    }

    private void CacheBaseLayout()
    {
        _maskBaseAlpha = Mask.Modulate.A;
        _panelBasePosition = PanelNode.Position;
        _titleBasePosition = TitleLabel.Position;
        _hintBasePosition = HintLabel.Position;
        _cancelBasePosition = CancelButton.Position;

        _buttonBasePositions = new Vector2[CharacterButtons.Count];
        for (int i = 0; i < CharacterButtons.Count; i++)
            _buttonBasePositions[i] = CharacterButtons[i].Position;
    }

    private void PlayAssembleAnimation()
    {
        CancelActiveTween();
        _isAnimating = true;
        Visible = true;
        ResetToAssembleStartState();

        var tween = CreateTween();
        _activeTween = tween;
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quint);

        tween.TweenProperty(Mask, "modulate:a", _maskBaseAlpha, 0.2f);
        tween.TweenProperty(PanelNode, "modulate:a", 1.0f, 0.26f).SetDelay(0.02f);
        tween.TweenProperty(PanelNode, "position", _panelBasePosition, 0.26f).SetDelay(0.02f);
        tween.TweenProperty(PanelNode, "scale", Vector2.One, 0.26f).SetDelay(0.02f);

        tween.TweenProperty(TitleLabel, "modulate:a", 1.0f, 0.2f).SetDelay(0.08f);
        tween.TweenProperty(TitleLabel, "position", _titleBasePosition, 0.2f).SetDelay(0.08f);
        tween.TweenProperty(HintLabel, "modulate:a", 1.0f, 0.2f).SetDelay(0.12f);
        tween.TweenProperty(HintLabel, "position", _hintBasePosition, 0.2f).SetDelay(0.12f);

        int visibleButtonCount = 0;
        for (int i = 0; i < CharacterButtons.Count; i++)
        {
            var button = CharacterButtons[i];
            if (!button.Visible)
                continue;

            float delay = 0.16f + 0.04f * visibleButtonCount;
            tween.TweenProperty(button, "modulate:a", 1.0f, 0.18f).SetDelay(delay);
            tween.TweenProperty(button, "position", _buttonBasePositions[i], 0.18f).SetDelay(delay);
            visibleButtonCount++;
        }

        tween.TweenProperty(CancelButton, "modulate:a", 1.0f, 0.18f).SetDelay(0.2f + 0.04f * visibleButtonCount);
        tween.TweenProperty(CancelButton, "position", _cancelBasePosition, 0.18f).SetDelay(0.2f + 0.04f * visibleButtonCount);
        tween.Finished += OnAssembleFinished;
    }

    private void PlayDisassembleAnimation()
    {
        CancelActiveTween();
        if (!Visible)
            return;

        _isAnimating = true;
        var tween = CreateTween();
        _activeTween = tween;
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quint);

        int visibleButtonCount = 0;
        for (int i = CharacterButtons.Count - 1; i >= 0; i--)
        {
            var button = CharacterButtons[i];
            if (!button.Visible)
                continue;

            float delay = 0.02f + 0.03f * visibleButtonCount;
            tween.TweenProperty(button, "modulate:a", 0.0f, 0.14f).SetDelay(delay);
            tween.TweenProperty(button, "position", _buttonBasePositions[i] + new Vector2(0, 8), 0.14f).SetDelay(delay);
            visibleButtonCount++;
        }

        tween.TweenProperty(CancelButton, "modulate:a", 0.0f, 0.14f).SetDelay(0.04f);
        tween.TweenProperty(CancelButton, "position", _cancelBasePosition + new Vector2(0, 8), 0.14f).SetDelay(0.04f);
        tween.TweenProperty(HintLabel, "modulate:a", 0.0f, 0.16f).SetDelay(0.08f);
        tween.TweenProperty(HintLabel, "position", _hintBasePosition + new Vector2(0, -8), 0.16f).SetDelay(0.08f);
        tween.TweenProperty(TitleLabel, "modulate:a", 0.0f, 0.16f).SetDelay(0.1f);
        tween.TweenProperty(TitleLabel, "position", _titleBasePosition + new Vector2(0, -10), 0.16f).SetDelay(0.1f);
        tween.TweenProperty(PanelNode, "modulate:a", 0.0f, 0.22f).SetDelay(0.12f);
        tween.TweenProperty(PanelNode, "position", _panelBasePosition + new Vector2(0, 20), 0.22f).SetDelay(0.12f);
        tween.TweenProperty(PanelNode, "scale", new Vector2(0.97f, 0.97f), 0.22f).SetDelay(0.12f);
        tween.TweenProperty(Mask, "modulate:a", 0.0f, 0.2f);
        tween.Finished += OnDisassembleFinished;
    }

    private void OnAssembleFinished()
    {
        _activeTween = null;
        _isAnimating = false;
        SetControlAlpha(Mask, _maskBaseAlpha);
        PanelNode.Position = _panelBasePosition;
        PanelNode.Scale = Vector2.One;
        TitleLabel.Position = _titleBasePosition;
        HintLabel.Position = _hintBasePosition;
        CancelButton.Position = _cancelBasePosition;
        for (int i = 0; i < CharacterButtons.Count; i++)
            CharacterButtons[i].Position = _buttonBasePositions[i];
    }

    private void OnDisassembleFinished()
    {
        _activeTween = null;
        _isAnimating = false;
        Visible = false;
        ResetToBaseState();
    }

    private void CancelActiveTween()
    {
        if (_activeTween == null)
            return;
        if (GodotObject.IsInstanceValid(_activeTween))
            _activeTween.Kill();
        _activeTween = null;
        _isAnimating = false;
    }

    private void ResetToAssembleStartState()
    {
        SetControlAlpha(Mask, 0.0f);
        SetControlAlpha(PanelNode, 0.0f);
        PanelNode.Position = _panelBasePosition + new Vector2(0, 20);
        PanelNode.Scale = new Vector2(0.97f, 0.97f);

        SetControlAlpha(TitleLabel, 0.0f);
        TitleLabel.Position = _titleBasePosition + new Vector2(0, -10);
        SetControlAlpha(HintLabel, 0.0f);
        HintLabel.Position = _hintBasePosition + new Vector2(0, -8);

        for (int i = 0; i < CharacterButtons.Count; i++)
        {
            CharacterButtons[i].Position = _buttonBasePositions[i] + new Vector2(0, 8);
            SetControlAlpha(CharacterButtons[i], 0.0f);
        }

        CancelButton.Position = _cancelBasePosition + new Vector2(0, 8);
        SetControlAlpha(CancelButton, 0.0f);
    }

    private void ResetToBaseState()
    {
        SetControlAlpha(Mask, _maskBaseAlpha);
        SetControlAlpha(PanelNode, 1.0f);
        PanelNode.Position = _panelBasePosition;
        PanelNode.Scale = Vector2.One;

        SetControlAlpha(TitleLabel, 1.0f);
        TitleLabel.Position = _titleBasePosition;
        SetControlAlpha(HintLabel, 1.0f);
        HintLabel.Position = _hintBasePosition;

        for (int i = 0; i < CharacterButtons.Count; i++)
        {
            CharacterButtons[i].Position = _buttonBasePositions[i];
            SetControlAlpha(CharacterButtons[i], 1.0f);
        }

        CancelButton.Position = _cancelBasePosition;
        SetControlAlpha(CancelButton, 1.0f);
    }

    private static void SetControlAlpha(CanvasItem node, float alpha)
    {
        if (node == null)
            return;
        node.Modulate = node.Modulate with { A = alpha };
    }
}
