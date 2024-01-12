using System.Numerics;
using static Raylib_cs.Raylib;

namespace Physics;

public class RigidConstraint : Constraint 
{
    public readonly float Length;

    public RigidConstraint(PointMass a, PointMass b)
    {
        A = a;
        B = b;
        Length = Vector2.Distance(A.Pos, B.Pos);
    }

    public override void Update()
    {
        Vector2 AtoB = B.Pos - A.Pos;
        float dist = AtoB.Length();
        float diff = Length - dist;
        float percentage = diff / dist / 2f;
        Vector2 offset = percentage * dist * Vector2.Normalize(AtoB);
        A.Pos -= offset;
        B.Pos += offset;
    }

    public override void Draw()
    {
        DrawLine((int) A.Pos.X, (int) A.Pos.Y, (int) B.Pos.X, (int) B.Pos.Y, Raylib_cs.Color.WHITE);
    }
}