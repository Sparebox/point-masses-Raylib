using System.Numerics;
using Collision;
using Raylib_cs;
using Sim;
using static Raylib_cs.Raylib;

namespace Physics;

public class MassShape
{
    private const float SpringDamping = 5e4f;

    public Constraint[] _constraints;
    public PointMass[] _points;

    private readonly Context _context;
    private bool _inflated;

    public MassShape(in Context context) 
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
        for (int i = 0; i < _points.Length; i++)
        {
            PointMass p1 = _points[i];
            PointMass p2 = _points[(i + 1) % _points.Length];
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
        Vector2 centerOfMass = CalculateCenterOfMass();
        for (int i = 0; i < _points.Length; i++)
        {
            PointMass p1 = _points[i];
            PointMass p2 = _points[(i + 1) % _points.Length];
            float baseLength = Vector2.Distance(p1.Pos, p2.Pos); 
            Vector2 closestPoint = Utils.Geometry.ClosestPointOnLine(p1.Pos, p2.Pos, centerOfMass);
            float height = Vector2.Distance(closestPoint, centerOfMass);
            area += 0.5f * baseLength * height;
        }
        return area;
    }

    public Vector2 CalculateCenterOfMass()
    {
        Vector2 centerOfMass = new();
        foreach (var p in _points)
        {
            centerOfMass += p.Pos;
        }
        centerOfMass /= _points.Length;
        return centerOfMass;
    }

    // Shape constructors

    public static MassShape Ball(float x, float y, float radius, float mass, int res, float stiffness, in Context context)
    {
        float angle = (float) Math.PI / 2f;
        MassShape s = new(context)
        {
            _inflated = true,
            _points = new PointMass[res],
        };
        List<Constraint> constraints = new();
        for (int i = 0; i < res; i++)
        {
            float x0 = radius * (float) Math.Cos(angle);
            float y0 = radius * (float) Math.Sin(angle);
            s._points[i] = new(x0 + x, y0 + y, mass / res, false, context);
            angle += 2f * (float) Math.PI / res;
        }
        for (int i = 0; i < res; i++)
        {
            constraints.Add(new SpringConstraint(s._points[i], s._points[(i + 1) % res], stiffness, SpringDamping));
        }
        s._constraints = constraints.ToArray();
        return s;
    }

    public static MassShape Chain(float x0, float y0, float x1, float y1, float mass, int res, (bool, bool) pins, in Context context)
    {
        MassShape c = new(context)
        {
            _inflated = false,
            _points = new PointMass[res],
            _constraints = new Constraint[res - 1]
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
            c._points[i] = new(start.X + i * spacing * dir.X, start.Y + i * spacing * dir.Y, mass / res, pinned, context);
        }
        // Constraints
        for (int i = 0; i < res - 1; i++)
        {
            c._constraints[i] = new RigidConstraint(c._points[i], c._points[i + 1]);
        }
        return c;
    }

    public static MassShape Cloth(float x, float y, float width, float height, float mass, int res, float stiffness, in Context context)
    {
        int numberOfConstraints = 2 * res * res - 2 * res;
        float pixelsPerConstraintW = width / res;
        float pixelsPerConstraintH = height / res;
        MassShape c = new(context)
        {
            _inflated = false,
            _points = new PointMass[res * res],
            _constraints = new Constraint[numberOfConstraints]
        };
        // Points
        for (int col = 0; col < res; col++)
        {
            for (int row = 0; row < res; row++)
            {
                bool pinned = row == 0;
                c._points[col * res + row] = new(x + col * pixelsPerConstraintW, y + row * pixelsPerConstraintH, mass, pinned, context);
            }
        }
        // Constraints
        int constraintIndex = 0;
        for (int col = 0; col < res; col++)
        {
            for (int row = 0; row < res; row++)
            {
                if (col != res - 1)
                {
                    c._constraints[constraintIndex++] = new SpringConstraint(c._points[col * res + row], c._points[(col + 1) * res + row], stiffness, SpringDamping);
                }
                if (row != res - 1)
                {
                    c._constraints[constraintIndex++] = new SpringConstraint(c._points[col * res + row], c._points[col * res + row + 1], stiffness, SpringDamping);
                }
            }
        }
        return c;
    }
}