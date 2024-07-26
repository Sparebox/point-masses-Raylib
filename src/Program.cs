using System.Numerics;
using Entities;
using Raylib_cs;
using rlImGui_cs;
using Tools;
using UI;
using Utils;
using static Raylib_cs.Raylib;

namespace Sim;

public class Program 
{   
    public const int WinW = 1600;
    public const int WinH = 900;
    public const int PauseThresholdFPS = 15;
    public const int QuadTreeUpdateMs = 50;
    public static readonly int TargetFPS = GetMonitorRefreshRate(GetCurrentMonitor());

    private static float _accumulator;
    private static Context _context;

    public static void Main() 
    {
        _context = Init();
        var _quadTreeUpdateThread = new Thread(new ParameterizedThreadStart(QuadTree.ThreadUpdate), 0)
        {
            IsBackground = true
        };
        _quadTreeUpdateThread.Start(_context);
        rlImGui.Setup(true);
        while (!WindowShouldClose())
        {
            if (!_context._simPaused)
            {
                Update();
            }
            HandleInput();
            Draw();
        }
        rlImGui.Shutdown();
        CloseWindow();
    }

    private static Context Init()
    {
        InitWindow(WinW, WinH, "Point-masses");
        SetTargetFPS(TargetFPS);
        
        float winWidthMeters = UnitConv.PixelsToMeters(WinW);
        float winHeightMeters = UnitConv.PixelsToMeters(WinH);
        Context ctx = new(timeStep: 1f / 60f, 5, gravity: new(0f, 9.81f))
        {
            QuadTree = new(
                UnitConv.PixelsToMeters(new Vector2(WinW / 2f, WinH / 2f)),
                UnitConv.PixelsToMeters(new Vector2(WinW, WinH)),
                1,
                6
            )
        };
        ctx.LineColliders = new() {
            new(0f, 0f, winWidthMeters, 0f, ctx),
            new(0f, 0f, 0f, winHeightMeters, ctx),
            new(winWidthMeters, 0f, winWidthMeters, winHeightMeters, ctx),
            new(0f, winHeightMeters, winWidthMeters, winHeightMeters, ctx)
        };
        ctx.SelectedTool = ctx.Tools[(int) ToolType.PullCom];
        ctx.SaveCurrentState();
        // Load textures
        ctx.TextureManager.LoadTexture("center_of_mass.png");
        return ctx;
    }

    private static void Update()
    {
        if (GetFPS() < PauseThresholdFPS) // Pause if running too slow
        {
            Console.WriteLine("Running too slow. Pausing sim");
            _context._simPaused = true;
        }
        _accumulator += GetFrameTime();
        while (_accumulator >= _context.TimeStep)
        {
            for (int i = 0; i < _context.Substeps; i++)
            {
                foreach (MassShape s in _context.MassShapes)
                {
                    s.Update();
                }
                if (!_context.NbodySim._running || (_context.NbodySim._running && _context.NbodySim._collisionsEnabled))
                {
                    MassShape.HandleCollisions(_context);
                }
            }
            _context.NbodySim.Update();
            // Remove deleted mass shapes if any deleted
            _context.Lock.EnterUpgradeableReadLock();
            if (_context.MassShapes.Where(s => s._toBeDeleted).Any())
            {
                if (_context.Lock.TryEnterWriteLock(0)) // Do not block the main thread if the lock is unavailable
                {
                    _context.MassShapes.RemoveWhere(s => s._toBeDeleted);
                    _context.Lock.ExitWriteLock();
                }
            }
            _context.Lock.ExitUpgradeableReadLock();
            _accumulator -= _context.TimeStep;
        }
    }

    private static void Draw()
    {
        BeginDrawing(); // raylib
        rlImGui.Begin(); // GUI
        ClearBackground(Color.Black);

        foreach (MassShape s in _context.MassShapes)
        {
            s.Draw();
        }
        foreach (var l in _context.LineColliders)
        {
            l.Draw();
        }
        if (_context._drawQuadTree)
        {
            _context.QuadTree.Draw();
        }
        _context.SelectedTool.Draw();
        Gui.DrawInfo(_context); // GUI
        
        rlImGui.End();
        EndDrawing(); // raylib
    }

    private static void HandleInput()
    {
        // Keys
        if (IsKeyPressed(KeyboardKey.G))
        {
            _context._gravityEnabled = !_context._gravityEnabled;
        }
        if (IsKeyPressed(KeyboardKey.F))
        {
            _context._drawForces = !_context._drawForces;
        }
        if (IsKeyPressed(KeyboardKey.Q))
        {
            _context._drawQuadTree = !_context._drawQuadTree;
        }
        if (IsKeyPressed(KeyboardKey.R))
        {
            _context.LoadSavedState();
        }
        if (IsKeyPressed(KeyboardKey.Space))
        {
            _context._simPaused = !_context._simPaused;
        }
        if (_context._toolEnabled && _context.SelectedTool.GetType() != typeof(NbodySim))
        {
            _context.SelectedTool.Update();
        }
        // Temporary demo keys
        if (IsKeyPressed(KeyboardKey.C))
        {
            _context.LoadClothScenario();
        }
        if (IsKeyPressed(KeyboardKey.B))
        {
            _context.LoadBenchmark(1000, 3f, 20f, new(WinW / 2f - 200f, 200f));
        }
        // Mouse
        if (GetMouseWheelMoveV().Y > 0f)
        {
            _context.SelectedTool.ChangeRadius(Tool.BaseRadiusChange);
            _context.SelectedTool.ChangeDirection(DEG2RAD * Tool.BaseAngleChange);
            if (_context.SelectedTool.GetType() == typeof(Spawn))
            {
                var spawnTool = (Spawn) _context.SelectedTool;
                spawnTool.UpdateSpawnTarget();
            }
        } 
        else if (GetMouseWheelMoveV().Y < 0f)
        {
            _context.SelectedTool.ChangeRadius(-Tool.BaseRadiusChange);
            _context.SelectedTool.ChangeDirection(DEG2RAD * -Tool.BaseAngleChange);
            if (_context.SelectedTool.GetType() == typeof(Spawn))
            {
                var spawnTool = (Spawn) _context.SelectedTool;
                spawnTool.UpdateSpawnTarget();
            }
        }
    }
}