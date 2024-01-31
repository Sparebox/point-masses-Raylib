namespace Physics;

public abstract class Constraint
{
    public PointMass PointA { get; set; }
    public PointMass PointB { get; set; }

    public abstract void Update();
    public abstract void Draw();
}