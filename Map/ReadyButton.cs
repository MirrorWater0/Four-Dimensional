using Godot;
using System;
using System.Threading.Tasks;

public partial class ReadyButton : Button
{
    public Map ThisMap => field ??= GetNode("/root/Map") as Map;
    PackedScene _readyScene = GD.Load("res://battle/UIScene/BattleReady/battle_ready.tscn") as PackedScene;
    private BattleReady ThisBattleReady;
    public CanvasLayer Layer => field ??= GetNode("/root/Map/BattleReadyLayer") as CanvasLayer;
    private Color _originalColor;
    public async override void _Ready()
    {
        ThisBattleReady = _readyScene.Instantiate() as BattleReady;
        ThisBattleReady.Modulate = new Color(1, 1, 1, 0);
        Layer.AddChild(ThisBattleReady);
        ButtonDown += Opean;
        MouseEntered += mouse_entered;
        MouseExited += mouse_right_entered;
        _originalColor = (Color)((ShaderMaterial)Material).GetShaderParameter("color");
    }

    public void Opean()
    {
        if (ThisBattleReady.Modulate.A == 0)
        {
            CreateTween().TweenProperty(ThisBattleReady, "modulate", new Color(1, 1, 1, 1), 0.3f);
            Layer.Layer = 1;
        }
        else
        {
            CreateTween().TweenProperty(ThisBattleReady, "modulate", new Color(1, 1, 1, 0), 0.3f);
            Layer.Layer = -1;
            ThisBattleReady.ComfirmTactics();
        }
        
    }

    public void mouse_entered()
    {
        ((ShaderMaterial)Material).SetShaderParameter("color", new Color(1, 1, 1, 1));
        CreateTween().TweenMethod(Callable.From<float>(value => ((ShaderMaterial)Material).SetShaderParameter("rotation_speed", value)),
            3f,0.5f,0.15f);
    }
    public void mouse_right_entered()
    {
        ((ShaderMaterial)Material).SetShaderParameter("color", _originalColor);
        CreateTween().TweenMethod(Callable.From<float>(value => ((ShaderMaterial)Material).SetShaderParameter("rotation_speed", value)),
            0.5f,3f,0.15f);
    }
}
