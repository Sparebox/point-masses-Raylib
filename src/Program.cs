using System.Numerics;
using PointMasses.Entities;
using PointMasses.Systems;
using Raylib_cs;
using rlImGui_cs;
using PointMasses.Tools;
using PointMasses.UI;
using PointMasses.Utils;
using static Raylib_cs.Raylib;

namespace PointMasses.Sim;

public class Program 
{   
    public static readonly int TargetFPS = GetMonitorRefreshRate(GetCurrentMonitor());

    private static float _accumulator;
    private static Context _ctx;

    public static void Main() 
    {
        _ctx = Init();
        var _quadTreeUpdateThread = new Thread(new ParameterizedThreadStart(QuadTree.ThreadUpdate), 0)
        {
            IsBackground = true
        };
        _quadTreeUpdateThread.Start(_ctx);
        rlImGui.Setup(true);
        while (!WindowShouldClose())
        {
            if (!_ctx._simPaused)
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
        InitWindow(Constants.WinW, Constants.WinH, "Point-masses");
        SetTargetFPS(TargetFPS);
        
        float winWidthMeters = UnitConv.PixelsToMeters(Constants.WinW);
        float winHeightMeters = UnitConv.PixelsToMeters(Constants.WinH);
        Context ctx = new(timeStep: 1f / 60f, 5, gravity: new(0f, 9.81f))
        {
            QuadTree = new(
                UnitConv.PixelsToMeters(new Vector2(Constants.WinW * 0.5f, Constants.WinH * 0.5f)),
                UnitConv.PixelsToMeters(new Vector2(Constants.WinW, Constants.WinH)),
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
        ctx.SaveCurrentState();
        // Load textures
        ctx.TextureManager.LoadTexture("center_of_mass.png");
        return ctx;
    }

    private static void Update()
    {
        if (GetFPS() < Constants.PauseThresholdFPS) // Pause if running too slow
        {
            AsyncConsole.WriteLine("Running too slow. Pausing sim");
            _ctx._simPaused = true;
        }
        _accumulator += GetFrameTime();
        while (_accumulator >= _ctx._timestep)
        {
            for (int i = 0; i < _ctx._substeps; i++)
            {
                foreach (MassShape s in _ctx.MassShapes)
                {
                    s.Update();
                }
                foreach (var system in _ctx.SubStepSystems)
                {
                    system.Update();
                }
            }
            foreach (var system in _ctx.Systems)
            {
                system.Update();
            }
            // Remove deleted mass shapes if any deleted
            _ctx.Lock.EnterUpgradeableReadLock();
            if (_ctx.MassShapes.Where(s => s._toBeDeleted).Any())
            {
                if (_ctx.Lock.TryEnterWriteLock(0)) // Do not block the main thread if the lock is unavailable
                {
                    _ctx.MassShapes.RemoveAll(s => s._toBeDeleted);
                    _ctx.Lock.ExitWriteLock();
                }
            }
            _ctx.Lock.ExitUpgradeableReadLock();
            _accumulator -= _ctx._timestep;
        }
    }

    private static void Draw()
    {
        BeginDrawing(); // raylib
        rlImGui.Begin(); // GUI
        ClearBackground(Color.Black);

        BeginMode2D(_ctx._camera);
        foreach (MassShape s in _ctx.MassShapes)
        {
            s.Draw();
        }
        foreach (var l in _ctx.LineColliders)
        {
            l.Draw();
        }
        foreach (var system in _ctx.Systems)
        {
            system.Draw();
        }
        foreach (var substepSystem in _ctx.SubStepSystems)
        {
            substepSystem.Draw();
        }
        if (_ctx._drawQuadTree)
        {
            _ctx.QuadTree.Draw();
        }
        EndMode2D();
        
        Gui.Draw(_ctx); // GUI
        
        rlImGui.End();
        EndDrawing(); // raylib
    }

    private static void HandleInput()
    {
        // Keys
        if (IsKeyPressed(KeyboardKey.G))
        {
            _ctx._gravityEnabled = !_ctx._gravityEnabled;
        }
        if (IsKeyPressed(KeyboardKey.F))
        {
            _ctx._drawForces = !_ctx._drawForces;
        }
        if (IsKeyPressed(KeyboardKey.Q))
        {
            _ctx._drawQuadTree = !_ctx._drawQuadTree;
        }
        if (IsKeyPressed(KeyboardKey.R))
        {
            _ctx.LoadSavedState();
        }
        if (IsKeyPressed(KeyboardKey.Space))
        {
            _ctx._simPaused = !_ctx._simPaused;
        }

        // Handle system inputs
        foreach (var system in _ctx.Systems)
        {
            system.UpdateInput();
        }
        foreach (var subStepSystem in _ctx.SubStepSystems)
        {
            subStepSystem.UpdateInput();
        }

        // Handle camera
        _ctx.UpdateCamera();

        // Temporary demo keys
        if (IsKeyPressed(KeyboardKey.C))
        {
            _ctx.LoadClothScenario();
        }
        if (IsKeyPressed(KeyboardKey.B))
        {
            _ctx.LoadBenchmark(1000, 3f, 20f, new(Constants.WinW * 0.5f - 200f, 200f));
        }
    }
}