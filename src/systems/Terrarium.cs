using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using PointMasses.Systems;
using PointMasses.Sim;

namespace PointMasses.systems;

public class Terrarium : ISystem
{   
    public bool _drawGrid = true;
    private readonly Grid _grid;

    public Terrarium(int topLeftX, int topLeftY, int cellsX, int cellsY, int cellSize, Context ctx)
    {
        _grid = new(topLeftX, topLeftY, cellsX, cellsY, cellSize, ctx);
    }

    public void Update()
    {
        
    }

    public void UpdateInput()
    {
        
    }
    
    public void Draw()
    {
        if (_drawGrid)
        {
            _grid.Draw();
        }
    }
}

internal class Grid
{
    public int NumX { get; init; }
    public int NumY { get; init; }
    public int CellSize { get; init; }
    public int NumCells { get; init; }
    public Vector2 TopLeftPos { get; init; }

    private readonly Cell[,] _cells;
    private readonly Context _ctx;

    public Grid(int topLeftX, int topLeftY, int numX, int numY, int cellSize, Context ctx)
    {
        TopLeftPos = new(topLeftX, topLeftY);
        NumX = numX;
        NumY = numY;
        NumCells = numX * numY;
        CellSize = cellSize;
        _cells = new Cell[NumX, NumY];
        _ctx = ctx;

        for (int i = 0; i < NumX; i++)
        {
            for (int j = 0; j < NumY; j++)
            {
                _cells[i, j] = new Cell(CellSize);
            }
        }
    }

    public void Draw()
    {
        float maxX = TopLeftPos.X + NumX * CellSize;
        float maxY = TopLeftPos.Y + NumY * CellSize;
        for (int i = 0; i < NumX + 1; i++)
        {
            float x = TopLeftPos.X + i * CellSize; 
            Vector2 start = _ctx.Camera.ViewPos(new(x, TopLeftPos.Y));
            Vector2 end = _ctx.Camera.ViewPos(new(x, maxY));
            DrawLineV(start, end, Color.White);
        }
        for (int j = 0; j < NumY + 1; j++)
        {
            float y = TopLeftPos.Y + j * CellSize;
            Vector2 start = _ctx.Camera.ViewPos(new(TopLeftPos.X, y));
            Vector2 end = _ctx.Camera.ViewPos(new(maxX, y));
            DrawLineV(start, end, Color.White);
        }
    }

    private readonly struct Cell
    {
        public float Size { get; init; }

        public Cell(float size)
        {
            Size = size;
        }
    }
}
