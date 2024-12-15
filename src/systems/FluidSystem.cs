using System.Numerics;
using Raylib_cs;
using PointMasses.Sim;
using PointMasses.Utils;
using static Raylib_cs.Raylib;

namespace PointMasses.Systems;

public class FluidSystem : ISystem
{
    private readonly Grid _grid;
    private const int IterationCount = 10;
    private const float Overrelaxation = 1.9f;
    private const float FluidDensity = 1f;
    private Vector2 _lastMousePos;

    public FluidSystem(Context ctx)
    {
        _grid = new Grid(ctx);
    }

    public void Update()
    {
        _grid.Update();
    }

    public void UpdateInput()
    {   
        if (IsMouseButtonDown(MouseButton.Left))
        {
            var mousePos = UnitConv.PtoM(GetMousePosition() - Grid.Pos);
            (int xIndex, int yIndex) = Grid.PosToIndex(mousePos);
            if (IsKeyDown(KeyboardKey.LeftShift))
            {
                _grid.SetVelocityAt(xIndex, yIndex, 50f * (mousePos - _lastMousePos));
            }
            else
            {
                try
                {
                    _grid.SetDensityAt(xIndex, yIndex, 1f);
                }
                catch (IndexOutOfRangeException e)
                {
                    Console.Error.WriteLineAsync(e.Message);
                }
            }
            _lastMousePos = mousePos;
        }
        if (IsMouseButtonDown(MouseButton.Right))
        {
            var mousePos = UnitConv.PtoM(GetMousePosition() - Grid.Pos);
            (int xIndex, int yIndex) = Grid.PosToIndex(mousePos);
            try
            {
                _grid.SetDensityAt(xIndex, yIndex, 0f);
            }
            catch (IndexOutOfRangeException e)
            {
                Console.Error.WriteLineAsync(e.Message);
            }
        }
        if (IsKeyPressed(KeyboardKey.V))
        {
            Grid.DrawVel = !Grid.DrawVel;
        }
        if (IsKeyDown(KeyboardKey.Right))
        {
            _grid.RotateVelocityAt(Grid.NumX / 2, Grid.NumY / 2, 5f);
        }
        if (IsKeyDown(KeyboardKey.Left))
        {
            _grid.RotateVelocityAt(Grid.NumX / 2, Grid.NumY / 2, -5f);
        }
    }

    public void Draw()
    {
        _grid.Draw();
    }

    private class Grid
    {
        public const int NumX = 30;
        public const int NumY = 30;
        public const float CellSize = 0.05f;
        public static Vector2 Pos;

        private Cell[,] Cells { get; set; }
        private readonly Context _ctx;

        public static bool DrawVel { get; set; } = true;

        public Grid(Context ctx)
        {
            _ctx = ctx;
            Pos = new(ctx.WinSize.X / 2 - 200, ctx.WinSize.Y / 2 - 400);
            Initialize();
        }

        public void Update()
        {
            Project();
            AdvectVel();
            AdvectDensity();
        }

        public void Draw()
        {
            for (int x = 0; x < NumX; x++)
            {
                for (int y = 0; y < NumY; y++)
                {
                    Cells[x, y].Draw(x, y);
                }
            }
        }

        public void SetDensityAt(int x, int y, float density)
        {
            if (x < 0 || x > NumX - 1 || y < 0 || y > NumY - 1)
            {
                throw new IndexOutOfRangeException(string.Format("Grid index ({0}, {1}) is out of bounds", x, y));
            }
            Cells[x, y].Density = density;
        }

        public void SetVelocityAt(int x, int y, in Vector2 vel)
        {
            if (x < 0 || x > NumX - 1 || y < 0 || y > NumY - 1)
            {
                throw new IndexOutOfRangeException(string.Format("Grid index ({0}, {1}) is out of bounds", x, y));
            }
            Cells[x, y]._vel = vel;
        }

