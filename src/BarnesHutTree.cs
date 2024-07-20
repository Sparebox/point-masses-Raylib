using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using Sim;
using Tools;
using Utils;
using Entities;

namespace GravitySim;

public class BarnesHutTree
{   
    public const int MaxDepth = 10;
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
        _boundary = new BoundingBox(new(_center.X - _size.X / 2f, _center.Y - _size.Y / 2f, 0f), new(_center.X + _size.X / 2f, _center.Y + _size.Y / 2f, 0f));
        _massShapes = new();
        _subdivided  = false;
        _depth = 0;
    }

    public void Update(Context ctx)
    {
        Clear();
        ctx.Lock.EnterReadLock();
        IEnumerable<MassShape> massShapes = ctx.MassShapes;
        ctx.Lock.ExitReadLock();
        foreach (var shape in massShapes)
        {
            Insert(shape);
        }
        ApplyGravityForces(ctx.NbodySim);
    }

    private void ApplyGravityForces(NbodySim nbodySim)
    {
        foreach (var shape in _massShapes)
        {
            ApplyGravityForce(shape, nbodySim);
        }
    }

    public bool Insert(MassShape shape)
    {
        if (!CheckCollisionBoxSphere(_boundary, new(shape.CenterOfMass, 0f), UnitConv.PixelsToMeters(1f)))
        {
            return false;
        }
        if (_massShapes.Count == 0 || _depth == MaxDepth) // External node or maximum depth reached
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
        Vector2 newSize = _size / 2f;
        _northEast ??= new BarnesHutTree(new(_center.X + newSize.X / 2f, _center.Y - newSize.Y / 2f), newSize);
        _southEast ??= new BarnesHutTree(new(_center.X + newSize.X / 2f, _center.Y + newSize.Y / 2f), newSize);
        _southWest ??= new BarnesHutTree(new(_center.X - newSize.X / 2f, _center.Y + newSize.Y / 2f), newSize);
        _northWest ??= new BarnesHutTree(new(_center.X - newSize.X / 2f, _center.Y - newSize.Y / 2f), newSize);
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

    private void ApplyGravityForce(MassShape shapeA, NbodySim nbodySim)
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
            if (dist == 0f || dist < nbodySim._minDist)
            {
                return;
            }
            Vector2 gravForce = GetGravForce(dir, nbodySim._gravConstant, dist, shapeA.Mass, shapeB.Mass);
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
        if (quotient < nbodySim._threshold) // Far away -> treating as a single body
        {
            if (dist < nbodySim._minDist)
            {
                return;
            }
            dir /= dist;
            Vector2 gravForce = GetGravForce(dir, nbodySim._gravConstant, dist, shapeA.Mass, _totalMass);
            shapeA.ApplyForce(gravForce);
        }
        else // Close to a center of mass
        {
            _northEast.ApplyGravityForce(shapeA, nbodySim);
            _southEast.ApplyGravityForce(shapeA, nbodySim);
            _southWest.ApplyGravityForce(shapeA, nbodySim);
            _northWest.ApplyGravityForce(shapeA, nbodySim);
        }
    }

    private static Vector2 GetGravForce(Vector2 dir, float gravConst, float dist, float massA, float massb)
    {
        return dir * gravConst * massA * massb / (dist * dist);
    }

    public void Draw()
    {
        DrawRectangleLines(
            UnitConv.MetersToPixels(_center.X - _size.X / 2f),
            UnitConv.MetersToPixels(_center.Y - _size.Y / 2f),
            UnitConv.MetersToPixels(_size.X),
            UnitConv.MetersToPixels(_size.Y),
            Color.Red
        );
        DrawText(
            _massShapes.Count.ToString(),
            UnitConv.MetersToPixels(_center.X),
            UnitConv.MetersToPixels(_center.Y),
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
