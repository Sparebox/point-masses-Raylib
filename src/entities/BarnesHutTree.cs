using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using PointMasses.Sim;
using PointMasses.Utils;
using PointMasses.Tools;
using PointMasses.Systems;
using PointMasses.Entities;

namespace Entities;

public class BarnesHutTree
{   
    private readonly BoundingBox _boundary;
    private readonly Vector2 _center;
    private readonly Vector2 _size;
    private readonly List<MassShape> _massShapes;
    private Vector2 _centerOfMass;
    private float _totalMass;
    private bool _subdivided;
    private BarnesHutTree _northEast;
    private BarnesHutTree _southEast;
    private BarnesHutTree _southWest;
    private BarnesHutTree _northWest;
    private uint _depth;

    public BarnesHutTree(in Vector2 center, in Vector2 size)
    {
        _center = center;
        _size = size;
        _boundary = new BoundingBox(new(_center.X - _size.X * 0.5f, _center.Y - _size.Y * 0.5f, 0f), new(_center.X + _size.X * 0.5f, _center.Y + _size.Y * 0.5f, 0f));
        _massShapes = new();
        _subdivided  = false;
        _depth = 0;
    }

    public void Update(Context ctx)
    {
        ctx.Lock.EnterReadLock();
        Clear();
        foreach (var shape in ctx.MassShapes)
        {
            Insert(shape);
        }
        ApplyGravityForces((NbodySystem) ctx.GetSystem(typeof(NbodySystem)), ctx._timestep);
        ctx.Lock.ExitReadLock();
    }

    private void ApplyGravityForces(NbodySystem nBodySystem, float stepSize)
    {
        foreach (var shape in _massShapes)
        {
            ApplyGravityForce(shape, nBodySystem, stepSize);
        }
    }

    public bool Insert(MassShape shape)
    {
        if (!CheckCollisionBoxSphere(_boundary, new(shape.CenterOfMass, 0f), UnitConv.PtoM(1f)))
        {
            return false;
        }
        if (_massShapes.Count == 0 || _depth == Constants.BarnesHutMaxDepth) // External node or maximum depth reached
        {
            // Insert into this quad
            _massShapes.Add(shape);
            UpdateCenterOfMass();
            return true;
        }
        if (_subdivided) // Internal node
        {
            // Insert into children
            _massShapes.Add(shape);
            UpdateCenterOfMass();
            if (_northEast.Insert(shape))
            {
                return true;
            }
            if (_southEast.Insert(shape))
            {
                return true;
            }
            if (_southWest.Insert(shape))
            {
                return true;
            }
            if (_northWest.Insert(shape))
            {
                return true;
            }
        }
        // This quad is full and can be subdivided
        Subdivide();
        // Move the points from this quad to the children
        _massShapes.Add(shape);
        foreach (var s in _massShapes)
        {
            if (_northEast.Insert(s))
            {
                continue;
            }
            if (_southEast.Insert(s))
            {
                continue;
            }
            if (_southWest.Insert(s))
            {
                continue;
            }
            if (_northWest.Insert(s))
            {
                continue;
            }
        }
        UpdateCenterOfMass();
        return true;
    }

    private void Subdivide()
    {
        Vector2 newSize = _size * 0.5f;
        _northEast ??= new BarnesHutTree(new(_center.X + newSize.X * 0.5f, _center.Y - newSize.Y * 0.5f), newSize);
        _southEast ??= new BarnesHutTree(new(_center.X + newSize.X * 0.5f, _center.Y + newSize.Y * 0.5f), newSize);
        _southWest ??= new BarnesHutTree(new(_center.X - newSize.X * 0.5f, _center.Y + newSize.Y * 0.5f), newSize);
        _northWest ??= new BarnesHutTree(new(_center.X - newSize.X * 0.5f, _center.Y - newSize.Y * 0.5f), newSize);
        uint newDepth = _depth + 1;
        _northEast._depth = newDepth;
        _southEast._depth = newDepth;
        _southWest._depth = newDepth;
        _northWest._depth = newDepth;
        _subdivided = true;
    }

    private void Clear()
    {
        _massShapes.Clear();
        _centerOfMass = Vector2.Zero;
        _totalMass = 0f;
        _depth = 0;
        if (_subdivided)
        {
            _subdivided = false;
            _northEast.Clear();
            _southEast.Clear();
            _southWest.Clear();
            _northWest.Clear();
        }
    }

