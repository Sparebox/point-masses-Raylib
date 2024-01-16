using System.Numerics;
using Collision;
using Physics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Sim;

public class Loop 
{   
    public const float TimeStep = 1f / 60f;
    public const int WinW = 1600;
    public const int WinH = 900;
    public const float PixelsPerMeter = 1f / 0.01f;
    private const float PullForceCoeff = 1e2f;
    private const int Substeps = 30;
    private const float SubStep = TimeStep / Substeps;

    public static bool GravityEnabled { get; set; }
    public static bool DrawForces { get; set; }
    public static readonly Vector2 Gravity = new(0f, 9.81f * PixelsPerMeter);

    private static float _accumulator;
    private static List<LineCollider> _lineColliders;
    private static MassShape _s;

    public static void Main() 
    {
        InitWindow(WinW, WinH, "Point-masses");
        SetTargetFPS(165);
        _lineColliders = new() {
            new(0f, 0f, WinW, 0f),
            new(0f, 0f, 0f, WinH),
            new(WinW, 0f, WinW, WinH),
            new(0f, WinH, WinW, WinH),
            new(0f, 900f, 1600f, 200f) 
        };
        _s = MassShape.Ball(WinW / 2f, WinH / 2f - 100f, 50f, 10f, 25, 1e3f, _lineColliders);
        GravityEnabled = false;

        while (!WindowShouldClose())
        {
            _accumulator += GetFrameTime();
            while (_accumulator >= TimeStep)
            {
                for (int i = 0; i < Substeps; i++)
                {
                    _s.Update(SubStep);
                    _accumulator -= SubStep;
                }
            }
            HandleInput();
            BeginDrawing();
            ClearBackground(Color.BLACK);
            _s.Draw();
            foreach (var l in _lineColliders)
            {
                l.Draw();
            }
            DrawInfo();
            EndDrawing();
        }
        CloseWindow();
    }

    private static void HandleInput()
    {
        if (IsKeyPressed(KeyboardKey.KEY_G))
        {
            GravityEnabled = !GravityEnabled;
        }
        if (IsKeyPressed(KeyboardKey.KEY_F))
        {
            DrawForces = !DrawForces;
        }
        if (IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
        {
            Vector2 mousePos = GetMousePosition();
            Vector2 com = _s.CalculateCenterOfMass();
            Vector2 force = PullForceCoeff * (mousePos - com);
            _s.ApplyForce(force);
            DrawLine((int) com.X, (int) com.Y, (int) mousePos.X, (int) mousePos.Y, Color.RED);
        }
    }
    private static void DrawInfo()
    {
        DrawText(string.Format("FPS: {0}", GetFPS()), 10, 10, 20, Color.YELLOW);
        DrawText(string.Format("Substeps: {0}", Substeps), 10, 30, 20, Color.YELLOW);
        DrawText(string.Format("Step: {0}", TimeStep), 10, 50, 20, Color.YELLOW);
        DrawText(string.Format("Substep: {0}", SubStep), 10, 70, 20, Color.YELLOW);
    }
}