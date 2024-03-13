using System.Numerics;
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
            if (!_context._simPaused)
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
            _gravityEnabled = false,
            _drawAABBS = false,
            _drawForces = false,
            _simPaused = true
        };
        context.SelectedTool = new PullCom(context);
        //context.MassShapes.Add(MassShape.Cloth(x: 300f, y: 50f, width: 700f, height: 700f, mass: 0.7f, res: 42, stiffness: 1e5f, context));
        // context.MassShapes.Add(MassShape.SoftBall(WinW / 2f - 300f, WinH / 2f - 200f, 50f, 10f, 20, 1000f, context));
        // context.MassShapes.Add(MassShape.SoftBall(WinW / 2f + 300f, WinH / 2f - 200f, 50f, 10f, 20, 1000f, context));
        // context.MassShapes.Add(MassShape.SoftBall(WinW / 2f - 300f, WinH / 2f, 50f, 10f, 20, 1000f, context));
        // context.MassShapes.Add(MassShape.SoftBall(WinW / 2f - 300f, WinH / 2f + 200f, 50f, 10f, 20, 1000f, context));
        //context.MassShapes.Add(MassShape.SoftBall(WinW / 2f - 300f, WinH / 2f + 200f, 200f, 10f, 20, 1000f, context));
        //context.MassShapes.Add(MassShape.Pendulum(WinW / 2f, 30f, 700f, 10f, 10, context));
        //context.MassShapes.Add(MassShape.Particle(200f, 50f, 10f, context));
        context.MassShapes.Add(MassShape.Box(WinW / 2f, WinH / 2f - 300f, 100f, 10f, context));
        context.MassShapes.Add(MassShape.Box(WinW / 2f, WinH / 2f, 100f, 10f, context));
        //context.MassShapes.Add(MassShape.SoftBox(WinW / 2f, WinH / 2f, 100f, 10f, 1e4f, context));
        //context.MassShapes.Add(MassShape.SoftBox(WinW / 2f, WinH / 2f + 200f, 100f, 10f, 1e4f, context));
        //context.MassShapes.Add(MassShape.HardBall(500f, 200f, 100f, 50f, 20, context));
        //context.MassShapes.Add(MassShape.HardBall(200f, 200f, 100f, 50f, 20, context));
        //context._ramp = new Entity.RotatingCollider(0f, 200f, WinW, WinH);
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
                MassShape.SolveCollisions(_context);
            }
            _accumulator -= _context._timeStep;
        }
    }

    private static void Draw()
    {
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
        
        // GUI
        rlImGui.Begin();
        DrawInfo();
        rlImGui.End();
        EndDrawing();
    }

    private static void HandleInput()
    {
        // Keys
        if (IsKeyPressed(KeyboardKey.G))
        {
            _context._gravityEnabled = !_context._gravityEnabled;
        }
        if (IsKeyPressed(KeyboardKey.F))
        {
            _context._drawForces = !_context._drawForces;
        }
        if (IsKeyPressed(KeyboardKey.B))
        {
            _context._drawAABBS = !_context._drawAABBS;
        }
        if (IsKeyPressed(KeyboardKey.R))
        {
            _context.LoadState();
        }
        if (IsKeyPressed(KeyboardKey.Space))
        {
            _context._simPaused = !_context._simPaused;
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
        ImGui.Begin("Simulation info", ImGuiWindowFlags.NoMove);
        ImGui.SetWindowPos(Vector2.Zero);
        ImGui.Text(string.Format("FPS: {0}", GetFPS()));
        ImGui.PushStyleColor(ImGuiCol.Text, _context._simPaused ? new Vector4(255f, 0f, 0f, 255f) : new Vector4(0f, 255f, 0f, 255f));
        ImGui.Checkbox(_context._simPaused ? "PAUSE" : "RUNNING", ref _context._simPaused);
        ImGui.PopStyleColor();
        ImGui.Text(string.Format("Masses: {0}", _context.MassCount));
        ImGui.Text(string.Format("Constraints: {0}", _context.ConstraintCount));
        ImGui.Text(string.Format("Substeps: {0}", _context._substeps));
        ImGui.Text(string.Format("Step: {0:0.0000} ms", _context._timeStep));
        ImGui.Text(string.Format("Substep: {0:0.0000} ms", _context._subStep));
        ImGui.Checkbox("Gravity", ref _context._gravityEnabled);
        ImGui.Checkbox("Draw forces", ref _context._drawForces);
        ImGui.Checkbox("Draw AABBs", ref _context._drawAABBS);
        if (ImGui.Combo("Tool", ref _context._selectedToolIndex, Tool.ToolsToString()))
        {
            Tool.ChangeToolType(_context);
        }
        ImGui.End();
    }
}