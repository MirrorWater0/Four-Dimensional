using System;
using System.Threading.Tasks;
using Godot;

public partial class ReadyButton : Button
{
    public Map ThisMap => field ??= GetNode("/root/Map") as Map;
    PackedScene _readyScene =
        GD.Load("res://battle/UIScene/BattleReady/battle_ready.tscn") as PackedScene;
    private BattleReady ThisBattleReady;
    [Export]
    public CanvasLayer Layer;
    private Color _originalColor;
    ColorRect ChangeEffect => field ??= GetNode("/root/Map/UI/ChangeEffect") as ColorRect;


    public override async void _Ready()
    {
        ButtonDown += Click;
        MouseEntered += mouse_entered;
        MouseExited += mouse_right_entered;
        _originalColor = (Color)((ShaderMaterial)Material).GetShaderParameter("color");
        ChangeEffect.Visible = false;
    }

    public void Click()
    {
        if (Layer.GetChildCount() == 0)
        {
            ThisBattleReady = _readyScene.Instantiate() as BattleReady;
            Layer.AddChild(ThisBattleReady);
            ThisBattleReady.StartAnimation();
            ChangeEffect.Visible = true;
            Tween tween = CreateTween();
            tween
                .TweenMethod(
                    Callable.From<float>(value =>
                        ((ShaderMaterial)ChangeEffect.Material).SetShaderParameter(
                            "progress",
                            value
                        )
                    ),
                    1f,
                    0.1f,
                    0.4f
                )
                .SetEase(Tween.EaseType.Out);
            tween
                .Chain()
                .TweenCallback(
                    Callable.From(() =>
                    {
                        ChangeEffect.Visible = false;
                    })
                );
        }
        else
        {
            Disabled = true;
            Tween tween = CreateTween();
            tween.TweenProperty(ThisBattleReady, "modulate", new Color(1, 1, 1, 0), 0.3f);
            ThisBattleReady.ComfirmTactics();
            tween
                .Chain()
                .TweenCallback(
                    Callable.From(() =>
                    {
                        Disabled = false;
                        ThisBattleReady.QueueFree();
                    })
                );
        }
    }

    public void mouse_entered()
    {
        ((ShaderMaterial)Material).SetShaderParameter("color", _originalColor);
        TweenShader("dist2", 1f);
        ((ShaderMaterial)Material).SetShaderParameter("color", new Color(1, 1, 1, 1));
        TweenShader("dist1", 1f);
        TweenShader("outer_ring_dist", 0.43f);
        TweenShader("triangle_dist", 0.45f);
    }

    public void mouse_right_entered()
    {
        ((ShaderMaterial)Material).SetShaderParameter("color", _originalColor);
        TweenShader("dist2", 0.5f);

        ((ShaderMaterial)Material).SetShaderParameter("color", _originalColor);
        TweenShader("dist1", 0.7f);

        TweenShader("outer_ring_dist", 0.27f);
        TweenShader("triangle_dist", 0.28f);
    }

    public void TweenShader(string var, float val)
    {
        CreateTween()
            .TweenMethod(
                Callable.From<float>(value =>
                    ((ShaderMaterial)Material).SetShaderParameter(var, value)
                ),
                ((ShaderMaterial)Material).GetShaderParameter(var),
                val,
                0.2f
            );
    }
}
