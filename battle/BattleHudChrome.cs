using Godot;

// Battle-only presentation. Existing controls retain their input and gameplay wiring.
public partial class BattleHudChrome : Control {
    private Battle _battle;
    private Button _endTurn;
    private ShaderMaterial _endTurnGlass;
    private ShaderMaterial _energyDial;
    private float _glassHover;
    private float _glassPress;
    private int _lastPlayerEnergy = -1;
    private float _energyPulse;
    private float _energyActivity = 1f;

    public override void _Ready() {
        _battle = GetParent()?.GetParent() as Battle;
        var layer = GetParent();
        _energyDial = layer.GetNode<ColorRect>("EnergyBG").Material as ShaderMaterial;
        _endTurn = layer.GetNode<Button>("BattleActionButtons/EndTurnButton");
        _endTurnGlass = _endTurn.GetNode<ColorRect>("CrystalSurface").Material as ShaderMaterial;
        _endTurn.GetNode<Label>("EndTurnKeyHint/Label").Text = MobilePlatform.IsMobile ? "" : "E";
    }

    public override void _Process(double delta) {
        if (GodotObject.IsInstanceValid(_energyDial) && GodotObject.IsInstanceValid(_battle)) {
            int energy = _battle.PlayerEnergy;
            if (_lastPlayerEnergy >= 0 && energy != _lastPlayerEnergy) {
                _energyPulse = 1f;
            }
            _lastPlayerEnergy = energy;
            _energyPulse = Mathf.Max(0f, _energyPulse - (float)delta * 1.6f);
            _energyActivity = Mathf.Lerp(_energyActivity, energy > 0 ? 1f : 0f, 1f - Mathf.Exp(-5f * (float)delta));
            _energyDial.SetShaderParameter("energy_activity", _energyActivity);
            _energyDial.SetShaderParameter("change_pulse", _energyPulse);
        }
        if (GodotObject.IsInstanceValid(_endTurn) && _endTurnGlass != null) {
            float blend = 1f - Mathf.Exp(-12f * (float)delta);
            _glassHover = Mathf.Lerp(_glassHover, _endTurn.IsHovered() && !_endTurn.Disabled ? 1f : 0f, blend);
            _glassPress = Mathf.Lerp(_glassPress, _endTurn.ButtonPressed ? 1f : 0f, blend);
            _endTurnGlass.SetShaderParameter("panel_size", _endTurn.Size);
            _endTurnGlass.SetShaderParameter("hover_amount", _glassHover);
            _endTurnGlass.SetShaderParameter("press_amount", _glassPress);
            _endTurnGlass.SetShaderParameter("disabled_amount", _endTurn.Disabled ? 1f : 0f);
        }
    }

}
