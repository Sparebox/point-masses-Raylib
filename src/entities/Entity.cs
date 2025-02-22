using System.Numerics;
using Raylib_cs;
using PointMasses.Sim;
using Newtonsoft.Json;

namespace PointMasses.Entities;

[JsonObject(MemberSerialization.OptIn)]
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
    [JsonProperty]
    public uint Id { get; init; }
    public abstract BoundingBox Aabb { get; }
    public abstract Vector2 Centroid { get; }
    public abstract Vector2 CenterOfMass { get; }

    private static uint _idCounter;
    protected Context Ctx { get; set; }
    [JsonProperty]
    protected float? _mass;
    [JsonProperty]
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

    public void SetContext(Context ctx)
    {
        Ctx = ctx;
    }

    public static void ResetIdCounter()
    {
        _idCounter = 0;
    }

    public abstract void Update();
    public abstract void Draw();
}

