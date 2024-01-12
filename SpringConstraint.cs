using System.Numerics;
using static Raylib_cs.Raylib;

namespace Physics;

public class SpringConstraint : Constraint
{
    public readonly float SpringConstant;
    public readonly float RestLength;

    public SpringConstraint(PointMass a, PointMass b, float springConstant)
    {
        A = a;
        B = b;
        SpringConstant = springConstant;
        RestLength = Vector2.Distance(A.Pos, B.Pos);
    }

    public override void Update()
    {
        Vector2 AtoB = B.Pos - A.Pos;
        float length = AtoB.Length();
        float diff = length - RestLength;
        Vector2 force = SpringConstant * diff * Vector2.Normalize(AtoB);
        A.ApplyForce(force);
        B.ApplyForce(-force);
    }

    public override void Draw()
    {
        DrawLine((int) A.Pos.X, (int) A.Pos.Y, (int) B.Pos.X, (int) B.Pos.Y, Raylib_cs.Color.WHITE);
    }
}