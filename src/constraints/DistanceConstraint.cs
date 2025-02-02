using System.Numerics;
using PointMasses.Entities;
using Raylib_cs;
using PointMasses.Sim;
using PointMasses.Utils;
using static Raylib_cs.Raylib;

namespace PointMasses.Physics;

public class DistanceConstraint : Constraint 
{
    public float Length { get; init; }
    public float Stiffness { get; init; }
    private readonly Context _ctx;

    public DistanceConstraint(in PointMass a, in PointMass b, float stiffness, Context ctx, float lengthMult = 1f, bool incrementId = true)
    {
        PointA = a;
        PointB = b;
        Length = lengthMult * Vector2.Distance(PointA._pos, PointB._pos);
        Stiffness = stiffness;
        _ctx = ctx;
        if (incrementId)
        {
            Id = _idCounter++;
        }
    }

    // Copy constructor
    public DistanceConstraint(in DistanceConstraint c)
    {
        PointA = c.PointA;
        PointB = c.PointB;
        Length = c.Length;
        Stiffness = c.Stiffness;
        _ctx = c._ctx;
        Id = _idCounter++;
    }

    public override void Update()
    {
        Vector2 AtoB = PointB._pos - PointA._pos;
        float length = AtoB.Length();
        float error = Length - length;
        if (Raymath.FloatEquals(error, 0f) == 1 || (PointA.Pinned && PointB.Pinned))
        {
            return;
        }
        Vector2 correctionVector = AtoB / length * error;
        Vector2 correctionA;
        Vector2 correctionB;
        float stiffnessCoeff = 1f - MathF.Pow(1f - Stiffness, 1f / _ctx._substeps);
        if (!PointA.Pinned)
        {
            correctionA = -PointA.InvMass / (PointA.InvMass + PointB.InvMass) * correctionVector;
            correctionA *= stiffnessCoeff;
            PointA._pos += correctionA;
        }
        if (!PointB.Pinned)
        {
            correctionB = PointB.InvMass / (PointA.InvMass + PointB.InvMass) * correctionVector;
            correctionB *= stiffnessCoeff;
            PointB._pos += correctionB;
        }
    }

    public override void Draw()
    {
        var pointAviewPos = UnitConv.MtoP(PointA._pos);
        var pointBviewPos = UnitConv.MtoP(PointB._pos);

        DrawLineV(
            pointAviewPos,
            pointBviewPos,
            Color.White
        );
    }
}