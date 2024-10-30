using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace PointMasses;

public class Camera
{
    public Vector2 Offset { get; private set; }
    public float _moveSpeed;

    public Camera(float moveSpeed)
    {
        Offset = new();
        _moveSpeed = moveSpeed;
    }

    public void UpdateInput()
    {
        Vector2 dir = new();
        if (IsKeyDown(KeyboardKey.A))
        {
            dir.X += 1f;
        }
        if (IsKeyDown(KeyboardKey.D))
        {
            dir.X -= 1f;
        }
        if (IsKeyDown(KeyboardKey.W))
        {
            dir.Y += 1f;
        }
        if (IsKeyDown(KeyboardKey.S))
        {
            dir.Y -= 1f;
        }
        if (dir.LengthSquared() != 0f)
        {
            Move(Vector2.Normalize(dir));
        }
    }

    public void Move(in Vector2 dir)
    {
        Offset += _moveSpeed * dir;
    }

    public void Reset()
    {
        Offset = Vector2.Zero;
    }

    public Vector2 ViewPos(in Vector2 pos)
    {
        return pos + Offset;
    }

    public Vector2 ViewPos(int x, int y)
    {
        return new(x + Offset.X, y + Offset.Y);
    }

    public Vector2 WorldPos(in Vector2 pos)
    {
        return pos - Offset;
    }

    public Vector2 WorldPos(int x, int y)
    {
        return new(x - Offset.X, y - Offset.Y);
    }

}

