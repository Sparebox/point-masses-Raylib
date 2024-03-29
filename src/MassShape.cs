using System.Numerics;
using Raylib_cs;
using Sim;
using static Raylib_cs.Raylib;

namespace Physics;

public class MassShape
{
    private static int _idCounter;
    private const float SpringDamping = 5e4f;

    public readonly int _id;

    public Vector2 TotalVisForce
    {
        get
        {
            return _points.Aggregate(
                new Vector2(),
                (totalVisForce, p) => totalVisForce += p._visForce
            );
        }
    }

    public float Mass { get { return _points.Select(p => p.Mass).Sum(); } }

    public Vector2 CenterOfMass
    {
        get
        {
            return _points.Aggregate(
                new Vector2(), 
                (centerOfMass, p) => centerOfMass += p.Mass * p.Pos, 
                centerOfMass => centerOfMass / Mass
            );
        }
    }

    
    // pseudo 2D volume a.k.a. area
    private float Volume
    {
        get
        {
            float area = 0f;
            for (int i = 0; i < _points.Count; i++)
            {
                PointMass p1 = _points[i];
                PointMass p2 = _points[(i + 1) % _points.Count];
                area += (p1.Pos.Y + p2.Pos.Y) * (p1.Pos.X - p2.Pos.X);
            }
            return area / 2f;
        }
    }
    
    public List<Constraint> _constraints;
    public List<PointMass> _points;
    private readonly Context _context;
    private readonly bool _inflated;
    private PressureVis _pressureVis;

    public MassShape(Context context, bool inflated) 
    {
        _context = context;
        _inflated = inflated;
        _id = _idCounter++;
    }

    // Copy constructor
    public MassShape(in MassShape shape)
    {
        _context = shape._context;
        _inflated = shape._inflated;
        _id = _idCounter++;
        _points = new();
        _constraints = new();
        foreach (var p in shape._points)
        {
            _points.Add(new PointMass(p));
        }
        foreach (var c in shape._constraints)
        {
            Constraint copyConstraint = null;
            if (c.GetType() == typeof(SpringConstraint))
            {
                copyConstraint = new SpringConstraint((SpringConstraint) c);
            }
            if (c.GetType() == typeof(RigidConstraint))
            {
                copyConstraint = new RigidConstraint((RigidConstraint) c);
            }
            copyConstraint.PointA = _points.Where(p => p == copyConstraint.PointA).First();
            copyConstraint.PointB = _points.Where(p => p == copyConstraint.PointB).First();
            _constraints.Add(copyConstraint);
        }
    }

    public void Update(float timeStep)
    {
        foreach (Constraint c in _constraints)
        {
            c.Update();
        }
        foreach (PointMass p in _points)
        {
            p.Update(timeStep);
        }
        if (_inflated)
        {
            Inflate(5e6f);
        }
    }

    public void Draw()
    {
        foreach (Constraint c in _constraints)
        {
            c.Draw();
        }
        foreach (PointMass p in _points)
        {
            p.Draw();
        }
        if (_context._drawAABBS)
        {
            BoundingBox AABB = GetAABB();
            DrawRectangleLines((int) AABB.Min.X, (int) AABB.Min.Y, (int) (AABB.Max.X - AABB.Min.X), (int) (AABB.Max.Y - AABB.Min.Y), Color.Red);
        }
        if (_context._drawForces)
        {
            if (_inflated && _pressureVis._lines != null)
            {
                // Draw pressure forces acting on normals
                foreach (VisLine line in _pressureVis._lines)
                {
                    DrawLine((int) line._start.X, (int) line._start.Y, (int) line._end.X, (int) line._end.Y, Color.Magenta);
                }
            }
            Vector2 COM = CenterOfMass;
            Vector2 totalVisForce = TotalVisForce;
            Utils.Graphic.DrawArrow(COM, COM + totalVisForce * 1e-2f, Color.Magenta);
        }
    }

    public void ApplyForce(in Vector2 force)
    {
        foreach (PointMass p in _points)
        {
            p.ApplyForce(force);
        }
    }

    public void ApplyForceCOM(in Vector2 force)
    {
        foreach (PointMass p in _points)
        {
            p.ApplyForce(p.Mass * force);
        }
    }

    public void Move(in Vector2 translation)
    {
        foreach (PointMass p in _points)
        {
            p.Pos += translation;
        }
    }

