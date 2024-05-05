namespace Physics;

public abstract class Constraint
{
#pragma warning disable CA2211 // Non-constant fields should not be visible
    protected static int _idCounter;
#pragma warning restore CA2211 // Non-constant fields should not be visible
    public int Id { get; init; }
    public PointMass PointA { get; set; }
    public PointMass PointB { get; set; }

    public abstract void Update();
    public abstract void Draw();
}