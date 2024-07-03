using System.Numerics;
using Raylib_cs;
using Sim;
using Utils;
using static Raylib_cs.Raylib;

namespace Entities;

public class QuadTree
{
    public uint MaxDepth { get; init;}
    private readonly uint _nodeCapacity;
    private readonly BoundingBox _boundary;
    private readonly Vector2 _center;
    private readonly Vector2 _size;
    private readonly List<MassShape> _massShapes;
    private bool _subdivided;
    private QuadTree _northEast;
    private QuadTree _southEast;
    private QuadTree _southWest;
    private QuadTree _northWest;
    private uint _depth;

    public QuadTree(in Vector2 center, in Vector2 size, uint nodeCapacity, uint maxDepth)
    {
        _center = center;
        _size = size;
        _boundary = new BoundingBox(new(_center.X - _size.X / 2f, _center.Y - _size.Y / 2f, 0f), new(_center.X + _size.X / 2f, _center.Y + _size.Y / 2f, 0f));
        _massShapes = new();
        _subdivided  = false;
        _nodeCapacity = nodeCapacity;
        _depth = 0;
        MaxDepth = maxDepth;
    }

    public void Update(Context context)
    {
        Clear();
        foreach (var shape in context.MassShapes)
        {
            Insert(shape);
        }
    }

    public void Insert(MassShape shape)
    {
        if (!CheckCollisionBoxes(_boundary, shape.Aabb))
        {
            return;
        }
        if (_subdivided) // Internal node
        {
            // Insert into children
            _northEast.Insert(shape);
            _southEast.Insert(shape);
            _southWest.Insert(shape);
            _northWest.Insert(shape);
            return;
        }
        // External node
        if (_massShapes.Count < _nodeCapacity || _depth >= MaxDepth)
        {
            // Insert into this quad
            _massShapes.Add(shape);
            return;
        }
        if (_depth < MaxDepth)
        {
            // This quad is full and can be subdivided
            Subdivide();
            // Move the points from this quad to the children
            _massShapes.Add(shape);
            foreach (var s in _massShapes)
            {
                _northEast.Insert(s);
                _southEast.Insert(s);
                _southWest.Insert(s);
                _northWest.Insert(s);
            }
            _massShapes.Clear();
        }
        return;
    }

    private void Subdivide()
    {
        Vector2 newSize = _size / 2f;
        _northEast ??= new QuadTree(new(_center.X + newSize.X / 2f, _center.Y - newSize.Y / 2f), newSize, _nodeCapacity, MaxDepth);
        _southEast ??= new QuadTree(new(_center.X + newSize.X / 2f, _center.Y + newSize.Y / 2f), newSize, _nodeCapacity, MaxDepth);
        _southWest ??= new QuadTree(new(_center.X - newSize.X / 2f, _center.Y + newSize.Y / 2f), newSize, _nodeCapacity, MaxDepth);
        _northWest ??= new QuadTree(new(_center.X - newSize.X / 2f, _center.Y - newSize.Y / 2f), newSize, _nodeCapacity, MaxDepth);
        _northEast._depth = _depth + 1;
        _southEast._depth = _depth + 1;
        _southWest._depth = _depth + 1;
        _northWest._depth = _depth + 1;
        _subdivided = true;
    }

    private void Clear()
    {
        _massShapes.Clear();
        _depth = 0;
        if (_subdivided)
        {
            _northEast.Clear();
            _southEast.Clear();
            _southWest.Clear();
            _northWest.Clear();
            _subdivided = false;
        }
    }

    private QuadTree GetClosestChildNode(MassShape shape)
    {
        QuadTree closestNode = null;
        float minDistSq = float.MaxValue;
        float distSq;
        distSq = Vector2.DistanceSquared(shape.Centroid, _northEast._center);
        if (distSq < minDistSq)
        {
            closestNode = _northEast;
            minDistSq = distSq;
        }
        distSq = Vector2.DistanceSquared(shape.Centroid, _southEast._center);
        if (distSq < minDistSq)
        {
            closestNode = _southEast;
            minDistSq = distSq;
        }
        distSq = Vector2.DistanceSquared(shape.Centroid, _southWest._center);
        if (distSq < minDistSq)
        {
            closestNode = _southWest;
            minDistSq = distSq;
        }
        distSq = Vector2.DistanceSquared(shape.Centroid, _northWest._center);
        if (distSq < minDistSq)
        {
            closestNode = _northWest;
        }
        return closestNode;
    }

    public void QueryShapes(in BoundingBox area, HashSet<MassShape> found)
    {
        if (!CheckCollisionBoxes(_boundary, area))
        {
            return;
        }
        if (!_subdivided) // External node
        {
            foreach (var shape in _massShapes)
            {
                found.Add(shape);
            }
        }
        else
        {
            _northEast.QueryShapes(in area, found);
            _southEast.QueryShapes(in area, found);
            _southWest.QueryShapes(in area, found);
            _northWest.QueryShapes(in area, found);
        }
        return;
    }

     public void QueryPoints(in BoundingBox area, HashSet<PointMass> found)
    {
        found ??= new();
        if (!CheckCollisionBoxes(_boundary, area))
        {
            return;
        }
        if (!_subdivided)
        {
            foreach (var shape in _massShapes)
            {
                foreach (var point in shape._points)
                {
                    found.Add(point);
                }
            }
        }
        else
        {
            _northEast.QueryPoints(in area, found);
            _southEast.QueryPoints(in area, found);
            _southWest.QueryPoints(in area, found);
            _northWest.QueryPoints(in area, found);
        }
        return;
    }

    public void Draw()
    {
        if (_massShapes.Count == 0 && !_subdivided)
        {
            return;
        }
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
