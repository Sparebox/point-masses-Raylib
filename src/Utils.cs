using System.Numerics;
using Physics;
using Raylib_cs;
using Sim;
using static Raylib_cs.Raylib;

namespace Utils;

public static class UnitConversion
{
    public const float PixelsPerMeter = 300f;

    public static float PixelsToMeters(float pixels)
    {
        return pixels / PixelsPerMeter;
    }

    public static float MetersToPixels(float meters)
    {
        return PixelsPerMeter * meters;
    }

    public static Vector2 PixelsToMeters(Vector2 pixels)
    {
        return new Vector2(pixels.X / PixelsPerMeter, pixels.Y / PixelsPerMeter);
    }

    public static Vector2 MetersToPixels(Vector2 meters)
    {
        return new Vector2(meters.X * PixelsPerMeter, meters.Y * PixelsPerMeter);
    }
}

public static class Geometry
{
    public static Vector2 ClosestPointOnLine(in Vector2 lineStart, in Vector2 lineEnd, in Vector2 point)
    {
        Vector2 startToPoint = point - lineStart;
        Vector2 startToEnd = lineEnd - lineStart;
        Vector2 startToEndNorm = Vector2.Normalize(startToEnd);
        float distOnLine = Vector2.Dot(startToPoint, startToEndNorm);
        if (distOnLine < 0f)
        {
            return new Vector2(lineStart.X, lineStart.Y);
        }
        if (distOnLine * distOnLine > startToEnd.LengthSquared())
        {
            return new Vector2(lineEnd.X, lineEnd.Y);
        }
        return lineStart + distOnLine * startToEndNorm;
    }
}

public static class Entities
{
    public readonly struct CollisionData
    {
        public PointMass PointMassA { get; init; }
        public PointMass PointMassB { get; init; }
        public Vector2 Normal { get; init; }
        public float Separation { get; init; }
    }
}

public static class Graphics
{
    public const float ArrowBranchLength = 20f;
    public const float ArrowAngle = 20f;

    public static void DrawArrow(float x0, float y0, float x1, float y1, Color color)
    {
        Vector2 start = new(x0, y0);
        Vector2 end = new(x1, y1);
        Vector2 dir = start - end;
        float lenSq = dir.LengthSquared();
        if (lenSq < 5f * 5f)
        {
            return;
        }
        dir = Vector2.Normalize(dir);
        float dirAngle = MathF.Atan2(dir.Y, dir.X);
        float radians = DEG2RAD * ArrowAngle;
        float angle1 = dirAngle + radians;
        float angle2 = dirAngle - radians;
        Vector2 branchA = new(ArrowBranchLength * MathF.Cos(angle1), ArrowBranchLength * MathF.Sin(angle1));
        Vector2 branchB = new(ArrowBranchLength * MathF.Cos(angle2), ArrowBranchLength * MathF.Sin(angle2));
        DrawLine((int) start.X, (int) start.Y, (int) end.X, (int) end.Y, color);
        DrawLine((int) end.X, (int) end.Y, (int) (end.X + branchA.X), (int) (end.Y + branchA.Y), color);
        DrawLine((int) end.X, (int) end.Y, (int) (end.X + branchB.X), (int) (end.Y + branchB.Y), color);
    }

    public static void DrawArrow(in Vector2 start, in Vector2 end, Color color)
    {
        DrawArrow(start.X, start.Y, end.X, end.Y, color);
    }
}