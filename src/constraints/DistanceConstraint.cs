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

    public DistanceConstraint(in PointMass a, in PointMass b, float stiffness, Context ctx, bool incrementId = true)
    {
        PointA = a;
        PointB = b;
        Length = Vector2.Distance(PointA.Pos, PointB.Pos);
        Stiffness = stiffness;
        _ctx = ctx;
        if (incrementId)
        {
            Id = _idCounter++;
        }
    }

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
        Vector2 AtoB = PointB.Pos - PointA.Pos;
        float dist = AtoB.Length();
        float error = Length - dist;
        if (Raymath.FloatEquals(error, 0f) == 1 || (PointA.Pinned && PointB.Pinned))
        {
            return;
        }
        Vector2 correctionVector = AtoB / dist * error;
        Vector2 correctionA;
        Vector2 correctionB;
        float stiffnessCoeff = 1f - MathF.Pow(1f - Stiffness, 1f / _ctx._substeps);
        if (!PointA.Pinned)
        {
            correctionA = -PointA.InvMass / (PointA.InvMass + PointB.InvMass) * correctionVector;
            correctionA *= stiffnessCoeff;
            PointA.Pos += correctionA;
        }
        if (!PointB.Pinned)
        {
            correctionB = PointB.InvMass / (PointA.InvMass + PointB.InvMass) * correctionVector;
            correctionB *= stiffnessCoeff;
            PointB.Pos += correctionB;
        }
    }

    public override void Draw()
    {
        var pointAviewPos = UnitConv.MetersToPixels(PointA.Pos);
        var pointBviewPos = UnitConv.MetersToPixels(PointB.Pos);

        DrawLineV(
            pointAviewPos,
            pointBviewPos,
            Color.White
        );
    }
}