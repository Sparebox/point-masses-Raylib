using System.Numerics;
using Collision;
using Physics;
using Raylib_cs;
using Tools;
using static Raylib_cs.Raylib;

namespace Sim;

public class Loop 
{   
    public const int WinW = 1600;
    public const int WinH = 900;
    public const int TargetFPS = 165;

    private static float _accumulator;
    private static Context _context;

    public static void Main() 
    {
        _context = Init();
        while (!WindowShouldClose())
        {
            if (!_context.SimPaused)
            {
                Update();
            }
            HandleInput();

            BeginDrawing();
            ClearBackground(Color.BLACK);

            foreach (MassShape s in _context.MassShapes)
            {
                s.Draw();
            }
            foreach (var l in _context.LineColliders)
            {
                l.Draw();
            }
            _context.SelectedTool.Draw();
            DrawInfo();

            EndDrawing();
        }
        CloseWindow();
    }

    private static Context Init()
    {
        InitWindow(WinW, WinH, "Point-masses");
        SetTargetFPS(TargetFPS);
        Context context = new(timeStep: 1f / 60f, 13, pixelsPerMeter: 1f / 0.01f, gravity: new(0f, 9.81f))
        {
            LineColliders = new() {
            new(0f, 0f, WinW, 0f),
            new(0f, 0f, 0f, WinH),
            new(WinW, 0f, WinW, WinH),
            new(0f, WinH, WinW, WinH),
            //new(0f, 900f, 1600f, 200f)
            },
            MassShapes = new(),
            GravityEnabled = false,
            DrawAABBs = false,
            DrawForces = false
        };
        context.SelectedTool = new PullCom(context);
        context.MassShapes.Add(MassShape.Cloth(x: 300f, y: 50f, width: 700f, height: 700f, mass: 0.7f, res: 42, stiffness: 1e5f, context));
        //context.MassShapes.Add(MassShape.Ball(WinW / 2f, WinH / 2f, 100f, 10f, 15, 500f, context));
        context.SaveState();
        return context;
    }

    private static void Update()
    {
        _accumulator += GetFrameTime();
        while (_accumulator >= _context._timeStep)
        {
            for (int i = 0; i < _context._substeps; i++)
            {
                foreach (MassShape s in _context.MassShapes)
                {
                    s.Update(_context._subStep);
                }
            }
            _accumulator -= _context._timeStep;
        }
    }

    private static void HandleInput()
    {
        // Keys
        if (IsKeyPressed(KeyboardKey.KEY_G))
        {
            _context.GravityEnabled = !_context.GravityEnabled;
        }
        if (IsKeyPressed(KeyboardKey.KEY_F))
        {
            _context.DrawForces = !_context.DrawForces;
        }
        if (IsKeyPressed(KeyboardKey.KEY_T))
        {
            Tool.ChangeToolType(_context);
        }
        if (IsKeyPressed(KeyboardKey.KEY_B))
        {
            _context.DrawAABBs = !_context.DrawAABBs;
        }
        if (IsKeyPressed(KeyboardKey.KEY_R))
        {
            _context.LoadState();
        }
        if (IsKeyPressed(KeyboardKey.KEY_SPACE))
        {
            _context.SimPaused = !_context.SimPaused;
        }
        // Mouse
        if (IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
        {
            _context.SelectedTool.Update();
        }
        if (GetMouseWheelMoveV().Y > 0f)
        {
            _context.SelectedTool.ChangeRadius(Tool.BaseRadiusChange);
            _context.SelectedTool.ChangeDirection(Tool.BaseAngleChange * (float) Math.PI / 180f);
        } 
        else if (GetMouseWheelMoveV().Y < 0f)
        {
            _context.SelectedTool.ChangeRadius(-Tool.BaseRadiusChange);
            _context.SelectedTool.ChangeDirection(Tool.BaseAngleChange * (float) -Math.PI / 180f);
        }
    }

    private static void DrawInfo()
    {
        DrawText(string.Format("FPS: {0}", GetFPS()), 10, 10, 20, Color.YELLOW);
        DrawText(string.Format("{0}", _context.SimPaused ? "PAUSED" : "RUNNING"), 10, 30, 20, _context.SimPaused ? Color.RED : Color.GREEN);
        DrawText(string.Format("Masses: {0}", _context.MassCount), 10, 50, 20, Color.YELLOW);
        DrawText(string.Format("Constraints: {0}", _context.ConstraintCount), 10, 70, 20, Color.YELLOW);
        DrawText(string.Format("Substeps: {0}", _context._substeps), 10, 90, 20, Color.YELLOW);
        DrawText(string.Format("Step: {0} ms", _context._timeStep), 10, 110, 20, Color.YELLOW);
        DrawText(string.Format("Substep: {0} ms", _context._subStep), 10, 130, 20, Color.YELLOW);
        DrawText(string.Format("Gravity: {0}", _context.GravityEnabled ? "Enabled" : "Disabled"), 10, 150, 20, Color.YELLOW);
        DrawText(string.Format("Tool: {0}", _context.SelectedTool.Type), 10, 170, 20, Color.YELLOW);
    }
}