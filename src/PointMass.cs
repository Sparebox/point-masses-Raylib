using System.Numerics;
using Collision;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Physics;

public class PointMass
{
    private static int _idCounter;

    public const float RestitutionCoeff = 0.9f;
    public const float FrictionCoeff = 0.001f;

    public readonly int _id;
    public Vector2 Pos { get; set; }
    public Vector2 PrevPos { get; set; }
    public Vector2 Acc { get; set; }
    public Vector2 Vel
    {
        get { return Pos - PrevPos; }
        set { PrevPos = Pos - value; }
    }
    public float Mass { get; }
    public float Radius { get; init; }

    public PointMass(float x, float y, float mass)
    {
        Pos = new(x, y);
        PrevPos = Pos;
        Acc = Vector2.Zero;
        Mass = mass;
        Radius = mass * 5f;
        _id = _idCounter++;
    }

    public void Update(in List<LineCollider> lineColliders)
    {
        if (Sim.Loop.GravityEnabled)
        {
            Acc += Sim.Loop.Gravity;
        }
        SolveCollisions(lineColliders);
        Vector2 vel = Vel;
        PrevPos = Pos;
        Pos += vel + Acc * Sim.Loop.TimeStep * Sim.Loop.TimeStep;
        Acc = Vector2.Zero;
    }

    public void Draw()
    {
        DrawCircleLines((int) Pos.X, (int) Pos.Y, Radius, Color.WHITE);
    }

    public void ApplyForce(in Vector2 f)
    {
        Acc += f / Mass;
    }

    public void SolveCollisions(in List<LineCollider> lineColliders)
    {
        foreach (LineCollider c in lineColliders)
        {
            Vector2 closestPoint = Tools.Geometry.ClosestPointOnLine(c.StartPos, c.EndPos, Pos);
            Vector2 closestToPoint = Pos - closestPoint;
            float distToCollider = closestToPoint.Length();
            if (distToCollider <= Radius)
            {
                // Collision
                Vector2 closestToPointNorm = Vector2.Normalize(closestToPoint);
                Vector2 reflectedVel = Tools.Geometry.ReflectVec(Vel, closestToPointNorm);
                // Correct penetration
                Pos += (Radius - distToCollider) * closestToPointNorm;
                Vel = RestitutionCoeff * reflectedVel;
                ApplyKineticFriction(closestToPointNorm);
            }
        }
    }

    private void ApplyKineticFriction(in Vector2 normal)
    {
        // Find direction perpendicular to the normal and opposite to the velocity
        Vector2 dir = Vel - Vector2.Dot(Vel, normal) * normal;
        if (dir.LengthSquared() == 0f)
        {
            return;
        }
        dir = -Vector2.Normalize(dir);
        Vector2 frictionF = dir * FrictionCoeff * Mass * Math.Abs(Vector2.Dot(Acc, normal));
        if (Sim.Loop.DrawForces)
        {
            DrawLine((int) Pos.X, (int) Pos.Y, (int) (Pos.X + frictionF.X), (int) (Pos.Y + frictionF.Y), Color.RED);
        }
        ApplyForce(frictionF);
    }
}