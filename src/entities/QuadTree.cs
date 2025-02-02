using System.Numerics;
using Raylib_cs;
using PointMasses.Sim;
using PointMasses.Utils;
using static Raylib_cs.Raylib;

namespace PointMasses.Entities;

public class QuadTree
{
    public static uint MaxDepth { get; set; }
    public static uint NodeCapacity { get; set; }
    
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

    public QuadTree(Vector2 center, Vector2 size, uint nodeCapacity, uint maxDepth)
    {
        _center = center;
        _size = size;
        _boundary = new BoundingBox(new(_center.X - _size.X * 0.5f, _center.Y - _size.Y * 0.5f, 0f), new(_center.X + _size.X * 0.5f, _center.Y + _size.Y * 0.5f, 0f));
        _massShapes = new();
        _subdivided  = false;
        NodeCapacity = nodeCapacity;
        _depth = 0;
        MaxDepth = maxDepth;
    }

    public void Update(Context ctx)
    {
        ctx.QuadTreeLock.EnterWriteLock();
        ctx.Lock.EnterReadLock();
        Clear();
        foreach (var shape in ctx.MassShapes)
        {
            Insert(shape);
        }
        ctx.Lock.ExitReadLock();
        ctx.QuadTreeLock.ExitWriteLock();
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
        if (_massShapes.Count < NodeCapacity || _depth >= MaxDepth)
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
    }

    private void Subdivide()
    {
        Vector2 newSize = _size * 0.5f;
        _northEast ??= new QuadTree(new(_center.X + newSize.X * 0.5f, _center.Y - newSize.Y * 0.5f), newSize, NodeCapacity, MaxDepth);
        _southEast ??= new QuadTree(new(_center.X + newSize.X * 0.5f, _center.Y + newSize.Y * 0.5f), newSize, NodeCapacity, MaxDepth);
        _southWest ??= new QuadTree(new(_center.X - newSize.X * 0.5f, _center.Y + newSize.Y * 0.5f), newSize, NodeCapacity, MaxDepth);
        _northWest ??= new QuadTree(new(_center.X - newSize.X * 0.5f, _center.Y - newSize.Y * 0.5f), newSize, NodeCapacity, MaxDepth);
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

    public void QueryShapes(ref BoundingBox area, HashSet<MassShape> found)
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
            _northEast.QueryShapes(ref area, found);
            _southEast.QueryShapes(ref area, found);
            _southWest.QueryShapes(ref area, found);
            _northWest.QueryShapes(ref area, found);
        }
        return;
    }

    public void QueryPoints(ref BoundingBox area, HashSet<PointMass> found)
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
            _northEast.QueryPoints(ref area, found);
            _southEast.QueryPoints(ref area, found);
            _southWest.QueryPoints(ref area, found);
            _northWest.QueryPoints(ref area, found);
        }
        return;
    }

    public void Draw()
    {
        if (_massShapes.Count == 0 && !_subdivided)
        {
            return;
        }
        Vector2 viewCenter = UnitConv.MtoP(_center);

        DrawRectangleLines(
            (int) viewCenter.X - UnitConv.MtoP(_size.X * 0.5f),
            (int) viewCenter.Y - UnitConv.MtoP(_size.Y * 0.5f),
            UnitConv.MtoP(_size.X),
            UnitConv.MtoP(_size.Y),
            Color.Red
        );
        if (!_subdivided)
        {
            DrawText(
                _massShapes.Count.ToString(),
                (int) viewCenter.X,
                (int) viewCenter.Y,
                15,
                Color.Yellow
            );
        } else
        {
            _northEast.Draw();
            _southEast.Draw();
            _southWest.Draw();
            _northWest.Draw();
        }
    }

    public static void ThreadUpdate(object _ctx)
    {
        Context ctx = (Context) _ctx;
        for (;;)
        {
            ctx.QuadTreePauseEvent.Wait();
            Thread.Sleep(Constants.QuadTreeUpdateMs);
            ctx.QuadTree.Update(ctx);
        }
    }

    public static void OnPauseChanged(object sender, bool paused)
    {
        Context ctx = (Context) sender;
        if (paused)
        {
            ctx.QuadTreePauseEvent.Reset();
        }
        else
        {
            ctx.QuadTreePauseEvent.Set();
        }
    }
}

