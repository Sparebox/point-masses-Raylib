using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Physics;

public class SpringConstraint : Constraint
{
    public readonly float SpringConstant;
    public readonly float RestLength;
    public readonly float DampingCoeff;

    public SpringConstraint(in PointMass a, in PointMass b, float stiffness, float damping)
    {
        A = a;
        B = b;
        SpringConstant = stiffness;
        RestLength = Vector2.Distance(A.Pos, B.Pos);
        DampingCoeff = damping;
    }

    public override void Update()
    {
        Vector2 AtoB = B.Pos - A.Pos;
        Vector2 BrelVel = B.Vel - A.Vel;
        float length = AtoB.Length();
        AtoB /= length;
        float diff = length - RestLength;
        float force = SpringConstant * diff;
        // damping
        force += DampingCoeff * Vector2.Dot(AtoB, BrelVel);
        Vector2 forceVec = force * AtoB;
        if (!A._pinned)
        {
            A.ApplyForce(forceVec);
        }
        if (!B._pinned)
        {
            B.ApplyForce(-forceVec);
        }
    }

    public override void Draw()
    {
        DrawLine((int) A.Pos.X, (int) A.Pos.Y, (int) B.Pos.X, (int) B.Pos.Y, Color.WHITE);
    }
}