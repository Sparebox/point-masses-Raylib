using System.Numerics;
using Entities;
using Raylib_cs;
using Sim;
using Utils;
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
        if (!PointA.Pinned)
        {
            PointA.ApplyForce(forceVec);
        }
        if (!PointB.Pinned)
        { 
            PointB.ApplyForce(-forceVec);
        }
    }

    public override void Draw()
    {
        DrawLineV(
            UnitConv.MetersToPixels(PointA.Pos),
            UnitConv.MetersToPixels(PointB.Pos),
            Color.White
        );
    }
}