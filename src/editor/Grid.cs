using System.Numerics;
using Raylib_cs;
using Sim;
using Utils;
using static Raylib_cs.Raylib;

namespace Editing;

public class Grid
{
    public bool SnappingEnabled { get; set; }

    private readonly Vector2[] _gridPoints;
    private readonly uint _pointsX;
    private readonly uint _pointsY;
    private readonly int _pointsPerMeter;

    public Grid(int pointsPerMeter)
    {
        _pointsPerMeter = pointsPerMeter;
        _pointsX = (uint) float.Ceiling(UnitConversion.PixelsToMeters(Program.WinW) * pointsPerMeter);
        _pointsY = (uint) float.Ceiling(UnitConversion.PixelsToMeters(Program.WinH) * pointsPerMeter);

        _gridPoints = new Vector2[_pointsX * _pointsY];

        for (uint y = 0; y < _pointsY; y++)
        {
            for (uint x = 0; x < _pointsX; x++)
            {
                _gridPoints[x + y * _pointsX].X = (float) x / _pointsPerMeter;
                _gridPoints[x + y * _pointsX].Y = (float) y / _pointsPerMeter;
            }
        }
    }

    public void Draw()
    {
        foreach (var point in _gridPoints)
        {
            DrawCircleLines((int) UnitConversion.MetersToPixels(point.X), (int) UnitConversion.MetersToPixels(point.Y), 1f, Color.White);
        }
    }

    private Vector2 FindClosestPoint(uint pixelX, uint pixelY)
    {
        float xPoint = UnitConversion.PixelsToMeters(pixelX) * _pointsPerMeter;
        float yPoint = UnitConversion.PixelsToMeters(pixelY) * _pointsPerMeter;
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
        return _gridPoints[GetIndex(xPointInt, yPointInt)];
    }

    private uint GetIndex(uint xPoint, uint yPoint)
    {
        return yPoint / _pointsX + xPoint;
    }
}