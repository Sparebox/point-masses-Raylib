using System.Numerics;
using Collision;
using static Raylib_cs.Raylib;

namespace Entity;

public class RotatingCollider
{
    public LineCollider _collider;
    public Vector2 _dir;

    public RotatingCollider(float x0, float y0, float x1, float y1)
    {
        _collider = new LineCollider(x0, y0, x1, y1);
        _dir = Vector2.Normalize(_collider.EndPos - _collider.StartPos);
    }

    public void Raise(float amount)
    {
        _collider.EndPos = Vector2.Transform(_collider.EndPos, Matrix3x2.CreateRotation(DEG2RAD * amount, _collider.StartPos));
    }

    public void Lower(float amount)
    {
        _collider.EndPos = Vector2.Transform(_collider.EndPos, Matrix3x2.CreateRotation(DEG2RAD * -amount, _collider.StartPos));
    }

    public void Draw()
    {
        _collider.Draw();
    }
}