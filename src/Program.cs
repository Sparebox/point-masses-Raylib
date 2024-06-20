using System.Numerics;
using Physics;
using Raylib_cs;
using rlImGui_cs;
using Tools;
using UI;
using Utils;
using static Raylib_cs.Raylib;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Sim;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class Program 
{   
    public const int WinW = 1600;
    public const int WinH = 900;
    public const float QuadTreeUpdateSeconds = 0.1f;

    private static float _accumulator;
    private static float _quadTreeAccumulator;
    private static Context _context;

    public static void Main() 
    {
        _context = Init();
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
        SetTargetFPS(GetMonitorRefreshRate(GetCurrentMonitor()));
        float winWidthMeters = UnitConv.PixelsToMeters(WinW);
        float winHeightMeters = UnitConv.PixelsToMeters(WinH);
        Context context = new(timeStep: 1f / 60f, 13, gravity: new(0f, 9.81f))
        {
            LineColliders = {
                new(0f, 0f, winWidthMeters, 0f),
                new(0f, 0f, 0f, winHeightMeters),
                new(winWidthMeters, 0f, winWidthMeters, winHeightMeters),
                new(0f, winHeightMeters, winWidthMeters, winHeightMeters),
            },
            QuadTree = new Entities.QuadTree(
                UnitConv.PixelsToMeters(new Vector2(WinW / 2f, WinH / 2f)),
                UnitConv.PixelsToMeters(new Vector2(WinW, WinH))
            )
        };
        context.SelectedTool = new PullCom(context);
        context.SaveCurrentState();
        return context;
    }

    private static void Update()
    {
        _accumulator += GetFrameTime();
        _quadTreeAccumulator += GetFrameTime();
        while (_quadTreeAccumulator >= QuadTreeUpdateSeconds)
        {
            _context.QuadTree.Update(_context);
            _quadTreeAccumulator -= QuadTreeUpdateSeconds;
        }
        while (_accumulator >= _context.TimeStep)
        {
            for (int i = 0; i < _context.Substeps; i++)
            {
                foreach (MassShape s in _context.MassShapes)
                {
                    s.Update();
                }
                if (_context.NbodySim._running && _context.NbodySim._collisionsEnabled)
                {
                    MassShape.HandleCollisions(_context);
                }
            }
            _context.NbodySim.Update();
            _context.MassShapes.RemoveWhere(s => s._toBeDeleted);
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
        if (IsKeyPressed(KeyboardKey.B))
        {
            _context._drawAABBS = !_context._drawAABBS;
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
        if (_context._toolEnabled)
        {
            _context.SelectedTool.Update();
        }
        // Temporary demo keys
        if (IsKeyPressed(KeyboardKey.C))
        {
            _context.LoadClothScenario();
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