    private void Inflate(float gasAmount)
    {
        for (int i = 0; i < _points.Count; i++)
        {
            PointMass p1 = _points[i];
            PointMass p2 = _points[(i + 1) % _points.Count];
            Vector2 P1ToP2 = p2.Pos - p1.Pos;
            float faceLength = P1ToP2.Length();
            Vector2 normal = new(P1ToP2.Y, -P1ToP2.X);
            normal /= faceLength;
            Vector2 force = faceLength * gasAmount / Volume / 2f * normal;
            p1.ApplyForce(force);
            p2.ApplyForce(force);
            if (_context._drawForces)
            {   
                _pressureVis._lines ??= new VisLine[_points.Count];
                if (_pressureVis._lines.Length != _points.Count)
                {
                    // Update line count since points changed
                    _pressureVis._lines = new VisLine[_points.Count];
                }
                VisLine line = new();
                line._start.X = p1.Pos.X + 0.5f * P1ToP2.X;
                line._start.Y = p1.Pos.Y + 0.5f * P1ToP2.Y;
                line._end.X = p1.Pos.X + 0.5f * P1ToP2.X + force.X * PressureVis.VisForceMult;
                line._end.Y = p1.Pos.Y + 0.5f * P1ToP2.Y + force.Y * PressureVis.VisForceMult;
                _pressureVis._lines[i] = line;
            }
        }
    }

    public BoundingBox GetAABB()
    {
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = 0f;
        float maxY = 0f;
        foreach (var p in _points)
        {
            if (p.Pos.X - p.Radius <= minX)
            {
                minX = p.Pos.X - p.Radius;
            }
            if (p.Pos.Y - p.Radius <= minY)
            {
                minY = p.Pos.Y - p.Radius;
            }
            if (p.Pos.X + p.Radius >= maxX)
            {
                maxX = p.Pos.X + p.Radius;
            }
            if (p.Pos.Y + p.Radius >= maxY)
            {
                maxY = p.Pos.Y + p.Radius;
            }
        }
        return new BoundingBox()
        {
            Max = new(maxX, maxY, 0f),
            Min = new(minX, minY, 0f)
        };
    }

