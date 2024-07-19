using System.Numerics;
using Raylib_cs;
using Sim;

namespace Entities;

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
    protected Context Context { get; init; }
    protected float? _mass;
    protected float _invMass;

    public Entity(Context context, float mass, uint? id = null)
    {
        if (id.HasValue)
        {
            Id = id.Value;
        }
        else
        {
            Id = _idCounter++;
        }
        Mass = mass;
        Context = context;
    }

    public Entity(Context context, uint? id = null)
    {
        if (id.HasValue)
        {
            Id = id.Value;
        }
        else
        {
            Id = _idCounter++;
        }
        Context = context;
    }

    public Entity() {}

    public abstract void Update();
    public abstract void Draw();
}

