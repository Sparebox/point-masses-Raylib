using System.Numerics;
using Collision;
using Physics;
using static Raylib_cs.Raylib;

namespace Sim;

public class Loop 
{   
    public const float TimeStep = 1f / 1000f;
    public const int WinW = 1600;
    public const int WinH = 900;
    public const float PixelsPerMeter = 1f / 0.01f;

    public static bool GravityEnabled { get; set; }
    public static bool DrawForces { get; set; }
    public static readonly Vector2 Gravity = new(0f, 9.81f * PixelsPerMeter);

    private static float _accumulator;
    private static List<LineCollider> _lineColliders;
    private static Vector2 _mouseClickPos;
    private static MassShape _s;

    public static void Main() 
    {
        InitWindow(WinW, WinH, "");
        SetTargetFPS(165);
        _lineColliders = new() {
            new(0f, 0f, WinW, 0f),
            new(0f, 0f, 0f, WinH),
            new(WinW, 0f, WinW, WinH),
            new(0f, WinH, WinW, WinH),
            //new(0f, 900f, 1600f, 800f) 
        };
        _s = MassShape.Circle(WinW / 2f, WinH / 2f, 100f, 30f, 60, _lineColliders);
        GravityEnabled = false;

        while (!WindowShouldClose())
        {
            SetWindowTitle(string.Format("Point-masses FPS: {0}", GetFPS()));
            _accumulator += GetFrameTime();
            while (_accumulator >= TimeStep)
            {
                _s.Update();
                _accumulator -= TimeStep;
            }
            HandleInput();
            BeginDrawing();
            ClearBackground(Raylib_cs.Color.BLACK);
            _s.Draw();
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
        if (IsKeyPressed(Raylib_cs.KeyboardKey.KEY_N))
        {
            DrawForces = !DrawForces;
        }
        if (IsMouseButtonPressed(Raylib_cs.MouseButton.MOUSE_BUTTON_LEFT))
        {
            _mouseClickPos = GetMousePosition();
        }
        if (IsMouseButtonDown(Raylib_cs.MouseButton.MOUSE_BUTTON_LEFT))
        {
            Vector2 displacement = GetMousePosition() - _mouseClickPos;
            _s.ApplyForce(displacement);
        }
    }
}