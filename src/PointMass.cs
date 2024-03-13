using System.Numerics;
using Collision;
using Raylib_cs;
using Sim;
using static Raylib_cs.Raylib;

namespace Physics;

public class PointMass
{
    private static int _idCounter;

    public const float RestitutionCoeff = 1f;
    public const float KineticFrictionCoeff = 0.1f;
    public const float StaticFrictionCoeff = 2f;

    public readonly int _id;
    public readonly bool _pinned;
    public Vector2 _visForce; // For force visualization
    public Vector2 Pos { get; set; }
    public Vector2 PrevPos { get; set; }
    public Vector2 Force { get; set; }
    public Vector2 Vel
    {
        get { return Pos - PrevPos; }
        set { PrevPos = Pos - value; }
    }
    public float Mass { get; }
    public float Radius { get; init; }
    
    private readonly Context _context;

    public PointMass(float x, float y, float mass, bool pinned, Context context)
    {
        Pos = new(x, y);
        PrevPos = Pos;
        Force = Vector2.Zero;
        Mass = mass;
        Radius = mass * 5f;
        _id = _idCounter++;
        _pinned = pinned;
        _context = context;
    }

    // Copy constructor
    public PointMass(in PointMass p)
    {
        Pos = p.Pos;
        PrevPos = Pos;
        Force = Vector2.Zero;
        Mass = p.Mass;
        Radius = p.Radius;
        _id = p._id;
        _pinned = p._pinned;
        _context = p._context;
    }

    public void Update(float timeStep)
    {
        if (_pinned)
        {
            return;
        }
        if (_context._gravityEnabled)
        {
            ApplyForce(Mass * _context._gravity);
        }
        SolveCollisions();
        Vector2 acc = Force / Mass;
        Vector2 vel = Vel;
        PrevPos = Pos;
        Pos += vel + acc * timeStep * timeStep;
        _visForce = Force;
        Force = Vector2.Zero;
    }

    public void Draw()
    {
        DrawCircleLines((int) Pos.X, (int) Pos.Y, Radius, Color.White);
    }

    public void ApplyForce(in Vector2 force)
    {
        Force += force;
        _visForce += force;
    }

    public void SolveCollisions()
    {
        foreach (LineCollider c in _context.LineColliders)
        {
            c.SolveCollision(this);
        }
        //_context._ramp.SolveCollision(this);
    }

    public void ApplyFriction(in Vector2 normal)
    {
        // Find vel direction perpendicular to the normal
        Vector2 dir = Vel - Vector2.Dot(Vel, normal) * normal;
        if (dir.LengthSquared() == 0f)
        {
            return;
        }
        float normalForce = Vector2.Dot(Force, normal);
        if (normalForce > 0f)
        {
            // The total force is not towards the normal
            return;
        }
        if (Force.LengthSquared() < Math.Pow(StaticFrictionCoeff * -normalForce, 2f))
        {
            // Apply static friction
            Vel += -dir;
            return;
        }
        // Apply kinetic friction
        dir = Vector2.Normalize(dir);
        ApplyForce(dir * KineticFrictionCoeff * normalForce);
        //Vector2 vis = dir * KineticFrictionCoeff * normalForce;
        //DrawLine((int) Pos.X, (int) Pos.Y, (int) (Pos.X + vis.X), (int) (Pos.Y + vis.Y), Color.Magenta);
    }

    public static bool operator == (PointMass a, PointMass b)
    {
        return a._id == b._id;
    }

    public static bool operator != (PointMass a, PointMass b)
    {
        return a._id != b._id;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || !obj.GetType().Equals(typeof(PointMass)))
        {
            return false;
        }
        return _id == ((PointMass) obj)._id;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}