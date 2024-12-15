using System.Numerics;
using PointMasses.Physics;
using PointMasses.Sim;

namespace PointMasses.Entities;

public partial class MassShape
{
    public static MassShape SoftBall(float x, float y, float radius, float mass, int res, float stiffness, float gasAmount, Context ctx)
    {
        float angle = MathF.PI * 0.5f;
        MassShape s = new(ctx, true)
        {
            _gasAmount = gasAmount
        };
        // Points
        for (int i = 0; i < res; i++)
        {
            float x0 = radius * MathF.Cos(angle);
            float y0 = radius * MathF.Sin(angle);
            s._points.Add(new(x0 + x, y0 + y, mass / res, false, ctx));
            angle += 2f * MathF.PI / res;
        }
        // Constraints
        for (int i = 0; i < res; i++)
        {
            s._constraints.Add(new DistanceConstraint(s._points[i], s._points[(i + 1) % res], stiffness, ctx));
        }
        return s;
    }

    public static ShapePreview SoftBallPreview(float x, float y, float radius, float mass, int res, Context ctx)
    {
        float angle = MathF.PI * 0.5f;
        ShapePreview preview = new();
        // Points
        for (int i = 0; i < res; i++)
        {
            float x0 = radius * MathF.Cos(angle);
            float y0 = radius * MathF.Sin(angle);
            preview._points.Add(new(x0 + x, y0 + y, mass / res, false, ctx, false));
            angle += 2f * MathF.PI / res;
        }
        // Constraints
        for (int i = 0; i < res; i++)
        {
            preview._constraints.Add(new DistanceConstraint(preview._points[i], preview._points[(i + 1) % res], 0f, ctx, incrementId: false));
        }
        return preview;
    }

    public static MassShape HardBall(float x, float y, float radius, float mass, int res, Context ctx)
    {
        float angle = MathF.PI * 0.5f;
        MassShape s = new(ctx, false);
        // Points
        for (int i = 0; i < res; i++)
        {
            float x0 = radius * MathF.Cos(angle);
            float y0 = radius * MathF.Sin(angle);
            s._points.Add(new(x0 + x, y0 + y, mass / res, false, ctx));
            angle += 2f * MathF.PI / res;
        }
        // Constraints
        HashSet<int> visitedPoints = new();
        for (int i = 0; i < res; i++)
        {
            s._constraints.Add(new DistanceConstraint(s._points[i], s._points[(i + 1) % res], 1f, ctx));
            if (!visitedPoints.Contains(i))
            {
                int nextIndex = res % 2 == 0 ? (i + res / 2 - 1) % res : (i + res / 2) % res;
                s._constraints.Add(new DistanceConstraint(s._points[i], s._points[nextIndex], 1f, ctx));
                visitedPoints.Add(i);
            }
        }
        
        return s;
    }

    public static ShapePreview HardBallPreview(float x, float y, float radius, float mass, int res, Context ctx)
    {
        float angle = MathF.PI * 0.5f;
        ShapePreview preview = new();
        // Points
        for (int i = 0; i < res; i++)
        {
            float x0 = radius * MathF.Cos(angle);
            float y0 = radius * MathF.Sin(angle);
            preview._points.Add(new(x0 + x, y0 + y, mass / res, false, ctx, false));
            angle += 2f * MathF.PI / res;
        }
        // Constraints
        HashSet<int> visitedPoints = new();
        for (int i = 0; i < res; i++)
        {
            preview._constraints.Add(new DistanceConstraint(preview._points[i], preview._points[(i + 1) % res], 1f, ctx, incrementId: false));
            if (!visitedPoints.Contains(i))
            {
                int nextIndex = res % 2 == 0 ? (i + res / 2 - 1) % res : (i + res / 2) % res;
                preview._constraints.Add(new DistanceConstraint(preview._points[i], preview._points[nextIndex], 1f, ctx, incrementId: false));
                visitedPoints.Add(i);
            }
        }
        return preview;
    }

