using System.Numerics;
using Raylib_cs;
using Sim;
using Utils;
using static Raylib_cs.Raylib;

namespace Editing;

public class Grid
{
    public bool SnappingEnabled { get; set; }

    private readonly int _pointsX;
    private readonly int _pointsY;
    private readonly float _viewWidthMeters = UnitConversion.PixelsToMeters(Program.WinW);
    private readonly float _viewHeightMeters = UnitConversion.PixelsToMeters(Program.WinH);
    private readonly int _pointsPerMeter;

    public Grid(int pointsPerMeter)
    {
        _pointsPerMeter = pointsPerMeter;
        _pointsX = (int) (_viewWidthMeters * pointsPerMeter);
        _pointsY = (int) (_viewHeightMeters * pointsPerMeter);
    }

    public void Draw()
    {
        for (int x = 0; x <= _pointsX; x++)
        {
            for (int y = 0; y <= _pointsY; y++)
            {
                DrawCircleLines((int) UnitConversion.MetersToPixels((float) x / _pointsPerMeter), (int) UnitConversion.MetersToPixels((float) y / _pointsPerMeter), 1f, Color.White);
            }
        } 
    }
}