    private void UpdateCenterOfMass()
    {
        _totalMass = _massShapes.Aggregate(0f, (_totalMass, s) => _totalMass + s.Mass);
        _centerOfMass = _massShapes.Aggregate(Vector2.Zero, (_centerOfMass, s) => _centerOfMass + s.CenterOfMass * s.Mass);
        _centerOfMass /= _totalMass;
    }

    private void ApplyGravityForce(MassShape shapeA, NbodySystem nBodySystem, float stepSize)
    {   
        Vector2 dir;
        float dist;
        if (!_subdivided)
        {
            if (!_massShapes.Any())
            {
                return;
            }
            MassShape shapeB = _massShapes.First();
            if (shapeA == shapeB)
            {
                return;
            }
            dir = shapeB.CenterOfMass - shapeA.CenterOfMass;
            dist = dir.Length();
            if (dist == 0f || dist < nBodySystem._minDist)
            {
                return;
            }
            Vector2 gravForce;
            if (nBodySystem._postNewtonianEnabled)
            {
                gravForce = GetPostNewtonianGravForce(dir, nBodySystem._gravConstant, dist, shapeA, shapeB.Mass, stepSize);
            }
            else
            {
                gravForce = GetNewtonianGravForce(dir, nBodySystem._gravConstant, dist, shapeA.Mass, shapeB.Mass);
            }
            shapeA.ApplyForce(gravForce);
            return;
        }
        dir = _centerOfMass - shapeA.CenterOfMass;
        dist = dir.Length();
        if (dist == 0f)
        {
            dist += float.Epsilon;
        }
        float quotient = _size.X / dist;
        if (quotient < nBodySystem._threshold) // Far away -> treating as a single body
        {
            if (dist < nBodySystem._minDist)
            {
                return;
            }
            dir /= dist;
            Vector2 gravForce;
            if (nBodySystem._postNewtonianEnabled)
            {
                gravForce = GetPostNewtonianGravForce(dir, nBodySystem._gravConstant, dist, shapeA, _totalMass, stepSize);
            }
            else
            {
                gravForce = GetNewtonianGravForce(dir, nBodySystem._gravConstant, dist, shapeA.Mass, _totalMass);
            }
            shapeA.ApplyForce(gravForce);
        }
        else // Close to a center of mass
        {
            _northEast.ApplyGravityForce(shapeA, nBodySystem, stepSize);
            _southEast.ApplyGravityForce(shapeA, nBodySystem, stepSize);
            _southWest.ApplyGravityForce(shapeA, nBodySystem, stepSize);
            _northWest.ApplyGravityForce(shapeA, nBodySystem, stepSize);
        }
    }

    private static Vector2 GetNewtonianGravForce(Vector2 dir, float gravConst, float dist, float massA, float massB)
    {
        return dir * gravConst * massA * massB / (dist * dist);
    }

    private static Vector2 GetPostNewtonianGravForce(Vector2 dir, float gravConst, float dist, MassShape shapeA, float massB, float stepSize)
    {
        Vector2 newtonianForce = GetNewtonianGravForce(dir, gravConst, dist, shapeA.Mass, massB);
        float relativisticCorrection = 1f + 3f * shapeA.Vel.LengthSquared() / stepSize / Constants.SpeedOfLightSq - 4f * gravConst * massB / (Constants.SpeedOfLightSq * dist);
        return relativisticCorrection * newtonianForce;
    }

    public void Draw()
    {
        DrawRectangleLines(
            UnitConv.MtoP(_center.X - _size.X * 0.5f),
            UnitConv.MtoP(_center.Y - _size.Y * 0.5f),
            UnitConv.MtoP(_size.X),
            UnitConv.MtoP(_size.Y),
            Color.Red
        );
        DrawText(
            _massShapes.Count.ToString(),
            UnitConv.MtoP(_center.X),
            UnitConv.MtoP(_center.Y),
            15,
            Color.Yellow
        );
        if (_subdivided)
        {
            _northEast.Draw();
            _southEast.Draw();
            _southWest.Draw();
            _northWest.Draw();
        }
    }
}
