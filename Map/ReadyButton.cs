using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ReadyButton : Button
{
    public Map ThisMap => field ??= GetNode("/root/Map") as Map;
    PackedScene _readyScene =
        GD.Load("res://battle/UIScene/BattleReady/BattleReady.tscn") as PackedScene;
    private BattleReady ThisBattleReady;

    [Export]
    public CanvasLayer Layer;
    private Color _originalColor;
    [Export]
    ColorRect ChangeEffect;

    public override void _Ready()
    {
        ButtonDown += Click;
        MouseEntered += mouse_entered;
        MouseExited += mouse_right_entered;
        _originalColor = (Color)((ShaderMaterial)Material).GetShaderParameter("color");
        ChangeEffect.Visible = false;
    }

    public async void Click()
    {
        if (!HasActiveBattleReady() && Layer != null)
            ThisBattleReady = Layer.GetChildren().OfType<BattleReady>().FirstOrDefault();

        if (!HasActiveBattleReady())
        {
            if (Layer == null || _readyScene == null)
            {
                GD.PushError("ReadyButton: Layer 或 BattleReady 场景未设置，无法打开备战界面。");
                return;
            }

            ThisBattleReady = _readyScene.Instantiate() as BattleReady;
            if (ThisBattleReady == null)
            {
                GD.PushError("ReadyButton: BattleReady 场景实例化失败。");
                return;
            }

            Layer.AddChild(ThisBattleReady);
            ThisBattleReady.StartAnimation();
            // ChangeEffect.Visible = true;
            // Tween tween = CreateTween();
            // tween
            //     .TweenMethod(
            //         Callable.From<float>(value =>
            //             ((ShaderMaterial)ChangeEffect.Material).SetShaderParameter(
            //                 "progress",
            //                 value
            //             )
            //         ),
            //         1f,
            //         0.1f,
            //         0.4f
            //     )
            //     .SetEase(Tween.EaseType.Out);
            // tween
            //     .Chain()
            //     .TweenCallback(
            //         Callable.From(() =>
            //         {
            //             ChangeEffect.Visible = false;
            //         })
            //     );
        }
        else
        {
            Disabled = true;
            try
            {
                ThisBattleReady.ComfirmTactics();
                await ThisBattleReady.PlayCloseAnimationAsync();
                if (GodotObject.IsInstanceValid(ThisBattleReady))
                    ThisBattleReady.QueueFree();
            }
            finally
            {
                Disabled = false;
                ThisBattleReady = null;
            }
        }
    }

    private bool HasActiveBattleReady()
    {
        return ThisBattleReady != null
            && GodotObject.IsInstanceValid(ThisBattleReady)
            && ThisBattleReady.IsInsideTree();
    }

    public void mouse_entered()
    {
        ((ShaderMaterial)Material).SetShaderParameter("color", _originalColor);
        GlobalFunction.TweenShader(this, "dist2", 1f, 0.2f);
        ((ShaderMaterial)Material).SetShaderParameter("color", new Color(1, 1, 1, 1));
        GlobalFunction.TweenShader(this, "dist1", 1f, 0.2f);
        GlobalFunction.TweenShader(this, "outer_ring_dist", 0.43f, 0.2f);
        GlobalFunction.TweenShader(this, "triangle_dist", 0.45f, 0.2f);
    }

    public void mouse_right_entered()
    {
        ((ShaderMaterial)Material).SetShaderParameter("color", _originalColor);
        GlobalFunction.TweenShader(this, "dist2", 0.5f, 0.2f);

        ((ShaderMaterial)Material).SetShaderParameter("color", _originalColor);
        GlobalFunction.TweenShader(this, "dist1", 0.7f, 0.2f);

        GlobalFunction.TweenShader(this, "outer_ring_dist", 0.27f, 0.2f);
        GlobalFunction.TweenShader(this, "triangle_dist", 0.28f, 0.2f);
    }
}
