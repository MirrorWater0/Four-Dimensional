using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>Replayable preview of the same installation flight and landing used by the battle queue.</summary>
public partial class ActionCardInstallationPreview : Control {
    private SkillCard _card;
    private ActionCardRack _rack;
    private Button _replay;
    private bool _playing;
    public override async void _Ready() {
        _card = GetNode<SkillCard>("FlyingCard");
        _rack = GetNode<ActionCardRack>("Rack");
        _replay = GetNode<Button>("Replay");
        _rack.SetCards([new(Skill.GetSkill(SkillID.BasicAttack)), new(Skill.GetSkill(SkillID.BasicDefense))]);
        _replay.Pressed += async () => await Play();
        await Frames(8);
        if (!OS.GetCmdlineUserArgs().Contains("--installation-capture")) {
            await Play();
            return;
        }
        bool passed = false;
        double previousTimeScale = Engine.TimeScale;
        try {
            string path = ProjectSettings.GlobalizePath("res://tmp/action_installation");
            Directory.CreateDirectory(path);
            Engine.TimeScale = 0.35;
            var images = new List<Image>();
            var times = new List<ulong>();
            Task flight = Play();
            int frame = 0;
            do {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                images.Add(GetViewport().GetTexture().GetImage());
                times.Add(Time.GetTicksMsec());
                frame++;
                await Frames(2);
            } while (!flight.IsCompleted && frame < 180);
            await flight;
            await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
            var restingCard = _rack.GetNode<SkillCard>("Cards/Card0/Card");
            if (!restingCard.Scale.IsEqualApprox(Vector2.One * _rack.CardScale)
                || !restingCard.Position.IsEqualApprox(Vector2.Zero) || !Mathf.IsZeroApprox(restingCard.Rotation))
                throw new InvalidOperationException("installation failed to settle");
            GetViewport().GetTexture().GetImage().SavePng(Path.Combine(path, "resting.png"));
            for (int i = 0; i < images.Count; i++) {
                images[i].SavePng(Path.Combine(path, $"frame_{i:000}.png"));
                images[i].Dispose();
            }
            File.WriteAllLines(Path.Combine(path, "timing.txt"), times.Select(time => time.ToString()));
            passed = true;
        }
        catch (Exception ex) { GD.PrintErr($"[ActionCardInstallationPreview] {ex}"); }
        finally { Engine.TimeScale = previousTimeScale; }
        GD.Print($"[ActionCardInstallationPreview] {(passed ? "PASS" : "FAIL")}");
        GetTree().Quit(passed ? 0 : 1);
    }

    private async Task Play() {
        if (_playing) return;
        _playing = true;
        _replay.Disabled = true;
        _card.SetSkill(Skill.GetSkill(SkillID.BasicAttack));
        _card.ConfigurePilePreviewVisuals();
        _card.PivotOffset = Vector2.Zero;
        _card.Position = new Vector2(835, 260);
        _card.Scale = Vector2.One * 0.72f;
        _card.Rotation = 0;
        _card.Modulate = Colors.White;
        _card.SetCardVisualVisible(true);
        _card.SetHoverUiEnabled(false);
        _card.Visible = true;
        _rack.BeginInstallation(0);
        try {
            await _card.FlyInstallationWithImpactAsync(_rack.GetSlotGlobalCenter(0), new CardInstallationOptions {
                OnArrival = (rotation, scale) => _rack.RevealInstallation(0, arrivalGlobalRotation: rotation, arrivalScale: scale),
            });
            await ToSignal(GetTree().CreateTimer(0.18), SceneTreeTimer.SignalName.Timeout);
        }
        finally {
            _rack.RevealInstallation(0);
            _playing = false;
            _replay.Disabled = false;
        }
    }
    private async Task Frames(int count) {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
}
