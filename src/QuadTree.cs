using System.Numerics;
using Physics;
using Raylib_cs;
using Sim;
using Tools;
using Utils;
using static Raylib_cs.Raylib;

namespace Entities;

public class QuadTree
{
    public const int QuadCapacity = 2; // > 1
    public uint MaxDepth { get; init;}
    private readonly BoundingBox _boundary;
    private readonly Vector2 _center;
    private readonly Vector2 _size;
    private readonly HashSet<MassShape> _massShapes;
    private bool _subdivided;
    private QuadTree _northEast;
    private QuadTree _southEast;
    private QuadTree _southWest;
    private QuadTree _northWest;
    private uint _depth;

    public QuadTree(in Vector2 center, in Vector2 size, uint maxDepth)
    {
        _center = center;
        _size = size;
        _boundary = new BoundingBox(new(_center.X - _size.X / 2f, _center.Y - _size.Y / 2f, 0f), new(_center.X + _size.X / 2f, _center.Y + _size.Y / 2f, 0f));
        _massShapes = new();
        _subdivided  = false;
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
        if (!CheckCollisionBoxes(_boundary, shape.AABB))
        {
            return;
        }
        if (_subdivided)
        {
            // Insert into children
            _northEast.Insert(shape);
            _southEast.Insert(shape);
            _southWest.Insert(shape);
            _northWest.Insert(shape);
            return;
        }
        if (_massShapes.Count < QuadCapacity || _depth >= MaxDepth)
        {
            // Insert into this quad
            _massShapes.Add(shape);
            return;
        }
        if (_depth >= MaxDepth)
        {
            return;
        }
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
        return;
    }

    private void Subdivide()
    {
        Vector2 newSize = _size / 2f;
        _northEast ??= new QuadTree(new(_center.X + newSize.X / 2f, _center.Y - newSize.Y / 2f), newSize, MaxDepth);
        _southEast ??= new QuadTree(new(_center.X + newSize.X / 2f, _center.Y + newSize.Y / 2f), newSize, MaxDepth);
        _southWest ??= new QuadTree(new(_center.X - newSize.X / 2f, _center.Y + newSize.Y / 2f), newSize, MaxDepth);
        _northWest ??= new QuadTree(new(_center.X - newSize.X / 2f, _center.Y - newSize.Y / 2f), newSize, MaxDepth);
        _northEast._depth = _depth + 1;
        _southEast._depth = _depth + 1;
        _southWest._depth = _depth + 1;
        _northWest._depth = _depth + 1;
        _subdivided = true;
    }

    private void Clear()
    {
        if (!_subdivided)
        {
            _massShapes.Clear();
            _depth = 0;
            return;
        }
        _subdivided = false;
        _northEast.Clear();
        _southEast.Clear();
        _southWest.Clear();
        _northWest.Clear();
    }

    public HashSet<MassShape> QueryShapes(in BoundingBox area, HashSet<MassShape> found = null)
    {
        found ??= new();
        if (!CheckCollisionBoxes(_boundary, area))
        {
            return found;
        }
        if (!_subdivided)
        {
            foreach (var shape in _massShapes)
            {
                if (CheckCollisionBoxes(area, shape.AABB))
                {
                    found.Add(shape);
                }
            }
        }
        else
        {
            _northEast.QueryShapes(in area, found);
            _southEast.QueryShapes(in area, found);
            _southWest.QueryShapes(in area, found);
            _northWest.QueryShapes(in area, found);
        }
        return found;
    }

     public HashSet<PointMass> QueryPoints(in BoundingBox area, HashSet<PointMass> found = null)
    {
        found ??= new();
        if (!CheckCollisionBoxes(_boundary, area))
        {
            return found;
        }
        if (!_subdivided)
        {
            foreach (var shape in _massShapes)
            {
                foreach (var point in shape._points)
                {
                    if (CheckCollisionBoxes(area, point.AABB))
                    {
                        found.Add(point);
                    }
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
        return found;
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
            UnitConv.MetersToPixels(_center.X - _size.X / 2f),
            UnitConv.MetersToPixels(_center.Y - _size.Y / 2f),
            10,
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
