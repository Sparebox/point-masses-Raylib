using System.Numerics;
using Sim;
using static Raylib_cs.Raylib;

namespace Collision;

public class RotatingCollider : LineCollider
{
    public RotatingCollider(float x0, float y0, float x1, float y1, Context context) : base(x0, y0, x1, y1, context) {}

    public RotatingCollider(RotatingCollider r, Context context) : base(r.StartPos.X, r.StartPos.Y, r.EndPos.X, r.EndPos.Y, context) {}

    public void Raise(float amount)
    {
        EndPos = Vector2.Transform(EndPos, Matrix3x2.CreateRotation(DEG2RAD * amount, StartPos));
    }

    public void Lower(float amount)
    {
        EndPos = Vector2.Transform(EndPos, Matrix3x2.CreateRotation(DEG2RAD * -amount, StartPos));
    }
}