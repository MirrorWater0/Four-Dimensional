using Godot;
using System;

public partial class PortaitFrame : Control
{
    public Charater Charater;
    public Button PortaitButton => field??= GetNode("Button") as Button;
    public TextureRect PortaitRect => field??= GetNode("TextureRect") as TextureRect;
}
