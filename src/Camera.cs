using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace point_masses.Camera;

public class Camera
{
    public float MoveSpeed { get; init; }

    private Vector2 _offset;

    public Camera(float moveSpeed)
    {
        _offset = new();
        MoveSpeed = moveSpeed;
    }

    public void UpdateInput()
    {
        Vector2 dir = new();
        if (IsKeyDown(KeyboardKey.A))
        {
            dir.X -= 1f;
        }
        if (IsKeyDown(KeyboardKey.D))
        {
            dir.X += 1f;
        }
        if (IsKeyDown(KeyboardKey.W))
        {
            dir.Y -= 1f;
        }
        if (IsKeyDown(KeyboardKey.S))
        {
            dir.Y += 1f;
        }
        if (dir.LengthSquared() != 0f)
        {
            Move(Vector2.Normalize(dir));
        }
    }

    public void Move(in Vector2 dir)
    {
        _offset += MoveSpeed * dir;
    }

    public void Reset()
    {
        _offset = Vector2.Zero;
    }

    public Vector2 GetOffsetCoords(in Vector2 pos)
    {
        return pos + _offset;
    }

    public Vector2 GetOffsetCoords(int x, int y)
    {
        return new(x + _offset.X, y + _offset.Y);
    }

}

