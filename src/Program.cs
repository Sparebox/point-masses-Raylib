using System.Numerics;
using PointMasses.Entities;
using Raylib_cs;
using rlImGui_cs;
using PointMasses.UI;
using PointMasses.Utils;
using static Raylib_cs.Raylib;
using ImGuiNET;

namespace PointMasses.Sim;

public class Program 
{   
    public static readonly int TargetFPS = GetMonitorRefreshRate(GetCurrentMonitor());

    private static float _accumulator;
    private static Context _ctx;

    public static void Main() 
    {
        _ctx = Init(0.8f, 1f);
        
        rlImGui.Setup(true);
        unsafe { ImGui.GetIO().NativePtr->IniFilename = null; } // Disable imgui.ini file
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

    private static Context Init(float winSizePercentage, float renderPercentage)
    {
        InitWindow(0, 0, "Point-masses");
        SetTargetFPS(TargetFPS);

        int winWidth = (int) (winSizePercentage * GetMonitorWidth(GetCurrentMonitor()));
        int winHeight = (int) (winSizePercentage * GetMonitorHeight(GetCurrentMonitor()));
        int renderWidth = (int) (renderPercentage * winWidth);
        int renderHeight = (int) (renderPercentage * winHeight);
        SetWindowSize(winWidth, winHeight);
        
        int winPosX = GetMonitorWidth(GetCurrentMonitor()) / 2 - winWidth / 2;
        int winPosY = GetMonitorHeight(GetCurrentMonitor()) / 2 - winHeight / 2;
        SetWindowPosition(winPosX, winPosY);
        
        float winWidthMeters = UnitConv.PixelsToMeters(winWidth);
        float winHeightMeters = UnitConv.PixelsToMeters(winHeight);
        Context ctx = new(timeStep: 1f / 60f, 5, gravity: new(0f, 9.81f))
        {
            QuadTree = new(
                new Vector2(winWidthMeters * 0.5f, winHeightMeters * 0.5f),
                new Vector2(winWidthMeters, winHeightMeters),
                1,
                6
            ),
            WinSize = new(winWidth, winHeight),
            RenderTexture = LoadRenderTexture(renderWidth, renderHeight)
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

        // Start quad tree update thread
        var quadTreeUpdateThread = new Thread(new ParameterizedThreadStart(QuadTree.ThreadUpdate), 0)
        {
            IsBackground = true
        };
        quadTreeUpdateThread.Start(ctx);
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

        BeginTextureMode(_ctx.RenderTexture);
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

        EndTextureMode();

        
        
        DrawTexturePro(
            _ctx.RenderTexture.Texture,
            new (0f, 0f, _ctx.RenderTexture.Texture.Width, -_ctx.RenderTexture.Texture.Height),
            new (0f, 0f, _ctx.WinSize.X, _ctx.WinSize.Y),
            Vector2.Zero,
            0f,
            Color.White
        );
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
            _ctx.LoadBenchmark(1000, 3f, 20f, new(_ctx.WinSize.X * 0.5f - 200f, 200f));
        }
    }
}