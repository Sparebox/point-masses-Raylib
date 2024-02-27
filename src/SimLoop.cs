using ImGuiNET;
using Physics;
using Raylib_cs;
using rlImGui_cs;
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
        rlImGui.Setup(true);
        while (!WindowShouldClose())
        {
            if (!_context.SimPaused)
            {
                Update();
            }
            HandleInput();

            BeginDrawing();
            ClearBackground(Color.Black);

            foreach (MassShape s in _context.MassShapes)
            {
                s.Draw();
            }
            foreach (var l in _context.LineColliders)
            {
                l.Draw();
            }
            //_context._ramp.Draw();
            _context.SelectedTool.Draw();
            DrawInfo();

            // GUI
            rlImGui.Begin();
            ImGui.ShowDemoWindow();
            rlImGui.End();
            EndDrawing();
        }
        rlImGui.Shutdown();
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
        //context.MassShapes.Add(MassShape.Cloth(x: 300f, y: 50f, width: 700f, height: 700f, mass: 0.7f, res: 42, stiffness: 1e5f, context));
        context.MassShapes.Add(MassShape.Ball(WinW / 2f - 300f, WinH / 2f - 200f, 50f, 10f, 20, 1000f, context));
        //context.MassShapes.Add(MassShape.Pendulum(WinW / 2f, 30f, 700f, 10f, 10, context));
        //context.MassShapes.Add(MassShape.Particle(200f, 50f, 10f, context));
        //context._ramp = new Entity.Ramp(0f, 200f, 1500f, 200f);
        //context.LineColliders.Add(context._ramp._collider);
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
        if (IsKeyPressed(KeyboardKey.G))
        {
            _context.GravityEnabled = !_context.GravityEnabled;
        }
        if (IsKeyPressed(KeyboardKey.F))
        {
            _context.DrawForces = !_context.DrawForces;
        }
        if (IsKeyPressed(KeyboardKey.T))
        {
            Tool.ChangeToolType(_context);
        }
        if (IsKeyPressed(KeyboardKey.B))
        {
            _context.DrawAABBs = !_context.DrawAABBs;
        }
        if (IsKeyPressed(KeyboardKey.R))
        {
            _context.LoadState();
        }
        if (IsKeyPressed(KeyboardKey.Space))
        {
            _context.SimPaused = !_context.SimPaused;
        }
        if (IsKeyDown(KeyboardKey.Up))
        {
            _context._ramp.Raise(0.01f);
        }
        if (IsKeyDown(KeyboardKey.Down))
        {
            _context._ramp.Lower(0.01f);
        }
        // Mouse
        if (IsMouseButtonDown(MouseButton.Left))
        {
            _context.SelectedTool.Update();
        }
        if (GetMouseWheelMoveV().Y > 0f)
        {
            _context.SelectedTool.ChangeRadius(Tool.BaseRadiusChange);
            _context.SelectedTool.ChangeDirection(DEG2RAD * Tool.BaseAngleChange);
        } 
        else if (GetMouseWheelMoveV().Y < 0f)
        {
            _context.SelectedTool.ChangeRadius(-Tool.BaseRadiusChange);
            _context.SelectedTool.ChangeDirection(DEG2RAD * -Tool.BaseAngleChange);
        }
    }

    private static void DrawInfo()
    {
        DrawText(string.Format("FPS: {0}", GetFPS()), 10, 10, 20, Color.Yellow);
        DrawText(string.Format("{0}", _context.SimPaused ? "PAUSED" : "RUNNING"), 10, 30, 20, _context.SimPaused ? Color.Red : Color.Green);
        DrawText(string.Format("Masses: {0}", _context.MassCount), 10, 50, 20, Color.Yellow);
        DrawText(string.Format("Constraints: {0}", _context.ConstraintCount), 10, 70, 20, Color.Yellow);
        DrawText(string.Format("Substeps: {0}", _context._substeps), 10, 90, 20, Color.Yellow);
        DrawText(string.Format("Step: {0} ms", _context._timeStep), 10, 110, 20, Color.Yellow);
        DrawText(string.Format("Substep: {0} ms", _context._subStep), 10, 130, 20, Color.Yellow);
        DrawText(string.Format("Gravity: {0}", _context.GravityEnabled ? "Enabled" : "Disabled"), 10, 150, 20, Color.Yellow);
        DrawText(string.Format("Tool: {0}", _context.SelectedTool.Type), 10, 170, 20, Color.Yellow);
    }
}