using System.Numerics;
using Raylib_cs;
using PointMasses.Entities;
using PointMasses.Sim;
using PointMasses.Utils;
using static Raylib_cs.Raylib;
using Newtonsoft.Json;

namespace PointMasses.Collision;

[JsonObject(MemberSerialization.OptIn)]
public class LineCollider : Entity
{
    [JsonProperty]
    public Vector2 _startPos;
    [JsonProperty]
    public Vector2 _endPos;
    public override Vector2 CenterOfMass => Centroid;
    public override Vector2 Centroid
    {
        get
        {
            if (_center.HasValue)
            {
                return _center.Value;
            }
            _center = Raymath.Vector2Lerp(_startPos, _endPos, 0.5f);
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
            if (_startPos.X < _endPos.X)
            {
                minX = _startPos.X;
                maxX = _endPos.X;
            }
            else
            {
                minX = _endPos.X;
                maxX = _startPos.X;
            }
            if (_startPos.Y < _endPos.X)
            {
                minY = _startPos.Y;
                maxY = _endPos.Y;
            }
            else
            {
                minY = _endPos.Y;
                maxY = _startPos.Y;
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
        _startPos = new(x0, y0);
        _endPos = new(x1, y1);
    }

    [JsonConstructor]
    public LineCollider(ref Vector2 start, ref Vector2 end, Context ctx) : base(ctx)
    {
        _startPos = start;
        _endPos = end;
    }

    public LineCollider(LineCollider c) : base(c.Ctx)
    {
        _startPos = c._startPos;
        _endPos = c._endPos;
    }

    public override void Update() {}

    public override void Draw()
    {
        DrawLineV(
            UnitConv.MtoP(_startPos),
            UnitConv.MtoP(_endPos),
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
        p._pos += colData.Separation * colData.Normal;
        // Impulse
        p.Vel = parallelVel + reflectedNormalVel; 
        // Friction
        p.ApplyFriction(colData.Normal);
    }

    public CollisionData? CheckCollision(PointMass p)
    {
        Vector2 closestPoint = Geometry.ClosestPointOnLine(ref _startPos, ref _endPos, ref p._pos);
        Vector2 closestToPoint = p._pos - closestPoint;
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