﻿using System.Numerics;
using Physics;
using Raylib_cs;
using Sim;
using static Raylib_cs.Raylib;

namespace Entities;

public class QuadTree
{
    public const int QuadCapacity = 5;

    private readonly BoundingBox _boundary;
    private readonly Vector2 _center;
    private readonly Vector2 _size;
    private readonly HashSet<MassShape> _massShapes;
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
    }

    public void Update(Context context)
    {
        Clear();
        DeleteEmptyQuads();
        foreach (var shape in context.MassShapes)
        {
            Insert(shape);
        }
    }

    public void Insert(MassShape shape)
    {
        if (!CheckCollisionBoxes(_boundary, shape.GetAABB()))
        {
            return;
        }
        if (_northEast is not null)
        {
            // Insert into children
            _northEast.Insert(shape);
            _southEast.Insert(shape);
            _southWest.Insert(shape);
            _northWest.Insert(shape);
            return;
        }
        if (_massShapes.Count < QuadCapacity)
        {
            // Insert into this quad
            _massShapes.Add(shape);
            return;
        }
        Subdivide();
        // move the points from this quad to the children
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
        _northEast = new QuadTree(new(_center.X + newSize.X / 2f, _center.Y - newSize.Y / 2f), newSize);
        _southEast = new QuadTree(new(_center.X + newSize.X / 2f, _center.Y + newSize.Y / 2f), newSize);
        _southWest = new QuadTree(new(_center.X - newSize.X / 2f, _center.Y + newSize.Y / 2f), newSize);
        _northWest = new QuadTree(new(_center.X - newSize.X / 2f, _center.Y - newSize.Y / 2f), newSize);
    }

    private bool ChildrenAreEmpty()
    {
        if (_northEast is null)
        {
            return true;
        }
        return !_northEast._massShapes.Any() && !_southEast._massShapes.Any() && !_southWest._massShapes.Any() && !_northWest._massShapes.Any();
    }

    private void Clear()
    {
        if (_northEast is null)
        {
            _massShapes.Clear();
        }
        else
        {
            _northEast.Clear();
            _southEast.Clear();
            _southWest.Clear();
            _northWest.Clear();
        }
    }

    private void DeleteEmptyQuads()
    {
        if (ChildrenAreEmpty())
        {
            _northEast = null;
            _southEast = null;
            _southWest = null;
            _northWest = null;
        }
    }

    public HashSet<MassShape> QueryAreaForShapes(BoundingBox area, HashSet<MassShape> found = null)
    {
        found ??= new();
        if (!CheckCollisionBoxes(_boundary, area))
        {
            return found;
        }
        if (_northEast is null)
        {
            foreach (var shape in _massShapes)
            {
                found.Add(shape);
            }
        }
        else
        {
            _northEast.QueryAreaForShapes(area, found);
            _southEast.QueryAreaForShapes(area, found);
            _southWest.QueryAreaForShapes(area, found);
            _northWest.QueryAreaForShapes(area, found);
        }
        return found;
    }

     public HashSet<PointMass> QueryAreaForPoints(BoundingBox area, HashSet<PointMass> found = null)
    {
        found ??= new();
        if (!CheckCollisionBoxes(_boundary, area))
        {
            return found;
        }
        if (_northEast is null)
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
            _northEast.QueryAreaForPoints(area, found);
            _southEast.QueryAreaForPoints(area, found);
            _southWest.QueryAreaForPoints(area, found);
            _northWest.QueryAreaForPoints(area, found);
        }
        return found;
    }

    public void Draw()
    {
        DrawRectangleLines((int) (_center.X - _size.X / 2f), (int) (_center.Y - _size.Y / 2f), (int) _size.X, (int) _size.Y, Color.Red);
        DrawText("Shapes: " + _massShapes.Count, (int) (_center.X - _size.X / 2f), (int) (_center.Y - _size.Y / 2f), 10, Color.Yellow);
        _northEast?.Draw();
        _southEast?.Draw();
        _southWest?.Draw();
        _northWest?.Draw();
    }
}