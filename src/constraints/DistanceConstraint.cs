using System.Numerics;
using Raylib_cs;
using Sim;
using Utils;
using static Raylib_cs.Raylib;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Physics;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class DistanceConstraint : Constraint 
{
    public float Length { get; init; }
    public float Stiffness { get; init; }
    private Context _context;

    public DistanceConstraint(in PointMass a, in PointMass b, float stiffness, Context context)
    {
        PointA = a;
        PointB = b;
        Length = Vector2.Distance(PointA.Pos, PointB.Pos);
        Stiffness = stiffness;
        _context = context;
        Id = _idCounter++;
    }

    public DistanceConstraint(in DistanceConstraint c)
    {
        PointA = c.PointA;
        PointB = c.PointB;
        Length = c.Length;
        Stiffness = c.Stiffness;
        _context = c._context;
        Id = _idCounter++;
    }

    public override void Update()
    {
        Vector2 AtoB = PointB.Pos - PointA.Pos;
        float dist = AtoB.Length();
        float error = Length - dist;
        if (Raymath.FloatEquals(error, 0f) == 1)
        {
            return;
        }
        Vector2 correctionVector = AtoB / dist * error;
        Vector2 correctionA = -PointA.InvMass / (PointA.InvMass + PointB.InvMass) * correctionVector;
        Vector2 correctionB = PointB.InvMass / (PointA.InvMass + PointB.InvMass) * correctionVector;
        float stiffnessCoeff = 1f - MathF.Pow(1f - Stiffness, 1f / _context.Substeps);
        correctionA *= stiffnessCoeff;
        correctionB *= stiffnessCoeff;
        PointA.Pos += correctionA;
        PointB.Pos += correctionB;
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