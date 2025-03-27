using System.Numerics;
using PointMasses.Sim;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace PointMasses.Input;

public class InputManager
{
    public static event EventHandler<bool> PauseChanged;
    public static bool InputEnabled { get; set; } = true;

    public static void HandleInput(Context ctx)
    {
        if (!InputEnabled)
        {
            return;
        }
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
            ctx.LoadSnapshot();
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

        // Demo keys
        if (IsKeyPressed(KeyboardKey.C))
        {
            ctx.LoadClothScenario();
        }
        if (IsKeyPressed(KeyboardKey.B))
        {
            ctx.LoadBenchmark(1000, 3f, 30f, new(ctx.WinSize.X * 0.5f - 200f, 200f));
            ctx.GetSystem<Systems.NbodySystem>()._running = true;
            ctx.SetTimestep(null, 10);
        }
    }

    public static Vector2 GetMousePos()
    {
        return Program.RenderSizePercentage * GetMousePosition();
    }

    private static void UpdateCamera(Context ctx)
    {
        if (IsKeyDown(KeyboardKey.W))
        {
            ctx._camera.Offset.Y += ctx._cameraMoveSpeed;
        }
        if (IsKeyDown(KeyboardKey.S))
        {
            ctx._camera.Offset.Y -= ctx._cameraMoveSpeed;
        }
        if (IsKeyDown(KeyboardKey.A))
        {
            ctx._camera.Offset.X += ctx._cameraMoveSpeed;
        }
        if (IsKeyDown(KeyboardKey.D))
        {
            ctx._camera.Offset.X -= ctx._cameraMoveSpeed;
        }
    }
}

