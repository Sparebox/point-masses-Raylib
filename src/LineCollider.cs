using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Collision;

public class LineCollider 
{
    public Vector2 StartPos { get; set; }
    public Vector2 EndPos { get; set; }

    public LineCollider(float x0, float y0, float x1, float y1)
    {
        StartPos = new(x0, y0);
        EndPos = new(x1, y1);
    }

    public LineCollider(in LineCollider c)
    {
        StartPos = c.StartPos;
        EndPos = c.EndPos;
    }

    public void Draw()
    {
        DrawLine((int) StartPos.X, (int) StartPos.Y, (int) EndPos.X, (int) EndPos.Y, Color.White);
    }
}