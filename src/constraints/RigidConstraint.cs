using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Physics;

public class RigidConstraint : Constraint 
{
    public readonly float Length;

    public RigidConstraint(in PointMass a, in PointMass b)
    {
        PointA = a;
        PointB = b;
        Length = Vector2.Distance(PointA.Pos, PointB.Pos);
    }

    public RigidConstraint(in RigidConstraint c)
    {
        PointA = c.PointA;
        PointB = c.PointB;
        Length = c.Length;
    }

    public override void Update()
    {
        Vector2 AtoB = PointB.Pos - PointA.Pos;
        float dist = AtoB.Length();
        float diff = Length - dist;
        float percentage = diff / dist / 2f;
        Vector2 offset = percentage * dist * Vector2.Normalize(AtoB);
        if (!PointA._pinned) {
            PointA.Pos -= offset;
        }
        if (!PointB._pinned)
        {
            PointB.Pos += offset;
        }
    }

    public override void Draw()
    {
        DrawLine((int) PointA.Pos.X, (int) PointA.Pos.Y, (int) PointB.Pos.X, (int) PointB.Pos.Y, Color.White);
    }
}