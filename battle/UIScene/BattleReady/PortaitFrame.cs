using Godot;
using System;

public partial class PortaitFrame : Control
{
    public int PortaitIndex;
    public Button PortaitButton => field??= GetNode("Button") as Button;
    public TextureRect PortaitRect => field??= GetNode("TextureRect") as TextureRect;
    public AnimationPlayer Animation => field??= GetNode("AnimationPlayer") as AnimationPlayer;
}