        public void RotateVelocityAt(int x, int y, float angle)
        {
            if (x < 0 || x > NumX - 1 || y < 0 || y > NumY - 1)
            {
                throw new IndexOutOfRangeException(string.Format("Grid index ({0}, {1}) is out of bounds", x, y));
            }
            Cells[x, y]._vel = 2f * Vector2.Normalize(Raymath.Vector2Rotate(Cells[x, y]._vel, angle * _ctx._timestep));
        }

        private void Initialize()
        {
            Cells = new Cell[NumX, NumY];
            for (int x = 0; x < NumX; x++)
            {
                for (int y = 0; y < NumY; y++)
                {
                    Cells[x, y] = new();
                    if (x == 0)
                    {
                        Cells[x, y].S = 0;
                    }
                    // if (x == 1 && y > 0 && y < NumY - 1)
                    // {
                    //     Cells[x, y]._vel.X = 1f;
                    // }
                    if (x == NumX / 2 && y == NumY / 2)
                    {
                        Cells[x, y]._vel.X = -1f;
                        Cells[x, y].S = 0;
                    }
                    if (x == NumX - 1)
                    {
                        Cells[x, y].S = 0;
                    }
                    if (y == 0 || y == NumY - 1)
                    {
                        Cells[x, y].S = 0;
                    }
                }
            }
        }

        private void Project()
        {   
            for (int i = 0; i < IterationCount; i++)
            {
                for (int x = 0; x < NumX - 1; x++)
                {
                    for (int y = 0; y < NumY - 1; y++)
                    {
                        if (Cells[x, y].S == 0)
                        {
                            continue;
                        }
                        int sUp = Cells[x, y - 1].S;
                        int sDown = Cells[x, y + 1].S;
                        int sLeft = Cells[x - 1, y].S;
                        int sRight = Cells[x + 1, y].S;
                        int s = sUp + sDown + sLeft + sRight;
                        if (s == 0)
                        {
                            continue;
                        }
                        float div = Overrelaxation * Divergence(x, y) / s;
                        Cells[x, y]._vel.X      += sLeft    * div;
                        Cells[x + 1, y]._vel.X  -= sRight   * div;
                        Cells[x, y]._vel.Y      -= sDown    * div;
                        Cells[x, y - 1]._vel.Y  += sUp      * div;

                        // Pressure
                        //Cells[x, y].Pressure += div * FluidDensity * CellSize / _ctx.Substep;
                    }
                }
            }
        }

        private void AdvectVel()
        {
            for (int x = 1; x < NumX - 1; x++)
            {
                for (int y = 1; y < NumY - 1; y++)
                {
                    if (Cells[x, y].S == 0)
                    {
                        continue;
                    }
                    Vector2 velocity;
                    Vector2 pos;
                    Vector2 prevPos;
                    float? sampledVel;

                    // Horizontal velocity
                    if (Cells[x - 1, y].S != 0)
                    {
                        velocity = new(Cells[x, y]._vel.X, AvgV(x, y));
                        pos = IndexToUvelPos(x, y);
                        prevPos = pos - velocity * _ctx._timestep;
                        sampledVel = SampleVelocity(prevPos, true);
                        if (sampledVel.HasValue)
                            Cells[x, y]._vel.X = sampledVel.Value;
                    }
                        
                    // Vertical velocity
                    if (Cells[x, y + 1].S != 0)
                    {
                        velocity = new(AvgU(x, y), Cells[x, y]._vel.Y);
                        pos = IndexToVvelPos(x, y);
                        prevPos = pos - velocity * _ctx._timestep;
                        sampledVel = SampleVelocity(prevPos, false);
                        if (sampledVel.HasValue)
                            Cells[x, y]._vel.Y = sampledVel.Value;
                    }
                }
            }
        }

        private void AdvectDensity()
        {
            for (int x = 1; x < NumX - 1; x++)
            {
                for (int y = 1; y < NumY - 1; y++)
                {
                    Vector2 velocity = new((Cells[x, y]._vel.X + Cells[x + 1, y]._vel.X) * 0.5f, (Cells[x, y]._vel.Y + Cells[x, y - 1]._vel.Y) * 0.5f);
                    Vector2 pos = IndexToSmokePos(x, y);
                    Vector2 prevPos = pos - velocity * _ctx._timestep;
                    float sampledDensity = SampleDensity(prevPos);
                    Cells[x, y].Density = sampledDensity;
                }
            }
        }

