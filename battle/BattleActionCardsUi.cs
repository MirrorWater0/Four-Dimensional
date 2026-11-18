using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;

/// <summary>Positions racks at formation anchors, independently of attack/hit animation movement.</summary>
[Tool]
public partial class BattleActionCardsUi : Control {
    [ExportGroup("Action Card Layout")]
    [Export] public Vector2 OwnerOffset { get; set; } = new(0, 204);
    [Export] public Vector2 EnemyOwnerOffset { get; set; } = new(0, 174);
    [Export] public float RackWidth { get; set; } = 210;
    [Export] public float CardScale { get; set; } = 0.29f;
    [Export] public float CardStep { get; set; } = 48;
    [Export] public bool FitCardsToWidth { get; set; } = true;
    [Export] public Vector2 ContentOffset { get; set; } = Vector2.Zero;
    [Export] public bool ShowShortNames { get; set; } = false;
    [Export] public int ShortNameFontSize { get; set; } = 13;
    [Export] public float ShortNameGap { get; set; }
    [Export] public float ShortNameHeight { get; set; } = 18;
    [Export(PropertyHint.Range, "0,1,0.01")] public float LayoutMoveDuration { get; set; } = 0.24f;
    [ExportGroup("Action Card Hover")]
    [Export] public float HoverLift { get; set; } = 10;
    [Export] public float HoverDuration { get; set; } = 0.16f;
    [Export] public Vector2 PreviewOffset { get; set; } = Vector2.Zero;
    [Export] public float PreviewScale { get; set; } = 1;
    [Export] public float PreviewHintGap { get; set; } = 5;
    [ExportGroup("Action Card Installation")]
    [Export(PropertyHint.Range, "0.03,0.3,0.01")] public float InstallWindupDuration { get; set; } = 0.09f;
    [Export(PropertyHint.Range, "0.1,0.8,0.01")] public float InstallFlyDuration { get; set; } = 0.32f;
    [Export(PropertyHint.Range, "80,700,10")] public float InstallArcHeight { get; set; } = 400;
    [Export(PropertyHint.Range, "0,150,1")] public float InstallPullback { get; set; } = 58;
    [Export(PropertyHint.Range, "1,80,1")] public float InstallTrailWidth { get; set; } = 42;
    [Export(PropertyHint.Range, "0.01,0.1,0.005")] public float InstallFlightScale { get; set; } = 0.035f;
    [Export(PropertyHint.Range, "1,2,0.05")] public float InstallImpactScale { get; set; } = 1.35f;

    private Battle _battle;
    private int _appliedLayoutHash;
    private bool _layoutApplied;
    private readonly Dictionary<PlayerCharacter, ActionCardRack> _racks = new();
    private readonly Dictionary<EnemyCharacter, ActionCardRack> _enemyRacks = new();
    private readonly Dictionary<EnemyCharacter, (Skill Skill, bool Stunned)> _enemyCards = new();
    private IEnumerable<ActionCardRack> AllRacks => _racks.Values.Concat(_enemyRacks.Values);
    private Skill _replacementPreviewSkill;
    public bool IsChoosingReplacement => _racks.Values.Any(rack => rack.IsChoosingReplacement);
    public bool IsInspectingCards => AllRacks.Any(rack => rack.HoveredIndex >= 0 || rack.IsChoosingReplacement);

    public override void _EnterTree() {
        // Map battles inherit a canvas layer; standalone battles use the default canvas.
        if (GetParent() is CanvasLayer layer && layer.GetParent() is Battle battle)
            layer.Layer = (battle.GetCanvasLayerNode()?.Layer ?? 0) + 1;
    }

    public override void _Ready() {
        if (Engine.IsEditorHint())
            return;
        foreach (var rack in GetChildren().OfType<ActionCardRack>())
            rack.Visible = false;
        _battle = GetParent()?.GetParent() as Battle;
        if (_battle == null)
            return;
        _battle.ActionCardsChanged += Refresh;
        _battle.ActionCardTriggered += Pulse;
        _battle.ActionCardUninstalled += PlayUninstall;
        _battle.ActionCardExhausted += PlayExhaust;
    }

