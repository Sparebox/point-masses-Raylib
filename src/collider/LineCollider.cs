using System.Numerics;
using Physics;
using Raylib_cs;
using Sim;
using Utils;
using static Raylib_cs.Raylib;
using static Utils.Entities;

namespace Collision;

public class LineCollider
{
    public Vector2 StartPos { get; set; }
    public Vector2 EndPos { get; set; }

    public LineCollider(float x0, float y0, float x1, float y1)
    {
        StartPos = new(x0, y0);
        EndPos = new(x1, y1);
    }

    public LineCollider(in Vector2 start, in Vector2 end)
    {
        StartPos = start;
        EndPos = end;
    }

    public LineCollider(in LineCollider c)
    {
        StartPos = c.StartPos;
        EndPos = c.EndPos;
    }

    public void Draw()
    {
        DrawLineV(
            UnitConv.MetersToPixels(StartPos),
            UnitConv.MetersToPixels(EndPos),
            Color.White
        );
    }

    public static void SolvePointCollision(CollisionData colData, Context context)
    {
        PointMass p = colData.PointMassA;
        // Collision
        Vector2 reflectedVel = Vector2.Reflect(p.Vel, colData.Normal);
        // Affect only velocity parallel to the normal
        Vector2 reflectedNormalVel = Vector2.Dot(reflectedVel, colData.Normal) * colData.Normal;
        Vector2 parallelVel = reflectedVel - reflectedNormalVel;
        reflectedNormalVel *= context._globalRestitutionCoeff;
        // Correct penetration
        p.Pos += colData.Separation * colData.Normal;
        p.Vel = parallelVel + reflectedNormalVel; 
        p.ApplyFriction(colData.Normal);
        
    }

    public CollisionData? CheckCollision(PointMass p)
    {
        Vector2 closestPoint = Geometry.ClosestPointOnLine(StartPos, EndPos, p.Pos);
        Vector2 closestToPoint = p.Pos - closestPoint;
        float distToCollider = closestToPoint.LengthSquared();
        if (distToCollider <= p.Radius * p.Radius)
        {
            distToCollider = MathF.Sqrt(distToCollider);
            CollisionData result = new () 
            { 
                PointMassA = p,
                Separation = p.Radius - distToCollider,
                Normal = new Vector2(closestToPoint.X / distToCollider, closestToPoint.Y / distToCollider)
            };
            return result;
        }
        return null;
    }
}