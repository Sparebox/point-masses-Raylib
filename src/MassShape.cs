using System.Numerics;
using Collision;
using static Raylib_cs.Raylib;

namespace Physics;

public class MassShape
{
    public Constraint[] _constraints;
    public PointMass[] _points;

    private readonly List<LineCollider> _lineColliders;

    public MassShape(in List<LineCollider> lineColliders) 
    {
        _lineColliders = lineColliders;
    }

    public void Update()
    {
        foreach (Constraint c in _constraints)
        {
            c.Update();
        }
        foreach (PointMass p in _points)
        {
            p.Update(_lineColliders);
        }
        Inflate(1e7f);
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

    public static MassShape Circle(float x, float y, float radius, float mass, int res, in List<LineCollider> lineColliders)
    {
        float angle = (float) Math.PI / 2f;
        MassShape s = new(lineColliders)
        {
            _points = new PointMass[res],
        };
        List<Constraint> constraints = new();
        for (int i = 0; i < res; i++)
        {
            float x0 = radius * (float) Math.Cos(angle);
            float y0 = radius * (float) Math.Sin(angle);
            s._points[i] = new(x0 + x, y0 + y, mass / res);
            angle += 2f * (float) Math.PI / res;
        }
        for (int i = 0; i < res; i++)
        {
            constraints.Add(new RigidConstraint(s._points[i], s._points[(i + 1) % res]));
        }
        // Create internal springs for structural integrity
        // HashSet<int> visitedPoints = new();
        // for (int i = 0; i < 2; i++) {
        //     int index = i;
        //     while (true) {
        //         if (visitedPoints.Contains(index)) {
        //             break;
        //         }
        //         constraints.Add(new SpringConstraint(s._points[index], s._points[(index + 2) % res], 1e4f));
        //         visitedPoints.Add(s._points[index]._id);
        //         index += 2;
        //         index %= res;
        //     }
        // }
        s._constraints = constraints.ToArray();
        return s;
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
            DrawLine((int) (p1.Pos.X + 0.5f * P1ToP2.X), (int) (p1.Pos.Y + 0.5f * P1ToP2.Y), (int) (p1.Pos.X + 0.5f * P1ToP2.X + force.X / 100f), (int) (p1.Pos.Y + 0.5f * P1ToP2.Y + force.Y / 100f), Raylib_cs.Color.WHITE);
            p1.ApplyForce(force);
            p2.ApplyForce(force);
        }
    }

    // pseudo 2D volume a.k.a. area
    private float CalculateVolume()
    {
        float area = 0f;
        Vector2 centerOfMass = new();
        foreach (var p in _points)
        {
            centerOfMass += p.Pos;
        }
        centerOfMass /= _points.Length;
        DrawCircle((int) centerOfMass.X, (int) centerOfMass.Y, 20, Raylib_cs.Color.RED);
        for (int i = 0; i < _points.Length; i++)
        {
            PointMass p1 = _points[i];
            PointMass p2 = _points[(i + 1) % _points.Length];
            float baseLength = Vector2.Distance(p1.Pos, p2.Pos); 
            Vector2 closestPoint = Tools.Geometry.ClosestPointOnLine(p1.Pos, p2.Pos, centerOfMass);
            float height = Vector2.Distance(closestPoint, centerOfMass);
            area += 0.5f * baseLength * height;
        }
        return area;
    }
}