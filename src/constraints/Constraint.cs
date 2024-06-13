namespace Physics;

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