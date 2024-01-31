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
        PointA = a;
        PointB = b;
        SpringConstant = stiffness;
        RestLength = Vector2.Distance(PointA.Pos, PointB.Pos);
        DampingCoeff = damping;
    }

    public SpringConstraint(in SpringConstraint c)
    {
        PointA = c.PointA;
        PointB = c.PointB;
        SpringConstant = c.SpringConstant;
        RestLength = c.RestLength;
        DampingCoeff = c.DampingCoeff;
    }

    public override void Update()
    {
        Vector2 AtoB = PointB.Pos - PointA.Pos;
        Vector2 BrelVel = PointB.Vel - PointA.Vel;
        float length = AtoB.Length();
        AtoB /= length;
        float diff = length - RestLength;
        float force = SpringConstant * diff;
        // damping
        force += DampingCoeff * Vector2.Dot(AtoB, BrelVel);
        Vector2 forceVec = force * AtoB;
        if (!PointA._pinned)
        {
            PointA.ApplyForce(forceVec);
        }
        if (!PointB._pinned)
        {
            PointB.ApplyForce(-forceVec);
        }
    }

    public override void Draw()
    {
        DrawLine((int) PointA.Pos.X, (int) PointA.Pos.Y, (int) PointB.Pos.X, (int) PointB.Pos.Y, Color.WHITE);
    }
}