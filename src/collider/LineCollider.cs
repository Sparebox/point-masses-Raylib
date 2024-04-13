using System.Numerics;
using Physics;
using Raylib_cs;
using static Raylib_cs.Raylib;

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
        DrawLine((int) StartPos.X, (int) StartPos.Y, (int) EndPos.X, (int) EndPos.Y, Color.White);
    }

    public void SolveCollision(PointMass p)
    {
        Vector2 closestPoint = Utils.Geometry.ClosestPointOnLine(StartPos, EndPos, p.Pos);
        Vector2 closestToPoint = p.Pos - closestPoint;
        float distToCollider = closestToPoint.LengthSquared();
        if (distToCollider <= p.Radius * p.Radius)
        {
            // Do expensive square root here
            distToCollider = (float) Math.Sqrt(distToCollider);
            // Collision
            Vector2 closestToPointNorm = Vector2.Normalize(closestToPoint);
            Vector2 reflectedVel = Vector2.Reflect(p.Vel, closestToPointNorm);
            // Reduce only normal aligned velocity
            Vector2 reflectedNormalVel = Vector2.Dot(reflectedVel, closestToPointNorm) * closestToPointNorm;
            Vector2 parallelVel = reflectedVel - reflectedNormalVel;
            reflectedNormalVel *= PointMass.RestitutionCoeff;
            // Correct penetration
            p.Pos += (p.Radius - distToCollider) * closestToPointNorm;
            p.Vel =  parallelVel + reflectedNormalVel; 
            p.ApplyFriction(closestToPointNorm);
        }
    }

    public bool CheckCollision(PointMass p)
    {
        Vector2 closestPoint = Utils.Geometry.ClosestPointOnLine(StartPos, EndPos, p.Pos);
        Vector2 closestToPoint = p.Pos - closestPoint;
        float distToCollider = closestToPoint.LengthSquared();
        if (distToCollider <= p.Radius * p.Radius)
        {
            return true;
        }
        return false;
    }
}