    public static bool CheckPointCollision(MassShape shape, in Vector2 point)
    {
        BoundingBox aabb = shape.GetAABB();
        Rectangle aabbRect = new()
        {
            Position = new(aabb.Min.X, aabb.Min.Y),
            Width = aabb.Max.X - aabb.Min.X,
            Height = aabb.Max.Y - aabb.Min.Y
        };
        if (!CheckCollisionPointRec(point, aabbRect))
        {
            return false;
        }
        Vector2 outsidePoint = new(point.X + aabb.Max.X - point.X + 5f, point.Y);
        int collisionCount = 0;
        for (int i = 0; i < shape._points.Count; i++)
        {
            Vector2 startPos = shape._points[i].Pos;
            Vector2 endPos = shape._points[(i + 1) % shape._points.Count].Pos;
            Vector2 collisionPoint = new();
            bool hadCollision = CheckCollisionLines(startPos, endPos, point, outsidePoint, ref collisionPoint);
            if (hadCollision)
            {
                collisionCount++;
            }
        }
        if (collisionCount > 0 && collisionCount % 2 != 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static void SolveCollisions(Context context)
    {
        foreach (var shapeA in context.MassShapes)
        {
            foreach (var point in shapeA._points)
            {
                foreach (var shapeB in context.MassShapes)
                {
                    if (shapeA == shapeB)
                    {
                        continue;
                    }
                    if (CheckPointCollision(shapeB, point.Pos))
                    {
                        HandleCollision(shapeB, point);
                    }
                }
            }
        }
    }

    private static void HandleCollision(MassShape shape, PointMass pointMass)
    {
        PointMass closestA = null;
        PointMass closestB = null;
        float closestDistSq = float.MaxValue;
        Vector2 closestPoint = new();
        for (int i = 0; i < shape._points.Count; i++)
        {
            PointMass lineStart = shape._points[i];
            PointMass lineEnd = shape._points[(i + 1) % shape._points.Count];
            Vector2 pointOnLine = Utils.Geometry.ClosestPointOnLine(lineStart.Pos, lineEnd.Pos, pointMass.Pos);
            float distSq = Vector2.DistanceSquared(pointOnLine, pointMass.Pos);
            if (distSq < closestDistSq)
            {
                closestA = lineStart;
                closestB = lineEnd;
                closestDistSq = distSq;
                closestPoint = pointOnLine;
            }
        }
        Vector2 pointToClosest = closestPoint - pointMass.Pos;
        if (pointToClosest.LengthSquared() == 0f)
        {
            return;
        }
        float totalOffset = pointToClosest.Length() / 2f * 0.9f; // 0.9 relaxation factor
        float lineLen = Vector2.Distance(closestA.Pos, closestB.Pos);
        if (lineLen == 0f)
        {
            return;
        }
        float distToB = Vector2.Distance(closestPoint, closestB.Pos);
        float aOffset = distToB / lineLen * totalOffset;
        float bOffset = totalOffset - aOffset;
        pointToClosest = Vector2.Normalize(pointToClosest);
        Vector2 avgVel = (closestA.Vel + closestB.Vel) / 2f;
        Vector2 relVel = pointMass.Vel - avgVel;
        pointMass.Pos += totalOffset * pointToClosest;
        closestA.Pos += aOffset * -pointToClosest;
        closestB.Pos += bOffset * -pointToClosest;
        // Apply impulse
        float combinedMass = closestA.Mass + closestB.Mass;
        float impulseMag = -(1f + PointMass.RestitutionCoeff) * Vector2.Dot(relVel, pointToClosest) / (1f / combinedMass + 1f / pointMass.Mass);
        Vector2 impulse = impulseMag * pointToClosest;
        pointMass.Vel = impulse / pointMass.Mass;
        closestA.Vel = -impulse / combinedMass / 2f;
        closestB.Vel = -impulse / combinedMass / 2f;
    }

    public static bool operator == (MassShape a, MassShape b)
    {
        return a._id == b._id;
    }

    public static bool operator != (MassShape a, MassShape b)
    {
        return a._id != b._id;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || !obj.GetType().Equals(typeof(MassShape)))
        {
            return false;
        }
        return _id == ((MassShape) obj)._id;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    private struct PressureVis
    {
        public const float VisForceMult = 1e-3f;
        public VisLine[] _lines;
    }

    private struct VisLine
    {
        public Vector2 _start;
        public Vector2 _end;
    }

    // Shape constructors

    public static MassShape SoftBall(float x, float y, float radius, float mass, int res, float stiffness, Context context)
    {
        float angle = (float) Math.PI / 2f;
        MassShape s = new(context, true)
        {
            _points = new(),
            _constraints = new()
        };
        // Points
        for (int i = 0; i < res; i++)
        {
            float x0 = radius * (float) Math.Cos(angle);
            float y0 = radius * (float) Math.Sin(angle);
            s._points.Add(new(x0 + x, y0 + y, mass / res, false, context));
            angle += 2f * (float) Math.PI / res;
        }
        // Constraints
        for (int i = 0; i < res; i++)
        {
            s._constraints.Add(new SpringConstraint(s._points[i], s._points[(i + 1) % res], stiffness, SpringDamping));
        }
        return s;
    }

    public static MassShape HardBall(float x, float y, float radius, float mass, int res, Context context)
    {
        float angle = (float) Math.PI / 2f;
        MassShape s = new(context, false)
        {
            _points = new(),
            _constraints = new()
        };
        // Points
        for (int i = 0; i < res; i++)
        {
            float x0 = radius * (float) Math.Cos(angle);
            float y0 = radius * (float) Math.Sin(angle);
            s._points.Add(new(x0 + x, y0 + y, mass / res, false, context));
            angle += 2f * (float) Math.PI / res;
        }
        // Constraints
        List<int> visitedPoints = new();
        for (int i = 0; i < res; i++)
        {
            s._constraints.Add(new RigidConstraint(s._points[i], s._points[(i + 1) % res]));
            if (!visitedPoints.Contains(i))
            {
                int nextIndex = res % 2 == 0 ? (i + res / 2 - 1) % res : (i + res / 2) % res;
                s._constraints.Add(new RigidConstraint(s._points[i], s._points[nextIndex]));
                visitedPoints.Add(i);
            }
        }
        
        return s;
    }

    public static MassShape Chain(float x0, float y0, float x1, float y1, float mass, int res, (bool, bool) pins, Context context)
    {
        MassShape c = new(context, false)
        {
            _points = new(),
            _constraints = new()
        };
        Vector2 start = new(x0, y0);
        Vector2 end = new(x1, y1);
        float len = Vector2.Distance(start, end);
        float spacing = len / (res - 1);
        Vector2 dir = (end - start) / len;
        // Points
        for (int i = 0; i < res; i++)
        {
            bool pinned;
            if (pins.Item1 && pins.Item2)
            {
                pinned = i == 0 || i == res - 1;
            } else if (pins.Item1)
            {
                pinned = i == 0;
            } else if (pins.Item2)
            {
                pinned = i == res - 1;
            } else
            {
                pinned = false;
            }
            c._points.Add(new(start.X + i * spacing * dir.X, start.Y + i * spacing * dir.Y, mass / res, pinned, context));
        }
        // Constraints
        for (int i = 0; i < res - 1; i++)
        {
            c._constraints.Add(new RigidConstraint(c._points[i], c._points[i + 1]));
        }
        return c;
    }

    public static MassShape Cloth(float x, float y, float width, float height, float mass, int res, float stiffness, Context context)
    {
        float pixelsPerConstraintW = width / res;
        float pixelsPerConstraintH = height / res;
        MassShape c = new(context, false)
        {
            _points = new(),
            _constraints = new()
        };
        // Points
        for (int col = 0; col < res; col++)
        {
            for (int row = 0; row < res; row++)
            {
                bool pinned = (col == 0 || col == res - 1) && row == 0;
                c._points.Add(new(x + col * pixelsPerConstraintW, y + row * pixelsPerConstraintH, mass, pinned, context));
            }
        }
        // Constraints
        for (int col = 0; col < res; col++)
        {
            for (int row = 0; row < res; row++)
            {
                if (col != res - 1)
                {
                    if (stiffness == 0f)
                    {
                        c._constraints.Add(new RigidConstraint(c._points[col * res + row], c._points[(col + 1) * res + row]));
                    } else
                    {
                        c._constraints.Add(new SpringConstraint(c._points[col * res + row], c._points[(col + 1) * res + row], stiffness, SpringDamping));
                    }
                }
                if (row != res - 1)
                {
                    if (stiffness == 0f)
                    {
                        c._constraints.Add(new RigidConstraint(c._points[col * res + row], c._points[col * res + row + 1]));
                    } else 
                    {
                        c._constraints.Add(new SpringConstraint(c._points[col * res + row], c._points[col * res + row + 1], stiffness, SpringDamping));
                    }
                }
            }
        }
        return c;
    }

    public static MassShape Pendulum(float x, float y, float length, float mass, int order, Context context)
    {
        if (order < 1)
        {
            return null;
        }
        MassShape c = new(context, false)
        {
            _points = new(),
            _constraints = new()
        };

        // Points
        for (int i = 0; i < order + 1; i++)
        {
            c._points.Add(new PointMass(x, y, mass / (order + 1), i == 0, context));
            y += length / (order + 1);
        }
        // Constraints
        for (int i = 0; i < order; i++)
        {
            c._constraints.Add(new RigidConstraint(c._points[i], c._points[i + 1]));
        }
        return c;
    }

    public static MassShape Box(float x, float y, float size, float mass, Context context)
    {
        MassShape c = new(context, false)
        {
            _points = new() 
            {
                new(x - size / 2f, y - size / 2f, mass / 4f, false, context),
                new(x - size / 2f, y + size / 2f, mass / 4f, false, context),
                new(x + size / 2f, y + size / 2f, mass / 4f, false, context),
                new(x + size / 2f, y - size / 2f, mass / 4f, false, context)
            },
            _constraints = new()
        };
        c._constraints.Add(new RigidConstraint(c._points[0], c._points[1]));
        c._constraints.Add(new RigidConstraint(c._points[1], c._points[2]));
        c._constraints.Add(new RigidConstraint(c._points[2], c._points[3]));
        c._constraints.Add(new RigidConstraint(c._points[3], c._points[0]));
        c._constraints.Add(new RigidConstraint(c._points[0], c._points[2]));

        return c;
    }

    public static MassShape SoftBox(float x, float y, float size, float mass, float stiffness, Context context)
    {
        MassShape c = new(context, false)
        {
            _points = new() 
            {
                new(x - size / 2f, y - size / 2f, mass / 4f, false, context),
                new(x - size / 2f, y + size / 2f, mass / 4f, false, context),
                new(x + size / 2f, y + size / 2f, mass / 4f, false, context),
                new(x + size / 2f, y - size / 2f, mass / 4f, false, context)
            },
            _constraints = new()
        };
        c._constraints.Add(new SpringConstraint(c._points[0], c._points[1], stiffness, SpringDamping));
        c._constraints.Add(new SpringConstraint(c._points[1], c._points[2], stiffness, SpringDamping));
        c._constraints.Add(new SpringConstraint(c._points[2], c._points[3], stiffness, SpringDamping));
        c._constraints.Add(new SpringConstraint(c._points[3], c._points[0], stiffness, SpringDamping));
        c._constraints.Add(new SpringConstraint(c._points[0], c._points[2], stiffness, SpringDamping));
        return c;
    }

    public static MassShape Particle(float x, float y, float mass, Context context)
    {
        MassShape c = new(context, false)
        {
            _points = new() { new(x, y, mass, false, context) },
            _constraints = new()
        };
        return c;
    }
}