        private float Divergence(int x, int y)
        {
            return Cells[x + 1, y]._vel.X - Cells[x, y]._vel.X + Cells[x, y]._vel.Y - Cells[x, y - 1]._vel.Y;
        }

        private float? SampleVelocity(in Vector2 pos, bool horizontal)
        {   
            (int xIndex, int yIndex) = PosToIndex(pos);
            if (xIndex < 1 || xIndex >= NumX - 1 || yIndex < 1 || yIndex >= NumY - 1)
            {
                return null;
            }
            var current     = Cells[xIndex, yIndex];
            var right       = Cells[xIndex + 1, yIndex];
            var down        = Cells[xIndex - 1, yIndex];
            var downRight   = Cells[xIndex + 1, yIndex + 1];

            (float xAmount, float yAmount) = PosToPercentage(pos);
            float a, b;
            
            if (horizontal)
            {
                a = Raymath.Lerp(current._vel.X, right._vel.X, xAmount);
                b = Raymath.Lerp(down._vel.X, downRight._vel.X, xAmount);
                return Raymath.Lerp(a, b, yAmount);
            }
            else
            {
                a = Raymath.Lerp(current._vel.Y, right._vel.Y, xAmount);
                b = Raymath.Lerp(down._vel.Y, downRight._vel.Y, xAmount);
                return Raymath.Lerp(a, b, yAmount);
            }
        }

        private float SampleDensity(in Vector2 pos)
        {   
            (int xIndex, int yIndex) = PosToIndex(pos);

            var current     = Cells[xIndex, yIndex];
            var up          = Cells[xIndex, yIndex - 1];
            var down        = Cells[xIndex, yIndex + 1];
            var left        = Cells[xIndex - 1, yIndex];
            var right       = Cells[xIndex + 1, yIndex];
            var downRight   = Cells[xIndex + 1, yIndex + 1];
            var downLeft    = Cells[xIndex - 1, yIndex + 1];
            var upRight     = Cells[xIndex + 1, yIndex - 1];
            var upLeft      = Cells[xIndex - 1, yIndex - 1];

            (float xAmount, float yAmount) = PosToPercentage(pos);
            float a, b;
            
            if (xAmount <= 0.5f && yAmount >= 0.5f)
            {
                a = Raymath.Lerp(left.Density, current.Density, xAmount + 0.5f);
                b = Raymath.Lerp(downLeft.Density, down.Density, xAmount + 0.5f);
                return Raymath.Lerp(a, b, yAmount - 0.5f);
            }
            else if (xAmount >= 0.5f && yAmount < 0.5f)
            {
                a = Raymath.Lerp(up.Density, upRight.Density, xAmount - 0.5f);
                b = Raymath.Lerp(current.Density, right.Density, xAmount - 0.5f);
                return Raymath.Lerp(a, b, yAmount + 0.5f);
            }
            else if (xAmount > 0.5f && yAmount >= 0.5f)
            {
                a = Raymath.Lerp(current.Density, right.Density, xAmount - 0.5f);
                b = Raymath.Lerp(down.Density, downRight.Density, xAmount - 0.5f);
                return Raymath.Lerp(a, b, yAmount - 0.5f);
            }
            else if (xAmount < 0.5f && yAmount <= 0.5f)
            {
                a = Raymath.Lerp(upLeft.Density, up.Density, xAmount + 0.5f);
                b = Raymath.Lerp(left.Density, current.Density, xAmount + 0.5f);
                return Raymath.Lerp(a, b, yAmount + 0.5f);
            }
            else
            {
                throw new Exception("Density sampling did not match any cases");
            }
        }

        private float AvgU(int x, int y)
        {
            return (Cells[x, y]._vel.X + Cells[x - 1, y]._vel.X + Cells[x - 1, y - 1]._vel.X + Cells[x, y - 1]._vel.X) * 0.25f;
        }

