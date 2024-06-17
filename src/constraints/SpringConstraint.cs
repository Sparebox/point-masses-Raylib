using System.Numerics;
using Raylib_cs;
using Utils;
using static Raylib_cs.Raylib;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Physics;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class SpringConstraint : Constraint
{
    public const float DefaultDamping = 5e4f;
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
        Id = _idCounter++;
    }

    public SpringConstraint(in SpringConstraint c)
    {
        PointA = c.PointA;
        PointB = c.PointB;
        SpringConstant = c.SpringConstant;
        RestLength = c.RestLength;
        DampingCoeff = c.DampingCoeff;
        Id = _idCounter++;
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
        DrawLine(
            UnitConv.MetersToPixels(PointA.Pos.X),
            UnitConv.MetersToPixels(PointA.Pos.Y),
            UnitConv.MetersToPixels(PointB.Pos.X),
            UnitConv.MetersToPixels(PointB.Pos.Y),
            Color.White
        );
    }
}