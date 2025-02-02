using System.Numerics;
using Raylib_cs;
using PointMasses.Collision;
using PointMasses.Sim;
using PointMasses.Utils;
using static Raylib_cs.Raylib;

namespace PointMasses.Entities;

public class PointMass : Entity
{
    public bool Pinned { get; init; }
    public Vector2 _visForce; // For force visualization
    public Vector2 _pos;
    public Vector2 _prevPos;
    public Vector2 _force;
    public Vector2 PrevForce { get; private set; }
    public Vector2 Vel
    {
        get { return _pos - _prevPos; }
        set { _prevPos = _pos - value; }
    }
    public Vector2 Momentum => Mass * Vel;
    public float Radius { get; init; }
    public override BoundingBox Aabb 
    {
        get
        {
            Vector3 min = new(_pos.X - Radius, _pos.Y - Radius, 0f);
            Vector3 max = new(_pos.X + Radius, _pos.Y + Radius, 0f);
            return new BoundingBox(min, max);
        }
    }
    public override Vector2 Centroid => _pos;
    public override Vector2 CenterOfMass => _pos;
    
    public PointMass(float x, float y, float mass, bool pinned, Context ctx, bool incrementId = true) : base(ctx, mass, incrementId: incrementId)
    {
        _pos = new(x, y);
        _prevPos = _pos;
        _force = Vector2.Zero;
        Radius = MassToRadius(mass);
        Pinned = pinned;
    }

    // Copy constructor
    public PointMass(PointMass p) : base(p.Ctx, p.Mass, p.Id)
    {
        _pos = p._pos;
        _prevPos = _pos;
        _force = Vector2.Zero;;
        Radius = p.Radius;
        Pinned = p.Pinned;
    }

    public override void Update()
    {
        if (Pinned)
        {
            return;
        }
        if (Ctx._gravityEnabled)
        {
            ApplyForce(Mass * Ctx.Gravity);
        }
        //SolveLineCollisions();
        Vector2 acc = _force * _invMass;
        Vector2 vel = Vel; // Save the velocity before previous position is reset
        _prevPos = _pos;
        _pos += vel + acc * Ctx.Substep * Ctx.Substep;
        _visForce = _force;
        PrevForce = _force;
        _force = Vector2.Zero;
    }

    public override void Draw()
    {
        DrawCircleLinesV(UnitConv.MtoP(_pos), UnitConv.MtoP(Radius), Color.White);
    }

    public void ApplyForce(Vector2 force)
    {
        _force += force;
        _visForce += force;
    }

    public static float RadiusToMass(float radius)
    {
        return radius / Constants.RadiusPerMassRatio;
    }

    public static float MassToRadius(float mass)
    {
        return mass * Constants.RadiusPerMassRatio;
    }

    public void ApplyFriction(Vector2 normal)
    {
        if (Vel.LengthSquared() == 0f)
        {
            return;
        }
        // Find vel direction perpendicular to the normal
        Vector2 perpVel = Vel - Vector2.Dot(Vel, normal) * normal;
        if (perpVel.LengthSquared() == 0f)
        {
            return;
        }
        float normalForce = Vector2.Dot(PrevForce, normal);
        if (normalForce >= 0f)
        {
            // The total force is not towards the normal
            return;
        }
        if (PrevForce.LengthSquared() < MathF.Pow(Ctx._globalStaticFrictionCoeff * -normalForce, 2f))
        {
            // Apply static friction
            Vel -= perpVel;
            return;
        }
        // Apply kinetic friction
        perpVel = Vector2.Normalize(perpVel);
        ApplyForce(perpVel * Ctx._globalKineticFrictionCoeff * normalForce);
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