using System.Numerics;
using static Raylib_cs.Raylib;

namespace Physics;

public class PointMass
{
    public Vector2 Pos { get; set; }
    public Vector2 PrevPos { get; set; }
    public Vector2 Acc { get; set; }
    public Vector2 Vel
    {
        get { return Vector2.Subtract(Pos, PrevPos); }
        set { PrevPos = Vector2.Subtract(Pos, value); }
    }
    public float Mass { get; }
    public float Radius { get; init; }

    public PointMass(float x, float y, float mass)
    {
        Pos = new(x, y);
        PrevPos = Pos;
        Acc = new();
        Mass = mass;
        Radius = mass * 5f;
    }

    public void Update()
    {
        if (Sim.Loop.GravityEnabled)
        {
            Acc += Sim.Loop.Gravity;
        }
        Vector2 vel = Vel;
        PrevPos = Pos;
        Pos += vel + Acc * Sim.Loop.TimeStep * Sim.Loop.TimeStep;
        Acc = Vector2.Zero;
    }

    public void Draw()
    {
        DrawCircleLines((int) Pos.X, (int) Pos.Y, Radius, Raylib_cs.Color.WHITE);
    }

    public void ApplyForce(Vector2 f)
    {
        Acc += f / Mass;
    }
}