using System.Numerics;
using Sim;

namespace Physics;

public partial class MassShape
{
    public static MassShape SoftBall(float x, float y, float radius, float mass, int res, float stiffness, float gasAmount, Context context)
    {
        float angle = MathF.PI / 2f;
        MassShape s = new(context, true)
        {
            _gasAmount = gasAmount
        };
        // Points
        for (int i = 0; i < res; i++)
        {
            float x0 = radius * MathF.Cos(angle);
            float y0 = radius * MathF.Sin(angle);
            s._points.Add(new(x0 + x, y0 + y, mass / res, false, context));
            angle += 2f * MathF.PI / res;
        }
        // Constraints
        for (int i = 0; i < res; i++)
        {
            s._constraints.Add(new DistanceConstraint(s._points[i], s._points[(i + 1) % res], stiffness, context));
        }
        return s;
    }

    public static MassShape HardBall(float x, float y, float radius, float mass, int res, Context context)
    {
        float angle = MathF.PI / 2f;
        MassShape s = new(context, false);
        // Points
        for (int i = 0; i < res; i++)
        {
            float x0 = radius * MathF.Cos(angle);
            float y0 = radius * MathF.Sin(angle);
            s._points.Add(new(x0 + x, y0 + y, mass / res, false, context));
            angle += 2f * MathF.PI / res;
        }
        // Constraints
        HashSet<int> visitedPoints = new();
        for (int i = 0; i < res; i++)
        {
            s._constraints.Add(new DistanceConstraint(s._points[i], s._points[(i + 1) % res], 1f, context));
            if (!visitedPoints.Contains(i))
            {
                int nextIndex = res % 2 == 0 ? (i + res / 2 - 1) % res : (i + res / 2) % res;
                s._constraints.Add(new DistanceConstraint(s._points[i], s._points[nextIndex], 1f, context));
                visitedPoints.Add(i);
            }
        }
        
        return s;
    }

    public static MassShape Chain(float x0, float y0, float x1, float y1, float mass, int res, (bool, bool) pins, Context context)
    {
        MassShape c = new(context, false);
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
            c._constraints.Add(new DistanceConstraint(c._points[i], c._points[i + 1], 1f, context));
        }
        return c;
    }

    public static MassShape Cloth(float x, float y, float width, float height, float mass, int res, float stiffness, Context context)
    {
        float metersPerConstraintW = width / res;
        float metersPerConstraintH = height / res;
        MassShape c = new(context, false);
        // Points
        for (int col = 0; col < res; col++)
        {
            for (int row = 0; row < res; row++)
            {
                bool pinned = (col == 0 || col == res - 1) && row == 0;
                c._points.Add(new(x + col * metersPerConstraintW, y + row * metersPerConstraintH, mass, pinned, context));
            }
        }
        // Constraints
        for (int col = 0; col < res; col++)
        {
            for (int row = 0; row < res; row++)
            {
                if (col != res - 1)
                {
                    c._constraints.Add(new DistanceConstraint(c._points[col * res + row], c._points[(col + 1) * res + row], stiffness, context));
                }
                if (row != res - 1)
                {
                    c._constraints.Add(new DistanceConstraint(c._points[col * res + row], c._points[col * res + row + 1], stiffness, context));
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
        MassShape c = new(context, false);
        // Points
        for (int i = 0; i < order + 1; i++)
        {
            c._points.Add(new PointMass(x, y, mass / (order + 1), i == 0, context));
            y += length / (order + 1);
        }
        // Constraints
        for (int i = 0; i < order; i++)
        {
            c._constraints.Add(new DistanceConstraint(c._points[i], c._points[i + 1], 1f, context));
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
        c._constraints.Add(new DistanceConstraint(c._points[0], c._points[1], 1f, context));
        c._constraints.Add(new DistanceConstraint(c._points[1], c._points[2], 1f, context));
        c._constraints.Add(new DistanceConstraint(c._points[2], c._points[3], 1f, context));
        c._constraints.Add(new DistanceConstraint(c._points[3], c._points[0], 1f, context));
        c._constraints.Add(new DistanceConstraint(c._points[0], c._points[2], 1f, context));
        return c;
    }

    public static MassShape SoftBox(float x, float y, float size, float mass, float stiffness, Context context)
    {
        MassShape c = new(context, false)
        {
            _points =  
            {
                new(x - size / 2f, y - size / 2f, mass / 4f, false, context),
                new(x - size / 2f, y + size / 2f, mass / 4f, false, context),
                new(x + size / 2f, y + size / 2f, mass / 4f, false, context),
                new(x + size / 2f, y - size / 2f, mass / 4f, false, context)
            }
        };
        c._constraints.Add(new DistanceConstraint(c._points[0], c._points[1], stiffness, context));
        c._constraints.Add(new DistanceConstraint(c._points[1], c._points[2], stiffness, context));
        c._constraints.Add(new DistanceConstraint(c._points[2], c._points[3], stiffness, context));
        c._constraints.Add(new DistanceConstraint(c._points[3], c._points[0], stiffness, context));
        c._constraints.Add(new DistanceConstraint(c._points[0], c._points[2], stiffness, context));
        return c;
    }

    public static MassShape Particle(float x, float y, float mass, Context context)
    {
        MassShape c = new(context, false)
        {
            _points = { new(x, y, mass, false, context) }
        };
        return c;
    }
}