        private float AvgV(int x, int y)
        {
            return (Cells[x, y]._vel.Y + Cells[x - 1, y]._vel.Y + Cells[x - 1, y - 1]._vel.Y + Cells[x, y - 1]._vel.Y) * 0.25f;
        }

        public static Vector2 IndexToUvelPos(int x, int y)
        {
            return new(CellSize * x, CellSize * (y + 0.5f));
        }

        public static Vector2 IndexToVvelPos(int x, int y)
        {
            return new(CellSize * (x + 0.5f), CellSize * (y + 1f));
        }

        public static (int, int) PosToIndex(in Vector2 pos)
        {
            return ((int) MathF.Truncate(pos.X / CellSize), (int) MathF.Truncate(pos.Y / CellSize));
        }

        private static Vector2 IndexToSmokePos(int x, int y)
        {
            return new(CellSize * (x + 0.5f), CellSize * (y + 0.5f));
        }

        // private static (int, int) SmokePosToIndex(in Vector2 pos)
        // {
        //     return ((int) MathF.Truncate(pos.X / CellSize - 0.5f), (int) MathF.Truncate(pos.Y / CellSize - 0.5f));
        // }

        private static (float, float) PosToPercentage(in Vector2 pos)
        {
            float fullX = pos.X / CellSize;
            float fullY = pos.Y / CellSize;
            float integralX = MathF.Truncate(fullX);
            float integralY = MathF.Truncate(fullY);
            float fractionX = fullX - integralX;
            float fractionY = fullY - integralY;
            return (fractionX, fractionY);
        }

    }

    private struct Cell
    {
        public Vector2 _vel;
        public float Density { get; set; }
        public int S { get; set; }
        public float Pressure { get; set; }
        // S = 1 -> fluid
        // S = 0 -> obstacle

        public Cell()
        {
            _vel = Vector2.Zero;
            S = 1;
            Density = 0f;
            Pressure = 0f;
        }

        public readonly void Draw(int x, int y)
        {
            x = (int) Grid.Pos.X + UnitConv.MtoP(x * Grid.CellSize);
            y = (int) Grid.Pos.Y + UnitConv.MtoP(y * Grid.CellSize);
            int GridPixelSize = UnitConv.MtoP(Grid.CellSize);
            int lightness = (int) (Density * 255);
          
            if (S == 0)
            {
                DrawRectangleLines(x, y, GridPixelSize, GridPixelSize, new Color(255, 255, 0, 255));
                return;
            }
            DrawRectangle(x, y, GridPixelSize, GridPixelSize, new Color(lightness, lightness, lightness, 255));
            DrawRectangleLines(x, y, GridPixelSize, GridPixelSize, new Color(255, 0, 255, 50));
            if (Grid.DrawVel) 
            {
                Graphics.DrawArrow(
                    x + GridPixelSize * 0.5f,
                    y + GridPixelSize * 0.5f,
                    x + GridPixelSize * 0.5f + _vel.X * 500f,
                    y + GridPixelSize * 0.5f + _vel.Y * 500f,
                    Color.Yellow
                );
            }
            //DrawText(string.Format("{0:0.0}", Density), x + GridPixelSize / 3, y + GridPixelSize / 3, 10, Color.Green);
            //DrawText(string.Format("{0:0.0}", _vel.Length()), x + GridPixelSize / 3, y + GridPixelSize / 3 + 10, 10, Color.Red);
            //DrawText(string.Format("{0:0.0}", Pressure), x + GridPixelSize / 3, y + GridPixelSize / 3 + 20, 10, Color.Blue);
            // Graphics.DrawArrow(
            //     uVelPos,
            //     new(uVelPos.X + _vel.X * 100, uVelPos.Y),
            //      Color.Yellow
            // );
            // Graphics.DrawArrow(
            //     vVelPos,
            //     new(vVelPos.X, vVelPos.Y + _vel.Y * 100),
            //      Color.Yellow
            // );
            
        }
    }

}