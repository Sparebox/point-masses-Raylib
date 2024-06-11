using System.Numerics;
using Raylib_cs;
using Sim;
using Utils;
using static Raylib_cs.Raylib;

namespace Editing;

public class Grid
{
    public bool SnappingEnabled { get; set; }
    public GridPoint[] GridPoints { get; init; }

    private const float PointSize = 1f;
    private readonly Editor _editor;
    private readonly uint _pointsX;
    private readonly uint _pointsY;
    private readonly int _pointsPerMeter;

    public Grid(int pointsPerMeter, Editor editor)
    {
        _pointsPerMeter = pointsPerMeter;
        _editor = editor;
        _pointsX = (uint) float.Ceiling(UnitConversion.PixelsToMeters(Program.WinW) * pointsPerMeter);
        _pointsY = (uint) float.Ceiling(UnitConversion.PixelsToMeters(Program.WinH) * pointsPerMeter);

        GridPoints = new GridPoint[_pointsX * _pointsY];

        for (uint y = 0; y < _pointsY; y++)
        {
            for (uint x = 0; x < _pointsX; x++)
            {
                GridPoints[GetIndexFromPoint(x, y)].Pos.X = (float) x / pointsPerMeter;
                GridPoints[GetIndexFromPoint(x, y)].Pos.Y = (float) y / pointsPerMeter;
            }
        }
    }

    public void Draw()
    {
        foreach (var point in GridPoints)
        {
            DrawCircleLines(
                (int) UnitConversion.MetersToPixels(point.Pos.X),
                (int) UnitConversion.MetersToPixels(point.Pos.Y),
                point.IsSelected ? _editor.CursorSize : PointSize,
                Color.White
            );
        }
    }

    public void ResetSelectedPoints()
    {
        for (int i = 0; i < GridPoints.Length; i++)
        {
            GridPoints[i].IsSelected = false;
        }
    }

    public ref GridPoint FindClosestGridPoint(uint pixelX, uint pixelY)
    {
        return ref GridPoints[GetIndexFromPixel(pixelX, pixelY)];
    }

    private uint[] GetClosestPoint(uint xPixels, uint yPixels)
    {
        float xPoint = UnitConversion.PixelsToMeters(xPixels) * _pointsPerMeter;
        float yPoint = UnitConversion.PixelsToMeters(yPixels) * _pointsPerMeter;
        float xPointFrac = xPoint - float.Floor(xPoint);
        float yPointFrac = yPoint - float.Floor(yPoint);
        uint xPointInt;
        uint yPointInt;
        if (xPointFrac > 0.5f)
        {
            xPointInt = (uint) float.Ceiling(xPoint);
        }
        else
        {
            xPointInt = (uint) float.Floor(xPoint);
        }
        if (yPointFrac > 0.5f)
        {
            yPointInt = (uint) float.Ceiling(yPoint);
        }
        else
        {
            yPointInt = (uint) float.Floor(yPoint);
        }
        return new uint[] { xPointInt, yPointInt };
    }

    private uint GetIndexFromPoint(uint xPoint, uint yPoint)
    {
        return xPoint + yPoint * _pointsX;
    }

    public uint GetIndexFromPixel(uint xPixels, uint yPixels)
    {
        var closestPoint = GetClosestPoint(xPixels, yPixels);
        return GetIndexFromPoint(closestPoint[0], closestPoint[1]);
    }

    public struct GridPoint
    {
        public Vector2 Pos;
        public bool IsSelected;

        public GridPoint()
        {
            Pos = new Vector2();
            IsSelected = false;
        }
    }
}