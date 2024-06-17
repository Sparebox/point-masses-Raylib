using System.Numerics;
using Collision;
using static Raylib_cs.Raylib;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Entity;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class RotatingCollider : LineCollider
{
    public RotatingCollider(float x0, float y0, float x1, float y1) : base(x0, y0, x1, y1) {}

    public RotatingCollider(RotatingCollider r) : base(r.StartPos.X, r.StartPos.Y, r.EndPos.X, r.EndPos.Y) {}

    public void Raise(float amount)
    {
        EndPos = Vector2.Transform(EndPos, Matrix3x2.CreateRotation(DEG2RAD * amount, StartPos));
    }

    public void Lower(float amount)
    {
        EndPos = Vector2.Transform(EndPos, Matrix3x2.CreateRotation(DEG2RAD * -amount, StartPos));
    }
}