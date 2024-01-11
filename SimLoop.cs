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

        PointMass mass = new(1500f, 730f, 10f);
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
                mass.Update(_lineColliders);
                _accumulator -= TimeStep;
            }
            HandleInput();
            BeginDrawing();
            ClearBackground(Raylib_cs.Color.BLACK);
            mass.Draw();
            foreach (var c in _lineColliders)
            {
                c.Draw();
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
