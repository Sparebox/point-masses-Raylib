namespace Physics;

public abstract class Constraint
{
    public PointMass A { get; init; }
    public PointMass B { get; init; }

    public abstract void Update();
    public abstract void Draw();
}