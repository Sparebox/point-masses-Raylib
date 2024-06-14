#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Physics;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public abstract class Constraint
{
#pragma warning disable CA2211 // Non-constant fields should not be visible
    protected static uint _idCounter;
#pragma warning restore CA2211 // Non-constant fields should not be visible
    public uint Id { get; init; }
    public PointMass PointA { get; set; }
    public PointMass PointB { get; set; }

    public abstract void Update();
    public abstract void Draw();
}