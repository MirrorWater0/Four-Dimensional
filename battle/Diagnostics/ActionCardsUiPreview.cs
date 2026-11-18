using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>Interactive UI study over the supplied battle screenshot; all installed cards are sample data.</summary>
public partial class ActionCardsUiPreview : Control {
    private ActionCardRack[] _racks;
    private int _count = 3;
    private int _pulseIndex;

    public override async void _Ready() {
        GD.Print("[ActionCardsUiPreview] ready");
        _racks = [GetNode<ActionCardRack>("Echo"), GetNode<ActionCardRack>("Mariya"), GetNode<ActionCardRack>("Kasiya")];
        foreach (var selected in _racks)
            selected.CardPressed += _ => {
                foreach (var other in _racks)
                    if (other != selected)
                        other.ClearPinnedPreview();
            };
        foreach (int count in new[] { 2, 3, 4, 5 }) {
            int capturedCount = count;
            GetNode<Button>($"Controls/Count{count}").Pressed += () => Populate(capturedCount);
        }
        GetNode<Button>("Controls/Trigger").Pressed += () => {
            foreach (var rack in _racks)
                rack.Pulse(_pulseIndex % _count);
            _pulseIndex++;
        };
        GetNode<Button>("Controls/Toggle").Pressed += () => {
            bool show = !_racks[0].Visible;
            foreach (var rack in _racks)
                rack.Visible = show;
        };
        Populate(3);
        if (OS.GetCmdlineUserArgs().Contains("--action-cards-capture")) {
            try {
                await CaptureAsync();
                GD.Print("[ActionCardsUiPreview] PASS: 2/3/4/5-card layout, hover, pin/unpin, clear, hide, pulse, and resize.");
                GetTree().Quit(0);
            }
            catch (Exception ex) {
                GD.PrintErr($"[ActionCardsUiPreview] FAIL: {ex}");
                GetTree().Quit(1);
            }
        }
    }

    private void Populate(int count) {
        _count = count;
        string[][] names = [
            ["连击", "淬刃", "回响", "蓄势", "追击"],
            ["守护", "祈愿", "庇护", "复苏", "共鸣"],
            ["破甲", "战意", "重击", "蓄力", "追猎"]
        ];
        string[][] pictures = [
            ["Echo/BasicAttack", "Echo/EchoBasicSpecial", "Echo/BasicDefense", "Echo/BasicGuard", "Echo/BasicAttack"],
            ["Mariya/BasicGuard", "Mariya/RebirthPrayer", "Mariya/SanctuaryForm", "Mariya/FinalGuard", "Mariya/CrystalGuard"],
            ["Kasiya/VulnerabilityStrike", "Kasiya/WarGodWill", "Kasiya/Smite", "Kasiya/Charge", "Kasiya/ReadyStance"]
        ];
        string[][] descriptions = [
            ["每轮结束时，额外进行一次普通攻击。", "普通攻击命中时，给目标附加流血。", "每轮第一次触发其他行动牌时，重复其效果。", "每轮开始时积累蓄能，为后续攻击提供资源。", "攻击流血目标后，发动一次追击。"],
            ["每轮开始时，为队伍中生命最低的角色提供护盾。", "每轮结束时，恢复生命最低的友方角色。", "本轮首次受到伤害时，降低此次伤害。", "每轮结束时，将剩余护盾的一部分转化为治疗。", "获得护盾时，为另一个友方角色提供少量护盾。"],
            ["普通攻击命中时，为目标附加易伤。", "每轮结束时积累战意，提高后续攻击强度。", "每轮结束时，对选定目标进行一次重击。", "本轮未进行攻击时，提高下轮攻击的伤害。", "击败敌人时，获得1点力量。"]
        ];
        for (int role = 0; role < _racks.Length; role++) {
            var cards = Enumerable.Range(0, count).Select(i => new ActionCardRack.CardEntry(
                Skill.CreatePlaceholder(Skill.SkillTypes.Ability), names[role][i], descriptions[role][i], GD.Load<Texture2D>($"res://asset/CardPicture/{pictures[role][i]}.png"))).ToArray();
            _racks[role].SetCards(cards);
        }
        foreach (int option in new[] { 2, 3, 4, 5 })
            GetNode<Button>($"Controls/Count{option}").ButtonPressed = option == count;
    }

