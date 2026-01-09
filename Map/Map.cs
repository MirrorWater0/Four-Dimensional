using Godot;
using System;
using Microsoft.Win32.SafeHandles;

public partial class Map : Control
{
    public Button DragButton => field??= GetNode("DragButton") as Button;
    public TextureRect GameMap => field??= GetNode("GameMap") as TextureRect;
    public Camera2D Camera => field??= GetNode("Camera") as Camera2D;

    private Vector2 _targetPos;
    private bool _isDrag;
    Vector2 _velocity = Vector2.Zero;
    private double _time;
    
    [Export] public Vector2 MinBoundary = Vector2.Zero;
    [Export] public Vector2 MaxBoundary = new Vector2(3235, 1970);
    public override void _Process(double delta)
    {

        if (_isDrag)
        {
            _velocity *= 0.9f;
            _velocity +=3000*(_targetPos - Camera.Position)*(float)delta;
            Camera.GlobalPosition += _velocity * (float)delta;
        }
        
    }

    public override void _Input(InputEvent @event)
    {

        if (@event is InputEventMouseMotion & _isDrag)
        {
            var eventmove = @event as InputEventMouseMotion;
            _targetPos -= eventmove.Relative / Camera.Zoom;
            _targetPos.X = Math.Min(2271, _targetPos.X);
            _targetPos.X = Math.Max(966, _targetPos.X);
            _targetPos.Y = Math.Min(1423, _targetPos.Y);
            _targetPos.Y = Math.Max(543, _targetPos.Y);
        } 
        
        if (Input.IsActionPressed("Wheelup"))
        {
            if (1.1 * Camera.Zoom.X < 2) Camera.Zoom = 1.1f * Camera.Zoom;
        }

        if (Input.IsActionPressed("Wheeldown"))
        {
            if(Camera.Zoom.X > 0.8) Camera.Zoom = 0.9f*Camera.Zoom;
        }
    }

    public override void _Ready()
    {
        _targetPos = Camera.Position;
        DragButton.ButtonDown += () => {_isDrag = true;_velocity = Vector2.Zero;_targetPos = Camera.GlobalPosition;};
        DragButton.ButtonUp += () => {_isDrag = false ;};
     }

     public void CloseWindow()
     {
         GetTree().Quit();
     }
    
}
