using System.Numerics;
using Collision;
using static Raylib_cs.Raylib;

namespace Physics;

public class PointMass
{
    public const float RestitutionCoeff = 0.95f;
    public const float FrictionCoeff = 0.1f;
    public const float BounceThreshold = 1e-4f;

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
        DrawCircleLines((int) Pos.X, (int) Pos.Y, Radius, Raylib_cs.Color.WHITE);
    }

    public void ApplyForce(in Vector2 f)
    {
        Acc += f / Mass;
    }

    public void SolveCollisions(in List<LineCollider> lineColliders)
    {
        foreach (LineCollider c in lineColliders)
        {
            Vector2 closestPoint = c.ClosestPointOnLine(Pos);
            Vector2 closestToPoint = Pos - closestPoint;
            float distToCollider = closestToPoint.Length();
            if (distToCollider <= Radius)
            {
                // Collision
                Vector2 closestToPointNorm = Vector2.Normalize(closestToPoint);
                Vector2 reflectedVel = Vel - closestToPointNorm * 2f * Vector2.Dot(Vel, closestToPointNorm);
                Pos += (Radius - distToCollider) * closestToPointNorm;
                if (Vel.Length() < BounceThreshold)
                {
                    continue;
                }
                Vel = RestitutionCoeff * reflectedVel;
                ApplyKineticFriction(closestToPointNorm);
            }
        }
    }

    private void ApplyKineticFriction(in Vector2 normal)
    {
        Vector2 dirA = new(normal.Y, normal.X);
        Vector2 dirB = new(-normal.Y, normal.X);
        Vector2 dir = Vector2.Dot(Vel, dirA) < 0f ? dirA : dirB;
        Vector2 frictionF = dir * FrictionCoeff * Mass * Math.Abs(Vector2.Dot(Acc, normal));
        ApplyForce(frictionF);
    }
}