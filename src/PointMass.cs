using System.Numerics;
using Collision;
using Raylib_cs;
using Sim;
using static Raylib_cs.Raylib;

namespace Physics;

public class PointMass
{
    private static int _idCounter;
    public const float RadiusToMassRatio = 2f;

    public readonly int _id;
    public readonly bool _pinned;
    public Vector2 _visForce; // For force visualization
    public Vector2 Pos { get; set; }
    public Vector2 PrevPos { get; set; }
    public Vector2 Force { get; set; }
    public Vector2 PrevForce { get; private set; }
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
        Radius = mass * RadiusToMassRatio;
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

    public void Update()
    {
        if (_pinned)
        {
            return;
        }
        if (_context._gravityEnabled)
        {
            ApplyForce(Mass * _context._gravity);
        }
        SolveLineCollisions();
        Vector2 acc = Force / Mass;
        Vector2 vel = Vel;
        PrevPos = Pos;
        Pos += vel + acc * _context._subStep * _context._subStep;
        _visForce = Force;
        PrevForce = Force;
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

    public void SolveLineCollisions()
    {
        foreach (LineCollider c in _context.LineColliders)
        {
            c.SolveCollision(this, _context);
        }
        //_context._ramp.SolveStaticCollision(this);
    }

    public void SolvePointToPointCollision(PointMass otherPoint)
    {   
        Vector2 normal = otherPoint.Pos - Pos;
        float dist = normal.LengthSquared();
        if (dist <= Math.Pow(Radius + otherPoint.Radius, 2f))
        {
            // Do expensive square root here
            dist = (float) Math.Sqrt(dist);
            if (dist == 0f)
            {
                return;
            }
            normal.X /= dist;
            normal.Y /= dist;
            // Apply impulse
            Vector2 relVel = otherPoint.Vel - Vel;
            float impulseMag = -(1f + _context._globalRestitutionCoeff) * Vector2.Dot(relVel, normal) / (1f / Mass + 1f / otherPoint.Mass);
            Vector2 impulse = impulseMag * normal;
            Vel += -impulse / Mass;
            otherPoint.Vel += impulse / otherPoint.Mass;
            Vector2 offsetVector = 0.5f * (Radius + otherPoint.Radius - dist) * normal;
            Pos += -offsetVector;
            otherPoint.Pos += offsetVector;
        }
    }

    public void ApplyFriction(in Vector2 normal)
    {
        if (Vel.LengthSquared() == 0f)
        {
            return;
        }
        // Find vel direction perpendicular to the normal
        Vector2 dir = Vel - Vector2.Dot(Vel, normal) * normal;
        if (dir.LengthSquared() == 0f)
        {
            return;
        }
        float normalForce = Vector2.Dot(PrevForce, normal);
        if (normalForce >= 0f)
        {
            // The total force is not towards the normal
            return;
        }
        if (PrevForce.LengthSquared() < Math.Pow(_context._globalStaticFrictionCoeff * -normalForce, 2f))
        {
            // Apply static friction
            Vel += -dir;
            return;
        }
        // Apply kinetic friction
        dir = Vector2.Normalize(dir);
        ApplyForce(dir * _context._globalKineticFrictionCoeff * normalForce);
        //Vector2 vis = dir * KineticFrictionCoeff * normalForce;
        //Utils.Graphic.DrawArrow((int) Pos.X, (int) Pos.Y, (int) (Pos.X + vis.X), (int) (Pos.Y + vis.Y), Color.Magenta);
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