using System.Numerics;
using Collision;
using Physics;
using static Raylib_cs.Raylib;

namespace Sim;

public class Loop 
{   
    public const float TimeStep = 1f / 60f;
    public const int WinW = 1600;
    public const int WinH = 900;
    public const float PixelsPerMeter = 1f / 0.01f;

    public static bool GravityEnabled { get; set; }
    public static readonly Vector2 Gravity = new(0f, 9.81f * PixelsPerMeter);

    private static float _accumulator;
    private static List<LineCollider> _lineColliders;

    public static void Main() 
    {
        InitWindow(WinW, WinH, "");
        SetTargetFPS(165);

        PointMass a = new(WinW / 2f - 50f, WinH / 2f, 5f);
        PointMass b = new(WinW / 2f + 50f, WinH / 2f + 50f, 5f);
        SpringConstraint c = new(a, b, 100f);
        _lineColliders = new() {
            new(0f, 0f, WinW, 0f),
            new(0f, 0f, 0f, WinH),
            new(WinW, 0f, WinW, WinH),
            new(0f, 900f, 1600f, 780f) 
        };
        GravityEnabled = true;

        while (!WindowShouldClose())
        {
            SetWindowTitle(string.Format("Point-masses FPS: {0}", GetFPS()));
            _accumulator += GetFrameTime();
            while (_accumulator >= TimeStep)
            {
                a.Update(_lineColliders);
                b.Update(_lineColliders);
                c.Update();
                _accumulator -= TimeStep;
            }
            HandleInput();
            BeginDrawing();
            ClearBackground(Raylib_cs.Color.BLACK);
            a.Draw();
            b.Draw();
            c.Draw();
            foreach (var l in _lineColliders)
            {
                l.Draw();
            }
            EndDrawing();
        }

        CloseWindow();
    }

    private static void HandleInput()
    {
        if (IsKeyPressed(Raylib_cs.KeyboardKey.KEY_G))
        {
            GravityEnabled = !GravityEnabled;
        }
    }
}
