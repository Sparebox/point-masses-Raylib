using System.Numerics;
using Collision;
using Raylib_cs;
using Sim;
using static Raylib_cs.Raylib;

namespace Physics;

public class MassShape
{
    private const float SpringDamping = 5e4f;

    public List<Constraint> _constraints;
    public List<PointMass> _points;

    private readonly Context _context;
    private bool _inflated;

    public MassShape(Context context) 
    {
        _context = context;
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
    }

    public void ApplyForce(in Vector2 force)
    {
        foreach (PointMass p in _points)
        {
            p.ApplyForce(force);
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
            Vector2 force = faceLength * gasAmount / CalculateVolume() / 2f * normal;
            if (_context.DrawForces)
            {
                DrawLine(
                    (int) (p1.Pos.X + 0.5f * P1ToP2.X), 
                    (int) (p1.Pos.Y + 0.5f * P1ToP2.Y), 
                    (int) (p1.Pos.X + 0.5f * P1ToP2.X + force.X / 100f), 
                    (int) (p1.Pos.Y + 0.5f * P1ToP2.Y + force.Y / 100f), 
                    Color.WHITE
                );
            }
            p1.ApplyForce(force);
            p2.ApplyForce(force);
        }
    }

    // pseudo 2D volume a.k.a. area
    private float CalculateVolume()
    {
        float area = 0f;
        Vector2 centerOfMass = CenterOfMass();
        for (int i = 0; i < _points.Count; i++)
        {
            PointMass p1 = _points[i];
            PointMass p2 = _points[(i + 1) % _points.Count];
            float baseLength = Vector2.Distance(p1.Pos, p2.Pos); 
            Vector2 closestPoint = Utils.Geometry.ClosestPointOnLine(p1.Pos, p2.Pos, centerOfMass);
            float height = Vector2.Distance(closestPoint, centerOfMass);
            area += 0.5f * baseLength * height;
        }
        return area;
    }

    public Vector2 CenterOfMass()
    {
        Vector2 centerOfMass = new();
        foreach (var p in _points)
        {
            centerOfMass += p.Pos;
        }
        centerOfMass /= _points.Count;
        return centerOfMass;
    }

    // Shape constructors

    public static MassShape Ball(float x, float y, float radius, float mass, int res, float stiffness, Context context)
    {
        float angle = (float) Math.PI / 2f;
        MassShape s = new(context)
        {
            _inflated = true,
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

    public static MassShape Chain(float x0, float y0, float x1, float y1, float mass, int res, (bool, bool) pins, Context context)
    {
        MassShape c = new(context)
        {
            _inflated = false,
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
        MassShape c = new(context)
        {
            _inflated = false,
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
}