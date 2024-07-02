using System.Numerics;
using Collision;
using Raylib_cs;
using Sim;
using Utils;
using static Raylib_cs.Raylib;
using static Utils.Entities;

namespace Physics;

public class PointMass : Entity
{
    public const float RadiusPerMassRatio = 0.01f; // aka inverse density

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
    
    public PointMass(float x, float y, float mass, bool pinned, Context context) : base(context, mass)
    {
        Pos = new(x, y);
        PrevPos = Pos;
        Force = Vector2.Zero;
        Radius = MassToRadius(mass);
        Pinned = pinned;
    }

    // Copy constructor
    public PointMass(PointMass p) : base(p.Context, p.Mass, p.Id)
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
        if (Context._gravityEnabled)
        {
            ApplyForce(Mass * Context.Gravity);
        }
        SolveLineCollisions();
        Vector2 acc = Force * _invMass;
        Vector2 vel = Vel; // Save the velocity before previous position is reset
        PrevPos = Pos;
        Pos += vel + acc * Context.SubStep * Context.SubStep;
        VisForce = Force;
        PrevForce = Force;
        Force = Vector2.Zero;
    }

    public override void Draw()
    {
        DrawCircleLinesV(UnitConv.MetersToPixels(Pos), UnitConv.MetersToPixels(Radius), Color.White);
    }

    public void ApplyForce(in Vector2 force)
    {
        Force += force;
        VisForce += force;
    }

    public void SolveLineCollisions()
    {
        foreach (LineCollider c in Context.LineColliders)
        {
            CollisionData? collisionResult = c.CheckCollision(this);
            if (collisionResult.HasValue)
            {
                LineCollider.SolvePointCollision(collisionResult.Value, Context);
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
                Normal = normal / dist,
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
        if (PrevForce.LengthSquared() < MathF.Pow(Context._globalStaticFrictionCoeff * -normalForce, 2f))
        {
            // Apply static friction
            Vel -= perpVel;
            return;
        }
        // Apply kinetic friction
        perpVel = Vector2.Normalize(perpVel);
        ApplyForce(perpVel * Context._globalKineticFrictionCoeff * normalForce);
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