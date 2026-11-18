using Godot;

public partial class ActionCardSlotsIcon : TextureRect {
    private PlayerCharacter _owner;
    private Label CountLabel => field ??= GetNode<Label>("Label");
    private int _shownCapacity = -1;
    private int _shownInstalled = -1;

    public override void _Ready() {
        _owner = GetParent()?.GetParent() as PlayerCharacter;
        Visible = _owner != null && !_owner.IsSummon;
        SetProcess(Visible);
        if (Visible)
            RefreshCount();
    }

    public override void _Process(double delta) {
        RefreshCount();
    }

    private void RefreshCount() {
        if (_owner == null || !GodotObject.IsInstanceValid(_owner))
            return;

        Battle battle = _owner.BattleNode;
        bool hasBattle = battle != null && GodotObject.IsInstanceValid(battle);
        int capacity = hasBattle ? battle.EffectiveActionCardSlotsPerCharacter : Battle.InitialActionCardSlotsPerCharacter;
        int installed = hasBattle ? battle.GetInstalledActionCardCount(_owner) : 0;
        if (_shownCapacity == capacity && _shownInstalled == installed)
            return;

        _shownCapacity = capacity;
        _shownInstalled = installed;
        CountLabel.Text = capacity.ToString();
        TooltipText = I18n.Format(
            "battle.action_card_slots.tooltip",
            "行动卡槽\n已安装：{installed}/{capacity}\n槽位上限：{maximum}\n满槽时打出行动牌可选择替换。",
            ("installed", installed),
            ("capacity", capacity),
            ("maximum", Battle.MaxActionCardSlotsPerCharacter)
        );
    }
}
