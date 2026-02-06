using Godot;
using System;
using System.Threading.Tasks;

public partial class SiteButton : Button
{
    public ColorRect Mask => field??= GetNode("/root/Map/UI/ColorRect") as ColorRect;
    public ColorRect Appearance => field??= GetNode("Appearance") as ColorRect;
    private ShaderMaterial mat;
    public override void _Ready()
    {
        mat = Appearance.Material.Duplicate() as ShaderMaterial;
        mat.ResourceLocalToScene = true;
        Appearance.Material = mat;
        ButtonDown += GotoSite;
        base._Ready();
    }

    public async void GotoSite()
    {
        GD.Print("Goto Site");
        CreateTween().TweenProperty(Mask, "color", new Color(0, 0, 0, 1), 0.3f);
        await Task.Delay(300);
        GetTree().ChangeSceneToFile("res://battle/Battle.tscn");
    }

    public void TweenShader(string var,float val)
    {
        Tween tween = CreateTween();
        tween.TweenMethod()
    }
}
