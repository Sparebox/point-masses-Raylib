using System.Numerics;
using Physics;
using Raylib_cs;
using Sim;
using static Raylib_cs.Raylib;

namespace Entities;

public class QuadTree
{
    public const int QuadCapacity = 4;
    public const int MaxSubdivisions = 5_000;

    private static int _subdivisions;
    private readonly BoundingBox _boundary;
    private readonly Vector2 _center;
    private readonly Vector2 _size;
    private readonly HashSet<MassShape> _massShapes;
    private bool _subdivided;
    private QuadTree _northEast;
    private QuadTree _southEast;
    private QuadTree _southWest;
    private QuadTree _northWest;

    public QuadTree(in Vector2 center, in Vector2 size)
    {
        _center = center;
        _size = size;
        _boundary = new BoundingBox(new(_center.X - _size.X / 2f, _center.Y - _size.Y / 2f, 0f), new(_center.X + _size.X / 2f, _center.Y + _size.Y / 2f, 0f));
        _massShapes = new();
        _subdivided  = false;
    }

    public void Update(Context context)
    {
        Clear();
        _subdivisions = 0;
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
        if (_massShapes.Count < QuadCapacity || _subdivisions >= MaxSubdivisions)
        {
            // Insert into this quad
            _massShapes.Add(shape);
            return;
        }
        if (_subdivisions >= MaxSubdivisions)
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
    }

    private void Subdivide()
    {
        Vector2 newSize = _size / 2f;
        _northEast ??= new QuadTree(new(_center.X + newSize.X / 2f, _center.Y - newSize.Y / 2f), newSize);
        _southEast ??= new QuadTree(new(_center.X + newSize.X / 2f, _center.Y + newSize.Y / 2f), newSize);
        _southWest ??= new QuadTree(new(_center.X - newSize.X / 2f, _center.Y + newSize.Y / 2f), newSize);
        _northWest ??= new QuadTree(new(_center.X - newSize.X / 2f, _center.Y - newSize.Y / 2f), newSize);
        _subdivided = true;
        _subdivisions++;
    }

    private void Clear()
    {
        if (!_subdivided)
        {
            _massShapes.Clear();
            return;
        }
        _subdivided = false;
        _northEast.Clear();
        _southEast.Clear();
        _southWest.Clear();
        _northWest.Clear();
    }

    public HashSet<MassShape> QueryShapes(BoundingBox area, HashSet<MassShape> found = null)
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
                found.Add(shape);
            }
        }
        else
        {
            _northEast.QueryShapes(area, found);
            _southEast.QueryShapes(area, found);
            _southWest.QueryShapes(area, found);
            _northWest.QueryShapes(area, found);
        }
        return found;
    }

     public HashSet<PointMass> QueryPoints(BoundingBox area, HashSet<PointMass> found = null)
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
                    found.Add(point);
                }
            }
        }
        else
        {
            _northEast.QueryPoints(area, found);
            _southEast.QueryPoints(area, found);
            _southWest.QueryPoints(area, found);
            _northWest.QueryPoints(area, found);
        }
        return found;
    }

    public void Draw()
    {
        DrawRectangleLines((int) (_center.X - _size.X / 2f), (int) (_center.Y - _size.Y / 2f), (int) _size.X, (int) _size.Y, Color.Red);
        DrawText("Shapes: " + _massShapes.Count, (int) (_center.X - _size.X / 2f), (int) (_center.Y - _size.Y / 2f), 10, Color.Yellow);
        if (_subdivided)
        {
            _northEast.Draw();
            _southEast.Draw();
            _southWest.Draw();
            _northWest.Draw();
        }
    }
}
