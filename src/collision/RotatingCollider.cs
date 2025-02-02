using System.Numerics;
using PointMasses.Sim;
using static Raylib_cs.Raylib;

namespace PointMasses.Collision;

public class RotatingCollider : LineCollider
{
    public RotatingCollider(float x0, float y0, float x1, float y1, Context ctx) : base(x0, y0, x1, y1, ctx) {}

    public RotatingCollider(RotatingCollider r, Context ctx) : base(r._startPos.X, r._startPos.Y, r._endPos.X, r._endPos.Y, ctx) {}

    public void Raise(float amount)
    {
        _endPos = Vector2.Transform(_endPos, Matrix3x2.CreateRotation(DEG2RAD * amount, _startPos));
    }

    public void Lower(float amount)
    {
        _endPos = Vector2.Transform(_endPos, Matrix3x2.CreateRotation(DEG2RAD * -amount, _startPos));
    }
}