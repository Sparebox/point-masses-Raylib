using System;
using PointMasses.Sim;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace PointMasses.Input;

public class InputManager
{
    public static event EventHandler<bool> PauseChanged;

    public static void HandleInput(Context ctx)
    {
        // Keys
        if (IsKeyPressed(KeyboardKey.G))
        {
            ctx._gravityEnabled = !ctx._gravityEnabled;
        }
        if (IsKeyPressed(KeyboardKey.F))
        {
            ctx._drawForces = !ctx._drawForces;
        }
        if (IsKeyPressed(KeyboardKey.Q))
        {
            ctx._drawQuadTree = !ctx._drawQuadTree;
        }
        if (IsKeyPressed(KeyboardKey.R))
        {
            ctx.LoadSavedState();
        }
        if (IsKeyPressed(KeyboardKey.Space))
        {
            ctx._simPaused = !ctx._simPaused;
            PauseChanged?.Invoke(ctx, ctx._simPaused);
        }

        // Handle system inputs
        foreach (var system in ctx.Systems)
        {
            system.UpdateInput();
        }
        foreach (var subStepSystem in ctx.SubStepSystems)
        {
            subStepSystem.UpdateInput();
        }

        // Handle camera
        UpdateCamera(ctx);

        // Temporary demo keys
        if (IsKeyPressed(KeyboardKey.C))
        {
            ctx.LoadClothScenario();
        }
        if (IsKeyPressed(KeyboardKey.B))
        {
            ctx.LoadBenchmark(1000, 3f, 20f, new(ctx.WinSize.X * 0.5f - 200f, 200f));
        }
    }

    private static void UpdateCamera(Context ctx)
    {
        Camera2D camera = ctx._camera;
        if (IsKeyDown(KeyboardKey.W))
        {
            camera.Offset.Y += ctx._cameraMoveSpeed;
        }
        if (IsKeyDown(KeyboardKey.S))
        {
            camera.Offset.Y -= ctx._cameraMoveSpeed;
        }
        if (IsKeyDown(KeyboardKey.A))
        {
            camera.Offset.X += ctx._cameraMoveSpeed;
        }
        if (IsKeyDown(KeyboardKey.D))
        {
            camera.Offset.X -= ctx._cameraMoveSpeed;
        }
    }
}

