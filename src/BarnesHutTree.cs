using System.Numerics;
using Physics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using Sim;
using Tools;
using Utils;

namespace GravitySim;

public class BarnesHutTree
{
    private readonly BoundingBox _boundary;
    private readonly Vector2 _center;
    private readonly Vector2 _size;
    private readonly HashSet<MassShape> _massShapes;
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

    public void Update(Context context)
    {
        Clear();
        foreach (var shape in context.MassShapes)
        {
            Insert(shape);
        }
    }

    public bool Insert(MassShape shape)
    {
        if (!CheckCollisionBoxes(_boundary, shape.AABB))
        {
            return false;
        }
        if (_massShapes.Count == 0) // External node
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
        _northEast._depth = _depth + 1;
        _southEast._depth = _depth + 1;
        _southWest._depth = _depth + 1;
        _northWest._depth = _depth + 1;
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
        if (_subdivided)
        {
            _northEast.UpdateCenterOfMass();
            _southEast.UpdateCenterOfMass();
            _southWest.UpdateCenterOfMass();
            _northWest.UpdateCenterOfMass();
        }
        _totalMass = _massShapes.Aggregate(0f, (_totalMass, s) => _totalMass + s.Mass);
        _centerOfMass = _massShapes.Aggregate(Vector2.Zero, (_centerOfMass, s) => _centerOfMass + s.CenterOfMass * s.Mass);
        _centerOfMass /= _totalMass;
    }

    public void CalculateGravity(MassShape shapeA, NbodySim nbodySim)
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
            Vector2 gravForce = dir * nbodySim._gravConstant * shapeA.Mass * shapeB.Mass / (dist * dist);
            shapeA.ApplyForce(gravForce);
            return;
        }
        dir = _centerOfMass - shapeA.CenterOfMass;
        dist = dir.Length();
        if (dist == 0f)
        {
            return;
        }
        float quotient = _size.X / dist;
        if (quotient < nbodySim._threshold) // Far away -> treating as a single body
        {
            if (dist < nbodySim._minDist)
            {
                return;
            }
            dir /= dist;
            Vector2 gravForce = dir * nbodySim._gravConstant * shapeA.Mass * _totalMass / (dist * dist);
            shapeA.ApplyForce(gravForce);
        }
        else // Close to a center of mass
        {
            _northEast.CalculateGravity(shapeA, nbodySim);
            _southEast.CalculateGravity(shapeA, nbodySim);
            _southWest.CalculateGravity(shapeA, nbodySim);
            _northWest.CalculateGravity(shapeA, nbodySim);
        }
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
            $"Shapes: {_massShapes.Count}",
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
