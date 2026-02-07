using System;
using System.Threading.Tasks;
using Godot;

public partial class SiteButton : Button
{
    [Export]
    public PackedScene Where;
    public ColorRect Mask => field ??= GetNode("/root/Map/UI/ColorRect") as ColorRect;
    public ColorRect Appearance => field ??= GetNode("Appearance") as ColorRect;
    public CanvasLayer SiteUILayer => field ??= GetNode("/root/Map/SiteUI") as CanvasLayer;
    private ShaderMaterial mat;

    public override void _Ready()
    {
        mat = Appearance.Material.Duplicate() as ShaderMaterial;
        mat.ResourceLocalToScene = true;
        Appearance.Material = mat;
        ButtonDown += GotoSite;
        MouseEntered += mouse_entered;
        MouseExited += mouse_exited;
        mat.SetShaderParameter("inner_ring_radius", 0.028f);
        mat.SetShaderParameter("broken_ring_radius", 0.14f);
        base._Ready();
    }

    public async void GotoSite()
    {
        // CreateTween().TweenProperty(Mask, "color", new Color(0, 0, 0, 1), 0.3f);

        // await Task.Delay(300);
        var node = Where.Instantiate() as Node;
        SiteUILayer.AddChild(node);
    }

    public void mouse_entered()
    {
        TweenShader("inner_ring_radius", 0.08f);
        TweenShader("broken_ring_radius", 0.18f);
    }

    public void mouse_exited()
    {
        TweenShader("inner_ring_radius", 0.028f);
        TweenShader("broken_ring_radius", 0.14f);
    }

    public void TweenShader(string var, float val)
    {
        Tween tween = CreateTween();
        tween.TweenMethod(
            Callable.From<float>(value => mat.SetShaderParameter(var, value)),
            mat.GetShaderParameter(var),
            val,
            0.3f
        );
    }
}
