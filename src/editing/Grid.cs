using System.Numerics;
using Raylib_cs;
using Sim;
using Tools;
using Utils;
using static Raylib_cs.Raylib;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Editing;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class Grid
{
    public bool SnappingEnabled { get; set; }
    public GridPoint[] GridPoints { get; set; }

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

    public int _pointsPerMeter;
    private const int PointSize = 1;
    private uint _pointsX;
    private uint _pointsY;

    public Grid(int pointsPerMeter) => SetGridScale(pointsPerMeter);

    public void Draw()
    {
        foreach (var point in GridPoints)
        {
            int pointSize = point.IsSelected ? 5 * PointSize : PointSize;
            DrawCircleLines(
                UnitConv.MetersToPixels(point.Pos.X),
                UnitConv.MetersToPixels(point.Pos.Y),
                pointSize,
                Color.White
            );
        }
    }

    public void SetGridScale(int pointsPerMeter)
    {
        _pointsPerMeter = pointsPerMeter;
        _pointsX = (uint) float.Ceiling(UnitConv.PixelsToMeters(Program.WinW) * _pointsPerMeter);
        _pointsY = (uint) float.Ceiling(UnitConv.PixelsToMeters(Program.WinH) * _pointsPerMeter);

        GridPoints = new GridPoint[_pointsX * _pointsY];

        for (uint y = 0; y < _pointsY; y++)
        {
            for (uint x = 0; x < _pointsX; x++)
            {
                GridPoints[GetIndexFromPoint(x, y)].Pos.X = (float) x / _pointsPerMeter;
                GridPoints[GetIndexFromPoint(x, y)].Pos.Y = (float) y / _pointsPerMeter;
            }
        }
    }

    public void ClearSelectedPoints()
    {
        for (int i = 0; i < GridPoints.Length; i++)
        {
            GridPoints[i].IsSelected = false;
        }
    }

    public ref GridPoint GetClosestGridPoint(uint xPixels, uint yPixels)
    {
        return ref GridPoints[GetIndexFromPixel(xPixels, yPixels)];
    }

    private uint[] GetClosestPoint(uint xPixels, uint yPixels)
    {
        float xPoint = UnitConv.PixelsToMeters(xPixels) * _pointsPerMeter;
        float yPoint = UnitConv.PixelsToMeters(yPixels) * _pointsPerMeter;
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

    private uint GetIndexFromPoint(uint xPoints, uint yPoints)
    {
        return xPoints + yPoints * _pointsX;
    }

    public uint GetIndexFromPixel(uint xPixels, uint yPixels)
    {
        var closestPoint = GetClosestPoint(xPixels, yPixels);
        return GetIndexFromPoint(closestPoint[0], closestPoint[1]);
    }
    
}