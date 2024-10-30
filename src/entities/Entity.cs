using System.Numerics;
using Raylib_cs;
using PointMasses.Sim;

namespace PointMasses.Entities;

public abstract class Entity
{
    public virtual float Mass
    {
        get => _mass ?? 0f;
        set 
        { 
            _mass = value;
            _invMass = value != 0f ? 1f / value : 0f; 
        }
    }
    public float InvMass => _invMass;
    public uint Id { get; init; }
    public abstract BoundingBox Aabb { get; }
    public abstract Vector2 Centroid { get; }
    public abstract Vector2 CenterOfMass { get; }

    private static uint _idCounter;
    protected Context Ctx { get; init; }
    protected float? _mass;
    protected float _invMass;

    public Entity(Context ctx, float mass, uint? id = null, bool incrementId = true)
    {
        if (id.HasValue)
        {
            Id = id.Value;
        }
        else if (incrementId)
        {
            Id = _idCounter++;
        }
        Mass = mass;
        Ctx = ctx;
    }

    public Entity(Context ctx, uint? id = null, bool incrementId = true)
    {
        if (id.HasValue)
        {
            Id = id.Value;
        }
        else if (incrementId)
        {
            Id = _idCounter++;
        }
        Ctx = ctx;
    }

    public Entity() {}

    public static void ResetIdCounter()
    {
        _idCounter = 0;
    }

    public abstract void Update();
    public abstract void Draw();
}