    public override void _Process(double delta) {
        int layoutHash = LayoutHash();
        if (!_layoutApplied || _appliedLayoutHash != layoutHash) {
            foreach (ActionCardRack rack in GetChildren().OfType<ActionCardRack>())
                ApplyLayout(rack);
            _appliedLayoutHash = layoutHash;
            _layoutApplied = true;
        }
        if (Engine.IsEditorHint() || _battle == null)
            return;
        foreach (PlayerCharacter player in _battle.PlayersList.Where(player => player != null && GodotObject.IsInstanceValid(player))) {
            if (!_racks.ContainsKey(player))
                Refresh(player);
            var rack = _racks[player];
            Vector2 anchor = player.GetParent<Node2D>().GetGlobalTransformWithCanvas() * _battle.GetFormationPosition(player.PositionIndex, -1);
            rack.Position = GetGlobalTransformWithCanvas().AffineInverse() * anchor
                + OwnerOffset - new Vector2(RackWidth / 2, 0);
            rack.Modulate = player.State == Character.CharacterState.Dying ? new Color(0.6f, 0.6f, 0.6f, 1) : Colors.White;
        }
        foreach (var pair in _enemyRacks.ToArray()) {
            if (GodotObject.IsInstanceValid(pair.Key) && pair.Key.IsInsideTree())
                continue;
            pair.Value.ClearHoverPreview();
            pair.Value.QueueFree();
            _enemyRacks.Remove(pair.Key);
            _enemyCards.Remove(pair.Key);
        }
        foreach (EnemyCharacter enemy in _battle.GetOrderedTeamCharacters(false, includeSummons: true)
            .OfType<EnemyCharacter>().Where(GodotObject.IsInstanceValid)) {
            RefreshIntention(enemy);
            if (!_enemyRacks.TryGetValue(enemy, out var rack))
                continue;
            Vector2 anchor = enemy.GetParent<Node2D>().GetGlobalTransformWithCanvas()
                * _battle.GetFormationPosition(enemy.PositionIndex, 1);
            rack.Position = GetGlobalTransformWithCanvas().AffineInverse() * anchor
                + EnemyOwnerOffset - new Vector2(RackWidth / 2, 0);
        }
        RefreshReplacementPreview();
    }

    private void ApplyLayout(ActionCardRack rack) {
        rack.HandControl = _battle?.CharacterControl;
        rack.Size = new Vector2(Math.Max(80, RackWidth), 130);
        rack.CardScale = Math.Clamp(CardScale, 0.1f, 0.8f);
        rack.CardStep = Math.Max(1, CardStep);
        rack.FitCardsToWidth = FitCardsToWidth;
        rack.ContentOffset = ContentOffset;
        rack.ShowShortNames = ShowShortNames;
        rack.ShortNameFontSize = ShortNameFontSize;
        rack.ShortNameGap = ShortNameGap;
        rack.ShortNameHeight = ShortNameHeight;
        rack.LayoutMoveDuration = LayoutMoveDuration;
        rack.HoverLift = HoverLift;
        rack.HoverDuration = HoverDuration;
        rack.PreviewOffset = PreviewOffset;
        rack.PreviewScale = PreviewScale;
        rack.PreviewHintGap = PreviewHintGap;
    }

    private int LayoutHash() {
        var hash = new HashCode();
        hash.Add(GetChildCount()); hash.Add(OwnerOffset); hash.Add(EnemyOwnerOffset); hash.Add(RackWidth);
        hash.Add(CardScale); hash.Add(CardStep); hash.Add(FitCardsToWidth); hash.Add(ContentOffset);
        hash.Add(ShowShortNames); hash.Add(ShortNameFontSize); hash.Add(ShortNameGap); hash.Add(ShortNameHeight);
        hash.Add(LayoutMoveDuration);
        hash.Add(HoverLift); hash.Add(HoverDuration); hash.Add(PreviewOffset); hash.Add(PreviewScale); hash.Add(PreviewHintGap);
        return hash.ToHashCode();
    }

    public ActionCardRack GetRack(PlayerCharacter player) => _racks.GetValueOrDefault(player);

    public void ClearHoverPreviews() {
        foreach (var rack in AllRacks)
            rack.ClearHoverPreview();
    }

