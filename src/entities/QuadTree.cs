using System.Numerics;
using Raylib_cs;
using Sim;
using Utils;
using static Raylib_cs.Raylib;

namespace Entities;

public class QuadTree
{
    public static uint MaxDepth { get; set; }
    public static uint NodeCapacity { get; set; }

    public ManualResetEventSlim PauseEvent { get; init; }
    public ReaderWriterLockSlim Lock { get; init; }
    
    private readonly List<Node> _nodes;
    private readonly Vector2 _center;
    private readonly Vector2 _size;

    public QuadTree(in Vector2 center, in Vector2 size, uint nodeCapacity, uint maxDepth)
    { 
        _center = center;
        _size = size;
        _nodes = new List<Node>();
        Lock = new ReaderWriterLockSlim();
        PauseEvent = new ManualResetEventSlim(true);
        NodeCapacity = nodeCapacity;
        MaxDepth = maxDepth;
    }

    public void Update(Context ctx)
    {
        Lock.EnterWriteLock();
        ctx.Lock.EnterReadLock();
        Clear();
        foreach (var shape in ctx.MassShapes)
        {
            Insert(shape, 0);
        }
        ctx.Lock.ExitReadLock();
        Lock.ExitWriteLock();
    }

    public void Insert(MassShape shape, int startNodeId)
    {
        for (int i = startNodeId; i < _nodes.Count; i++)
        {
            var node = _nodes[i];
            if (!node.IsLeaf)
            {
                continue;
            }
            if (!CheckCollisionBoxes(node._boundingBox, shape.Aabb))
            {
                continue;
            }
            if (node._massShapes.Count < NodeCapacity || node._depth >= MaxDepth)
            {
                node._massShapes.Add(shape);
                continue;
            }
            int childrenIdStart = Subdivide(i, out MassShape shapeToMove);
            Insert(shapeToMove, childrenIdStart);
        }
        
    }

    private int Subdivide(int nodeId, out MassShape shapeToMove)
    {
        var node = _nodes[nodeId];
        shapeToMove = node._massShapes.First();
        int childrenIdStart = _nodes.Count;
        node._children = childrenIdStart;
        node._massShapes.Clear();
        _nodes[nodeId] = node;
        var childNodes = node.Subdivide();
        _nodes.AddRange(childNodes);
        return childrenIdStart;
    }

    private void Clear()
    {
       _nodes.Clear();
       _nodes.Add(new Node(_center, _size, 0));
    }

    public void QueryShapes(in BoundingBox area, HashSet<MassShape> found)
    {
        foreach (var node in _nodes)
        {
            if (!node.IsLeaf)
            {
                continue;
            }
            if (!CheckCollisionBoxes(node._boundingBox, area))
            {
                continue;
            }
            foreach (var shape in node._massShapes)
            {
                found.Add(shape);
            }
        }
    }

    public void QueryPoints(in BoundingBox area, HashSet<PointMass> found)
    {
        found ??= new();
        foreach (var node in _nodes)
        {
            if (!node.IsLeaf)
            {
                continue;
            }
            if (!CheckCollisionBoxes(node._boundingBox, area))
            {
                continue;
            }
            foreach (var shape in node._massShapes)
            {
                foreach (var point in shape._points)
                {
                    found.Add(point);
                }
            }
        }
    }

    public void Draw()
    {
        for (int i = 0; i < _nodes.Count; i++)
        {
            _nodes[i].Draw();
        }
    }

    public static void ThreadUpdate(object _ctx)
    {
        Context ctx = (Context) _ctx;
        for (;;)
        {
            ctx.QuadTree.PauseEvent.Wait();
            Thread.Sleep(Constants.QuadTreeUpdateMs);
            ctx.QuadTree.Update(ctx);
        }
    }

    private struct Node
    {
        public BoundingBox _boundingBox;
        public int _children;
        public int _depth;
        public readonly List<MassShape> _massShapes;

        private readonly Vector2 _center;
        private readonly Vector2 _size;

        public readonly bool IsLeaf => _children == 0;

        public Node(Vector2 center, Vector2 size, int depth)
        {
            _children = 0;
            _boundingBox = new BoundingBox(new(center.X - size.X * 0.5f, center.Y - size.Y * 0.5f, 0f), new(center.X + size.X * 0.5f, center.Y + size.Y * 0.5f, 0f));
            _massShapes = new List<MassShape>();
            _center = center;
            _size = size;
            _depth = depth;
        }

        public readonly Node[] Subdivide()
        {
            var nodes = new Node[4];
            for (int i = 0; i < 4; i++)
            {
                // 00 | 01
                // 10 | 11
                Vector2 newSize = _size * 0.5f;
                Vector2 newCenter = new(
                    _center.X + ((i & 1) - 0.5f) * newSize.X,
                    _center.Y + ((i >> 1) - 0.5f) * newSize.Y
                );
                nodes[i] = new Node(newCenter, newSize, _depth + 1);
            }
            return nodes;
        }

        public readonly void Draw()
        {
            if (!IsLeaf)
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
        }
    }
}

