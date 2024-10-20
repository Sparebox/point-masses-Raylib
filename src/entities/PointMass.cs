using System.Numerics;
using Collision;
using Raylib_cs;
using Sim;
using Utils;
using static Raylib_cs.Raylib;

namespace Entities;

public class PointMass : Entity
{
    public bool Pinned { get; init; }
    public Vector2 VisForce { get; set; } // For force visualization
    public Vector2 Pos { get; set; }
    public Vector2 PrevPos { get; set; }
    public Vector2 Force { get; set; }
    public Vector2 PrevForce { get; private set; }
    public Vector2 Vel
    {
        get { return Pos - PrevPos; }
        set { PrevPos = Pos - value; }
    }
    public Vector2 Momentum => Mass * Vel;
    public float Radius { get; init; }
    public override BoundingBox Aabb 
    {
        get
        {
            Vector3 min = new(Pos.X - Radius, Pos.Y - Radius, 0f);
            Vector3 max = new(Pos.X + Radius, Pos.Y + Radius, 0f);
            return new BoundingBox(min, max);
        }
    }
    public override Vector2 Centroid => Pos;
    public override Vector2 CenterOfMass => Pos;
    
    public PointMass(float x, float y, float mass, bool pinned, Context ctx) : base(ctx, mass)
    {
        Pos = new(x, y);
        PrevPos = Pos;
        Force = Vector2.Zero;
        Radius = MassToRadius(mass);
        Pinned = pinned;
    }

    // Copy constructor
    public PointMass(PointMass p) : base(p.Ctx, p.Mass, p.Id)
    {
        Pos = p.Pos;
        PrevPos = Pos;
        Force = Vector2.Zero;;
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
        Vector2 acc = Force * _invMass;
        Vector2 vel = Vel; // Save the velocity before previous position is reset
        PrevPos = Pos;
        Pos += vel + acc * Ctx.Substep * Ctx.Substep;
        VisForce = Force;
        PrevForce = Force;
        Force = Vector2.Zero;
    }

    public override void Draw()
    {
        var drawPos = Ctx.Camera.GetOffsetCoords(UnitConv.MetersToPixels(Pos));
        DrawCircleLinesV(drawPos, UnitConv.MetersToPixels(Radius), Color.White);
    }

    public void ApplyForce(in Vector2 force)
    {
        Force += force;
        VisForce += force;
    }

    public static float RadiusToMass(float radius)
    {
        return radius / Constants.RadiusPerMassRatio;
    }

    public static float MassToRadius(float mass)
    {
        return mass * Constants.RadiusPerMassRatio;
    }

    public void ApplyFriction(in Vector2 normal)
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