using System.Numerics;

namespace Tools;

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