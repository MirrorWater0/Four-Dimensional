using Godot;
using System;

[Tool] // 加上这个标签，在编辑器里就能看到效果
public partial class EllipsePathGenerator : Path2D
{
    [Export] public Vector2 Radius = new Vector2(300, 150); // 椭圆的长短轴
    [Export] public bool UpdatePath = false; // 在面板点一下这个开关来刷新

    public override void _Process(double delta)
    {
        if (UpdatePath)
        {
            UpdatePath = false;
            GenerateEllipse();
        }
    }

    public void GenerateEllipse()
    {
        Curve2D curve = new Curve2D();
        
        // 贝塞尔模拟圆形的魔数 Kappa
        float kappa = 0.552284749831f;
        Vector2 handle = Radius * kappa;

        // 定义 4 个顶点及其控制柄
        // 右点
        curve.AddPoint(new Vector2(Radius.X, 0), new Vector2(0, -handle.Y), new Vector2(0, handle.Y));
        // 下点
        curve.AddPoint(new Vector2(0, Radius.Y), new Vector2(handle.X, 0), new Vector2(-handle.X, 0));
        // 左点
        curve.AddPoint(new Vector2(-Radius.X, 0), new Vector2(0, handle.Y), new Vector2(0, -handle.Y));
        // 上点
        curve.AddPoint(new Vector2(0, -Radius.Y), new Vector2(-handle.X, 0), new Vector2(handle.X, 0));

        // 闭合路径
        curve.AddPoint(new Vector2(Radius.X, 0));

        this.Curve = curve;
        GD.Print("椭圆路径已生成！");
    }
}