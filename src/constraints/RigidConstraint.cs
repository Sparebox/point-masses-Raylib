using System.Numerics;
using Raylib_cs;
using Utils;
using static Raylib_cs.Raylib;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Physics;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class RigidConstraint : Constraint 
{
    public readonly float Length;

    public RigidConstraint(in PointMass a, in PointMass b)
    {
        PointA = a;
        PointB = b;
        Length = Vector2.Distance(PointA.Pos, PointB.Pos);
        Id = _idCounter++;
    }

    public RigidConstraint(in RigidConstraint c)
    {
        PointA = c.PointA;
        PointB = c.PointB;
        Length = c.Length;
        Id = _idCounter++;
    }

    public override void Update()
    {
        Vector2 AtoB = PointB.Pos - PointA.Pos;
        float dist = AtoB.Length();
        float diff = Length - dist;
        float percentage = diff / dist / 2f;
        Vector2 offset = percentage * AtoB;
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
        DrawLine(
            UnitConv.MetersToPixels(PointA.Pos.X),
            UnitConv.MetersToPixels(PointA.Pos.Y),
            UnitConv.MetersToPixels(PointB.Pos.X),
            UnitConv.MetersToPixels(PointB.Pos.Y),
            Color.White
        );
    }
}