using System.Numerics;
using Raylib_cs;
using PointMasses.Entities;
using PointMasses.Sim;
using PointMasses.Utils;
using static Raylib_cs.Raylib;

namespace PointMasses.Collision;

public class LineCollider : Entity
{
    public Vector2 StartPos { get; set; }
    public Vector2 EndPos { get; set; }
    public override Vector2 CenterOfMass => Centroid;
    public override Vector2 Centroid
    {
        get
        {
            if (_center.HasValue)
            {
                return _center.Value;
            }
            _center = Raymath.Vector2Lerp(StartPos, EndPos, 0.5f);
            return _center.Value;
        }
    }
    public override BoundingBox Aabb
    {
        get
        {
            if (_aabb.HasValue)
            {
                return _aabb.Value;
            }
            float minX, minY, maxX, maxY;
            if (StartPos.X < EndPos.X)
            {
                minX = StartPos.X;
                maxX = EndPos.X;
            }
            else
            {
                minX = EndPos.X;
                maxX = StartPos.X;
            }
            if (StartPos.Y < EndPos.X)
            {
                minY = StartPos.Y;
                maxY = EndPos.Y;
            }
            else
            {
                minY = EndPos.Y;
                maxY = StartPos.Y;
            }
            _aabb = new BoundingBox()
            {
                Min = new(minX, minY, 0f),
                Max = new(maxX, maxY, 0f)
            };
            return _aabb.Value;
        }
    }
    private BoundingBox? _aabb;
    private Vector2? _center;

    public LineCollider(float x0, float y0, float x1, float y1, Context ctx) : base(ctx)
    {
        StartPos = new(x0, y0);
        EndPos = new(x1, y1);
    }

    public LineCollider(in Vector2 start, in Vector2 end, Context ctx) : base(ctx)
    {
        StartPos = start;
        EndPos = end;
    }

    public LineCollider(LineCollider c) : base(c.Ctx)
    {
        StartPos = c.StartPos;
        EndPos = c.EndPos;
    }

    public override void Update() {}

    public override void Draw()
    {
        DrawLineV(
            UnitConv.MtoP(StartPos),
            UnitConv.MtoP(EndPos),
            Color.White
        );
    }

    public static void SolvePointCollision(in CollisionData colData, Context ctx)
    {
        PointMass p = colData.PointMassA;
        // Collision
        Vector2 reflectedVel = Vector2.Reflect(p.Vel, colData.Normal);
        // Affect only velocity parallel to the normal
        Vector2 reflectedNormalVel = Vector2.Dot(reflectedVel, colData.Normal) * colData.Normal;
        Vector2 parallelVel = reflectedVel - reflectedNormalVel;
        reflectedNormalVel *= ctx._globalRestitutionCoeff;
        // Correct penetration
        p.Pos += colData.Separation * colData.Normal;
        // Impulse
        p.Vel = parallelVel + reflectedNormalVel; 
        // Friction
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