    public void RefreshIntention(EnemyCharacter enemy, bool refreshText = false) {
        if (_battle == null || enemy == null || !GodotObject.IsInstanceValid(enemy) || enemy.IsPlayer)
            return;
        bool visible = enemy.IntentionCardsVisible && enemy.State == Character.CharacterState.Normal;
        if (!_enemyRacks.TryGetValue(enemy, out var rack)) {
            if (!visible)
                return;
            rack = GetChildren().OfType<ActionCardRack>().FirstOrDefault(candidate =>
                candidate.Name.ToString().StartsWith("EnemyRack", StringComparison.Ordinal)
                && !_enemyRacks.Values.Contains(candidate));
            if (rack == null) {
                rack = GD.Load<PackedScene>("res://battle/UIScene/ActionCardRack.tscn").Instantiate<ActionCardRack>();
                AddChild(rack);
            }
            _enemyRacks[enemy] = rack;
            rack.CardPressed += _ => {
                foreach (var other in AllRacks)
                    if (other != rack)
                        other.ClearHoverPreview();
            };
            rack.OwnerName = enemy.CharacterName;
            rack.OwnerKey = enemy.Name.ToString();
            rack.HideEnergyCost = true;
            ApplyLayout(rack);
        }
        if (!visible)
            rack.ClearHoverPreview();
        rack.Visible = visible;
        if (!visible)
            return;
        var state = (Skill: enemy.CurrentIntentionSkill, Stunned: enemy.HasActiveStun());
        if (!_enemyCards.TryGetValue(enemy, out var previous) || previous != state) {
            _enemyCards[enemy] = state;
            rack.SetCards(state.Stunned
                ? [new ActionCardRack.CardEntry(null, "眩晕", "本次行动无法行动。")]
                : state.Skill != null ? [new ActionCardRack.CardEntry(state.Skill)] : []);
        }
        else if (refreshText)
            rack.RefreshCardText();
    }

    public void PulseIntention(EnemyCharacter enemy) {
        if (_enemyRacks.TryGetValue(enemy, out var rack))
            rack.Pulse(0);
    }

    private void Refresh(PlayerCharacter player) {
        if (!_racks.TryGetValue(player, out var rack)) {
            var available = GetChildren().OfType<ActionCardRack>().FirstOrDefault(candidate =>
                !candidate.Name.ToString().StartsWith("EnemyRack", StringComparison.Ordinal)
                && !AllRacks.Contains(candidate));
            rack = available ?? GD.Load<PackedScene>("res://battle/UIScene/ActionCardRack.tscn").Instantiate<ActionCardRack>();
            if (available == null)
                AddChild(rack);
            _racks[player] = rack;
            rack.CardPressed += _ => {
                foreach (var other in AllRacks)
                    if (other != rack)
                        other.ClearHoverPreview();
            };
        }
        rack.OwnerName = player.CharacterName;
        rack.OwnerKey = player.CharacterKey;
        ApplyLayout(rack);
        rack.SetCards(_battle.GetInstalledActionCards(player).Select(entry => new ActionCardRack.CardEntry(entry.Skill)).ToArray());
        rack.Visible = rack.CardCount > 0;
    }

    private void Pulse(PlayerCharacter player, int index) => GetRack(player)?.Pulse(index);

    public void ShowReplacementPreview(Skill incoming) {
        _replacementPreviewSkill = incoming;
        RefreshReplacementPreview();
    }

    private void RefreshReplacementPreview() {
        foreach (var pair in _racks) {
            bool replacing = _replacementPreviewSkill?.IsActionCard == true
                && _replacementPreviewSkill.OwnerCharater == pair.Key
                && pair.Key.State != Character.CharacterState.Dying
                && _battle.GetInstalledActionCardCount(pair.Key) >= _battle.EffectiveActionCardSlotsPerCharacter;
            pair.Value.SetUninstallPreview(replacing);
        }
    }

    private void PlayUninstall(PlayerCharacter player, Skill skill) {
        var rack = GetRack(player);
        SkillCard card = rack?.GetInstalledCardVisual(skill);
        if (card != null)
            _battle.CharacterControl?.PlayActionCardDiscardAnimation(card, skill);
        rack?.SetUninstallPreview(false);
    }

    private void PlayExhaust(PlayerCharacter player, Skill skill) {
        var rack = GetRack(player);
        SkillCard card = rack?.GetInstalledCardVisual(skill);
        if (card != null)
            _battle.CharacterControl?.PlayActionCardExhaustAnimation(card, skill);
        rack?.ClearHoverPreview();
    }

    public async Task<int> SelectReplacementAsync(PlayerCharacter player, Skill incoming, CancellationToken token) {
        foreach (var rack in AllRacks)
            rack.ClearHoverPreview();
        Refresh(player);
        return await _racks[player].SelectReplacementAsync(incoming, token);
    }

    public override void _ExitTree() {
        if (_battle != null && GodotObject.IsInstanceValid(_battle)) {
            _battle.ActionCardsChanged -= Refresh;
            _battle.ActionCardTriggered -= Pulse;
            _battle.ActionCardUninstalled -= PlayUninstall;
            _battle.ActionCardExhausted -= PlayExhaust;
        }
    }
}