    private async Task Frames(int count = 12) {
        for (int i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task Save(string file) {
        await Frames();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        string directory = ProjectSettings.GlobalizePath("res://tmp/action_cards_ui");
        Directory.CreateDirectory(directory);
        Error error = GetViewport().GetTexture().GetImage().SavePng(Path.Combine(directory, file));
        if (error != Error.Ok)
            throw new InvalidOperationException($"Screenshot failed: {error}");
    }

    private async Task CaptureAsync() {
        MovePointer(new Vector2(1800, 150));
        await CheckReflowAsync();
        Populate(3);
        await Save("01_three_cards.png");
        MovePointer(new Vector2(162, 678));
        await Frames(20);
        if (_racks[0].HoveredIndex != 0)
            throw new InvalidOperationException($"First overlapping card cannot be hovered: index={_racks[0].HoveredIndex}, localMouse={_racks[0].GetLocalMousePosition()}.");
        await Save("02_hover.png");
        Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = new Vector2(162, 678) });
        Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = new Vector2(162, 678) });
        await Frames(2);
        MovePointer(new Vector2(1800, 150));
        await Frames();
        if (!_racks[0].GetNode<Control>("DetailLayer/Detail").Visible)
            throw new InvalidOperationException("Click did not pin the card preview.");
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, Pressed = false });
        await Frames();
        if (_racks[0].GetNode<Control>("DetailLayer/Detail").Visible)
            throw new InvalidOperationException("Escape did not clear the pinned preview.");
        MovePointer(new Vector2(210, 678));
        await Frames(20);
        if (_racks[0].HoveredIndex != 1)
            throw new InvalidOperationException("Middle overlapping card cannot be hovered.");
        MovePointer(new Vector2(1800, 150));
        await Frames();
        if (_racks.Any(rack => rack.HoveredIndex != -1))
            throw new InvalidOperationException("Hover did not clear.");
        Populate(4);
        await Save("03_four_cards.png");
        Populate(5);
        await Save("04_five_cards.png");
        foreach (var rack in _racks) {
            for (int i = 0; i < rack.CardCount; i++) {
                var card = rack.GetNode<Control>($"Cards/Card{i}");
                if (card.Position.X < -0.1f || card.Position.X + card.Size.X > rack.Size.X + 0.1f)
                    throw new InvalidOperationException("Card exceeds its owner rack.");
            }
            rack.Pulse(0);
            rack.Visible = false;
        }
        await Frames();
        foreach (var rack in _racks)
            rack.Visible = true;
        Populate(2);
        await Save("05_two_cards.png");
        _racks[0].SetCards([]);
        if (_racks[0].CardCount != 0)
            throw new InvalidOperationException("Empty card rack did not clear.");
        Populate(3);
        GetTree().Root.Size = new Vector2I(1280, 720);
        await Save("06_1280x720.png");
    }

    private async Task CheckReflowAsync() {
        var rack = _racks[0];
        var first = new ActionCardRack.CardEntry(Skill.GetSkill(SkillID.BasicAttack));
        var second = new ActionCardRack.CardEntry(Skill.GetSkill(SkillID.BasicDefense));
        var third = new ActionCardRack.CardEntry(Skill.GetSkill(SkillID.BasicGuard));
        var slot0 = rack.GetNode<Control>("Cards/Card0");
        var slot2 = rack.GetNode<Control>("Cards/Card2");
        rack.SetCards([first]);
        await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
        Vector2 before = slot0.Position;
        rack.SetCards([first, second]);
        if (!slot0.Position.IsEqualApprox(before))
            throw new InvalidOperationException("Adding a card teleported the existing card.");
        await ToSignal(GetTree().CreateTimer(0.07), SceneTreeTimer.SignalName.Timeout);
        float destination = (rack.Size.X - 240 * rack.CardScale - rack.CardStep) / 2;
        if (!(slot0.Position.X < before.X && slot0.Position.X > destination))
            throw new InvalidOperationException("Existing card did not travel between its old and new positions.");
        before = slot0.Position;
        rack.SetCards([first, second, third]);
        if (!slot0.Position.IsEqualApprox(before))
            throw new InvalidOperationException("A second insertion snapped an in-progress reflow.");
        await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
        before = slot2.Position;
        rack.SetCards([third]);
        if (!slot0.Position.IsEqualApprox(before))
            throw new InvalidOperationException("Removing preceding cards lost the survivor's position.");
        await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
        if (!slot0.Position.IsEqualApprox(new Vector2((rack.Size.X - 240 * rack.CardScale) / 2, 0)))
            throw new InvalidOperationException("Survivor did not settle in the new centered slot.");
        rack.BeginInstallation(0);
        rack.RevealInstallation(0);
        var card = slot0.GetNode<SkillCard>("Card");
        Vector2 arrivalScale = card.Scale;
        rack.SetCards([third, second]);
        if (!card.Scale.IsEqualApprox(arrivalScale))
            throw new InvalidOperationException("Reflow interrupted an installation's tiny arrival pose.");
        await ToSignal(GetTree().CreateTimer(0.35), SceneTreeTimer.SignalName.Timeout);
        if (!card.Scale.IsEqualApprox(Vector2.One * rack.CardScale) || !card.Position.IsEqualApprox(Vector2.Zero))
            throw new InvalidOperationException("Installation did not finish while other cards made room.");
        GD.Print("[ActionCardsUiPreview] reflow add/interruption/removal/installation PASS");
    }

    private void MovePointer(Vector2 position) {
        Input.ParseInputEvent(new InputEventMouseMotion { Position = position, GlobalPosition = position });
    }
}
