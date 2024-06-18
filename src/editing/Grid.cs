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
    public List<uint> SelectedPointIndices { get; init; }
    public List<(uint, uint)> ConstrainedPointIndexPairs { get; init; }

    public struct GridPoint
    {
        public Vector2 _pos;
        public bool IsSelected { get; set; }
        public bool IsConstrained { get; set; }
        public bool IsPinned { get; set; }

        public GridPoint()
        {
            _pos = new Vector2();
            IsSelected = false;
            IsConstrained = false;
            IsPinned = false;
        }
    }

    public int _pointsPerMeter;
    private const int PointSize = 2;
    private uint _pointsX;
    private uint _pointsY;

    public Grid(int pointsPerMeter)
    {
        SelectedPointIndices = new();
        ConstrainedPointIndexPairs = new();
        SetGridScale(pointsPerMeter);
    }

    public void Draw()
    {
        foreach (var point in GridPoints)
        {
            DrawRectangleLines(
                UnitConv.MetersToPixels(point._pos.X),
                UnitConv.MetersToPixels(point._pos.Y),
                PointSize,
                PointSize,
                Color.White
            );
            if (point.IsSelected)
            {
                Color color = point.IsConstrained ? Color.Purple : Color.Yellow;
                int toolRadiusPixels = UnitConv.MetersToPixels(Tool.Radius);
                DrawCircleLinesV(
                    UnitConv.MetersToPixels(point._pos),
                    toolRadiusPixels,
                    color
                );
                if (point.IsPinned)
                {
                    DrawRectangleLines(
                        UnitConv.MetersToPixels(point._pos.X) - toolRadiusPixels / 2,
                        UnitConv.MetersToPixels(point._pos.Y) - toolRadiusPixels / 2,
                        toolRadiusPixels,
                        toolRadiusPixels,
                        color
                    );
                }
            }
        }
        foreach (var pair in ConstrainedPointIndexPairs)
        {
            DrawLineV(
                UnitConv.MetersToPixels(GridPoints[pair.Item1]._pos),
                UnitConv.MetersToPixels(GridPoints[pair.Item2]._pos),
                Color.Purple
            );
        }
    }

    public void SetGridScale(int pointsPerMeter)
    {
        _pointsPerMeter = pointsPerMeter;
        _pointsX = (uint) float.Ceiling(UnitConv.PixelsToMeters(Program.WinW) * _pointsPerMeter);
        _pointsY = (uint) float.Ceiling(UnitConv.PixelsToMeters(Program.WinH) * _pointsPerMeter);

        GridPoints = new GridPoint[_pointsX * _pointsY];
        SelectedPointIndices.Clear();
        ConstrainedPointIndexPairs.Clear();

        for (uint y = 0; y < _pointsY; y++)
        {
            for (uint x = 0; x < _pointsX; x++)
            {
                GridPoints[GetIndexFromPoint(x, y)]._pos.X = (float) x / _pointsPerMeter;
                GridPoints[GetIndexFromPoint(x, y)]._pos.Y = (float) y / _pointsPerMeter;
            }
        }
    }

    public void ClearSelectedPoints()
    {
        for (int i = 0; i < GridPoints.Length; i++)
        {
            GridPoints[i].IsSelected = false;
            GridPoints[i].IsConstrained = false;
            GridPoints[i].IsPinned = false;
        }
        SelectedPointIndices.Clear();
        ConstrainedPointIndexPairs.Clear();
    }

    public void ToggleGridPoint(int xPixels, int yPixels, bool select, bool pin)
    {
        ref var closestGridPoint = ref GetClosestGridPoint(xPixels, yPixels);
        uint gridIndex = GetIndexFromPixel(xPixels, yPixels);
        if (select)
        {
            closestGridPoint.IsSelected = true;
            closestGridPoint.IsPinned = pin;
            if (!SelectedPointIndices.Contains(gridIndex))
            {
                SelectedPointIndices.Add(gridIndex);
            }
            return;
        }
        // Deselect
        closestGridPoint.IsSelected = false;
        closestGridPoint.IsConstrained = false;
        closestGridPoint.IsPinned = false;
        SelectedPointIndices.Remove(gridIndex);
    }

    public ref GridPoint GetClosestGridPoint(int xPixels, int yPixels)
    {
        return ref GridPoints[GetIndexFromPixel(xPixels, yPixels)];
    }

    public uint[] GetClosestPoint(int xPixels, int yPixels)
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

    public uint GetIndexFromPoint(uint xPoints, uint yPoints)
    {
        return xPoints + yPoints * _pointsX;
    }

    public uint GetIndexFromPixel(int xPixels, int yPixels)
    {
        var closestPoint = GetClosestPoint(xPixels, yPixels);
        return GetIndexFromPoint(closestPoint[0], closestPoint[1]);
    }
    
}