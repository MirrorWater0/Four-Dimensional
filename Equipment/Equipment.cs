using System;
using Godot;

public partial class Equipment
{
    public int Power { get; private set; }
    public int Survivability { get; private set; }
    public int Speed { get; private set; }
    public int MaxLife { get; private set; }
    public string Description { get; private set; }

    public virtual void SpecialEffect() { }
}
