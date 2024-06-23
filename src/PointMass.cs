using System.Data;
using System.Numerics;
using Collision;
using Raylib_cs;
using Sim;
using Utils;
using static Raylib_cs.Raylib;
using static Utils.Entities;

namespace Physics;

public class PointMass
{
    private static uint _idCounter;
    private readonly Context _context;
    public const float RadiusPerMassRatio = 0.01f;

    public uint Id { get; init; }
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
    public float InvMass { get { return 1f / Mass; }}
    public float Radius { get; init; }
    public BoundingBox AABB 
    {
        get
        {
            Vector3 min = new(Pos.X - Radius, Pos.Y - Radius, 0f);
            Vector3 max = new(Pos.X + Radius, Pos.Y + Radius, 0f);
            return new BoundingBox(min, max);
        }
    }
    
    public PointMass(float x, float y, float mass, bool pinned, Context context)
    {
        Pos = new(x, y);
        PrevPos = Pos;
        Force = Vector2.Zero;
        Mass = mass;
        Radius = MassToRadius(mass);
        Id = _idCounter++;
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
        Id = p.Id;
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
            ApplyForce(Mass * _context.Gravity);
        }
        SolveLineCollisions();
        Vector2 acc = Force / Mass;
        Vector2 vel = Vel; // Save the velocity before previous position is reset
        PrevPos = Pos;
        Pos += vel + acc * _context.SubStep * _context.SubStep;
        _visForce = Force;
        PrevForce = Force;
        Force = Vector2.Zero;
    }

    public void Draw()
    {
        DrawCircleLines(UnitConv.MetersToPixels(Pos.X), UnitConv.MetersToPixels(Pos.Y), UnitConv.MetersToPixels(Radius), Color.White);
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
            CollisionData? collisionResult = c.CheckCollision(this);
            if (collisionResult.HasValue)
            {
                LineCollider.SolvePointCollision(collisionResult.Value, _context);
            }
        }
    }

    public CollisionData? CheckPointToPointCollision(PointMass otherPoint)
    {
        Vector2 normal = otherPoint.Pos - Pos;
        float dist = normal.LengthSquared();
        if (dist <= MathF.Pow(Radius + otherPoint.Radius, 2f))
        {
            dist = MathF.Sqrt(dist);
            if (dist == 0f)
            {
                return null;
            }
            var result = new CollisionData()
            {
                PointMassA = this,
                PointMassB = otherPoint,
                Normal = new(normal.X / dist, normal.Y / dist),
                Separation = Radius + otherPoint.Radius - dist,
            };
            return result;
        }
        return null;
    }

    public static void HandlePointToPointCollision(in CollisionData colData, Context context)
    {   
        // Save pre-collision velocities
        Vector2 thisPreVel = colData.PointMassA.Vel;
        Vector2 otherPreVel = colData.PointMassB.Vel;
        Vector2 relVel = otherPreVel - thisPreVel;
        // Correct penetration
        Vector2 offsetVector = 0.5f * colData.Separation * colData.Normal;
        colData.PointMassA.Pos += -offsetVector;
        colData.PointMassB.Pos += offsetVector;
        // Apply impulse
        float impulseMag = -(1f + context._globalRestitutionCoeff) * Vector2.Dot(relVel, colData.Normal) / (colData.PointMassA.InvMass + colData.PointMassB.InvMass);
        Vector2 impulse = impulseMag * colData.Normal;
        colData.PointMassA.Vel = thisPreVel -impulse * colData.PointMassA.InvMass;
        colData.PointMassB.Vel = otherPreVel + impulse * colData.PointMassB.InvMass;
    }

    public static float RadiusToMass(float radius)
    {
        return radius / RadiusPerMassRatio;
    }

    public static float MassToRadius(float mass)
    {
        return mass * RadiusPerMassRatio;
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
        if (PrevForce.LengthSquared() < MathF.Pow(_context._globalStaticFrictionCoeff * -normalForce, 2f))
        {
            // Apply static friction
            Vel += -dir;
            Vel = Vector2.Zero;
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
        return Id == ((PointMass) obj).Id;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}