    public static MassShape Chain(float x0, float y0, float x1, float y1, float mass, float stiffness, float lengthMult, int res, (bool, bool) pins, Context ctx)
    {
        MassShape c = new(ctx, false);
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
            c._points.Add(new(start.X + i * spacing * dir.X, start.Y + i * spacing * dir.Y, mass / res, pinned, ctx));
        }
        // Constraints
        for (int i = 0; i < res - 1; i++)
        {
            c._constraints.Add(new DistanceConstraint(c._points[i], c._points[i + 1], stiffness, ctx, lengthMult));
        }
        return c;
    }

    public static MassShape Cloth(float x, float y, float width, float height, float mass, int res, float stiffness, bool crossHatched, Context ctx)
    {
        float metersPerConstraintW = width / res;
        float metersPerConstraintH = height / res;
        MassShape c = new(ctx, false);
        // Points
        for (int col = 0; col < res; col++)
        {
            for (int row = 0; row < res; row++)
            {
                bool pinned = (col == 0 || col == res - 1) && row == 0;
                c._points.Add(new(x + col * metersPerConstraintW, y + row * metersPerConstraintH, mass, pinned, ctx));
            }
        }
        // Constraints
        for (int col = 0; col < res; col++)
        {
            for (int row = 0; row < res; row++)
            {
                if (col != res - 1) // Not at last column
                {
                    c._constraints.Add(new DistanceConstraint(c._points[col * res + row], c._points[(col + 1) * res + row], stiffness, ctx));
                }
                if (row != res - 1) // Not at last row
                {
                    c._constraints.Add(new DistanceConstraint(c._points[col * res + row], c._points[col * res + row + 1], stiffness, ctx));
                }
                if (crossHatched && col != res - 1 && row != res - 1) // Not at last column or row
                {
                    c._constraints.Add(new DistanceConstraint(c._points[col * res + row], c._points[(col + 1) * res + row + 1], stiffness, ctx));
                }
                if (crossHatched && col != res - 1 && row != 0) // Not at lasat column and not on the first row
                {
                    c._constraints.Add(new DistanceConstraint(c._points[col * res + row], c._points[(col + 1) * res + row - 1], stiffness, ctx));
                }
            }
        }
        return c;
    }

    public static MassShape Pendulum(float x, float y, float length, float mass, int order, Context ctx)
    {
        if (order < 1)
        {
            return null;
        }
        MassShape c = new(ctx, false);
        // Points
        for (int i = 0; i < order + 1; i++)
        {
            c._points.Add(new PointMass(x, y, mass / (order + 1), i == 0, ctx));
            y += length / (order + 1);
        }
        // Constraints
        for (int i = 0; i < order; i++)
        {
            c._constraints.Add(new DistanceConstraint(c._points[i], c._points[i + 1], 1f, ctx));
        }
        return c;
    }

    public static MassShape Box(float x, float y, float size, float mass, Context ctx)
    {
        MassShape c = new(ctx, false)
        {
            _points = new() 
            {
                new(x - size * 0.5f, y - size * 0.5f, mass * 0.25f, false, ctx),
                new(x - size * 0.5f, y + size * 0.5f, mass * 0.25f, false, ctx),
                new(x + size * 0.5f, y + size * 0.5f, mass * 0.25f, false, ctx),
                new(x + size * 0.5f, y - size * 0.5f, mass * 0.25f, false, ctx)
            },
            _constraints = new()
        };
        c._constraints.Add(new DistanceConstraint(c._points[0], c._points[1], 1f, ctx));
        c._constraints.Add(new DistanceConstraint(c._points[1], c._points[2], 1f, ctx));
        c._constraints.Add(new DistanceConstraint(c._points[2], c._points[3], 1f, ctx));
        c._constraints.Add(new DistanceConstraint(c._points[3], c._points[0], 1f, ctx));
        c._constraints.Add(new DistanceConstraint(c._points[0], c._points[2], 1f, ctx));
        return c;
    }

    public static ShapePreview BoxPreview(float x, float y, float size, float mass, Context ctx)
    {
        ShapePreview preview = new()
        {
            _points = new() 
            {
                new(x - size * 0.5f, y - size * 0.5f, mass * 0.25f, false, ctx, false),
                new(x - size * 0.5f, y + size * 0.5f, mass * 0.25f, false, ctx, false),
                new(x + size * 0.5f, y + size * 0.5f, mass * 0.25f, false, ctx, false),
                new(x + size * 0.5f, y - size * 0.5f, mass * 0.25f, false, ctx, false)
            },
            _constraints = new()
        };
        preview._constraints.Add(new DistanceConstraint(preview._points[0], preview._points[1], 1f, ctx, incrementId: false));
        preview._constraints.Add(new DistanceConstraint(preview._points[1], preview._points[2], 1f, ctx, incrementId: false));
        preview._constraints.Add(new DistanceConstraint(preview._points[2], preview._points[3], 1f, ctx, incrementId: false));
        preview._constraints.Add(new DistanceConstraint(preview._points[3], preview._points[0], 1f, ctx, incrementId: false));
        preview._constraints.Add(new DistanceConstraint(preview._points[0], preview._points[2], 1f, ctx, incrementId: false));
        return preview;
    }

    public static MassShape SoftBox(float x, float y, float size, float mass, float stiffness, Context ctx)
    {
        MassShape c = new(ctx, false)
        {
            _points =  
            {
                new(x - size * 0.5f, y - size * 0.5f, mass * 0.25f, false, ctx),
                new(x - size * 0.5f, y + size * 0.5f, mass * 0.25f, false, ctx),
                new(x + size * 0.5f, y + size * 0.5f, mass * 0.25f, false, ctx),
                new(x + size * 0.5f, y - size * 0.5f, mass * 0.25f, false, ctx)
            }
        };
        c._constraints.Add(new DistanceConstraint(c._points[0], c._points[1], stiffness, ctx));
        c._constraints.Add(new DistanceConstraint(c._points[1], c._points[2], stiffness, ctx));
        c._constraints.Add(new DistanceConstraint(c._points[2], c._points[3], stiffness, ctx));
        c._constraints.Add(new DistanceConstraint(c._points[3], c._points[0], stiffness, ctx));
        c._constraints.Add(new DistanceConstraint(c._points[0], c._points[2], stiffness, ctx));
        return c;
    }

    public static ShapePreview SoftBoxPreview(float x, float y, float size, float mass, Context ctx)
    {
        ShapePreview preview = new()
        {
            _points =  
            {
                new(x - size * 0.5f, y - size * 0.5f, mass * 0.25f, false, ctx, false),
                new(x - size * 0.5f, y + size * 0.5f, mass * 0.25f, false, ctx, false),
                new(x + size * 0.5f, y + size * 0.5f, mass * 0.25f, false, ctx, false),
                new(x + size * 0.5f, y - size * 0.5f, mass * 0.25f, false, ctx, false)
            }
        };
        preview._constraints.Add(new DistanceConstraint(preview._points[0], preview._points[1], 0f, ctx, incrementId: false));
        preview._constraints.Add(new DistanceConstraint(preview._points[1], preview._points[2], 0f, ctx, incrementId: false));
        preview._constraints.Add(new DistanceConstraint(preview._points[2], preview._points[3], 0f, ctx, incrementId: false));
        preview._constraints.Add(new DistanceConstraint(preview._points[3], preview._points[0], 0f, ctx, incrementId: false));
        preview._constraints.Add(new DistanceConstraint(preview._points[0], preview._points[2], 0f, ctx, incrementId: false));
        return preview;
    }

    public static MassShape Particle(float x, float y, float mass, Context ctx)
    {
        MassShape c = new(ctx, false)
        {
            _points = { new(x, y, mass, false, ctx, false) }
        };
        return c;
    }

    public static ShapePreview ParticlePreview(float x, float y, float mass, Context ctx)
    {
        ShapePreview preview = new()
        {
            _points = { new(x, y, mass, false, ctx, false) }
        };
        return preview;
    }

    public struct ShapePreview
    {
        public List<Constraint> _constraints;
        public List<PointMass> _points;

        public readonly Vector2 Centroid
        {
            get
            {
                var points = _points;
                return points.Aggregate(
                    new Vector2(),
                    (centroid, p) => centroid += p.Pos,
                    centroid => centroid / points.Count
                );
            }
        }

        public ShapePreview()
        {
            _points = new();
            _constraints = new();
        }

        public readonly void Draw()
        {
            foreach (var c in _constraints)
            {
                c.Draw();
            }
            foreach (var p in _points)
            {
                p.Draw();
            }
        }

        public readonly void SetPos(in Vector2 newPos)
        {
            Vector2 translation = newPos - Centroid;
            foreach (PointMass p in _points)
            {
                p.Pos += translation;
                p.PrevPos = p.Pos;
            }
        }
    }
}
